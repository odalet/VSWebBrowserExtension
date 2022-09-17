using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Highlighting;
using MiniBrowser.Utils;
using NLog;
using NLog.Config;
using NLog.Layouts;

namespace MiniBrowser.Logging
{
    public sealed partial class WpfLogViewer : UserControl, IDisposable
    {
        private const string formatString = "${time}|${pad:padding=-5:inner=${level:uppercase=true}}|${pad:padding=-20:fixedLength=True:alignmentOnTruncation=right:inner=${logger:shortName=true}}|${message}${onexception:inner=${newline}${exception:format=tostring}}";

        private static readonly ILogger log = LogManager.GetCurrentClassLogger();
        private static readonly SimpleLayout layout = new SimpleLayout(formatString);
        private readonly LogEventMemoryTarget logTarget;
        private readonly LogColorizer colorizer;
        private LogLevel thresholdLogLevel = LogLevel.Trace;

        private const int maxLineDisplayed = 100000;
        private const int nbrOfLineToDeleteWhenLimitIsReached = 50000;

        public WpfLogViewer()
        {
            InitializeComponent();

            debugBar.Visibility = Visibility.Collapsed;

            colorizer = CreateColorizer();
            logBox.TextArea.TextView.LineTransformers.Add(colorizer);

            logTarget = new LogEventMemoryTarget();
            logTarget.EventReceived += info => DispatchLog(info);

            LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, logTarget));
            LogManager.ReconfigExistingLoggers();

            foreach (var level in LogLevel.AllLevels.OrderBy(l => l.Ordinal))
                _ = levelBox.Items.Add(level);

            levelBox.SelectedItem = thresholdLogLevel;
            levelBox.SelectionChanged += (s, _) => thresholdLogLevel = SelectedLogLevel;

            ClearCommand = new RelayCommand(() =>
            {
                if (colorizer != null) colorizer.Clear();
                logBox.Document.Text = string.Empty;
            }, () => !string.IsNullOrEmpty(logBox.Document.Text));

            CopyAllCommand = new RelayCommand(() =>
            {
                try
                {
                    var text = logBox.Document.Text;
                    var data = new DataObject(text);
                    var html = HtmlClipboard.CreateHtmlFragment(
                        logBox.Document, null, null, new HtmlOptions(logBox.Options));
                    HtmlClipboard.SetHtml(data, html);
                    Clipboard.SetDataObject(data, true);
                }
                catch (Exception ex)
                {
                    // There was a problem while writing to the clipboard... let's log it!
                    log.Error(ex);
                }
            }, () => !string.IsNullOrEmpty(logBox.Document.Text));
        }

        public ICommand ClearCommand { get; }
        public ICommand CopyAllCommand { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => logTarget.Dispose();

        /// <summary>
        /// Gets or sets the selected log level.
        /// </summary>
        /// <value>The selected log level.</value>
        public LogLevel SelectedLogLevel => (LogLevel)levelBox.SelectedItem;

        private void DispatchLog(LogEventInfo entry)
        {
            if (!Dispatcher.CheckAccess()) _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action<LogEventInfo>(e => LogEntryToTextBox(e)), entry);
            else LogEntryToTextBox(entry);
        }

        private void LogEntryToTextBox(LogEventInfo entry)
        {
            if (thresholdLogLevel.Ordinal > entry.Level.Ordinal)
                return;

            var start = logBox.Document.TextLength;
            logBox.AppendText(layout.Render(entry));
            logBox.AppendText("\r");
            var end = logBox.Document.TextLength;
            colorizer.AddLogLineInfo(new LogLineInfo
            {
                StartOffset = start,
                EndOffset = end,
                Level = entry.Level
            });

            if (logBox.Document.LineCount > maxLineDisplayed)
            {
                // Delete old lines in textbox
                var line = logBox.Document.GetLineByNumber(nbrOfLineToDeleteWhenLimitIsReached);
                var endOffset = line.EndOffset + line.DelimiterLength;
                logBox.Document.Remove(0, endOffset);

                // Delete old data in colorizer
                if (colorizer != null)
                    colorizer.ClearOldData(nbrOfLineToDeleteWhenLimitIsReached);
            }

            logBox.ScrollToEnd();
        }

        private static LogColorizer CreateColorizer()
        {
            var logLinesStyle = new Dictionary<LogLevel, LogLineStyle>
            {
                [LogLevel.Fatal] = new LogLineStyle { ForegroundBrush = Brushes.Red, FontWeight = FontWeights.Bold },
                [LogLevel.Error] = new LogLineStyle { ForegroundBrush = Brushes.Red },
                [LogLevel.Warn] = new LogLineStyle { ForegroundBrush = Brushes.Orange, FontWeight = FontWeights.Bold },
                [LogLevel.Info] = new LogLineStyle { FontWeight = FontWeights.Bold },
                [LogLevel.Debug] = new LogLineStyle { ForegroundBrush = Brushes.Blue },
                [LogLevel.Trace] = new LogLineStyle()
            };

            return new LogColorizer(ll => logLinesStyle.ContainsKey(ll) ? logLinesStyle[ll] : null);
        }

        // Debug Bar

        private static int counter;
        private void FatalButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Fatal($"Fatal #{counter}");
        }

        private void ErrorButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Error($"Error #{counter}");
        }

        private void WarningButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Warn($"Warning #{counter}");
        }

        private void InfoButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Info($"Info #{counter}");
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Debug($"Debug #{counter}");
        }

        private void TraceButton_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            log.Debug($"Trace #{counter}");
        }
    }
}
