using System;
using System.Windows;
using System.Linq;

namespace InstallerGUI
{
    public partial class Program
    {
        [System.STAThread]
        public static void Main(string[] args)
        {
            if (args.Contains("--verify"))
            {
                try
                {
                    Console.WriteLine("Verifying embedded package...");
                    var window = new MainWindow();
                    // ExtractEmbeddedPackage is called in constructor
                    Console.WriteLine("Verification successful!");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Verification failed: {ex.Message}");
                    Environment.Exit(1);
                }
            }

            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}