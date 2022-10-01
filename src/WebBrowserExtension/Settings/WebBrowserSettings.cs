using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Serilog;
using Serilog.Events;

namespace WebBrowserExtension.Settings
{
    [Export(typeof(IWebBrowserSettings))]
    public sealed class WebBrowserSettings : IWebBrowserSettings
    {
        private const string defaultHomePage = "https://www.microsoft.com/";
        private const string settingsKey = nameof(WebBrowserSettings);
        
        private readonly ILogger log = Log.Logger;
        private readonly WritableSettingsStore settingsStore;

        [ImportingConstructor]
        public WebBrowserSettings(SVsServiceProvider vsServiceProvider)
        {
            try
            {
                var manager = new ShellSettingsManager(vsServiceProvider);
                settingsStore = manager.GetWritableSettingsStore(SettingsScope.UserSettings);
                if (settingsStore == null)
                    log.Error($"{nameof(WebBrowserSettings)} Constructor: could not retrieve an instance of {nameof(WritableSettingsStore)}");
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{nameof(WebBrowserSettings)} Constructor: could not retrieve an instance of {nameof(WritableSettingsStore)}");
            }

            // Defaults
            homePage = defaultHomePage;
            minimumLogLevel = LogEventLevel.Verbose;

            Load();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string homePage;
        public string HomePage
        {
            get => homePage;
            set => Set(ref homePage, value);
        }

        private LogEventLevel minimumLogLevel;
        public LogEventLevel MinimumLogLevel
        {
            get => minimumLogLevel;
            set => Set(ref minimumLogLevel, value);
        }

        public void Load()
        {
            try
            {
                // NB: the stored url can be intentionally empty, meaning the user wished to open the browser with a blank page
                HomePage = settingsStore.GetString(settingsKey, nameof(HomePage), defaultHomePage) ?? "";
                MinimumLogLevel = (LogEventLevel)settingsStore.GetInt32(settingsKey, nameof(MinimumLogLevel), (int)LogEventLevel.Information);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to load settings");
            }
        }

        public void Save()
        {
            try
            {
                if (!settingsStore.CollectionExists(settingsKey))
                    settingsStore.CreateCollection(settingsKey);

                settingsStore.SetString(settingsKey, nameof(HomePage), HomePage ?? "");
                settingsStore.SetInt32(settingsKey, nameof(MinimumLogLevel), (int)MinimumLogLevel);
            }
            catch (Exception ex)
            {
                log.Error(ex, $"{nameof(WebBrowserSettings)}: Failed to save settings");
            }
        }

        private bool Set<T>(ref T target, T value, [CallerMemberName] string propName = null)
        {
            if (EqualityComparer<T>.Default.Equals(target, value)) return false;
            target = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            return true;
        }
    }
}
