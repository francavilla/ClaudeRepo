using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
