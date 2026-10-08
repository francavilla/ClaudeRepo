using System.Threading.Tasks;

namespace DesktopAppTemplate.Core.Mvvm
{
    /// <summary>
    /// Base di una pagina navigabile. Per aggiungere una pagina: derivare da questa classe,
    /// registrarla come <see cref="PageViewModel"/> e creare la view corrispondente in ciascuna UI.
    /// </summary>
    public abstract class PageViewModel : ObservableObject
    {
        private bool _isSelected;

        protected PageViewModel(string title, string icon, int order)
        {
            Title = title;
            Icon = icon;
            Order = order;
        }

        public string Title { get; }

        /// <summary>Glifo (font Segoe MDL2 Assets) mostrato nel menu laterale.</summary>
        public string Icon { get; }

        /// <summary>Posizione nel menu laterale (ordine crescente).</summary>
        public int Order { get; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set { SetProperty(ref _isSelected, value); }
        }

        /// <summary>Chiamato ogni volta che la pagina diventa quella attiva.</summary>
        public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
    }
}
