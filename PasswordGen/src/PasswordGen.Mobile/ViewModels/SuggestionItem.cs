using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using PasswordGen.Core.Generation;

namespace PasswordGen.Mobile.ViewModels;

/// <summary>Una proposta di password mostrata nell'elenco.</summary>
public sealed class SuggestionItem
{
    public SuggestionItem(GeneratedPassword password, ICommand copyCommand)
    {
        Text = password.Text;
        Mode = password.Mode;
        LevelText = PasswordStrength.Describe(password.Level);
        BitsText = ((int)Math.Floor(password.EntropyBits)).ToString(CultureInfo.CurrentCulture) + " bit";
        LevelColor = password.Level switch
        {
            StrengthLevel.Weak => Color.FromArgb("#DC2626"),
            StrengthLevel.Fair => Color.FromArgb("#B45309"),
            _ => Color.FromArgb("#15803D"),
        };
        CopyCommand = copyCommand;
    }

    public string Text { get; }

    public GenerationMode Mode { get; }

    public string LevelText { get; }

    public string BitsText { get; }

    public Color LevelColor { get; }

    public ICommand CopyCommand { get; }
}
