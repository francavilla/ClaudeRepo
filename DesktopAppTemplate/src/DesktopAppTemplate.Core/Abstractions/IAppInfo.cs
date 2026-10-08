namespace DesktopAppTemplate.Core.Abstractions
{
    /// <summary>Informazioni sull'applicazione in esecuzione.</summary>
    public interface IAppInfo
    {
        string Name { get; }
        string Version { get; }
        string Description { get; }
        string RuntimeDescription { get; }
    }
}
