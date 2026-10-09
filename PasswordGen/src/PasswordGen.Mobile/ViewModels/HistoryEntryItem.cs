using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;

namespace PasswordGen.Mobile.ViewModels;

/// <summary>Una riga dello storico: la password è mascherata finché non viene mostrata.</summary>
public sealed class HistoryEntryItem : ObservableObject
{
    private const string Mask = "••••••••••••";

    private readonly HistoryEntry _entry;
    private bool _isRevealed;

    public HistoryEntryItem(HistoryEntry entry, Action<HistoryEntryItem> copy, bool isCurrent)
    {
        _entry = entry;
        IsCurrent = isCurrent;
        ToggleCommand = new Command(() => IsRevealed = !IsRevealed, () => _entry.HasPassword);
        CopyCommand = new Command(() => copy(this), () => _entry.HasPassword);
    }

    /// <summary>La voce più recente: la password in uso.</summary>
    public bool IsCurrent { get; }

    public bool IsNotCurrent => !IsCurrent;

    public int Number => _entry.Number;

    public string Title => "#" + _entry.Number;

    public string Password => _entry.Password;

    public bool HasPassword => _entry.HasPassword;

    public string DateText
    {
        get
        {
            var date = _entry.Date;
            return date.HasValue ? date.Value.ToString("d", CultureInfo.CurrentCulture) : _entry.DateText;
        }
    }

    public string ModeText
    {
        get
        {
            if (!_entry.HasPassword)
            {
                return "solo data";
            }

            return _entry.Mode switch
            {
                GenerationMode.Syllables => "sillabe",
                GenerationMode.Random => "casuale",
                _ => "parole",
            };
        }
    }

    public bool IsRevealed
    {
        get => _isRevealed;
        set
        {
            if (SetProperty(ref _isRevealed, value))
            {
                OnPropertyChanged(nameof(DisplayText));
                OnPropertyChanged(nameof(ToggleText));
            }
        }
    }

    public string DisplayText => !_entry.HasPassword
        ? "(registrata solo la data)"
        : _isRevealed ? _entry.Password : Mask;

    public string ToggleText => _isRevealed ? "Nascondi" : "Mostra";

    public ICommand ToggleCommand { get; }

    public ICommand CopyCommand { get; }
}
