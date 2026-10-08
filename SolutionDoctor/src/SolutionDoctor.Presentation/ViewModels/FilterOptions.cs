using SolutionDoctor.Core.Model;

namespace SolutionDoctor.Presentation.ViewModels
{
    /// <summary>Voce del filtro per gravità: mostra i problemi di gravità pari o superiore al minimo.</summary>
    public sealed class SeverityFilterOption
    {
        public SeverityFilterOption(string label, Severity minimum)
        {
            Label = label;
            Minimum = minimum;
        }

        public string Label { get; private set; }

        public Severity Minimum { get; private set; }
    }

    /// <summary>Voce del filtro per regola; <see cref="RuleId"/> nullo = tutte le regole.</summary>
    public sealed class RuleFilterOption
    {
        public RuleFilterOption(string ruleId, string label)
        {
            RuleId = ruleId;
            Label = label;
        }

        public string RuleId { get; private set; }

        public string Label { get; private set; }
    }
}
