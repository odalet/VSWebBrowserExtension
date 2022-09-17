using System;
using NLog;
using NLog.Targets;

namespace MiniBrowser.Logging
{
    internal sealed class LogEventMemoryTarget : Target
    {
        public event Action<LogEventInfo> EventReceived;

        /// <summary>
        /// Writes logging event to the log target. Must be overridden in inheriting
        /// classes.
        /// </summary>
        /// <param name="logEvent">Logging event to be written out.</param>
        protected override void Write(LogEventInfo logEvent) => EventReceived?.Invoke(logEvent);
    }
}
