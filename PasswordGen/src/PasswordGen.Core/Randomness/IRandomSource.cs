namespace PasswordGen.Core.Randomness
{
    /// <summary>Sorgente di numeri casuali: in produzione è crittografica, nei test è deterministica.</summary>
    public interface IRandomSource
    {
        /// <summary>Numero intero uniforme in [0, <paramref name="maxExclusive"/>).</summary>
        int Next(int maxExclusive);
    }
}
