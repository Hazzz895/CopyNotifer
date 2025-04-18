using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace CopyNotifer
{
    public sealed class ClipboardHooker : IDisposable
    {
        private const int WM_CLIPBOARDUPDATE = 0x031D;
        private readonly HwndSourceHook _hook;
        private readonly HwndSource _hwndSource;
        private bool _disposed;

        public event EventHandler ClipboardChanged;

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        public ClipboardHooker(HwndSource hwndSource)
        {
            _hwndSource = hwndSource ?? throw new ArgumentNullException(nameof(hwndSource));

            if (!AddClipboardFormatListener(_hwndSource.Handle))
                throw new Win32Exception(Marshal.GetLastWin32Error());

            _hook = WndProc;
            _hwndSource.AddHook(_hook);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_CLIPBOARDUPDATE:
                    ClipboardChanged?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;
                default:
                    handled = false;
                    break;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;
            _disposed = true;
            if (disposing)
                _hwndSource.RemoveHook(_hook);
            if (!RemoveClipboardFormatListener(_hwndSource.Handle) && disposing)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        ~ClipboardHooker() => Dispose(false);
    }
}
