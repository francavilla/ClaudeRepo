using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Mvvm;

namespace PasswordGen.ViewModels
{
    /// <summary>Una proposta di password mostrata nell'elenco.</summary>
    public sealed class SuggestionViewModel : ObservableObject
    {
        public SuggestionViewModel(GeneratedPassword password, ICommand copyCommand, bool isPrimary)
        {
            IsPrimary = isPrimary;
            StrengthFraction = System.Math.Min(1.0, System.Math.Max(0.0, password.EntropyBits / 100.0));
            Text = password.Text;
            Mode = password.Mode;
            Level = password.Level;
            LevelText = PasswordStrength.Describe(password.Level);
            BitsText = Round(password.EntropyBits) + " bit";
            CopyCommand = copyCommand;
        }

        /// <summary>La prima proposta della serie: si mostra in grande.</summary>
        public bool IsPrimary { get; private set; }

        public bool IsSecondary
        {
            get { return !IsPrimary; }
        }

        /// <summary>Robustezza da 0 a 1 (100 bit o più = barra piena), per la barra colorata.</summary>
        public double StrengthFraction { get; private set; }

        private string _copyText = "Copia";
        private int _copyVersion;

        /// <summary>Testo del pulsante: diventa «Copiata» per un paio di secondi dopo la copia.</summary>
        public string CopyText
        {
            get { return _copyText; }
            private set { SetProperty(ref _copyText, value); }
        }

        public void ShowCopied()
        {
            CopyText = "Copiata \u2713";
            var application = System.Windows.Application.Current;
            if (application == null)
            {
                return;
            }

            var version = ++_copyVersion;
            var timer = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.Background, application.Dispatcher)
            {
                Interval = System.TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                if (version == _copyVersion)
                {
                    CopyText = "Copia";
                }
            };
            timer.Start();
        }

        public string Text { get; private set; }

        public GenerationMode Mode { get; private set; }

        public StrengthLevel Level { get; private set; }

        public string LevelText { get; private set; }

        public string BitsText { get; private set; }

        public ICommand CopyCommand { get; private set; }

        private static string Round(double bits)
        {
            return ((int)System.Math.Floor(bits)).ToString(CultureInfo.CurrentCulture);
        }
    }
}
