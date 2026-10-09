using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using SolutionDoctor.Core.Model;
using SolutionDoctor.Presentation.ViewModels;
using Xunit;

namespace SolutionDoctor.Presentation.Tests
{
    /// <summary>
    /// SolutionDoctor.App è WPF e si compila solo su Windows, quindi il XAML non può essere compilato dove girano questi test.
    /// Questi controlli verificano comunque ciò che di solito si scopre solo in esecuzione: ogni {Binding} deve puntare a una
    /// proprietà che esiste (nel ViewModel o nella riga mostrata), ogni risorsa deve esistere ed essere definita prima di
    /// essere usata, ogni gestore di evento deve esistere nel code-behind, ogni x:Static deve risolversi.
    /// </summary>
    public class XamlContractTests
    {
        private static readonly XNamespace Xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        private static readonly Regex BindingPattern = new Regex(@"\{Binding(?<args>[^}]*)\}");
        private static readonly Regex StaticResourcePattern = new Regex(@"\{StaticResource\s+(?<key>[^}\s]+)\s*\}");
        private static readonly Regex StaticMemberPattern = new Regex(@"\{x:Static\s+(?<prefix>\w+):(?<type>\w+)\.(?<member>\w+)\s*\}");
        private static readonly HashSet<string> EventAttributes = new HashSet<string>
        {
            "Click", "Drop", "DragOver", "PreviewDrop", "PreviewDragOver", "Loaded", "Closing", "Closed",
            "MouseDoubleClick", "SelectionChanged", "TextChanged", "Checked", "Unchecked"
        };

        private static readonly string[] ItemScopeProperties =
        {
            "Columns", "ItemTemplate", "ItemContainerStyle", "RowStyle", "CellStyle", "RowDetailsTemplate"
        };

        // ------------------------------------------------------------------ File

