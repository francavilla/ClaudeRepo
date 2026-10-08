namespace SolutionDoctor.Core.Model
{
    /// <summary>Una form o uno user control, con le metriche del solo code-behind (Designer escluso).</summary>
    public sealed class UiClassInfo
    {
        public string Project { get; set; }

        public string FullName { get; set; }

        public string FilePath { get; set; }

        public int Line { get; set; }

        /// <summary>Righe di codice scritto a mano (somma delle parti partial, Designer escluso).</summary>
        public int Lines { get; set; }

        public int Handlers { get; set; }
    }
}
