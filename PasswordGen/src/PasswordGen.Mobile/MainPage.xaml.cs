using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnDisappearing()
    {
        // La password attuale non deve restare in memoria quando l'app va in secondo piano.
        _viewModel.PreviousPassword = string.Empty;
        PreviousEntry.Text = string.Empty;
        base.OnDisappearing();
    }
}
