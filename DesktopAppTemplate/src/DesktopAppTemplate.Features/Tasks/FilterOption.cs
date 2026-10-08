namespace DesktopAppTemplate.Features.Tasks
{
    /// <summary>Voce del filtro mostrata nella UI.</summary>
    public sealed class FilterOption
    {
        public FilterOption(string label, TaskFilter value)
        {
            Label = label;
            Value = value;
        }

        public string Label { get; }
        public TaskFilter Value { get; }

        public override string ToString() => Label;
    }
}
