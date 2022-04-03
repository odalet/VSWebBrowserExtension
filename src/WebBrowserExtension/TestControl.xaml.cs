using System.Windows;
using System.Windows.Controls;
using Serilog;

namespace WebBrowserExtension
{
    public partial class TestControl : UserControl
    {
        private readonly ILogger log = Log.Logger;

        public TestControl()
        {
            InitializeComponent();
            Loaded += TestControl_Loaded;
            Unloaded += TestControl_Unloaded;
        }

        private void TestControl_Loaded(object sender, RoutedEventArgs e) =>
            log.Debug("TestControl: loaded");

        private void TestControl_Unloaded(object sender, RoutedEventArgs e) =>
            log.Debug("TestControl: unloaded");
    }
}
