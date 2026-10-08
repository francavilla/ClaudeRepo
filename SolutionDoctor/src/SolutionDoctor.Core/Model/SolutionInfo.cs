using System.Collections.Generic;

namespace SolutionDoctor.Core.Model
{
    public sealed class SolutionInfo
    {
        public string Name { get; set; }

        /// <summary>Percorso del .sln; null se l'analisi è partita da una cartella o da un singolo progetto.</summary>
        public string Path { get; set; }

        /// <summary>Cartella rispetto alla quale sono espressi tutti i percorsi relativi.</summary>
        public string RootDirectory { get; set; }

        public List<ProjectInfo> Projects { get; } = new List<ProjectInfo>();

        /// <summary>Problemi non bloccanti incontrati durante la lettura (progetti mancanti o illeggibili).</summary>
        public List<string> Warnings { get; } = new List<string>();
    }
}
