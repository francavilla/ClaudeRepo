using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
