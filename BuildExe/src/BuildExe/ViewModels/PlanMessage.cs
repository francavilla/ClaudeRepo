namespace BuildExe.ViewModels
{
    public sealed class PlanMessage
    {
        public PlanMessage(string text, bool isError)
        {
            Text = text;
            IsError = isError;
        }

        public string Text { get; private set; }

        public bool IsError { get; private set; }
    }
}
