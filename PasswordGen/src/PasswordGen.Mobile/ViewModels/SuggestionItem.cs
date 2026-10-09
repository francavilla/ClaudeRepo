using System.Globalization;
using System.Windows.Input;
using Microsoft.Maui.Graphics;
using PasswordGen.Core.Generation;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile.ViewModels;

/// <summary>Una proposta di password mostrata nell'elenco.</summary>
public sealed class SuggestionItem
{
    public SuggestionItem(GeneratedPassword password, ICommand copyCommand, bool isPrimary)
    {
        IsPrimary = isPrimary;
        StrengthFraction = Math.Min(1.0, Math.Max(0.0, password.EntropyBits / 100.0));
        Text = password.Text;
        Mode = password.Mode;
        LevelText = PasswordStrength.Describe(password.Level);
        BitsText = ((int)Math.Floor(password.EntropyBits)).ToString(CultureInfo.CurrentCulture) + " bit";
        LevelColor = password.Level switch
        {
            StrengthLevel.Weak => AppPalette.StrengthWeak,
            StrengthLevel.Fair => AppPalette.StrengthFair,
            _ => AppPalette.StrengthGood,
        };
        CopyCommand = copyCommand;
    }

    /// <summary>La prima proposta della serie: si mostra in grande.</summary>
    public bool IsPrimary { get; }

    public bool IsSecondary => !IsPrimary;

    /// <summary>Robustezza da 0 a 1 (100 bit o più = barra piena), per la barra colorata.</summary>
    public double StrengthFraction { get; }

    public string Text { get; }

    public GenerationMode Mode { get; }

    public string LevelText { get; }

    public string BitsText { get; }

    public Color LevelColor { get; }

    public ICommand CopyCommand { get; }
}
