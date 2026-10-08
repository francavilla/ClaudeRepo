using System.Linq;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class CodeSmellDetectorTests
    {
        private static FileAnalysis Analyze(string body)
        {
            return CodeSmellDetector.AnalyzeSource(
                "using System; using System.Data.SqlClient; using System.IO; using System.Windows.Forms;\n"
                + "namespace Demo { public class Form1 : Form {\n" + body + "\n} }",
                "Form1.cs");
        }

        private static string Handler(string body)
        {
            return "private void btn_Click(object sender, EventArgs e) {\n" + body + "\n}";
        }

        [Fact]
        public void HandlerConAccessoAlDatabase_WF002()
        {
            var result = Analyze(Handler("using (var cn = new SqlConnection(\"x\")) { cn.Open(); }"));

            var finding = Assert.Single(result.Findings, f => f.RuleId == RuleCatalog.DataAccessInHandler);
            Assert.Equal(Severity.Medium, finding.Severity);
            Assert.Equal("Demo.Form1", finding.Subject);
            Assert.Contains("database", finding.Message);
        }

        [Fact]
        public void HandlerConAccessoAFile_WF002()
        {
            var result = Analyze(Handler("var t = File.ReadAllText(\"a.txt\");"));

            var finding = Assert.Single(result.Findings, f => f.RuleId == RuleCatalog.DataAccessInHandler);
            Assert.Contains("file system", finding.Message);
        }

        [Fact]
        public void AccessoAiDatiFuoriDaUnHandler_NonScatta()
        {
            var result = Analyze("private void Carica() { var cn = new SqlConnection(\"x\"); }");

            Assert.DoesNotContain(result.Findings, f => f.RuleId == RuleCatalog.DataAccessInHandler);
        }

        [Fact]
        public void DoEvents_WF003()
        {
            var result = Analyze("private void Lavora() { Application.DoEvents(); System.Windows.Forms.Application.DoEvents(); }");

            Assert.Equal(2, result.Findings.Count(f => f.RuleId == RuleCatalog.DoEvents));
        }

        [Theory]
        [InlineData("new SqlCommand(\"SELECT * FROM T WHERE N='\" + name + \"'\", cn)")]
        [InlineData("new SqlDataAdapter(\"SELECT * FROM T WHERE N=\" + id, cn)")]
        [InlineData("new SqlCommand($\"SELECT * FROM T WHERE N={name}\", cn)")]
        [InlineData("new SqlCommand(string.Format(\"SELECT {0}\", name), cn)")]
        [InlineData("new SqlCommand((\"SELECT \" + \"x\") + name, cn)")]
        public void SqlCostruitoPerConcatenazione_WF004(string creation)
        {
            var result = Analyze("private void Q(SqlConnection cn, string name, int id) { var c = " + creation + "; }");

            var finding = Assert.Single(result.Findings, f => f.RuleId == RuleCatalog.ConcatenatedSql);
            Assert.Equal(Severity.High, finding.Severity);
        }

        [Theory]
        [InlineData("new SqlCommand(\"SELECT * FROM T WHERE N=@n\", cn)")]
        [InlineData("new SqlCommand(\"SELECT * \" + \"FROM T\", cn)")]
        [InlineData("new SqlCommand($\"SELECT * FROM T\", cn)")]
        [InlineData("new SqlCommand(sql, cn)")]
        public void SqlParametricoOCostante_NonScatta(string creation)
        {
            var result = Analyze("private void Q(SqlConnection cn, string sql) { var c = " + creation + "; }");

            Assert.DoesNotContain(result.Findings, f => f.RuleId == RuleCatalog.ConcatenatedSql);
        }

        [Fact]
        public void CommandTextConcatenato_WF004()
        {
            var result = Analyze(
                "private void Q(SqlCommand cmd, string n) { cmd.CommandText = \"SELECT \" + n; cmd.CommandText += n; cmd.CommandText = \"SELECT 1\"; }");

            Assert.Equal(2, result.Findings.Count(f => f.RuleId == RuleCatalog.ConcatenatedSql));
        }

        [Fact]
        public void StatoStaticoModificabile_WF005()
        {
            var result = Analyze(
                "public static string Utente; private static int _contatore; "
                + "public static Form1 Instance { get; set; } "
                + "public const int Max = 3; private static readonly object Lock = new object(); "
                + "public static string Nome { get { return \"x\"; } }");

            var findings = result.Findings.Where(f => f.RuleId == RuleCatalog.MutableStaticState).ToList();
            Assert.Equal(3, findings.Count);
            Assert.Equal(Severity.Medium, findings.Single(f => f.Message.Contains("'Utente'")).Severity);
            Assert.Equal(Severity.Low, findings.Single(f => f.Message.Contains("'_contatore'")).Severity);
            Assert.Equal(Severity.Medium, findings.Single(f => f.Message.Contains("'Instance'")).Severity);
        }

        [Fact]
        public void InvokeRequired_UnSoloFindingPerFileConIlConteggio()
        {
            var result = Analyze(
                "void A() { if (InvokeRequired) Invoke(new Action(A)); } void B() { if (this.InvokeRequired) { } }");

            var finding = Assert.Single(result.Findings, f => f.RuleId == RuleCatalog.ScatteredInvokeRequired);
            Assert.Equal(Severity.Info, finding.Severity);
            Assert.Contains("2 volte", finding.Message);

            var single = Analyze("void A() { if (InvokeRequired) { } }");
            Assert.Contains("1 volta:", single.Findings.Single(f => f.RuleId == RuleCatalog.ScatteredInvokeRequired).Message);
        }

        [Fact]
        public void HandlerLungo_WF007_ConGravitaCrescente()
        {
            var sixty = string.Concat(Enumerable.Repeat("var x = 1;\n", 60));
            var twoHundred = string.Concat(Enumerable.Repeat("var x = 1;\n", 200));

            Assert.Equal(Severity.Low, Assert.Single(Analyze(Handler(sixty)).Findings, f => f.RuleId == RuleCatalog.LongHandler).Severity);
            Assert.Equal(Severity.Medium, Assert.Single(Analyze(Handler(twoHundred)).Findings, f => f.RuleId == RuleCatalog.LongHandler).Severity);
            Assert.DoesNotContain(Analyze(Handler("var x = 1;")).Findings, f => f.RuleId == RuleCatalog.LongHandler);
        }

        [Fact]
        public void MetodoConFirmaDiHandlerMaArgomentiDiversi_NonEUnHandler()
        {
            var result = Analyze("private void M(string a, EventArgs e) { var cn = new SqlConnection(\"x\"); }");

            Assert.DoesNotContain(result.Findings, f => f.RuleId == RuleCatalog.DataAccessInHandler);
            Assert.Equal(0, result.Parts.Single().Handlers);
        }

        [Fact]
        public void ClasseUi_RiconosciutaDallaClasseBaseODaInitializeComponent()
        {
            var form = CodeSmellDetector.AnalyzeSource("public partial class A : Form { }", "A.cs");
            var byCtor = CodeSmellDetector.AnalyzeSource("public partial class B { public B() { InitializeComponent(); } }", "B.cs");
            var plain = CodeSmellDetector.AnalyzeSource("public class C : Base { }", "C.cs");
            var custom = CodeSmellDetector.AnalyzeSource("public class D : MetroForm { }", "D.cs");

            Assert.True(form.Parts.Single().IsUi);
            Assert.True(byCtor.Parts.Single().IsUi);
            Assert.False(plain.Parts.Single().IsUi);
            Assert.True(custom.Parts.Single().IsUi);
        }

        [Fact]
        public void CodiceNonCompilabile_NonFaFallireLAnalisi()
        {
            var result = CodeSmellDetector.AnalyzeSource("public class X : Form { void M( { ", "X.cs");

            Assert.NotNull(result);
        }
    }
}
