using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;

namespace PasswordGen.ViewModels
{
    /// <summary>Una proposta di password mostrata nell'elenco.</summary>
    public sealed class SuggestionViewModel
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
