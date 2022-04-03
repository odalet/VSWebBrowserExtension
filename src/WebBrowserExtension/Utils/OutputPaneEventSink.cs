using System;
using System.IO;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace WebBrowserExtension.Utils
{
    /// <summary>
    /// A serilog sink that writes logs to the VS output window in a dedicated section.
    /// </summary>
    internal sealed class OutputPaneEventSink : ILogEventSink
    {
        private static readonly Guid paneGuid = new Guid("8851EA3E-6283-4C9A-B31B-97D26037E6D3");
        private readonly IVsOutputWindowPane pane;
        private readonly ITextFormatter formatter;

        public OutputPaneEventSink(IVsOutputWindow outputWindow, string outputTemplate)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            formatter = new MessageTemplateTextFormatter(outputTemplate, null);
            _ = ErrorHandler.ThrowOnFailure(outputWindow.CreatePane(paneGuid, "Web Browser Extension", 1, 1));
            _ = outputWindow.GetPane(paneGuid, out pane);
        }

        public void Emit(LogEvent logEvent)
        {
            var sw = new StringWriter();
            formatter.Format(logEvent, sw);
            var message = sw.ToString();

            ThreadHelper.ThrowIfNotOnUIThread();

            if (pane is IVsOutputWindowPaneNoPump noPump)
                noPump.OutputStringNoPump(message);
            else
                _ = ErrorHandler.ThrowOnFailure(pane.OutputStringThreadSafe(message));

            if (logEvent.Level == LogEventLevel.Error)
                _ = pane.Activate();
        }
    }
}
