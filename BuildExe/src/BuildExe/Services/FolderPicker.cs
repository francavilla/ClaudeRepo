using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace BuildExe.Services
{
    /// <summary>
    /// Selettore di cartelle moderno (IFileOpenDialog con FOS_PICKFOLDERS), al posto del
    /// FolderBrowserDialog ad albero di WinForms. Nessuna dipendenza esterna.
    /// </summary>
    internal static class FolderPicker
    {
        private const uint FosPickFolders = 0x00000020;
        private const uint FosForceFileSystem = 0x00000040;
        private const uint FosNoChangeDir = 0x00000008;
        private const uint SigdnFileSysPath = 0x80058000;
        private const int ErrorCancelled = unchecked((int)0x800704C7);

        public static string Show(Window owner, string title, string initialDirectory)
        {
            var dialog = (IFileOpenDialog)new FileOpenDialogCoClass();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                dialog.SetOptions(options | FosPickFolders | FosForceFileSystem | FosNoChangeDir);
                dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialDirectory))
                {
                    IShellItem folder;
                    var iid = typeof(IShellItem).GUID;
                    if (SHCreateItemFromParsingName(initialDirectory, IntPtr.Zero, ref iid, out folder) == 0)
                    {
                        dialog.SetFolder(folder);
                    }
                }

                var hwnd = owner != null ? new WindowInteropHelper(owner).Handle : IntPtr.Zero;
                var hr = dialog.Show(hwnd);
                if (hr == ErrorCancelled)
                {
                    return null;
                }

                Marshal.ThrowExceptionForHR(hr);

                IShellItem result;
                dialog.GetResult(out result);
                string path;
                result.GetDisplayName(SigdnFileSysPath, out path);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, out IShellItem ppv);

        [ComImport]
        [Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogCoClass
        {
        }

        // Solo i metodi fino a GetResult: l'ordine deve rispecchiare la vtable di IFileDialog.
        [ComImport]
        [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            int Show(IntPtr parent);

            void SetFileTypes(uint cFileTypes, IntPtr rgFilterSpec);

            void SetFileTypeIndex(uint iFileType);

            void GetFileTypeIndex(out uint piFileType);

            void Advise(IntPtr pfde, out uint pdwCookie);

            void Unadvise(uint dwCookie);

            void SetOptions(uint fos);

            void GetOptions(out uint pfos);

            void SetDefaultFolder(IShellItem psi);

            void SetFolder(IShellItem psi);

            void GetFolder(out IShellItem ppsi);

            void GetCurrentSelection(out IShellItem ppsi);

            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);

            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            void GetResult(out IShellItem ppsi);
        }

        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);

            void GetParent(out IShellItem ppsi);

            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        }
    }
}
