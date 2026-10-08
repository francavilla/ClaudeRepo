using System.Windows.Forms;
using DesktopAppTemplate.Features.About;

namespace DesktopAppTemplate.UI.WinForms.Views
{
    /// <summary>Pagina "Informazioni" in Windows Forms. L'aspetto è nel designer (AboutView.Designer.cs).</summary>
    public partial class AboutView : UserControl
    {
        /// <summary>Costruttore per il designer; a runtime chiamare poi <see cref="Bind"/>.</summary>
        public AboutView()
        {
            InitializeComponent();
        }

        public AboutView(AboutViewModel viewModel)
            : this()
        {
            Bind(viewModel);
        }

        public void Bind(AboutViewModel viewModel)
        {
            pageTitleLabel.Text = viewModel.Title;
            nameLabel.Text = viewModel.AppName + "   " + viewModel.Version;
            descriptionLabel.Text = viewModel.Description;
            uiValueLabel.Text = viewModel.UiName;
            runtimeValueLabel.Text = viewModel.Runtime;
        }
    }
}
