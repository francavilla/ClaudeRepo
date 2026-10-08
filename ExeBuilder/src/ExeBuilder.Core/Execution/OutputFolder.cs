using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExeBuilder.Core.Execution
{
    /// <summary>
    /// La cartella di output viene svuotata prima di ogni build. Poiché è una cancellazione
    /// ricorsiva di un percorso scelto dall'utente, prima si verifica che sia sicuro farlo.
    /// </summary>
    public static class OutputFolder
    {
        private static readonly Environment.SpecialFolder[] SystemFolders =
        {
            Environment.SpecialFolder.Windows,
            Environment.SpecialFolder.System,
            Environment.SpecialFolder.SystemX86,
            Environment.SpecialFolder.ProgramFiles,
            Environment.SpecialFolder.ProgramFilesX86,
            Environment.SpecialFolder.CommonApplicationData,
            Environment.SpecialFolder.UserProfile,
            Environment.SpecialFolder.Desktop,
            Environment.SpecialFolder.DesktopDirectory,
            Environment.SpecialFolder.MyDocuments,
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolder.LocalApplicationData
        };

        /// <summary>
        /// Restituisce null se la cartella può essere svuotata, altrimenti il motivo del rifiuto.
        /// </summary>
        /// <param name="outputDirectory">Cartella di output (assoluta).</param>
        /// <param name="protectedDirectories">Cartelle con i sorgenti (progetti, solution): non devono stare dentro l'output.</param>
        public static string ValidateForCleaning(string outputDirectory, IEnumerable<string> protectedDirectories)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                return null;
            }

            var output = Normalize(outputDirectory);
            if (string.Equals(output, Normalize(Path.GetPathRoot(output)), StringComparison.OrdinalIgnoreCase))
            {
                return "La cartella di output è la radice di un'unità: non può essere svuotata. Indicare una sottocartella.";
            }

            foreach (var folder in SystemFolders)
            {
                var path = Environment.GetFolderPath(folder);
                if (!string.IsNullOrEmpty(path) && IsSameOrInside(Normalize(path), output))
                {
                    return "La cartella di output è o contiene una cartella di sistema (" + path + "): non può essere svuotata. Indicare una cartella dedicata.";
                }
            }

            foreach (var source in (protectedDirectories ?? Enumerable.Empty<string>()).Where(d => !string.IsNullOrWhiteSpace(d)))
            {
                if (IsSameOrInside(Normalize(source), output))
                {
                    return "La cartella di output contiene i sorgenti (" + source + "): svuotandola verrebbero cancellati. Indicare una cartella dedicata, ad esempio una sottocartella.";
                }
            }

            return null;
        }

        /// <summary>
        /// Svuota la cartella (la cartella stessa resta). Restituisce il numero di file eliminati.
        /// I file in sola lettura vengono sbloccati; i link (junction/symlink) vengono rimossi senza seguirli.
        /// </summary>
        public static int Clean(string directory)
        {
            if (!Directory.Exists(directory))
            {
                return 0;
            }

            var reason = ValidateForCleaning(directory, null);
            if (reason != null)
            {
                throw new InvalidOperationException(reason);
            }

            return CleanContents(new DirectoryInfo(directory));
        }

        private static int CleanContents(DirectoryInfo directory)
        {
            var deleted = 0;
            foreach (var file in directory.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
                file.Delete();
                deleted++;
            }

            foreach (var child in directory.GetDirectories())
            {
                if ((child.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    // Junction o symlink: si elimina il collegamento, non il contenuto della destinazione.
                    child.Delete(false);
                    continue;
                }

                deleted += CleanContents(child);
                child.Attributes = FileAttributes.Directory;
                child.Delete(false);
            }

            return deleted;
        }

        /// <summary>True se <paramref name="path"/> coincide con <paramref name="container"/> o vi è contenuto.</summary>
        internal static bool IsSameOrInside(string path, string container)
        {
            if (string.Equals(path, container, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var prefix = container.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? container
                : container + Path.DirectorySeparatorChar;
            return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string path)
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetPathRoot(full);
            // Toglie il separatore finale, tranne che per la radice ("C:\", "/").
            return full.Length > root.Length ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : full;
        }
    }
}
