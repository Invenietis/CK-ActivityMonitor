using System;
using System.Runtime.CompilerServices;

namespace CK.Core;

public static partial class ActivityMonitorSimpleSenderExtension
{
    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with an exception. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>),
    /// it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="ex">The exception to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              Exception ex,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, null, out var finalTags )
                                            ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber, true )
                                            : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with a text message. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>),
    /// it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="text">The text to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              string text,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, null, out var finalTags )
                                            ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber, true )
                                            : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <inheritdoc cref="OpenGroup(IActivityMonitor, LogLevel, string, int, string?)"/>
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              [InterpolatedStringHandlerArgument( "monitor", "level" )] LogHandler.GroupLog text,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var t = text._handler.ToStringAndClear();
        var d = t != null
                ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber, true )
                : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }


    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with a text message associated to an exception. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <see cref="IActivityMonitor.AutoTags"/>),
    /// it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="text">The text to log.</param>
    /// <param name="ex">The exception to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              string? text,
                                              Exception? ex,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, null, out var finalTags )
                                            ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, ex, fileName, lineNumber, true )
                                            : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <inheritdoc cref="OpenGroup(IActivityMonitor, LogLevel, string, Exception?, int, string?)"/>
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              [InterpolatedStringHandlerArgument( "monitor", "level" )] LogHandler.GroupLog text,
                                              Exception? ex,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var t = text._handler.ToStringAndClear();
        var d = t != null
                ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber, true )
                : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    #region Debug with tags.

    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with an exception and tags. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and
    /// <see cref="IActivityMonitor.AutoTags"/>), it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="tags">Optional tags for this log.</param>
    /// <param name="ex">The exception to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              CKTrait? tags,
                                              Exception ex,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, tags, out var finalTags )
                                           ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, null, ex, fileName, lineNumber, true )
                                           : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with a text message and tags. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and
    /// <see cref="IActivityMonitor.AutoTags"/>), it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="tags">Optional tags for this log.</param>
    /// <param name="text">The text to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              CKTrait? tags,
                                              string text,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, tags, out var finalTags )
                                           ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, null, fileName, lineNumber, true )
                                           : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <inheritdoc cref="OpenGroup(IActivityMonitor, LogLevel, CKTrait?, string, int, string?)"/>
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              CKTrait? tags,
                                              [InterpolatedStringHandlerArgument( "monitor", "level", "tags" )] LogHandler.GroupLogWithTags text,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var t = text._handler.ToStringAndClear();
        var d = t != null
                ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, null, fileName, lineNumber, true )
                : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <summary>
    /// Opens a given <see cref="LogLevel"/> group with a text message associated to an <see cref="Exception"/> or <see cref="CKExceptionData"/> and tags. 
    /// Regardless of whether it will be emitted or not (this depends on <see cref="IActivityMonitor.ActualFilter"/>, 
    /// the global default <see cref="ActivityMonitor.DefaultFilter"/> and may also depend on <paramref name="tags"/> and
    /// <see cref="IActivityMonitor.AutoTags"/>), it must always be closed.
    /// </summary>
    /// <param name="monitor">This <see cref="IActivityMonitor"/>.</param>
    /// <param name="level">The log level.</param>
    /// <param name="tags">Optional tags for this log.</param>
    /// <param name="text">The text to log.</param>
    /// <param name="error">The <see cref="Exception"/> or <see cref="CKExceptionData"/> to log.</param>
    /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
    /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler).</param>
    /// <returns>A disposable object that can be used to set a function that provides a conclusion text and/or close the group.</returns>
    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              CKTrait? tags,
                                              string? text,
                                              object? error,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var d = monitor.ShouldLogGroup( level, tags, out var finalTags )
                                           ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, finalTags, text, error, fileName, lineNumber, true )
                                           : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    /// <inheritdoc cref="OpenGroup(IActivityMonitor, LogLevel, CKTrait?, Exception, int, string?)"/>
    public static IDisposableGroup OpenGroup( this IActivityMonitor monitor,
                                              LogLevel level,
                                              CKTrait? tags,
                                              [InterpolatedStringHandlerArgument( "monitor", "level", "tags" )] LogHandler.GroupLogWithTags text,
                                              Exception? ex,
                                              [CallerLineNumber] int lineNumber = 0,
                                              [CallerFilePath] string? fileName = null )
    {
        var t = text._handler.ToStringAndClear();
        var d = t != null
                ? monitor.CreateActivityMonitorLogData( level | LogLevel.IsFiltered, text._handler.FinalTags, t, ex, fileName, lineNumber, true )
                : default;
        return monitor.UnfilteredOpenGroup( ref d );
    }

    #endregion
}
