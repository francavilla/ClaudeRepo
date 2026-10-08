using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class HistoryPage : ContentPage
{
    public HistoryPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
