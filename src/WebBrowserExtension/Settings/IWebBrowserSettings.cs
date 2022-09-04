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
}
