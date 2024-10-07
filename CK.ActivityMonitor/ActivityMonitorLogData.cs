using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CK.Core;


/// <summary>
/// Data required by <see cref="IActivityLineEmitter.UnfilteredLog"/> and <see cref="IActivityMonitor.UnfilteredOpenGroup"/>.
/// </summary>
public struct ActivityMonitorLogData
{
    string _text;
    readonly Exception? _exception;
    CKExceptionData? _exceptionData;
    ActivityMonitorExternalLogData? _externalData;
    readonly string? _fileName;
    // Tags, MonitorId and Depth are changed only while replaying logs from the InternalMonitor.
    string _monitorId;
    CKTrait _tags;
    int _depth;
    readonly int _lineNumber;
    readonly LogLevel _level;
    DateTimeStamp _logTime;
    /// <summary>
    /// bit 0: isParallel.
    /// bit 1: isOpenGroup.
    /// bit 2: isFrozen
    /// </summary>
    byte _flags;

    /// <summary>
    /// Direct with no check constructor except that if <paramref name="text"/> is null or empty
    /// the text is set to the exception's message or to <see cref="ActivityMonitor.NoLogText"/>.
    /// If <paramref name="exception"/> is not null but not an Exception or a CKExceptionData an ArgumentException is thrown.
    /// </summary>
    /// <param name="monitorId">The monitor identifier.</param>
    /// <param name="logTime">The log time.</param>
    /// <param name="depth">The log depth.</param>
    /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
    /// <param name="isParallel">Whether the log is a parallel or emitted from a monitor.</param>
    /// <param name="isOpenGroup">Whether the log is opens a group.</param>
    /// <param name="finalTags">The final tags (combines the monitors and the line's ones).</param>
    /// <param name="text">The text.</param>
    /// <param name="exception">Optional exception.</param>
    /// <param name="fileName">Optional file name.</param>
    /// <param name="lineNumber">Line number.</param>
    internal ActivityMonitorLogData( string monitorId,
                                     DateTimeStamp logTime,
                                     int depth,
                                     bool isParallel,
                                     bool isOpenGroup,
                                     LogLevel level,
                                     CKTrait finalTags,
                                     string? text,
                                     object? exception,
                                     [CallerFilePath] string? fileName = null,
                                     [CallerLineNumber] int lineNumber = 0 )
    {
        Throw.DebugAssert( monitorId.Length >= ActivityMonitor.MinMonitorUniqueIdLength && !monitorId.Any( c => char.IsWhiteSpace( c ) ) );
        Throw.DebugAssert( finalTags != null && finalTags.Context == ActivityMonitor.Tags.Context );
        Throw.DebugAssert( exception is null || exception is Exception || exception is CKExceptionData );

        _exception = exception as Exception;
        _exceptionData = exception as CKExceptionData;
        if( text == null || text.Length == 0 )
        {
            text = _exception == null || _exception.Message.Length == 0
                    ? ActivityMonitor.NoLogText
                    : _exception.Message;
        }
        if( exception != null && _exception == null && _exceptionData == null )
        {
            Throw.ArgumentException( nameof( exception ), $"Invalid type '{exception.GetType().ToCSharpName()}'." );
        }
        _text = text;
        _tags = finalTags;
        _externalData = null;
        _logTime = logTime;
        _fileName = fileName;
        _depth = depth;
        _flags = (byte)(isParallel ? 1 : 0);
        _flags |= (byte)(isOpenGroup ? 2 : 0);
        _lineNumber = lineNumber;
        Throw.DebugAssert( (int)LogLevel.NumberOfBits == 7 );
        _level = level & (LogLevel)0b1111111;
        _monitorId = monitorId;
        _level = level;
    }

    /// <summary>
    /// Initializes a new <see cref="ActivityMonitorLogData"/> from an existing <see cref="ActivityMonitorExternalLogData"/>.
    /// </summary>
    /// <param name="data">The source external data.</param>
    public ActivityMonitorLogData( ActivityMonitorExternalLogData data )
    {
        _text = data.Text;
        _tags = data.Tags;
        _exceptionData = data.ExceptionData;
        _exception = null;
        _fileName = data.FileName;
        _lineNumber = data.LineNumber;
        _depth = data.Depth;
        _monitorId = data.MonitorId;
        _logTime = data.LogTime;
        _externalData = data;
        _level = data.Level;
        _flags = (byte)(data.IsParallel ? 1 : 0);
        _flags |= (byte)(data.IsOpenGroup ? 2 : 0);
        _flags |= 4;
    }

    /// <summary>
    /// Gets the text of the log.
    /// </summary>
    public readonly string Text => _text;

    /// <summary>
    /// Gets the monitor identifier.
    /// </summary>
    public readonly string MonitorId => _monitorId;

    /// <summary>
    /// Gets this log depth. 0 for root logs.
    /// </summary>
    public readonly int Depth => _depth;

    /// <summary>
    /// Tags (from <see cref="ActivityMonitor.Tags"/> context) of the log line combined
    /// with the current <see cref="IActivityMonitor.AutoTags"/>.
    /// </summary>
    public readonly CKTrait Tags => _tags;

    /// <summary>
    /// Gets the exception of the log.
    /// Note that this can be null but <see cref="ExceptionData"/> may not be null if this <see cref="ActivityMonitorLogData"/>
    /// has been built from a <see cref="ActivityMonitorExternalLogData"/>.
    /// </summary>
    public readonly Exception? Exception => _exception;

