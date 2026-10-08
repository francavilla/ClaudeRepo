using System;
using System.IO;

namespace SolutionDoctor.Core.Inventory
{
    internal static class PathUtil
    {
        /// <summary>Percorso di <paramref name="full"/> relativo a <paramref name="root"/>, con '/' come separatore.</summary>
        public static string Relative(string root, string full)
        {
            var sep = Path.DirectorySeparatorChar;
            var r = Path.GetFullPath(root).TrimEnd(sep) + sep;
            var f = Path.GetFullPath(full);
            if (f.StartsWith(r, StringComparison.OrdinalIgnoreCase))
            {
                f = f.Substring(r.Length);
            }

            return f.Replace('\\', '/');
        }

        /// <summary>Converte i separatori di un percorso MSBuild/sln in quelli del sistema operativo.</summary>
        public static string Normalize(string path)
        {
            return path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }
}
