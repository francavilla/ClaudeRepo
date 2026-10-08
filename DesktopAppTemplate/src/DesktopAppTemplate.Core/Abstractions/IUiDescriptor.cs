namespace DesktopAppTemplate.Core.Abstractions
{
    /// <summary>Descrive l'interfaccia utente attiva (es. "WPF", "Windows Forms").</summary>
    public interface IUiDescriptor
    {
        string Name { get; }
    }
}
