using System;
using System.IO;
using System.Windows;

namespace InstallerGUI
{
    public partial class App : Application
    {
        private static string logPath = Path.Combine(Path.GetTempPath(), "installer_debug.txt");

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            try
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                LogError($"Startup error: {ex}");
                throw;
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError($"Dispatcher error: {e.Exception}");
            e.Handled = false;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            LogError($"Unhandled error: {ex}");
        }

        private static void LogError(string message)
        {
            try
            {
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch { }
        }
    }
}
