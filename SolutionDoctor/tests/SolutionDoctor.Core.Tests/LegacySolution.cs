using System.Linq;

namespace SolutionDoctor.Core.Tests
{
    /// <summary>Crea su disco una piccola solution WinForms legacy con problemi noti.</summary>
    internal static class LegacySolution
    {
        public static string Create(TempDirectory dir)
        {
            dir.Write("Legacy.sln", Samples.Solution("App\\App.csproj", "Lib\\Lib.csproj"));
            dir.Write("Lib/Lib.csproj", Samples.LibProject());
            dir.Write("Lib/Util.cs", "namespace Lib { public class Util { public static string Ultimo; } }");

            dir.Write("App/App.csproj", Samples.ClassicWinFormsProject);
            dir.Write("App/packages.config", Samples.PackagesConfig);
            dir.Write("App/obj/Debug/Generato.cs", "public class G : System.Windows.Forms.Form { void M() { System.Windows.Forms.Application.DoEvents(); } }");
            dir.Write("App/Form1.Designer.cs", "namespace App { partial class Form1 { void InitializeComponent() { System.Windows.Forms.Application.DoEvents(); } } }");

            var filler = string.Concat(Enumerable.Repeat("            var x = 1;\n", 420));
            dir.Write("App/Form1.cs",
                "using System; using System.Data.SqlClient; using System.Windows.Forms;\n"
                + "namespace App {\n"
                + "public partial class Form1 : Form {\n"
                + "    public Form1() { InitializeComponent(); }\n"
                + "    private void btnCerca_Click(object sender, EventArgs e) {\n"
                + "        var cmd = new SqlCommand(\"SELECT * FROM T WHERE N='\" + txt.Text + \"'\");\n"
                + "        Application.DoEvents();\n"
                + "    }\n"
                + "    private void btnAltro_Click(object sender, EventArgs e) {\n" + filler + "    }\n"
                + "}\n}\n");
            dir.Write("App/Servizio.cs", "namespace App { public class Servizio { private void M() { System.Windows.Forms.Application.DoEvents(); } } }");
            return dir.Path;
        }
    }
}
