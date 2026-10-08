using Microsoft.Maui.ApplicationModel;
using PasswordGen.Core.Security;

namespace PasswordGen.Mobile.Services;

public sealed class DialogService : IDialogService
{
    private const string Cancel = "Annulla";

    private static Page CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    public async Task<bool> ConfirmAsync(string message, string title)
    {
        var page = CurrentPage;
        return page != null && await page.DisplayAlert(title, message, "Sì", "No");
    }

    public async Task<int> ChooseAsync(string title, IReadOnlyList<string> options)
    {
        var page = CurrentPage;
        if (page == null)
        {
            return -1;
        }

        var choice = await page.DisplayActionSheet(title, Cancel, null, options.ToArray());
        if (choice == null || choice == Cancel)
        {
            return -1;
        }

        for (var i = 0; i < options.Count; i++)
        {
            if (options[i] == choice)
            {
                return i;
            }
        }

        return -1;
    }

    public async Task<string> AskNewSecretAsync(CredentialKind kind)
    {
        var page = CurrentPage;
        if (page == null)
        {
            return null;
        }

        var setup = new CredentialSetupPage(kind);
        await MainThread.InvokeOnMainThreadAsync(() => page.Navigation.PushModalAsync(setup, true));
        return await setup.Result;
    }
}
