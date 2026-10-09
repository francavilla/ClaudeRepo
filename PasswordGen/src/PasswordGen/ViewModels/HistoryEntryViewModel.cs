using System;
using System.Globalization;
using System.Windows.Input;
using PasswordGen.Core.Generation;
using PasswordGen.Core.History;
using PasswordGen.Mvvm;

namespace PasswordGen.ViewModels
{
    /// <summary>Una riga dello storico: la password è mascherata finché non viene mostrata.</summary>
    public sealed class HistoryEntryViewModel : ObservableObject
    {
        private const string Mask = "••••••••••••";

        private readonly HistoryEntry _entry;
        private bool _isRevealed;

        public HistoryEntryViewModel(HistoryEntry entry, Action<HistoryEntryViewModel> copy, Action<HistoryEntryViewModel> editLabel, bool isCurrent)
        {
            _entry = entry;
            EditLabelCommand = new RelayCommand(() => editLabel(this));
            IsCurrent = isCurrent;
            ToggleCommand = new RelayCommand(() => IsRevealed = !IsRevealed, () => _entry.HasPassword);
            CopyCommand = new RelayCommand(() => copy(this), () => _entry.HasPassword);
        }

        /// <summary>Titolo facoltativo («dove la uso»); stringa vuota se non c'è.</summary>
        public string Label
        {
            get { return _entry.Label ?? string.Empty; }
        }

        public bool HasLabel
        {
            get { return !string.IsNullOrEmpty(_entry.Label); }
        }

        public ICommand EditLabelCommand { get; private set; }

        /// <summary>La voce più recente: la password in uso.</summary>
        public bool IsCurrent { get; private set; }

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

        public int Number
        {
            get { return _entry.Number; }
        }

        public string Title
        {
            get { return "#" + _entry.Number; }
        }

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

                switch (_entry.Mode)
                {
                    case GenerationMode.Syllables: return "sillabe";
                    case GenerationMode.Random: return "casuale";
                    default: return "parole";
                }
            }
        }

        public bool HasPassword
        {
            get { return _entry.HasPassword; }
        }

        public string Password
        {
            get { return _entry.Password; }
        }

        public bool IsRevealed
        {
            get { return _isRevealed; }
            private set
            {
                if (SetProperty(ref _isRevealed, value))
                {
                    OnPropertyChanged(nameof(DisplayText));
                    OnPropertyChanged(nameof(ToggleText));
                }
            }
        }

        /// <summary>Rimette la password sotto maschera (quando l'app si blocca).</summary>
        public void Hide()
        {
            IsRevealed = false;
        }

        public string DisplayText
        {
            get
            {
                if (!_entry.HasPassword)
                {
                    return "(registrata solo la data)";
                }

                return _isRevealed ? _entry.Password : Mask;
            }
        }

        public string ToggleText
        {
            get { return _isRevealed ? "Nascondi" : "Mostra"; }
        }

        public ICommand ToggleCommand { get; private set; }

        public ICommand CopyCommand { get; private set; }

    }
}
