using System;
using System.IO;

namespace SolutionDoctor.Core.Tests
{
    /// <summary>Cartella temporanea per i file dei test; eliminata al Dispose.</summary>
    internal sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SolutionDoctorTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; private set; }

        public string Write(string relativePath, string content)
        {
            var full = System.IO.Path.Combine(Path, relativePath.Replace('\\', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full));
            File.WriteAllText(full, content);
            return full;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
            }
        }
    }
}