        private static string AppFolder()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "src", "SolutionDoctor.App");
                if (File.Exists(Path.Combine(candidate, "MainWindow.xaml")))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Cartella src/SolutionDoctor.App non trovata risalendo da " + AppContext.BaseDirectory);
        }

        private static XDocument Load(string relativePath)
        {
            return XDocument.Load(Path.Combine(AppFolder(), relativePath), LoadOptions.SetLineInfo);
        }

        private static string Where(XObject node)
        {
            var info = (IXmlLineInfo)node;
            return info.HasLineInfo() ? "riga " + info.LineNumber : "?";
        }

        // ------------------------------------------------------------------ Struttura

        [Theory]
        [InlineData("App.xaml")]
        [InlineData("MainWindow.xaml")]
        [InlineData("Themes/Theme.xaml")]
        public void IFileXaml_SonoXmlBenFormato(string file)
        {
            Assert.NotNull(Load(file).Root);
        }

        [Fact]
        public void LeClassiDichiarateNelXaml_EsistonoNelCodeBehind()
        {
            foreach (var pair in new[] { Tuple.Create("App.xaml", "App.xaml.cs"), Tuple.Create("MainWindow.xaml", "MainWindow.xaml.cs") })
            {
                var declared = (string)Load(pair.Item1).Root.Attribute(Xaml + "Class");
                var code = File.ReadAllText(Path.Combine(AppFolder(), pair.Item2));
                var lastDot = declared.LastIndexOf('.');
                var ns = declared.Substring(0, lastDot);
                var name = declared.Substring(lastDot + 1);

                Assert.Matches(@"namespace\s+" + Regex.Escape(ns) + @"\b", code);
                Assert.Matches(@"partial\s+class\s+" + name + @"\b", code);
            }
        }

        [Fact]
        public void IlProgettoAppReferenziaCoreEPresentationEIncludeLIcona()
        {
            var csproj = File.ReadAllText(Path.Combine(AppFolder(), "SolutionDoctor.App.csproj"));

            Assert.Contains("<UseWPF>true</UseWPF>", csproj);
            Assert.Contains("net8.0-windows", csproj);
            Assert.Contains(@"..\SolutionDoctor.Core\SolutionDoctor.Core.csproj", csproj);
            Assert.Contains(@"..\SolutionDoctor.Presentation\SolutionDoctor.Presentation.csproj", csproj);
            Assert.Contains(@"<Resource Include=""Assets\SolutionDoctor.ico"" />", csproj);
            Assert.True(File.Exists(Path.Combine(AppFolder(), "Assets", "SolutionDoctor.ico")));
            Assert.True(File.Exists(Path.Combine(AppFolder(), "app.manifest")));
        }

        [Fact]
        public void LIconaDellaFinestra_PuntaAUnFileEsistente()
        {
            var icon = (string)Load("MainWindow.xaml").Root.Attribute("Icon");

            Assert.True(File.Exists(Path.Combine(AppFolder(), icon.Replace('/', Path.DirectorySeparatorChar))), icon);
        }

        [Fact]
        public void LaSolutionIncludeTuttiIProgetti()
        {
            var solution = File.ReadAllText(Path.Combine(AppFolder(), "..", "..", "SolutionDoctor.sln"));

            foreach (var project in new[] { "SolutionDoctor.Core", "SolutionDoctor.Cli", "SolutionDoctor.Presentation", "SolutionDoctor.App", "SolutionDoctor.Core.Tests", "SolutionDoctor.Presentation.Tests" })
            {
                Assert.Contains("\"" + project + "\"", solution);
            }

            // Ogni progetto deve avere le quattro righe di configurazione, altrimenti MSBuild lo salta in silenzio
            // (la CI risulterebbe verde senza averlo compilato).
            var guids = Regex.Matches(solution, "Project\\(\"\\{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC\\}\"\\) = \"[^\"]+\", \"[^\"]+\", \"(?<guid>\\{[^}]+\\})\"")
                .Cast<Match>().Select(m => m.Groups["guid"].Value).ToList();
            Assert.Equal(6, guids.Count);
            foreach (var guid in guids)
            {
                foreach (var configuration in new[] { "Debug", "Release" })
                {
                    Assert.Contains(guid + "." + configuration + "|Any CPU.ActiveCfg = " + configuration + "|Any CPU", solution);
                    Assert.Contains(guid + "." + configuration + "|Any CPU.Build.0 = " + configuration + "|Any CPU", solution);
                }
            }
        }

        // ------------------------------------------------------------------ Risorse

        [Fact]
        public void IlTema_UsaSoloRisorseGiaDefinite()
        {
            var children = Load("Themes/Theme.xaml").Root.Elements().ToList();
            var defined = new HashSet<string>();

            foreach (var child in children)
            {
                foreach (var reference in References(child))
                {
                    Assert.True(defined.Contains(reference.Key),
                        "Theme.xaml " + Where(reference.Node) + ": la risorsa '" + reference.Key + "' è usata prima di essere definita (o non esiste).");
                }

                var key = (string)child.Attribute(Xaml + "Key");
                if (key != null)
                {
                    defined.Add(key);
                }
            }
        }

        [Fact]
        public void LaFinestra_UsaSoloRisorseDelTemaODellApplicazione()
        {
            var available = new HashSet<string>(Keys(Load("Themes/Theme.xaml").Root.Elements()));
            available.UnionWith(Keys(Load("App.xaml").Descendants().Where(e => e.Name.LocalName != "ResourceDictionary")));

            foreach (var reference in References(Load("MainWindow.xaml").Root))
            {
                Assert.True(available.Contains(reference.Key),
                    "MainWindow.xaml " + Where(reference.Node) + ": risorsa '" + reference.Key + "' inesistente.");
            }
        }

        [Fact]
        public void LeRisorseDelTema_HannoChiaviUniche()
        {
            var keys = Keys(Load("Themes/Theme.xaml").Root.Elements()).ToList();

            Assert.Equal(keys.Count, keys.Distinct().Count());
        }

        private static IEnumerable<string> Keys(IEnumerable<XElement> elements)
        {
            return elements.Select(e => (string)e.Attribute(Xaml + "Key")).Where(k => k != null);
        }

        private sealed class Reference
        {
            public string Key;
            public XObject Node;
        }

        private static IEnumerable<Reference> References(XElement element)
        {
            foreach (var node in element.DescendantsAndSelf())
            {
                foreach (var attribute in node.Attributes())
                {
                    foreach (Match match in StaticResourcePattern.Matches(attribute.Value))
                    {
                        yield return new Reference { Key = match.Groups["key"].Value, Node = attribute };
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Binding

        [Fact]
        public void IBindingDellaFinestra_PuntanoAProprietaEsistenti()
        {
            var problems = new List<string>();
            var resolved = new List<string>();
            Walk(Load("MainWindow.xaml").Root, typeof(MainViewModel), problems, resolved);

            Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));

            // Prova che il controllo ha davvero percorso la finestra, nei contesti giusti: finestra, righe delle griglie, template.
            Assert.True(resolved.Count > 60, "binding risolti: " + resolved.Count);
            foreach (var expected in new[]
            {
                "MainViewModel.IsBacklogTab", "MainViewModel.AnalyzeCommand", "MainViewModel.SelectedFinding.Message",
                "BacklogRow.Rank", "BacklogRow.PriorityText", "FindingRow.Location", "FindingRow.SeverityLabel",
                "ProjectRow.Frameworks", "MigrationStepRow.DependsOn"
            })
            {
                Assert.Contains(expected, resolved);
            }
        }

        [Fact]
        public void IBindingDelTema_PuntanoAProprietaDelleRigheMostrate()
        {
            var known = new[] { typeof(FindingRow), typeof(BacklogRow), typeof(MainViewModel) };
            var problems = new List<string>();

            foreach (var attribute in Load("Themes/Theme.xaml").Descendants().SelectMany(e => e.Attributes()))
            {
                foreach (var path in BindingPaths(attribute.Value))
                {
                    if (!known.Any(t => Resolve(t, path) != null))
                    {
                        problems.Add("Theme.xaml " + Where(attribute) + ": '" + string.Join(".", path) + "' non esiste in nessun tipo mostrato.");
                    }
                }
            }

            Assert.True(problems.Count == 0, Environment.NewLine + string.Join(Environment.NewLine, problems));
        }

        private static void Walk(XElement element, Type scope, List<string> problems, List<string> resolved = null)
        {
            foreach (var attribute in element.Attributes())
            {
                CheckAttribute(element, attribute, scope, problems, resolved);
            }

            var itemType = ItemTypeOf(element, scope);
            foreach (var child in element.Elements())
            {
                var childScope = scope;
                if (itemType != null && IsItemScopeProperty(element, child))
                {
                    childScope = itemType;
                }

                Walk(child, childScope, problems, resolved);
            }
        }

        private static bool IsItemScopeProperty(XElement owner, XElement child)
        {
            var name = child.Name.LocalName;
            var dot = name.IndexOf('.');
            return dot > 0 && ItemScopeProperties.Contains(name.Substring(dot + 1));
        }

        private static Type ItemTypeOf(XElement element, Type scope)
        {
            var source = (string)element.Attribute("ItemsSource");
            if (source == null)
            {
                return null;
            }

            var path = BindingPaths(source).FirstOrDefault();
            var property = path == null ? null : Resolve(scope, path);
            if (property == null)
            {
                return null;
            }

            var enumerable = property.PropertyType.GetInterfaces().Concat(new[] { property.PropertyType })
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerable != null ? enumerable.GetGenericArguments()[0] : null;
        }

        private static void CheckAttribute(XElement element, XAttribute attribute, Type scope, List<string> problems, List<string> resolved)
        {
            var location = "MainWindow.xaml " + Where(attribute) + " (" + element.Name.LocalName + "." + attribute.Name.LocalName + ")";

            foreach (Match match in BindingPattern.Matches(attribute.Value))
            {
                var args = match.Groups["args"].Value;
                if (args.Contains("ElementName") || args.Contains("RelativeSource") || args.Contains("Source="))
                {
                    continue;
                }

                var path = ExtractPath(args);
                if (path == null)
                {
                    continue;
                }

                var property = Resolve(scope, path);
                if (property == null)
                {
                    problems.Add(location + ": '" + string.Join(".", path) + "' non esiste su " + scope.Name + ".");
                    continue;
                }

                if (resolved != null)
                {
                    resolved.Add(scope.Name + "." + string.Join(".", path));
                }

                var oneWay = args.Contains("Mode=OneWay") || args.Contains("Mode=OneTime");
                var isTextBoxText = element.Name.LocalName == "TextBox" && attribute.Name.LocalName == "Text";
                var twoWayByDefault = isTextBoxText || attribute.Name.LocalName == "IsChecked" || attribute.Name.LocalName == "SelectedItem";
                if ((twoWayByDefault && !oneWay || args.Contains("Mode=TwoWay")) && !HasPublicSetter(scope, path))
                {
                    problems.Add(location + ": il binding è bidirezionale ma '" + string.Join(".", path) + "' non ha un setter pubblico.");
                }

                if (attribute.Name.LocalName == "Command" && !typeof(ICommand).IsAssignableFrom(property.PropertyType))
                {
                    problems.Add(location + ": '" + string.Join(".", path) + "' non è un ICommand.");
                }

                if (args.Contains("BoolToVisibility") && property.PropertyType != typeof(bool))
                {
                    problems.Add(location + ": BoolToVisibility richiede un bool, '" + string.Join(".", path) + "' è " + property.PropertyType.Name + ".");
                }
            }

            if (attribute.Name.LocalName == "DisplayMemberPath")
            {
                var itemType = ItemTypeOf(element, scope);
                if (itemType == null || Resolve(itemType, new[] { attribute.Value }) == null)
                {
                    problems.Add(location + ": DisplayMemberPath '" + attribute.Value + "' non esiste sul tipo degli elementi.");
                }
            }

            if (attribute.Name.LocalName == "SortMemberPath" && Resolve(scope, new[] { attribute.Value }) == null)
            {
                problems.Add(location + ": SortMemberPath '" + attribute.Value + "' non esiste su " + scope.Name + ".");
            }
        }

        private static string[] ExtractPath(string args)
        {
            var text = args.Trim();
            if (text.Length == 0 || text.StartsWith(",", StringComparison.Ordinal))
            {
                return null; // {Binding} oppure {Binding Converter=...}: nessun percorso
            }

            var first = text.Split(',')[0].Trim();
            if (first.StartsWith("Path=", StringComparison.Ordinal))
            {
                first = first.Substring("Path=".Length).Trim();
            }
            else if (first.Contains("="))
            {
                return null; // opzione senza percorso, es. {Binding Mode=OneWay}
            }

            return first.Length == 0 || first == "." ? null : first.Split('.');
        }

        private static IEnumerable<string[]> BindingPaths(string value)
        {
            foreach (Match match in BindingPattern.Matches(value))
            {
                var args = match.Groups["args"].Value;
                if (args.Contains("ElementName") || args.Contains("RelativeSource") || args.Contains("Source="))
                {
                    continue;
                }

                var path = ExtractPath(args);
                if (path != null)
                {
                    yield return path;
                }
            }
        }

        /// <summary>Segue il percorso (anche annidato, es. SelectedFinding.Message) e restituisce l'ultima proprietà, o null.</summary>
        private static PropertyInfo Resolve(Type start, string[] path)
        {
            PropertyInfo property = null;
            var type = start;
            foreach (var segment in path)
            {
                property = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    return null;
                }

                type = property.PropertyType;
            }

            return property;
        }

        private static bool HasPublicSetter(Type start, string[] path)
        {
            var property = Resolve(start, path);
            return property != null && property.GetSetMethod(false) != null;
        }

        // ------------------------------------------------------------------ Gestori di eventi e x:Static

        [Fact]
        public void IGestoriDiEvento_EsistonoNelCodeBehind()
        {
            var code = File.ReadAllText(Path.Combine(AppFolder(), "MainWindow.xaml.cs"));
            var root = Load("MainWindow.xaml").Root;
            var handlers = root.DescendantsAndSelf()
                .SelectMany(e => e.Attributes())
                .Where(a => EventAttributes.Contains(a.Name.LocalName) && Regex.IsMatch(a.Value, @"^\w+$"))
                .ToList();

            Assert.NotEmpty(handlers); // c'è almeno il trascinamento dei file
            foreach (var handler in handlers)
            {
                Assert.True(Regex.IsMatch(code, @"void\s+" + handler.Value + @"\s*\("),
                    "MainWindow.xaml " + Where(handler) + ": il gestore '" + handler.Value + "' non esiste in MainWindow.xaml.cs.");
            }
        }

        [Theory]
        [InlineData("Themes/Theme.xaml")]
        [InlineData("MainWindow.xaml")]
        public void GliXStatic_SiRisolvonoSuTipiEMembriEsistenti(string file)
        {
            var root = Load(file).Root;
            var namespaces = root.Attributes()
                .Where(a => a.IsNamespaceDeclaration && a.Value.StartsWith("clr-namespace:", StringComparison.Ordinal))
                .ToDictionary(a => a.Name.LocalName, a => a.Value.Substring("clr-namespace:".Length).Split(';')[0]);
            var assemblies = new[] { typeof(Severity).Assembly, typeof(MainViewModel).Assembly };

            foreach (var attribute in root.DescendantsAndSelf().SelectMany(e => e.Attributes()))
            {
                foreach (Match match in StaticMemberPattern.Matches(attribute.Value))
                {
                    var prefix = match.Groups["prefix"].Value;
                    Assert.True(namespaces.ContainsKey(prefix), file + " " + Where(attribute) + ": prefisso '" + prefix + "' non dichiarato.");

                    var fullName = namespaces[prefix] + "." + match.Groups["type"].Value;
                    var type = assemblies.Select(a => a.GetType(fullName)).FirstOrDefault(t => t != null);
                    Assert.True(type != null, file + " " + Where(attribute) + ": tipo '" + fullName + "' non trovato.");

                    var member = match.Groups["member"].Value;
                    Assert.True(
                        type.GetField(member, BindingFlags.Public | BindingFlags.Static) != null
                        || type.GetProperty(member, BindingFlags.Public | BindingFlags.Static) != null,
                        file + " " + Where(attribute) + ": membro '" + member + "' assente su " + type.Name + ".");
                }
            }
        }

        [Fact]
        public void IlTestDeiBindingRiconosceGliErrori()
        {
            // Verifica del verificatore: un XAML con errori deve produrre segnalazioni, altrimenti i test sopra non valgono nulla.
            var xaml = XElement.Parse(
                "<Grid xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\">"
                + "<TextBox Text=\"{Binding NonEsiste}\" />"
                + "<TextBox Text=\"{Binding StatusText}\" />"
                + "<Button Command=\"{Binding StatusText}\" />"
                + "<Border Visibility=\"{Binding StatusText, Converter={StaticResource BoolToVisibility}}\" />"
                + "<DataGrid ItemsSource=\"{Binding Findings}\"><DataGrid.Columns><Col Binding=\"{Binding Manca}\" /></DataGrid.Columns></DataGrid>"
                + "</Grid>");
            var problems = new List<string>();

            Walk(xaml, typeof(MainViewModel), problems);

            Assert.Contains(problems, p => p.Contains("'NonEsiste' non esiste"));
            Assert.Contains(problems, p => p.Contains("setter pubblico") && p.Contains("StatusText"));
            Assert.Contains(problems, p => p.Contains("non è un ICommand"));
            Assert.Contains(problems, p => p.Contains("BoolToVisibility richiede un bool"));
            Assert.Contains(problems, p => p.Contains("'Manca' non esiste su FindingRow"));
        }
    }
}
