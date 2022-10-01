using System;
using System.ComponentModel;
using Serilog.Events;

namespace WebBrowserExtension.Settings
{
    public interface IWebBrowserSettings : INotifyPropertyChanged
    {
        string HomePage { get; set; }
        LogEventLevel MinimumLogLevel { get; set; }
        void Save();
        void Load();
    }

    internal static class WebBrowserSettingsExtensions
    {
        public static Uri GetHomePageUri(this IWebBrowserSettings settings) =>
            new Uri(string.IsNullOrEmpty(settings.HomePage) ? "about:blank" : settings.HomePage);
    }
}
