using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WebBrowserExtension.Settings;
using WebBrowserExtension.Utils;

namespace WebBrowserExtension
{
    [Guid(PackageGuidString)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(WebBrowserWindow))]
    [ProvideOptionPage(typeof(WebBrowserOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    [ProvideProfile(typeof(WebBrowserOptionsPage), Constants.ExtensionName, "General", 0, 0, true)]
    public sealed class WebBrowserExtensionPackage : AsyncPackage
    {
        public const string PackageGuidString = "1ba34956-275f-48c6-889b-a8834db18c23";
        
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that relies on services provided by Visual Studio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await base.InitializeAsync(cancellationToken, progress);
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            InitializeLogging();
            await WebBrowserCommand.InitializeAsync(this);
        }

        private void InitializeLogging()
        {
            const string format = "{Timestamp:HH:mm:ss.fff} [{Level}] {Pid} {Message}{NewLine}{Exception}";
            var outputWindow = this.GetService<SVsOutputWindow, IVsOutputWindow>();

            var levelSwitch = new LoggingLevelSwitch { MinimumLevel = LogEventLevel.Verbose };
            (Exception exception, string message) = (null, "");
            try
            {
                var settings = this.GetService<IWebBrowserSettings>();
                levelSwitch.MinimumLevel = settings.MinimumLogLevel;
                settings.PropertyChanged += (s, e) => levelSwitch.MinimumLevel = settings.MinimumLogLevel;
            }
            catch (Exception ex)
            {
                exception = ex;
                message = $"{nameof(WebBrowserExtensionPackage)}.{nameof(InitializeLogging)}(): Could not retrieve Logging Configuration";
            }

            var sink = new OutputPaneEventSink(outputWindow, outputTemplate: format);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Sink(sink, levelSwitch: levelSwitch)
                .WriteTo.Trace(outputTemplate: format)
                .CreateLogger();

            if (exception != null)
                Log.Logger.Error(exception, message ?? $"{exception.Message}");
            else
                Log.Logger.Verbose("Logging initialization complete");
        }
    }
}
