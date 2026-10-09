using System.Windows.Input;
using PasswordGen.Core.Policy;
using PasswordGen.Mobile.Services;

namespace PasswordGen.Mobile.ViewModels;

/// <summary>Un carattere speciale dell'elenco: tasto da toccare per ammetterlo o escluderlo dalle password.</summary>
public sealed class SpecialCharItem : ObservableObject
{
    private bool _isAllowed = true;

    public SpecialCharItem(char character, Action<SpecialCharItem> toggle)
    {
        Character = character;
        Text = character.ToString();
        Name = PasswordPolicy.SpecialName(character);
        ToggleCommand = new Command(() => toggle(this));
    }

    public char Character { get; }

    public string Text { get; }

    public string Name { get; }

    public ICommand ToggleCommand { get; }

    /// <summary>Vero se il carattere può comparire nelle password.</summary>
    public bool IsAllowed
    {
        get => _isAllowed;
        set
        {
            if (SetProperty(ref _isAllowed, value))
            {
                Refresh();
            }
        }
    }

    // Tasto ammesso: sfondo azzurrino e carattere nel colore d'accento; escluso: sfondo della scheda, carattere grigio e barrato.
    public Color ChipBackground => _isAllowed ? AppPalette.SelectedBackground : AppPalette.CardBackground;

    public Color ChipTextColor => _isAllowed ? AppPalette.Accent : AppPalette.MutedText;

    public TextDecorations ChipDecorations => _isAllowed ? TextDecorations.None : TextDecorations.Strikethrough;

    /// <summary>Anche al cambio di tema del telefono: i colori dipendono dalla palette.</summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(ChipBackground));
        OnPropertyChanged(nameof(ChipTextColor));
        OnPropertyChanged(nameof(ChipDecorations));
    }
}
