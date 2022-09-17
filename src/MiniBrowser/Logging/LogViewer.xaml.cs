using System;
using System.Windows;
using System.Windows.Controls;

namespace MiniBrowser.Logging
{
    public sealed partial class LogViewer : UserControl, IDisposable
    {
        private readonly WpfLogViewer logViewer;

        public LogViewer()
        {
            InitializeComponent();

            logViewer = new WpfLogViewer
            {
                Name = "logViewer",
                IsEnabled = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            AddChild(logViewer);
        }

        public void Dispose() => logViewer.Dispose();
    }
}
