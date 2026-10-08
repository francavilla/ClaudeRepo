using System;
using System.Threading;
using System.Threading.Tasks;
using SolutionDoctor.Core.Analysis;
using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Presentation.Services
{
    /// <summary>L'analisi vista dal ViewModel: asincrona, con avanzamento e annullamento.</summary>
    public interface IAnalyzer
    {
        Task<AnalysisReport> AnalyzeAsync(string path, AnalysisOptions options, IProgress<string> progress, CancellationToken cancellationToken);
    }

    /// <summary>Esegue <see cref="SolutionAnalyzer"/> su un thread del pool, così la finestra resta reattiva.</summary>
    public sealed class CoreAnalyzer : IAnalyzer
    {
        public Task<AnalysisReport> AnalyzeAsync(string path, AnalysisOptions options, IProgress<string> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() => new SolutionAnalyzer().Analyze(path, options, progress, cancellationToken), cancellationToken);
        }
    }
}
