using System.Linq;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;
using Xunit;

namespace SolutionDoctor.Core.Tests
{
    public class DependencyGraphTests
    {
        private static ProjectInfo Project(string name, params ProjectInfo[] references)
        {
            var project = new ProjectInfo { Name = name, Path = "/s/" + name + "/" + name + ".csproj" };
            project.ProjectReferences.AddRange(references.Select(r => r.Path));
            return project;
        }

        [Fact]
        public void LeFoglieVengonoPrima()
        {
            var core = Project("Core");
            var data = Project("Data", core);
            var ui = Project("UI", data, core);
            var util = Project("Util");

            var graph = DependencyGraph.Build(new[] { ui, data, util, core });

            Assert.Equal(new[] { "Core", "Data", "UI", "Util" }, graph.MigrationOrder.Select(p => p.Name));
            Assert.True(Position(graph, "Core") < Position(graph, "Data"));
            Assert.True(Position(graph, "Data") < Position(graph, "UI"));
            Assert.Empty(graph.CycleMembers);
        }

        [Fact]
        public void OrdineStabile_AlfabeticoAParita()
        {
            var graph = DependencyGraph.Build(new[] { Project("C"), Project("A"), Project("B") });

            Assert.Equal(new[] { "A", "B", "C" }, graph.MigrationOrder.Select(p => p.Name));
        }

        [Fact]
        public void CicloDiRiferimenti_IndividuatoENonOrdinato()
        {
            var a = Project("A");
            var b = Project("B", a);
            a.ProjectReferences.Add(b.Path);
            var c = Project("C", a);
            var free = Project("Free");

            var graph = DependencyGraph.Build(new[] { a, b, c, free });

            Assert.Equal(new[] { "Free" }, graph.MigrationOrder.Select(p => p.Name));
            Assert.Equal(new[] { "A", "B", "C" }, graph.CycleMembers.Select(p => p.Name));
        }

        [Fact]
        public void RiferimentiAProgettiFuoriDallaSolution_Ignorati()
        {
            var project = Project("A");
            project.ProjectReferences.Add("/altrove/X.csproj");
            project.ProjectReferences.Add(project.Path);

            var graph = DependencyGraph.Build(new[] { project });

            Assert.Equal(new[] { "A" }, graph.MigrationOrder.Select(p => p.Name));
            Assert.Empty(graph.DependenciesOf(project));
        }

        private static int Position(DependencyGraph graph, string name)
        {
            return graph.MigrationOrder.FindIndex(p => p.Name == name);
        }
    }
}
