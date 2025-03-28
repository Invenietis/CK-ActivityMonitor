using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace CK.Core;

/// <summary>
/// Cached <see cref="ActivityMonitorLogData"/>: these objects are pooled.
/// See <see cref="CurrentPoolCapacity"/> and <see cref="MaximalCapacity"/>.
/// <para>
/// The only way to acquire such cached data is to call <see cref="ActivityMonitorLogData.AcquireExternalData()"/>.
/// </para>
/// </summary>
public sealed partial class ActivityMonitorExternalLogData
{
    [AllowNull] string _text;
    [AllowNull] CKTrait _tags;
    [AllowNull] string _monitorId;
    CKExceptionData? _exceptionData;
    string? _fileName;
    int _lineNumber;
    int _depth;
    int _refCount;
    DateTimeStamp _logTime;
    LogLevel _level;
    byte _flags;

    /// <inheritdoc cref="ActivityMonitorLogData.Text"/>
    public string Text => _text;

    /// <inheritdoc cref="ActivityMonitorLogData.MonitorId"/>
    public string MonitorId => _monitorId;

    /// <inheritdoc cref="ActivityMonitorLogData.Tags"/>
    public CKTrait Tags => _tags;

    /// <summary>
    /// Gets the exception data if there's an exception.
    /// </summary>
    public CKExceptionData? ExceptionData => _exceptionData;

    /// <inheritdoc cref="ActivityMonitorLogData.FileName"/>
    public string? FileName => _fileName;

    /// <inheritdoc cref="ActivityMonitorLogData.LineNumber"/>
    public int LineNumber => _lineNumber;

    /// <inheritdoc cref="ActivityMonitorLogData.Level"/>
    public LogLevel Level => _level;

    /// <inheritdoc cref="ActivityMonitorLogData.Depth"/>
    public int Depth => _depth;

    /// <inheritdoc cref="ActivityMonitorLogData.MaskedLevel"/>
    public LogLevel MaskedLevel => _level & LogLevel.Mask;

    /// <inheritdoc cref="ActivityMonitorLogData.LogTime"/>
    public DateTimeStamp LogTime => _logTime;

    /// <inheritdoc cref="ActivityMonitorLogData.IsParallel"/>
    public bool IsParallel => (_flags & 1) != 0;

    /// <inheritdoc cref="ActivityMonitorLogData.IsOpenGroup"/>
    public bool IsOpenGroup => (_flags & 2) != 0;

    /// <inheritdoc cref="ActivityMonitorLogData.GetLogKeyString"/>
    public string GetLogKeyString() => $"{_monitorId}.{_logTime}";

    // Private constructor.
    ActivityMonitorExternalLogData()
    {
    }

    internal void Initialize( ref ActivityMonitorLogData data, bool usePool )
    {
        _text = data.Text;
        _tags = data.Tags;
        _exceptionData = data.ExceptionData;
        _fileName = data.FileName;
        _lineNumber = data.LineNumber;
        _depth = data.Depth;
        _logTime = data.LogTime;
        _monitorId = data.MonitorId;
        _level = data.Level;
        _flags = (byte)(data.IsParallel ? 1 : 0);
        _flags |= (byte)(data.IsOpenGroup ? 2 : 0);
        _refCount = 1;
        if( usePool ) _flags |= 4;
    }

    /// <summary>
    /// Adds a reference to this cached data. <see cref="Release()"/> must be called once for each call to AddRef.
    /// This has no effect if <see cref="UsePool"/> is false.
    /// </summary>
    public void AddRef() => Interlocked.Increment( ref _refCount );

    /// <summary>
    /// Releases this cached data.
    /// This has no effect if <see cref="UsePool"/> is false.
    /// </summary>
    public void Release()
    {
        if( (_flags & 4) != 0 )
        {
            int refCount = Interlocked.Decrement( ref _refCount );
            if( refCount == 0 )
            {
                _text = null;
                _exceptionData = null;
                _fileName = null;
                _lineNumber = 0;
                Release( this );
                return;
            }
            Throw.CheckState( refCount > 0 );
        }
    }

    /// <summary>
    /// Gets whether this data uses the pool: it MUST be <see cref="Release()"/>.
    /// </summary>
    public bool UsePool => (_flags & 4) != 0;

}
