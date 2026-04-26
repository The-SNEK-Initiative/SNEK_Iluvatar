using System;
using System.Windows;

namespace DeveloperTool
{
    public partial class App : Application
    {
        private static string logPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "snek_dev_startup.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            System.IO.File.WriteAllText(logPath, $"[{System.DateTime.Now}] DeveloperTool OnStartup called\n");
            
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            base.OnStartup(e);
            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now}] DeveloperTool base.OnStartup finished\n");
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now}] DISPATCHER CRASH: {e.Exception.Message}\n{e.Exception.StackTrace}\n");
            MessageBox.Show($"A critical error occurred: {e.Exception.Message}", "SNEK Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
            Application.Current.Shutdown();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now}] DOMAIN CRASH: {ex.Message}\n{ex.StackTrace}\n");
            }
            else
            {
                System.IO.File.AppendAllText(logPath, $"[{System.DateTime.Now}] DOMAIN CRASH (Unknown object): {e.ExceptionObject}\n");
            }
        }
    }
}
