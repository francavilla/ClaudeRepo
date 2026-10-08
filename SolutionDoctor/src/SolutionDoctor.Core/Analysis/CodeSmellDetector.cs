using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>
    /// Rileva gli "smell" tipici delle applicazioni WinForms in un file C#.
    /// L'analisi è solo sintattica: non richiede di compilare né di risolvere i riferimenti,
    /// quindi funziona anche su solution che non compilano sulla macchina corrente.
    /// </summary>
    internal static class CodeSmellDetector
    {
        private const int LongHandlerLines = 50;
        private const int VeryLongHandlerLines = 150;

        private static readonly HashSet<string> DatabaseTypes = new HashSet<string>
        {
            "SqlConnection", "SqlCommand", "SqlDataAdapter",
            "OleDbConnection", "OleDbCommand", "OleDbDataAdapter",
            "OdbcConnection", "OdbcCommand", "OdbcDataAdapter",
            "OracleConnection", "OracleCommand", "OracleDataAdapter",
            "SQLiteConnection", "SQLiteCommand", "SQLiteDataAdapter",
            "MySqlConnection", "MySqlCommand", "MySqlDataAdapter",
            "NpgsqlConnection", "NpgsqlCommand", "NpgsqlDataAdapter"
        };

        private static readonly HashSet<string> CommandTypes = new HashSet<string>(
            DatabaseTypes.Where(t => t.EndsWith("Command") || t.EndsWith("DataAdapter")));

        public static FileAnalysis AnalyzeSource(string source, string relativePath)
        {
            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
            var result = new FileAnalysis();
            new Walker(relativePath, result).Visit(tree.GetRoot());
            return result;
        }

        private static int LineOf(SyntaxNode node)
        {
            return node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        }

        private static string LastName(TypeSyntax type)
        {
            var qualified = type as QualifiedNameSyntax;
            if (qualified != null)
            {
                return qualified.Right.Identifier.Text;
            }

            var simple = type as SimpleNameSyntax;
            return simple != null ? simple.Identifier.Text : type.ToString();
        }

        private static bool IsUiBaseType(string name)
        {
            return name == "Form" || name == "UserControl" || name.EndsWith("Form") || name.EndsWith("UserControl");
        }

        /// <summary>True se l'espressione costruisce una stringa a runtime (concatenazione, interpolazione, Format).</summary>
        private static bool IsDynamicString(ExpressionSyntax expression)
        {
            var parenthesized = expression as ParenthesizedExpressionSyntax;
            if (parenthesized != null)
            {
                return IsDynamicString(parenthesized.Expression);
            }

            var binary = expression as BinaryExpressionSyntax;
            if (binary != null && binary.IsKind(SyntaxKind.AddExpression))
            {
                return HasNonLiteralOperand(binary);
            }

            var interpolated = expression as InterpolatedStringExpressionSyntax;
            if (interpolated != null)
            {
                return interpolated.Contents.OfType<InterpolationSyntax>().Any();
            }

            var invocation = expression as InvocationExpressionSyntax;
            var member = invocation != null ? invocation.Expression as MemberAccessExpressionSyntax : null;
            if (member != null)
            {
                var owner = member.Expression.ToString();
                var method = member.Name.Identifier.Text;
                return (owner == "string" || owner == "String") && (method == "Format" || method == "Concat" || method == "Join");
            }

            return false;
        }

        private static bool HasNonLiteralOperand(BinaryExpressionSyntax add)
        {
            foreach (var operand in new[] { add.Left, add.Right })
            {
                var inner = operand as BinaryExpressionSyntax;
                if (inner != null && inner.IsKind(SyntaxKind.AddExpression))
                {
                    if (HasNonLiteralOperand(inner))
                    {
                        return true;
                    }
                }
                else if (!operand.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class ClassFrame
        {
            public string FullName;
            public bool IsUi;
            public int Handlers;
        }

        private sealed class Walker : CSharpSyntaxWalker
        {
            private readonly string _file;
            private readonly FileAnalysis _result;
            private readonly Stack<ClassFrame> _frames = new Stack<ClassFrame>();
            private int _invokeRequiredCount;
            private int _invokeRequiredLine;
            private string _invokeRequiredSubject;

            public Walker(string file, FileAnalysis result)
            {
                _file = file;
                _result = result;
            }

            private string Subject
            {
                get { return _frames.Count > 0 ? _frames.Peek().FullName : "(file)"; }
            }

            public override void VisitCompilationUnit(CompilationUnitSyntax node)
            {
                base.VisitCompilationUnit(node);
                if (_invokeRequiredCount > 0)
                {
                    Add(RuleCatalog.ScatteredInvokeRequired, Severity.Info,
                        "InvokeRequired usato " + _invokeRequiredCount + (_invokeRequiredCount == 1 ? " volta" : " volte") + ": centralizzare il marshalling verso la UI.",
                        _invokeRequiredLine, _invokeRequiredSubject);
                }
            }

            public override void VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                var frame = new ClassFrame
                {
                    FullName = FullNameOf(node),
                    IsUi = node.BaseList != null && node.BaseList.Types.Any(t => IsUiBaseType(LastName(t.Type)))
                };

                _frames.Push(frame);
                base.VisitClassDeclaration(node);
                _frames.Pop();

                var span = node.GetLocation().GetLineSpan();
                _result.Parts.Add(new ClassPart
                {
                    FullName = frame.FullName,
                    IsUi = frame.IsUi,
                    StartLine = span.StartLinePosition.Line + 1,
                    EndLine = span.EndLinePosition.Line + 1,
                    Handlers = frame.Handlers,
                    FilePath = _file
                });
            }

            public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
            {
                if (IsEventHandler(node) && _frames.Count > 0)
                {
                    _frames.Peek().Handlers++;
                    CheckHandler(node);
                }

                base.VisitMethodDeclaration(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var identifier = node.Expression as IdentifierNameSyntax;
                if (identifier != null && identifier.Identifier.Text == "InitializeComponent" && _frames.Count > 0)
                {
                    _frames.Peek().IsUi = true;
                }

                var member = node.Expression as MemberAccessExpressionSyntax;
                if (member != null && member.Name.Identifier.Text == "DoEvents"
                    && (member.Expression.ToString() == "Application" || member.Expression.ToString().EndsWith(".Application")))
                {
                    Add(RuleCatalog.DoEvents, Severity.Medium,
                        "Application.DoEvents(): il thread UI è bloccato da lavoro sincrono.", LineOf(node), Subject);
                }

                base.VisitInvocationExpression(node);
            }

            public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
            {
                var typeName = LastName(node.Type);
                if (CommandTypes.Contains(typeName) && node.ArgumentList != null && node.ArgumentList.Arguments.Count > 0
                    && IsDynamicString(node.ArgumentList.Arguments[0].Expression))
                {
                    Add(RuleCatalog.ConcatenatedSql, Severity.High,
                        "Comando SQL costruito per concatenazione (" + typeName + "): rischio di SQL injection, usare parametri.",
                        LineOf(node), Subject);
                }

                base.VisitObjectCreationExpression(node);
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                var member = node.Left as MemberAccessExpressionSyntax;
                if (member != null && member.Name.Identifier.Text == "CommandText"
                    && (node.IsKind(SyntaxKind.AddAssignmentExpression) || IsDynamicString(node.Right)))
                {
                    Add(RuleCatalog.ConcatenatedSql, Severity.High,
                        "CommandText costruito per concatenazione: rischio di SQL injection, usare parametri.",
                        LineOf(node), Subject);
                }

                base.VisitAssignmentExpression(node);
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (IsMutableStatic(node.Modifiers))
                {
                    foreach (var variable in node.Declaration.Variables)
                    {
                        AddStatic(variable.Identifier.Text, node.Modifiers, LineOf(variable));
                    }
                }

                base.VisitFieldDeclaration(node);
            }

            public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                var accessors = node.AccessorList;
                var isAutoWithSetter = accessors != null
                    && accessors.Accessors.Any(a => a.Body == null && a.ExpressionBody == null
                        && (a.IsKind(SyntaxKind.SetAccessorDeclaration) || a.IsKind(SyntaxKind.InitAccessorDeclaration)));
                if (isAutoWithSetter && node.Modifiers.Any(SyntaxKind.StaticKeyword))
                {
                    AddStatic(node.Identifier.Text, node.Modifiers, LineOf(node));
                }

                base.VisitPropertyDeclaration(node);
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.Text == "InvokeRequired")
                {
                    if (_invokeRequiredCount == 0)
                    {
                        _invokeRequiredLine = LineOf(node);
                        _invokeRequiredSubject = Subject;
                    }

                    _invokeRequiredCount++;
                }

                base.VisitIdentifierName(node);
            }

            private static bool IsMutableStatic(SyntaxTokenList modifiers)
            {
                return modifiers.Any(SyntaxKind.StaticKeyword)
                    && !modifiers.Any(SyntaxKind.ConstKeyword)
                    && !modifiers.Any(SyntaxKind.ReadOnlyKeyword);
            }

            private void AddStatic(string name, SyntaxTokenList modifiers, int line)
            {
                var isPrivate = !modifiers.Any(SyntaxKind.PublicKeyword)
                    && !modifiers.Any(SyntaxKind.InternalKeyword)
                    && !modifiers.Any(SyntaxKind.ProtectedKeyword);
                Add(RuleCatalog.MutableStaticState, isPrivate ? Severity.Low : Severity.Medium,
                    "Stato statico modificabile '" + name + "': stato globale condiviso tra le form.", line, Subject);
            }

            private static bool IsEventHandler(MethodDeclarationSyntax method)
            {
                var parameters = method.ParameterList.Parameters;
                return parameters.Count == 2
                    && parameters[0].Type != null && (parameters[0].Type.ToString() == "object" || parameters[0].Type.ToString() == "Object")
                    && parameters[1].Type != null && LastName(parameters[1].Type).EndsWith("EventArgs");
            }

            private void CheckHandler(MethodDeclarationSyntax method)
            {
                var kinds = new List<string>();
                var nodes = method.DescendantNodes().ToList();

                if (nodes.OfType<ObjectCreationExpressionSyntax>().Any(c => DatabaseTypes.Contains(LastName(c.Type))))
                {
                    kinds.Add("database");
                }

                if (nodes.OfType<InvocationExpressionSyntax>().Any(i =>
                {
                    var m = i.Expression as MemberAccessExpressionSyntax;
                    return m != null && (m.Expression.ToString() == "File" || m.Expression.ToString() == "Directory");
                }))
                {
                    kinds.Add("file system");
                }

                var name = method.Identifier.Text;
                if (kinds.Count > 0)
                {
                    Add(RuleCatalog.DataAccessInHandler, Severity.Medium,
                        "L'handler '" + name + "' accede direttamente a " + string.Join(" e ", kinds) + ": estrarre in un servizio/repository.",
                        LineOf(method), Subject);
                }

                var span = method.GetLocation().GetLineSpan();
                var lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
                if (lines > LongHandlerLines)
                {
                    Add(RuleCatalog.LongHandler, lines > VeryLongHandlerLines ? Severity.Medium : Severity.Low,
                        "L'handler '" + name + "' è lungo " + lines + " righe: la logica va estratta.", LineOf(method), Subject);
                }
            }

            private void Add(string rule, Severity severity, string message, int line, string subject)
            {
                _result.Findings.Add(new Finding(rule, severity, message, _file, line, subject));
            }

            private static string FullNameOf(ClassDeclarationSyntax node)
            {
                var parts = new List<string> { node.Identifier.Text };
                foreach (var ancestor in node.Ancestors())
                {
                    var outer = ancestor as ClassDeclarationSyntax;
                    if (outer != null)
                    {
                        parts.Add(outer.Identifier.Text);
                    }

                    var ns = ancestor as BaseNamespaceDeclarationSyntax;
                    if (ns != null)
                    {
                        parts.Add(ns.Name.ToString());
                    }
                }

                parts.Reverse();
                return string.Join(".", parts);
            }
        }
    }
}
