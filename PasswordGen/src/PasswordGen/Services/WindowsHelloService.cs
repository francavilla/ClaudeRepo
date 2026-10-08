using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Security.Credentials.UI;

namespace PasswordGen.Services
{
    /// <summary>
    /// Windows Hello tramite <see cref="UserConsentVerifier"/>. Per le app desktop la finestra va richiesta con l'handle
    /// della finestra (interfaccia COM <c>IUserConsentVerifierInterop</c>, Windows 11); su Windows 10 si usa la richiesta semplice.
    /// L'app non vede mai PIN, impronta o volto: riceve solo l'esito.
    /// </summary>
    public sealed class WindowsHelloService : IWindowsHello
    {
        public async Task<HelloResult> CheckAvailabilityAsync()
        {
            try
            {
                var availability = await UserConsentVerifier.CheckAvailabilityAsync().AsTask();
                switch (availability)
                {
                    case UserConsentVerifierAvailability.Available:
                        return HelloResult.Ok;
                    case UserConsentVerifierAvailability.DeviceBusy:
                        return HelloResult.Failed("Windows Hello è occupato: riprova tra un momento");
                    case UserConsentVerifierAvailability.DisabledByPolicy:
                        return HelloResult.Failed("Windows Hello è disattivato dai criteri della tua organizzazione");
                    case UserConsentVerifierAvailability.NotConfiguredForUser:
                        return HelloResult.Failed("Windows Hello non è configurato: imposta un PIN, un'impronta o il volto in Impostazioni > Account > Opzioni di accesso");
                    default:
                        return HelloResult.Failed("Windows Hello non è disponibile su questo computer");
                }
            }
            catch (Exception ex)
            {
                return HelloResult.Failed("Windows Hello non risponde: " + ex.Message);
            }
        }

        public async Task<HelloResult> AuthenticateAsync(IntPtr window, string message)
        {
            try
            {
                UserConsentVerificationResult result;
                if (window != IntPtr.Zero)
                {
                    try
                    {
                        result = await RequestForWindowAsync(window, message);
                    }
                    catch (Exception)
                    {
                        // L'interfaccia con l'handle non c'è su questa versione di Windows: richiesta semplice.
                        result = await UserConsentVerifier.RequestVerificationAsync(message).AsTask();
                    }
                }
                else
                {
                    result = await UserConsentVerifier.RequestVerificationAsync(message).AsTask();
                }

                switch (result)
                {
                    case UserConsentVerificationResult.Verified:
                        return HelloResult.Ok;
                    case UserConsentVerificationResult.Canceled:
                        return HelloResult.Failed("verifica annullata");
                    case UserConsentVerificationResult.RetriesExhausted:
                        return HelloResult.Failed("troppi tentativi falliti");
                    case UserConsentVerificationResult.DeviceBusy:
                        return HelloResult.Failed("Windows Hello è occupato");
                    case UserConsentVerificationResult.DisabledByPolicy:
                        return HelloResult.Failed("Windows Hello è disattivato dai criteri dell'organizzazione");
                    case UserConsentVerificationResult.NotConfiguredForUser:
                        return HelloResult.Failed("Windows Hello non è configurato per questo utente");
                    default:
                        return HelloResult.Failed("Windows Hello non è disponibile");
                }
            }
            catch (Exception ex)
            {
                return HelloResult.Failed("errore di Windows Hello: " + ex.Message);
            }
        }

        private static Task<UserConsentVerificationResult> RequestForWindowAsync(IntPtr window, string message)
        {
            var factory = (IUserConsentVerifierInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(UserConsentVerifier));
            var iid = typeof(IAsyncOperation<UserConsentVerificationResult>).GUID;
            return factory.RequestVerificationForWindowAsync(window, message, ref iid).AsTask();
        }

        [ComImport]
        [Guid("39E050C3-4E74-441A-8DC0-B81104DF949C")]
        [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
        private interface IUserConsentVerifierInterop
        {
            IAsyncOperation<UserConsentVerificationResult> RequestVerificationForWindowAsync(
                IntPtr appWindow,
                [MarshalAs(UnmanagedType.HString)] string message,
                [In] ref Guid riid);
        }
    }
}
