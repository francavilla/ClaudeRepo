namespace ExeBuilder.ViewModels
{
    public enum PlanMessageKind
    {
        Info,
        Warning,
        Error
    }

    public sealed class PlanMessage
    {
        public PlanMessage(string text, PlanMessageKind kind)
        {
            Text = text;
            Kind = kind;
        }

        public string Text { get; private set; }

        public PlanMessageKind Kind { get; private set; }
    }
}
