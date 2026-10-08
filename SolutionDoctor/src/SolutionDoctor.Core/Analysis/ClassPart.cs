namespace SolutionDoctor.Core.Analysis
{
    /// <summary>Una porzione (file) di una classe, eventualmente partial.</summary>
    internal sealed class ClassPart
    {
        public string FullName { get; set; }

        public bool IsUi { get; set; }

        public int StartLine { get; set; }

        public int EndLine { get; set; }

        public int Handlers { get; set; }

        public string FilePath { get; set; }

        public int Lines
        {
            get { return EndLine - StartLine + 1; }
        }
    }
}
