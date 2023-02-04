using System;
using System.Runtime.CompilerServices;

#pragma warning disable CA1822 // Mark members as static

namespace CK.Core
{
    public sealed partial class StaticGate
    {
        /// <summary>
        /// Relay to the <see cref="ActivityMonitor.StaticLogger"/>. This is a singleton instance: as an reference type this
        /// can be nullable, as a sealed class this wrapper is optimal.
        /// </summary>
        public sealed class Logger
        {
            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait, string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Debug( string text,
                               Exception? ex = null,
                               [CallerFilePath] string? fileName = null,
                               [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Debug( text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Trace( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Trace( text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Info( string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Info( text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Warn( string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Warn( text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Error( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Error( text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Fatal( string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Fatal( text, ex, fileName, lineNumber );


            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Debug( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Debug, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Trace( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Trace, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Info( CKTrait tags,
                                     string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Info, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Warn( CKTrait tags,
                                     string text,
                                     Exception? ex = null,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Warn, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Error( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Error, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Debug(CKTrait,string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Fatal( CKTrait tags,
                                      string text,
                                      Exception? ex = null,
                                      [CallerFilePath] string? fileName = null,
                                      [CallerLineNumber] int lineNumber = 0 ) => Log( LogLevel.Fatal, tags, text, ex, fileName, lineNumber );


            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Log(LogLevel, string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Log( LogLevel level,
                                    string text,
                                    Exception? ex = null,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Log( level, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.Log(LogLevel, CKTrait, string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void Log( LogLevel level,
                                    CKTrait tags,
                                    string text,
                                    Exception? ex = null,
                                    [CallerFilePath] string? fileName = null,
                                    [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.Log( level, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.UnfilteredLog(LogLevel, string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void UnfilteredLog( LogLevel level,
                                              string text,
                                              Exception? ex = null,
                                              [CallerFilePath] string? fileName = null,
                                              [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.UnfilteredLog( level, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.UnfilteredLog(LogLevel, CKTrait, string, Exception?, string?, int)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void UnfilteredLog( LogLevel level,
                                              CKTrait tags,
                                              string text,
                                              Exception? ex = null,
                                              [CallerFilePath] string? fileName = null,
                                              [CallerLineNumber] int lineNumber = 0 ) => ActivityMonitor.StaticLogger.UnfilteredLog(level, tags, text, ex, fileName, lineNumber );

            /// <inheritdoc cref="ActivityMonitor.StaticLogger.SendData(ref ActivityMonitorLogData)"/>
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
            public void SendData( ref ActivityMonitorLogData data ) => ActivityMonitor.StaticLogger.SendData( ref data );
        }

    }

}
