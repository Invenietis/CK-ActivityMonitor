using System;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Provides OpenXXX and XXX (Debug, Trace, Info,...Fatal) and Log extension methods for <see cref="IActivityMonitor"/>
    /// (and <see cref="IActivityLogger"/>).
    /// </summary>
    public static partial class ActivityMonitorSimpleSenderExtension
    {
        /// <summary>
        /// Emits a <see cref="LogLevel"/> exception if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger, LogLevel level, Exception ex, [CallerLineNumber]int lineNumber = 0, [CallerFilePath]string? fileName = null )
        {
            if( logger.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger, LogLevel level, string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? fileName = null )
        {
            if( logger.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber );
                logger.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityLogger,LogLevel,string,int,string?)"/>
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                [InterpolatedStringHandlerArgument( nameof(logger), nameof(level) )] LogHandler.LineLog text,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }


        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                string? text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if( logger.ShouldLogLine( level, null, out var finalTags ) )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref d );
                return true;
            }
            return false;   
        }

        /// <inheritdoc cref="Log(IActivityLogger,LogLevel,string,Exception,int,string?)"/>
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                [InterpolatedStringHandlerArgument( nameof(logger), nameof(level) )] LogHandler.LineLog text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var d = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref d );
                return true;
            }
            return false;
        }

        #region Log with tags.

        /// <summary>
        /// Emits a <see cref="LogLevel"/> with an exception and tags if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                CKTrait tags,
                                Exception ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if( logger.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message and tags if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger, LogLevel level, CKTrait tags, string text, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? fileName = null )
        {
            if( logger.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityLogger,LogLevel,CKTrait,string,int,string?)"/>
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                CKTrait tags,
                                [InterpolatedStringHandlerArgument( nameof(logger), nameof(level), nameof(tags) )] LogHandler.LineLogWithTags text,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Emits a <see cref="LogLevel"/> text message associated to an exception and tags if it must be emitted (this depends on <see cref="IActivityLogger.ActualFilter"/>, 
        /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and <see cref="IActivityLogger.AutoTags"/>).
        /// </summary>
        /// <param name="logger">This <see cref="IActivityLogger"/>.</param>
        /// <param name="level">The log level.</param>
        /// <param name="tags">The tags for this log.</param>
        /// <param name="text">The text to log.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
        /// <returns>True if the log has been emitted, false otherwise.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                CKTrait tags,
                                string? text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            if(logger.ShouldLogLine( level, tags, out var finalTags ) )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }

        /// <inheritdoc cref="Log(IActivityLogger,LogLevel,CKTrait,string,Exception,int,string?)"/>
        public static bool Log( this IActivityLogger logger,
                                LogLevel level,
                                CKTrait tags,
                                [InterpolatedStringHandlerArgument( nameof(logger), nameof(level), nameof(tags) )] LogHandler.LineLogWithTags text,
                                Exception? ex,
                                [CallerLineNumber] int lineNumber = 0,
                                [CallerFilePath] string? fileName = null )
        {
            var t = text._handler.ToStringAndClear();
            if( t != null )
            {
                var line = new ActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber );
                logger.UnfilteredLog( ref line );
                return true;
            }
            return false;
        }
        #endregion

    }
}
