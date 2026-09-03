using System;
using System.Runtime.InteropServices;

namespace ProxyPacToggler.Windows
{
    internal static class Native
    {
        private const int InternetOptionSettingsChanged = 39;
        private const int InternetOptionRefresh = 37;

        public const int AttachParentProcess = -1;

        [DllImport("wininet.dll", SetLastError = true)]
        private static extern bool InternetSetOption(IntPtr session, int option,
                                                     IntPtr buffer, int bufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(int processId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr icon);

        // Without this, running programs keep the old proxy configuration.
        public static void NotifyProxyChanged()
        {
            InternetSetOption(IntPtr.Zero, InternetOptionSettingsChanged, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, InternetOptionRefresh, IntPtr.Zero, 0);
        }
    }
}
