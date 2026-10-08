using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;

namespace PasswordGen.ViewModels
{
    /// <summary>Una proposta di password mostrata nell'elenco.</summary>
    public sealed class SuggestionViewModel
    {
        public SuggestionViewModel(GeneratedPassword password, ICommand copyCommand)
        {
            Text = password.Text;
            Mode = password.Mode;
            Level = password.Level;
            LevelText = PasswordStrength.Describe(password.Level);
            BitsText = Round(password.EntropyBits) + " bit";
            CopyCommand = copyCommand;
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
