using System.Globalization;
using System.IO;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Presentation.ViewModels
{
    /// <summary>Una classe candidata al refactoring, nella tabella "Da dove cominciare".</summary>
    public sealed class BacklogRow
    {
        public BacklogRow(int rank, BacklogItem item, string rootDirectory)
        {
            Rank = rank;
            Subject = item.Subject;
            Project = item.Project ?? string.Empty;
            FilePath = item.FilePath;
            FullPath = Path.GetFullPath(Path.Combine(rootDirectory, item.FilePath.Replace('/', Path.DirectorySeparatorChar)));
            LinesText = item.Lines > 0 ? item.Lines.ToString(CultureInfo.InvariantCulture) : "—";
            Lines = item.Lines;
            Handlers = item.Handlers;
            FindingCount = item.FindingCount;
            Churn = item.Churn;
            Priority = item.Priority;
            PriorityText = item.Priority.ToString("0.#", CultureInfo.InvariantCulture);
            Rules = item.Rules;
        }

        public int Rank { get; private set; }

        public string Subject { get; private set; }

        public string Project { get; private set; }

        public string FilePath { get; private set; }

        public string FullPath { get; private set; }

        public int Lines { get; private set; }

        /// <summary>Righe del code-behind, oppure "—" per le classi che non sono form.</summary>
        public string LinesText { get; private set; }

        public int Handlers { get; private set; }

        public int FindingCount { get; private set; }

        public int Churn { get; private set; }

        public double Priority { get; private set; }

        public string PriorityText { get; private set; }

        public string Rules { get; private set; }
    }
}
