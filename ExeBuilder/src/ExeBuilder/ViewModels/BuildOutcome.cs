namespace ExeBuilder.ViewModels
{
    /// <summary>Esito dell'ultima build, mostrato nel banner sopra la console.</summary>
    public enum BuildOutcome
    {
        None,
        Running,
        Succeeded,
        Failed,
        Cancelled
    }
}
