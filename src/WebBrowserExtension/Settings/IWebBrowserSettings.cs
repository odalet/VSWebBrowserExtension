using System;
using System.ComponentModel;
using Serilog.Events;
using WebBrowserExtension.Utils;

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
        public static Uri GetHomePageUri(this IWebBrowserSettings settings) => UriHelper.MakeUri(settings.HomePage);
    }
}