    /// <summary>
    /// Gets the <see cref="CKExceptionData"/> that captures exception information 
    /// if it exists.
    /// If this log data has not been built from a <see cref="ActivityMonitorExternalLogData"/>
    /// and if <see cref="Exception"/> is not null, <see cref="CKExceptionData.CreateFrom(Exception)"/>
    /// is automatically called.
    /// <para>
    /// If this log data comes from a <see cref="ActivityMonitorExternalLogData"/>, it may have
    /// this data but <see cref="Exception"/> is always null.
    /// </para>
    /// </summary>
    public CKExceptionData? ExceptionData
    {
        get
        {
            if( _exceptionData == null && Exception != null )
            {
                Throw.DebugAssert( _externalData == null, "Called before cached data initialization." );
                _exceptionData = CKExceptionData.CreateFrom( Exception );
            }
            return _exceptionData;
        }
    }


    /// <summary>
    /// Acquires a cached data.
    /// <para>
    /// The acquired object MUST be <see cref="ActivityMonitorExternalLogData.Release()"/>.
    /// </para>
    /// </summary>
    /// <returns>A cached log data for this.</returns>
    public ActivityMonitorExternalLogData AcquireExternalData()
    {
        var e = _externalData;
        if( e == null )
        {
            Freeze();
            return _externalData = ActivityMonitorExternalLogData.Acquire( ref this );
        }
        e.AddRef();
        return e;
    }

    /// <summary>
    /// Creates a non pooled cached data: calling <see cref="ActivityMonitorExternalLogData.Release()"/> is useless.
    /// </summary>
    /// <returns>An independent cached log data for this.</returns>
    public ActivityMonitorExternalLogData CreateNonPooledData()
    {
        return ActivityMonitorExternalLogData.CreateNonPooled( ref this );
    }

    /// <summary>
    /// Gets whether the <see cref="Text"/> is actually the <see cref="P:Exception"/> message (or <see cref="ExceptionData"/> message).
    /// </summary>
    public readonly bool IsTextTheExceptionMessage => ReferenceEquals( _exception?.Message ?? _exceptionData?.Message, _text );

    /// <summary>
    /// Gets the name of the source file that emitted the log.
    /// </summary>
    public readonly string? FileName => _fileName;

    /// <summary>
    /// Gets the line number in the source file that emitted the log. 
    /// </summary>
    public readonly int LineNumber => _lineNumber;

    /// <summary>
    /// Log level. Can not be <see cref="LogLevel.None"/>.
    /// If the log has been successfully filtered, the <see cref="LogLevel.IsFiltered"/> bit flag is set.
    /// </summary>
    public readonly LogLevel Level => _level;

    /// <summary>
    /// The actual level (<see cref="LogLevel.Debug"/> to <see cref="LogLevel.Fatal"/>) associated to this group
    /// without <see cref="LogLevel.IsFiltered"/> bit flag.
    /// </summary>
    public readonly LogLevel MaskedLevel => Level & LogLevel.Mask;

    /// <summary>
    /// Gets the time of the log.
    /// </summary>
    public readonly DateTimeStamp LogTime => _logTime;

    /// <summary>
    /// Gets whether this log data has been successfully filtered (otherwise it is an unfiltered log).
    /// </summary>
    public readonly bool IsFilteredLog => (Level & LogLevel.IsFiltered) != 0;

    /// <summary>
    /// Gets whether this is a log line emitted by a <see cref="IStaticLogger"/> or a <see cref="IParallelLogger"/>.
    /// </summary>
    public readonly bool IsParallel => (_flags & 1) != 0;

    /// <summary>
    /// Gets whether this lines opens a group.
    /// </summary>
    public readonly bool IsOpenGroup => (_flags & 2) != 0;

    /// <summary>
    /// Gets whether this data is locked.
    /// </summary>
    public readonly bool IsFrozen => (_flags & 4) != 0;

    /// <summary>
    /// Gets the <see cref="ActivityMonitor.LogKey.ToString()"/> for this data: "{MonitorId}.{LogTime}".
    /// <para>
    /// This avoids a useless instantiation of a <see cref="ActivityMonitor.LogKey"/>.
    /// </para>
    /// </summary>
    public readonly string GetLogKeyString() => $"{_monitorId}.{_logTime}";

    /// <summary>
    /// Freezes this data.
    /// </summary>
    public void Freeze() => _flags |= 4;

    /// <summary>
    /// Explicitly sets the <see cref="LogTime"/>.
    /// This should obviously be used with care and cannot be called if <see cref="IsFrozen"/> is true.
    /// </summary>
    /// <param name="logTime">The time log.</param>
    public void SetLogTime( DateTimeStamp logTime )
    {
        Throw.CheckState( !IsFrozen );
        _logTime = logTime;
    }

    /// <summary>
    /// Resets the <see cref="Text"/>.
    /// This should obviously be used with care and cannot be called if <see cref="IsFrozen"/> is true.
    /// </summary>
    /// <param name="text">The text.</param>
    public void SetText( string text )
    {
        Throw.CheckNotNullOrEmptyArgument( text );
        Throw.CheckState( !IsFrozen );
        _text = text;
    }

    /// <summary>
    /// Explicitly sets the <see cref="Tags"/>.
    /// This should obviously be used with care and cannot be called if <see cref="IsFrozen"/> is true.
    /// </summary>
    /// <param name="tags">The tags.</param>
    public void SetTags( CKTrait tags )
    {
        Throw.CheckArgument( tags != null && tags.Context == ActivityMonitor.Tags.Context );
        Throw.CheckState( !IsFrozen );
        _tags = tags;
    }

    internal void MutateForReplay( int depth )
    {
        _tags |= ActivityMonitor.Tags.InternalMonitor;
        _depth = depth;
    }

}
