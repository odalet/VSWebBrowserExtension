using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Web.WebView2.Wpf;
using Serilog;
using WebBrowserExtension.Settings;
using WebBrowserExtension.Utils;

namespace WebBrowserExtension
{
    [Guid("8ab2cef3-7c52-4e4a-8d07-1dd7f9f90a1c")]
    public sealed class WebBrowserWindow : ToolWindowPane, IVsWindowFrameNotify2
    {
        private readonly ILogger log = Log.Logger;
        private readonly WebBrowserWindowControl control;
        private readonly WebView2 webView;
        private readonly DTE dte;

        public WebBrowserWindow() : base(null)
        {
            Caption = Constants.ExtensionName;
            control = new WebBrowserWindowControl { SetTitleAction = x => Caption = x };
            Content = control;
            webView = control.webView;

            // Retrieve DTE and listen to "Visual Studio Shutdown" event
            ThreadHelper.ThrowIfNotOnUIThread();
            dte = (DTE)Microsoft.VisualStudio.Shell.Package.GetGlobalService(typeof(DTE));
            dte.Events.DTEEvents.OnBeginShutdown += OnVisualStudioShutDown;
        }
        
        public int OnClose(ref uint pgrfSaveOptions)
        {
            log.Debug($"{nameof(WebBrowserWindow)}: OnClose({pgrfSaveOptions})");
            return VSConstants.S_OK;
        }

        protected override void Initialize()
        {
            log.Verbose($"Initializing {nameof(WebBrowserWindow)}");
            base.Initialize();

            try
            {
                var settings = this.GetService<IWebBrowserSettings>();
                webView.Source = new Uri(settings.HomePage);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{nameof(WebBrowserWindow)}.{nameof(Initialize)}(): Could not set Home Page");
            }

            log.Verbose($"Initialized {nameof(WebBrowserWindow)}");
        }

        private void OnVisualStudioShutDown()
        {
            log.Debug($"{nameof(WebBrowserWindow)}: Visual Studio is closing");
            CleanupControl();
        }

        // Not sure if this is necessary. It seems VS takes care of the cleaning up 
        // Never mind, let's keep it that way for now...
        private void CleanupControl()
        {
            log.Debug("Cleaning up the Web Browser control instance");
            if (webView != null)
                webView.Dispose();
        }
    }
}
