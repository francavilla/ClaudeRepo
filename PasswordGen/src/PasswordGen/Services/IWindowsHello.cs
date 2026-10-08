using System;
using System.Threading.Tasks;

namespace PasswordGen.Services
{
    /// <summary>Esito di una verifica con Windows Hello.</summary>
    public sealed class HelloResult
    {
        private HelloResult(bool success, string reason)
        {
            Success = success;
            Reason = reason;
        }

        public bool Success { get; private set; }

        /// <summary>Motivo del mancato accesso, in italiano (vuoto se la verifica è riuscita).</summary>
        public string Reason { get; private set; }

        public static HelloResult Ok
        {
            get { return new HelloResult(true, string.Empty); }
        }

        public static HelloResult Failed(string reason)
        {
            return new HelloResult(false, reason);
        }
    }

    /// <summary>Verifica dell'identità dell'utente con Windows Hello (PIN, impronta o volto).</summary>
    public interface IWindowsHello
    {
        /// <summary>Controlla che Windows Hello sia configurato su questo computer.</summary>
        Task<HelloResult> CheckAvailabilityAsync();

        /// <summary>Mostra la finestra di Windows Hello; <paramref name="window"/> è l'handle della finestra dell'app (può essere zero).</summary>
        Task<HelloResult> AuthenticateAsync(IntPtr window, string message);
    }
}
