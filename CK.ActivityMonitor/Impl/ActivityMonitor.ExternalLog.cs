using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Exposes static methods that immediately relay log data to <see cref="OnExternalLog"/> event.
        /// <para>
        /// Nothing is done with these logs at this level: this is to be used by client code of this library, typically CK.Monitoring.
        /// </para>
        /// <para>
        /// This is to be used rarely: only if there's really no way to bind the calling context to a real <see cref="IActivityMonitor"/>.
        /// Handlers of the OnExternalLog event should use the <see cref="ExternalLogMonitorUniqueId"/> as the monitor identifier.
        /// </para>
        /// </summary>
        public class ExternalLog
        {
            /// <summary>
            /// The handler signature of external logs.
            /// <para>
            /// The "by ref" argument here is to avoid any copy of the data. The data should not be altered by calling one of
            /// the 2 mutating methods <see cref="ActivityMonitorLogData.SetExplicitLogTime(DateTimeStamp)"/> or <see cref="ActivityMonitorLogData.SetExplicitTags(CKTrait)"/>
            /// unless you absolutely know what you are doing.
            /// </para>
            /// </summary>
            /// <param name="data"></param>
            public delegate void Handler( ref ActivityMonitorLogData data );

            /// <summary>
            /// Raised when Send is called.
            /// <para>
            /// Such events should be handled very quickly (typically by queuing a projection of the
            /// data or the data itself in a concurrent queue or a channel).
            /// </para>
            /// <para>
            /// This is a static event: the callbacks registered here will be referenced until removed: care should be
            /// taken to unregister callbacks otherwise referenced objects will never be garbage collected.
            /// </para>
            /// </summary>
            static public event Handler? OnExternalLog;

            /// <summary>
            /// Calls <see cref="Log(LogLevel,string,Exception,string,int)"/> for this level.
            /// Log will be filtered only by <see cref="DefaultFilter"/>'s Line level filter.
            /// </summary>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Debug( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Debug, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Trace( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Trace, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Info( string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Info, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Warn( string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Warn, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Error( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Error, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Fatal( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Fatal, text, ex, fileName, lineNumber );

            /// <summary>
            /// Calls <see cref="Log(LogLevel,CKTrait,string,Exception,string,int)"/> for this level.
            /// Log will be filtered by <see cref="DefaultFilter"/>'s Line level filter and current <see cref="Tags.Filters"/>.
            /// </summary>
            /// <param name="tags">The log tags.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Debug( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Debug, tags, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Trace( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Trace, tags, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Info( CKTrait tags,
                                     string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Info, tags, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Warn( CKTrait tags,
                                     string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Warn, tags, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Error( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Error, tags, text, ex, fileName, lineNumber );
            /// <inheritdoc cref="Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Fatal( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Fatal, tags, text, ex, fileName, lineNumber );

            /// <summary>
            /// Tests <see cref="DefaultFilter"/>.<see cref="LogFilter.Line">Line</see> against <paramref name="level"/>
            /// and calls <see cref="UnfilteredLog(LogLevel, string, Exception?, string?, int)"/> if the log level is enough.
            /// </summary>
            /// <param name="level">The log level that will be filtered by the static DefaultFilter.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            static public void Log( LogLevel level,
                                    string text,
                                    Exception? ex = null,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 )
            {
                if( ((int)level & (int)LogLevel.Mask) >= (int)DefaultFilter.Line )
                {
                    UnfilteredLog( level | LogLevel.IsFiltered, text, ex, fileName, lineNumber );
                }
            }

            /// <summary>
            /// Filters log by <see cref="DefaultFilter"/>.<see cref="LogFilter.Line">Line</see> against <paramref name="level"/>
            /// and current <see cref="Tags.Filters"/>, then calls <see cref="UnfilteredLog(LogLevel, string, Exception?, string?, int)"/>
            /// if level and tags fit.
            /// </summary>
            /// <param name="level">The log level that will be filtered by the static DefaultFilter.</param>
            /// <param name="tags">The log tags.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static public void Log( LogLevel level,
                                    CKTrait tags,
                                    string text,
                                    Exception? ex = null,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 )
            {
                Throw.CheckNotNullArgument( tags );
                // Using empty tags here would be quite stupid: let a single code path be executed
                // instead of "optimizing" a rare case.
                if( Tags.ApplyForLine( tags, (int)DefaultFilter.Line, (int)level ) )
                {
                    UnfilteredLog( level | LogLevel.IsFiltered, tags, text, ex, fileName, lineNumber );
                }
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with an explicit level and no tags. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// </summary>
            /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> to skip any subsequent filtering.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            static public void UnfilteredLog( LogLevel level,
                                              string text,
                                              Exception? ex = null,
                                              [CallerFilePath] string? fileName = null,
                                              [CallerLineNumber] int lineNumber = 0 )
            {
                var d = new ActivityMonitorLogData( level, Tags.Empty, text, ex, fileName, lineNumber );
                OnExternalLog?.Invoke( ref d );
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with an explicit level and optional tags. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// </summary>
            /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> to skip any subsequent filtering.</param>
            /// <param name="tags">The log tags.</param>
            /// <param name="text">The text of the log.</param>
            /// <param name="ex">Optional associated exception.</param>
            /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
            /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
            static public void UnfilteredLog( LogLevel level,
                                              CKTrait tags,
                                              string text,
                                              Exception? ex = null,
                                              [CallerFilePath] string? fileName = null,
                                              [CallerLineNumber] int lineNumber = 0 )
            {
                var d = new ActivityMonitorLogData( level, tags, text, ex, fileName, lineNumber );
                OnExternalLog?.Invoke( ref d );
            }

            /// <summary>
            /// Raises the <see cref="OnExternalLog"/> event with the logged data. 
            /// <para>
            /// This can be used to log in context-less situations (when no <see cref="IActivityMonitor"/> exists) but
            /// this should be avoided as much as possible.
            /// </para>
            /// <para>
            /// The <see cref="ActivityMonitorLogData.Level"/> can be flagged with <see cref="LogLevel.IsFiltered"/> bit.
            /// If not, this log is considered "unfiltered" and should not be filtered anymore.
            /// </para>
            /// </summary>
            /// <param name="data">Data that describes the log. </param>
            public static void SendData( ref ActivityMonitorLogData data )
            {
                OnExternalLog?.Invoke( ref data );               
            }

        }
    }

}
