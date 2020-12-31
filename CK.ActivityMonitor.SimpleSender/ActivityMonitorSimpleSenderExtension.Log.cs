using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public static partial class ActivityMonitorSimpleSenderExtension
    {
        /// <summary>
        /// Emits a <see cref="LogLevel"/> exception if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Exception? ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, ActivityMonitor.Tags.Empty, null, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, string? text, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, null, ActivityMonitor.Tags.Empty, text, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception if it must be emitted 
        /// (this depends on <see cref="IActivityMonitor.ActualFilter"/>, the global default <see cref="ActivityMonitor.DefaultFilter"/> 
        /// and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, string? text, Exception? ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, ActivityMonitor.Tags.Empty, text, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Generates and emits a <see cref="LogLevel"/> text message if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Func<string?>? text, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, null, ActivityMonitor.Tags.Empty, text?.Invoke(), @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Generates and emits a <see cref="LogLevel"/> text message associated to an exception if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Func<string?>? text, Exception? ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, ActivityMonitor.Tags.Empty, text?.Invoke(), @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        #region Log with tags.

        /// <summary>
        /// Emits a <see cref="LogLevel"/> with an exception and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Exception? ex, CKTrait? tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, tags, null, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, string? text, CKTrait? tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, null, tags, text, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception and tags if it must be emitted 
        /// (this depends on <see cref="IActivityMonitor.ActualFilter"/>, the global default <see cref="ActivityMonitor.DefaultFilter"/> 
        /// and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, string? text, Exception? ex, CKTrait? tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if(@this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, tags, text, @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Generates and emits a <see cref="LogLevel"/> text message and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Func<string?>? text, CKTrait? tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if(@this.ShouldLogLine(level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, null, tags, text?.Invoke(), @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        /// <summary>
        /// Generates and emits a <see cref="LogLevel"/> text message associated to an exception and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="fileName"/> and <paramref name="lineNumber"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">A function (that will be called only if required) that returns the text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        public static void Log( this IActivityMonitor @this, LogLevel level, Func<string?>? text, Exception? ex, CKTrait? tags, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( @this.ShouldLogLine( level, fileName, lineNumber ) )
            {
                @this.UnfilteredLog( new ActivityMonitorLogData( level | LogLevel.IsFiltered, ex, tags, text?.Invoke(), @this.NextLogTime(), fileName, lineNumber ) );
            }
        }

        #endregion

    }
}
