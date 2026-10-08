using Microsoft.Maui.Storage;

namespace PasswordGen.Mobile.Services;

/// <summary>
/// Il selettore di file di Android non restituisce un percorso utilizzabile (spesso è un indirizzo content://): il file viene quindi
/// copiato nella cartella privata dell'app, dove nessun altro programma può leggerlo.
/// </summary>
public sealed class WordFileService : IWordFileService
{
    private const long MaxBytes = 5 * 1024 * 1024;

    private static string Folder => Path.Combine(FileSystem.AppDataDirectory, "words");

    public async Task<PickedWordFile> PickAsync()
    {
        FileResult picked;
        ExternalActivity.IsActive = true;   // il selettore di file porta l'app in secondo piano: non deve far scattare il blocco
        try
        {
            picked = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Scegli il file delle parole" });
        }
        finally
        {
            ExternalActivity.IsActive = false;
        }

        if (picked == null)
        {
            return null;
        }

        Directory.CreateDirectory(Folder);

        var name = Path.GetFileName(picked.FileName);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "parole.txt";
        }

        var temp = Path.Combine(Folder, "in-arrivo.tmp");
        try
        {
            long total = 0;
            using (var input = await picked.OpenReadAsync())
            using (var output = File.Create(temp))
            {
                var buffer = new byte[16 * 1024];
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    total += read;
                    if (total > MaxBytes)
                    {
                        throw new IOException("Il file è troppo grande (massimo 5 MB).");
                    }

                    await output.WriteAsync(buffer, 0, read);
                }
            }
        }
        catch (Exception)
        {
            TryDelete(temp);
            throw;
        }

        return new PickedWordFile(temp, Path.Combine(Folder, name));
    }

    public void Commit(PickedWordFile file)
    {
        TryDelete(file.FinalPath);
        File.Move(file.TempPath, file.FinalPath);
    }

    public void Discard(PickedWordFile file)
    {
        TryDelete(file.TempPath);
    }

    public void Delete(string path)
    {
        if (!string.IsNullOrEmpty(path) && path.StartsWith(Folder, StringComparison.Ordinal))
        {
            TryDelete(path);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Non è un problema se il file resta: verrà sovrascritto la volta dopo.
        }
    }
}
