using PasswordGen.Mobile.ViewModels;

namespace PasswordGen.Mobile;

public partial class SettingsPage : ContentPage
{
    public SettingsPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    /// <summary>Tocco sull'intestazione di una sezione: apre o chiude il contenuto e gira la freccia.</summary>
    private void OnSectionTapped(object sender, TappedEventArgs e)
    {
        if (sender is not Grid header || header.Parent is not VerticalStackLayout section || section.Children.Count < 2)
        {
            return;
        }

        if (section.Children[1] is not VerticalStackLayout content)
        {
            return;
        }

        content.IsVisible = !content.IsVisible;
        foreach (var child in header.Children)
        {
            if (child is Label label && (label.Text == "▾" || label.Text == "▸"))
            {
                label.Text = content.IsVisible ? "▾" : "▸";
            }
        }
    }
}
