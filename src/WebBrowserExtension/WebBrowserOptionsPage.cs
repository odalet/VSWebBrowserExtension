using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Serilog;
using WebBrowserExtension.Settings;
using WebBrowserExtension.Utils;

namespace WebBrowserExtension
{
    [ComVisible(true), Guid("6a23a02d-5801-4562-b257-b58370eb4e32")]
    public sealed class WebBrowserOptionsPage : UIElementDialogPage
    {
        private readonly ILogger log = Log.Logger;
        private WebBrowserOptionsPageControl control;

        protected override UIElement Child => control ?? (control = new WebBrowserOptionsPageControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            log.Debug($"{nameof(WebBrowserOptionsPage)}: OnActivate(Cancel: {e.Cancel})");
            base.OnActivate(e);
            control.Settings = Site.GetService<IWebBrowserSettings>();
        }

        public override void LoadSettingsFromStorage()
        {
            log.Debug($"{nameof(WebBrowserOptionsPage)}: LoadSettingFromStorage()");
            base.LoadSettingsFromStorage();
            control?.Settings?.Load();
        }

        public override void SaveSettingsToStorage()
        {
            log.Debug($"{nameof(WebBrowserOptionsPage)}: SaveSettingsToStorage()");
            base.SaveSettingsToStorage();
            control?.Settings?.Save();
        }
    }
}
