using System.Windows.Controls;
using WebBrowserExtension.Settings;

namespace WebBrowserExtension
{
    public partial class WebBrowserOptionsPageControl : UserControl
    {
        public WebBrowserOptionsPageControl() => InitializeComponent();

        public IWebBrowserSettings Settings
        {
            get => DataContext as IWebBrowserSettings;
            set => DataContext = value;
        }
    }
}
