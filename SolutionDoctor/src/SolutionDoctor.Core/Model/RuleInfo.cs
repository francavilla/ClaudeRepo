namespace SolutionDoctor.Core.Model
{
    /// <summary>Descrizione di una regola, usata nella legenda del report.</summary>
    public sealed class RuleInfo
    {
        public RuleInfo(string id, string title, string advice)
        {
            Id = id;
            Title = title;
            Advice = advice;
        }

        public string Id { get; private set; }

        public string Title { get; private set; }

        /// <summary>Come intervenire.</summary>
        public string Advice { get; private set; }
    }
}
