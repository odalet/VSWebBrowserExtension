using System;
using System.Windows;
using NLog;

namespace MiniBrowser
{
    public partial class App : Application
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        static App()
        {
            log.Info("Starting Application...");
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private static void OnCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string details;
            if (e.ExceptionObject is Exception exception)
            {
                details = exception.Message;
                log.Fatal(exception, $"Fatal Error: {details}");
            }
            else
            {
                details = e.ExceptionObject?.ToString() ?? "<null>";
                log.Fatal($"Fatal Error: {details}");
            }

            _ = MessageBox.Show($"A fatal error occurred:\r\n\r\n{details}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public App() => Exit += (s, e) => log.Info("Exiting");
    }
}
