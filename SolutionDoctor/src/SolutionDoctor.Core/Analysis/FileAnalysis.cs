using System.Collections.Generic;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Esito dell'analisi sintattica di un singolo file sorgente.</summary>
    internal sealed class FileAnalysis
    {
        public List<Finding> Findings { get; } = new List<Finding>();

        public List<ClassPart> Parts { get; } = new List<ClassPart>();
    }
}
