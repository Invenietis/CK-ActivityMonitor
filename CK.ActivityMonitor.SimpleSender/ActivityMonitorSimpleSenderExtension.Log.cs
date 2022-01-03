using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    public static partial class ActivityMonitorSimpleSenderExtension
    {
        /// <summary>
        /// Emits a <see cref="LogLevel"/> exception if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor, LogLevel level, Exception ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( monitor.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor, LogLevel level, string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? fileName = null )
        {
            if( monitor.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber );
                monitor.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityMonitor,LogLevel,string,int,string?)"/>
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                [InterpolatedStringHandlerArgument( "monitor", "level" )] LogHandler.LineLog text,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }


        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                string? text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if( monitor.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref d );
                return true;
            }
            return false;   
        }

        /// <inheritdoc cref="Log(IActivityMonitor,LogLevel,string,Exception,int,string?)"/>
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                [InterpolatedStringHandlerArgument( "monitor", "level" )] LogHandler.LineLog text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        #region Log with tags.

        /// <summary>
        /// Emits a <see cref="LogLevel"/> with an exception and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                CKTrait tags,
                                Exception ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if( monitor.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor, LogLevel level, CKTrait tags, string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? fileName = null )
        {
            if( monitor.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityMonitor,LogLevel,CKTrait,string,int,string?)"/>
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                CKTrait tags,
                                [InterpolatedStringHandlerArgument( "monitor", "level", "tags" )] LogHandler.LineLogWithTags text,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception and tags if it must be emitted (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityMonitor.AutoTags"/>).
        /// </summary>
        /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                CKTrait tags,
                                string? text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if(monitor.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityMonitor,LogLevel,CKTrait,string,Exception,int,string?)"/>
        public static bool Log( this IActivityMonitor monitor,
                                LogLevel level,
                                CKTrait tags,
                                [InterpolatedStringHandlerArgument( "monitor", "level", "tags" )] LogHandler.LineLogWithTags text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber );
                monitor.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }
        #endregion

    }
}
