using System;

namespace CK.Core
{
    /// <summary>
    /// Five standard log levels in increasing order used by <see cref="IActivityMonitor"/>.
    /// </summary>
    [Flags]
    public enum LogLevel
    {
        /// <summary>
        /// No logging level.
        /// </summary>
        None = 0,

        /// <summary>
        /// Debug logging level (the most verbose level).
        /// </summary>
        Debug = 1,

        /// <summary>
        /// A trace logging level (quite verbose level).
        /// </summary>
        Trace = 2,

        /// <summary>
        /// An info logging level.
        /// </summary>
        Info = 4,

        /// <summary>
        /// A warn logging level.
        /// </summary>
        Warn = 8,

        /// <summary>
        /// An error logging level: denotes an error for the current activity. 
        /// This error does not necessarily abort the activity.
        /// </summary>
        Error = 16,

        /// <summary>
        /// A fatal error logging level: denotes an error that breaks (aborts)
        /// the current activity. This kind of error may have important side effects
        /// on the system.
        /// </summary>
        Fatal = 32,

        /// <summary>
        /// Mask that covers actual levels to easily ignore <see cref="IsFiltered"/> bit.
        /// </summary>
        Mask = 63,

        /// <summary>
        /// Flag that denotes a log level that has been filtered.
        /// When this flag is not set, the <see cref="IActivityMonitor.UnfilteredOpenGroup"/> or <see cref="IActivityLogger.UnfilteredLog"/> has been 
        /// called directly. When set, the log has typically been emitted through the extension methods that challenge the 
        /// monitor's <see cref="IActivityMonitor.ActualFilter">actual filter</see> and <see cref="ActivityMonitor.DefaultFilter"/> static configuration
        /// and/or the <see cref="ActivityMonitor.Tags"/>.
        /// </summary>
        IsFiltered = 64,

        /// <summary>
        /// Number of bits actually covered by this bit flag.
        /// </summary>
        NumberOfBits = 7
    }

    /// <summary>
    /// Extends <see cref="LogFilter"/>.
    /// </summary>
    public static class LogLevelExtensions
    {
        /// <summary>
        /// Returns 'F', 'E', 'W', 'i', 't' or 'd'. 
        /// </summary>
        /// <param name="l">This log level.</param>
        /// <returns>The char.</returns>
        public static char ToChar( this LogLevel l ) => (l & LogLevel.Mask) switch
                                                        {
                                                            LogLevel.Fatal => 'F',
                                                            LogLevel.Error => 'E',
                                                            LogLevel.Warn => 'W',
                                                            LogLevel.Info => 'i',
                                                            LogLevel.Trace => 't',
                                                            _ => 'd',
                                                        };
    }
}
