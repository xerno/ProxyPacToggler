using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using ProxyPacToggler.Cli;
using ProxyPacToggler.Core;
using ProxyPacToggler.Ui;
using ProxyPacToggler.Windows;

namespace ProxyPacToggler
{
    internal static class Program
    {
        private const string ApplicationName = "ProxyPacToggler";

        [STAThread]
        private static int Main(string[] args)
        {
            string dataDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                ApplicationName);

            ILog log = new FileLog(dataDirectory);

            // Routed before anything else is built: the watcher needs none of it.
            if (args.Length == 1 && CommandLine.Normalize(args[0]) == "watch")
                return new Watcher(log).Run();

            AutoStartEntry autoStart = new AutoStartEntry(
                ApplicationName, Application.ExecutablePath, log);

            using (NetworkVpnMonitor monitor = new NetworkVpnMonitor())
            using (SystemLifecycle lifecycle = new SystemLifecycle(log))
            {
                ProxyController controller = new ProxyController(
                    new RegistryProxySettings(log),
                    monitor,
                    new IniSettingsStore(dataDirectory, log),
                    log);

                return args.Length > 0
                    ? new CommandLine(controller, autoStart, SystemConsole.AttachedToCaller()).Run(args)
                    : RunTray(controller, monitor, lifecycle, autoStart, log);
            }
        }

        private static int RunTray(ProxyController controller, IVpnMonitor monitor,
                                   SystemLifecycle lifecycle, AutoStartEntry autoStart, ILog log)
        {
            bool acquired;
            using (new Mutex(true, ProcessGuard.TrayMutex, out acquired))
            {
                if (!acquired)
                {
                    // Staying silent beats nagging on every logon.
                    log.Write("another instance is already running, exiting");
                    return CommandLine.ExitOk;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                InstallCrashLogging(log);

                using (TrayApp app = new TrayApp(controller, monitor, lifecycle, autoStart, log))
                {
                    app.Start();
                    Application.Run();
                }

                log.Write("message loop ended");
                return CommandLine.ExitOk;
            }
        }

        private static void InstallCrashLogging(ILog log)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                log.Write("unhandled UI exception: " + e.Exception);
            };

            AppDomain.CurrentDomain.UnhandledException += delegate(object sender,
                                                                   UnhandledExceptionEventArgs e)
            {
                log.Write("FATAL: " + e.ExceptionObject);
            };
        }
    }
}
