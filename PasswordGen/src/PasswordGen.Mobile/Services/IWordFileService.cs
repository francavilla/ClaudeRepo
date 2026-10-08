namespace PasswordGen.Mobile.Services;

/// <summary>File scelto dall'utente, copiato in modo provvisorio nella cartella privata dell'app.</summary>
public sealed class PickedWordFile
{
    public PickedWordFile(string tempPath, string finalPath)
    {
        TempPath = tempPath;
        FinalPath = finalPath;
    }

    /// <summary>Copia provvisoria, da leggere e validare.</summary>
    public string TempPath { get; }

    /// <summary>Dove finirà la copia definitiva se il file è valido (mantiene il nome originale).</summary>
    public string FinalPath { get; }
}

/// <summary>Scelta e conservazione del file di parole dell'utente.</summary>
public interface IWordFileService
{
    /// <summary>Fa scegliere un file e ne fa una copia provvisoria; restituisce null se l'utente annulla.</summary>
    Task<PickedWordFile> PickAsync();

    /// <summary>Rende definitiva la copia (sostituisce un file con lo stesso nome).</summary>
    void Commit(PickedWordFile file);

    /// <summary>Elimina la copia provvisoria.</summary>
    void Discard(PickedWordFile file);

    /// <summary>Elimina una copia definitiva (solo dentro la cartella dei file di parole dell'app).</summary>
    void Delete(string path);
}
