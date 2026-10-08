namespace PasswordGen.Mobile.Services;

/// <summary>
/// Vero mentre l'app mostra una schermata di Android (scelta di un file, per esempio): l'app esce dal primo piano e rientra,
/// e questo non va contato come uscita ai fini del blocco.
/// </summary>
public static class ExternalActivity
{
    public static bool IsActive { get; set; }
}
