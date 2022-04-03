using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using WebBrowserExtension.Utils;

namespace WebBrowserExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(WebBrowserWindow))]
    public sealed class WebBrowserExtensionPackage : AsyncPackage
    {
        public const string PackageGuidString = "1ba34956-275f-48c6-889b-a8834db18c23";
        
        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
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

            //var settings = this.GetMefService<IAvaloniaVSSettings>();
            //var levelSwitch = new LoggingLevelSwitch { MinimumLevel = settings.MinimumLogVerbosity };
            //settings.PropertyChanged += (s, e) => levelSwitch.MinimumLevel = settings.MinimumLogVerbosity;

            var levelSwitch = new LoggingLevelSwitch() { MinimumLevel = LogEventLevel.Verbose };

            var sink = new OutputPaneEventSink(outputWindow, outputTemplate: format);
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .WriteTo.Sink(sink, levelSwitch: levelSwitch)
                .WriteTo.Trace(outputTemplate: format)
                .CreateLogger();
        }
    }
}
