namespace SolutionDoctor.Presentation.Services
{
    /// <summary>Preferenze ricordate tra un avvio e l'altro.</summary>
    public sealed class AppSettings
    {
        public string LastPath { get; set; } = string.Empty;

        public bool UseGit { get; set; } = true;

        public int GitMonths { get; set; } = 12;
    }

    public interface ISettingsStore
    {
        AppSettings Load();

        void Save(AppSettings settings);
    }
}
