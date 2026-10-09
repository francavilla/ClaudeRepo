using System;
using System.Windows.Input;
using PasswordGen.Core.Policy;
using PasswordGen.Mvvm;

namespace PasswordGen.ViewModels
{
    /// <summary>Un carattere speciale dell'elenco: tasto da toccare per ammetterlo o escluderlo dalle password.</summary>
    public sealed class SpecialCharViewModel : ObservableObject
    {
        private bool _isAllowed = true;

        public SpecialCharViewModel(char character, Action<SpecialCharViewModel> toggle)
        {
            Character = character;
            Text = character.ToString();
            Name = PasswordPolicy.SpecialName(character);
            ToggleCommand = new RelayCommand(() => toggle(this));
        }

        public char Character { get; private set; }

        public string Text { get; private set; }

        /// <summary>Nome parlato, per il suggerimento («E commerciale»).</summary>
        public string Name { get; private set; }

        public ICommand ToggleCommand { get; private set; }

        /// <summary>Vero se il carattere può comparire nelle password.</summary>
        public bool IsAllowed
        {
            get { return _isAllowed; }
            set { SetProperty(ref _isAllowed, value); }
        }

        public string ToolTipText
        {
            get { return Name + (_isAllowed ? ": ammesso (tocca per escluderlo)" : ": escluso (tocca per ammetterlo)"); }
        }
    }
}
