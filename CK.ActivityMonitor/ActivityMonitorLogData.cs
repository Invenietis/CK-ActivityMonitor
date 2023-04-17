using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CK.Core
{

    /// <summary>
    /// Data required by <see cref="IActivityLogger.UnfilteredLog"/> and <see cref="IActivityMonitor.UnfilteredOpenGroup"/>.
    /// </summary>
    public struct ActivityMonitorLogData
    {
        /// <summary>
        /// Direct with no check constructor except that if <paramref name="text"/> is null or empty
        /// the text is set to the exception's message or to <see cref="ActivityMonitor.NoLogText"/>.
        /// </summary>
        /// <param name="monitorId">The monitor identifier.</param>
        /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
        /// <param name="finalTags">The final tags (combines the monitors and the line's ones).</param>
        /// <param name="text">The text.</param>
        /// <param name="exception">Optional exception.</param>
        /// <param name="fileName">Optional file name.</param>
        /// <param name="lineNumber">Line number.</param>
        public ActivityMonitorLogData( string monitorId,
                                       LogLevel level,
                                       CKTrait finalTags,
                                       string? text,
                                       Exception? exception,
                                       [CallerFilePath] string? fileName = null,
                                       [CallerLineNumber] int lineNumber = 0 )
        {
            Throw.CheckArgument( monitorId != null && monitorId.Length >= ActivityMonitor.MinMonitorUniqueIdLength );
            Throw.CheckArgument( finalTags != null && finalTags.Context == ActivityMonitor.Tags.Context );
            if( text == null || text.Length == 0 )
            {
                text = exception == null || exception.Message.Length == 0
                        ? ActivityMonitor.NoLogText
                        : exception.Message;
            }
            Text = text;
            _tags = finalTags;
            Exception = exception;
            _exceptionData = null;
            _externalData = null;
            FileName = fileName;
            LineNumber = lineNumber;
            _logTime = default;
            Debug.Assert( (int)LogLevel.NumberOfBits == 7 );
            level &= (LogLevel)0b1111111;
            _monitorId = monitorId;
            Level = level;
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorLogData"/> from an existing <see cref="ActivityMonitorExternalLogData"/>.
        /// </summary>
        /// <param name="data">The source external data.</param>
        /// <param name="resetLogTime">
        /// True to reset the <see cref="LogTime"/>: the initial log time is lost, it can be set by
        /// calling <see cref="SetExplicitLogTime(DateTimeStamp)"/> or will be automatically set when the
        /// data will eventually be sent.
        /// <para>
        /// By default the <see cref="ActivityMonitorExternalLogData.LogTime"/> is used: the initial log time is preserved.
        /// </para>
        /// </param>
        public ActivityMonitorLogData( ActivityMonitorExternalLogData data, bool resetLogTime = false )
        {
            Text = data.Text;
            Level = data.Level;
            _tags = data.Tags;
            _exceptionData = data.ExceptionData;
            Exception = null;
            FileName = data.FileName;
            LineNumber = data.LineNumber;
            _monitorId = data.MonitorId;
            if( resetLogTime )
            {
                _logTime = default;
                _externalData = null;
            }
            else
            {
                _logTime = data.LogTime;
                _externalData = data;
            }
        }

        /// <summary>
        /// Text of the log.
        /// </summary>
        public readonly string Text;

        // This is changed only while replaying logs from the InternalMonitor.
        string _monitorId;

        /// <summary>
        /// Monitor identifier.
        /// </summary>
        public string MonitorId => _monitorId;

        // Tags and LogTime can be changed.
        CKTrait _tags;

        /// <summary>
        /// Tags (from <see cref="ActivityMonitor.Tags"/> context) of the log line combined
        /// with the current <see cref="IActivityMonitor.AutoTags"/>.
        /// </summary>
        public readonly CKTrait Tags => _tags;

        /// <summary>
        /// Internal is required since the InternalMonitor adds its tag when replaying
        /// and the ExternalData may have already been acquired.
        /// </summary>
        /// <param name="tags">Non null tags.</param>
        internal void SetTags( CKTrait tags )
        {
            Debug.Assert( tags != null && tags.Context == ActivityMonitor.Tags.Context );
            _tags = tags;
        }

        /// <summary>
        /// Exception of the log.
        /// Note that this can be null but <see cref="ExceptionData"/> may not be null if this <see cref="ActivityMonitorLogData"/>
        /// has been built from a <see cref="ActivityMonitorExternalLogData"/>.
        /// </summary>
        public readonly Exception? Exception;

        internal CKExceptionData? _exceptionData;

        /// <summary>
        /// Gets the <see cref="CKExceptionData"/> that captures exception information 
        /// if it exists.
        /// If this log data has not been built from a <see cref="ActivityMonitorExternalLogData"/>
        /// and if <see cref="P:Exception"/> is not null, <see cref="CKExceptionData.CreateFrom(Exception)"/>
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
                    Debug.Assert( _externalData == null, "Called before cached data initialization." );
                    _exceptionData = CKExceptionData.CreateFrom( Exception );
                }
                return _exceptionData;
            }
        }

        ActivityMonitorExternalLogData? _externalData;

        /// <summary>
        /// Acquires a cached data from this one (this locks this data, the <see cref="LogTime"/> must be <see cref="DateTimeStamp.IsKnown"/>).
        /// Use <see cref="AcquireExternalData(DateTimeStampProvider,bool)"/> to set the <see cref="LogTime"/>.
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
                Throw.CheckState( "AcquireExternalData must be called once the LogTime is known. " + Environment.NewLine +
                                  "If the external data must be obtained before the call to UnfilteredLog or UnfilteredOpenGroup, then " +
                                  "SetExplicitLogTime must be used to set the LogTime.", _logTime.IsKnown );
                return _externalData = ActivityMonitorExternalLogData.Acquire( ref this );
            }
            e.AddRef();
            return e;
        }

        /// <summary>
        /// Acquires a cached data from this one, ensuring that the <see cref="LogTime"/> is set.
        /// This must be used by <see cref="IActivityLogger.UnfilteredLog(ref ActivityMonitorLogData)"/>
        /// implementations on the target <see cref="IActivityMonitor.SafeStampProvider"/> (that must not be null).
        /// <para>
        /// The <paramref name="sequence"/> is provided by the "emitter" of the log: it guaranties that all log entries
        /// from a logger are stamped with an ever increasing (unique) date.
        /// </para>
        /// <para>
        /// The acquired object MUST be <see cref="ActivityMonitorExternalLogData.Release()"/>.
        /// </para>
        /// </summary>
        /// <param name="sequence">The thread safe <see cref="DateTimeStampProvider"/> to use.</param>
        /// <param name="forceSetLogTime">
        /// Optionally sets the LogTime even if it is already <see cref="DateTimeStamp.IsKnown"/>.
        /// Note that if AcquireExternalData() has already been called, this is ignored (the LogTime is definitely settled).
        /// </param>
        /// <returns>A cached log data for this.</returns>
        public ActivityMonitorExternalLogData AcquireExternalData( DateTimeStampProvider sequence, bool forceSetLogTime = false )
        {
            Throw.CheckNotNullArgument( sequence );
            var e = _externalData;
            if( e == null )
            {
                if( !_logTime.IsKnown || forceSetLogTime )
                {
                    SetLogTime( sequence.GetNextNow() );
                }
                return _externalData = ActivityMonitorExternalLogData.Acquire( ref this );
            }
            e.AddRef();
            return e;
        }

        /// <summary>
        /// Gets whether the <see cref="Text"/> is actually the <see cref="P:Exception"/> message (or <see cref="ExceptionData"/> message).
        /// </summary>
        public readonly bool IsTextTheExceptionMessage => ReferenceEquals( Exception?.Message ?? _exceptionData?.Message, Text );

        /// <summary>
        /// Name of the source file that emitted the log.
        /// </summary>
        public readonly string? FileName;

        /// <summary>
        /// Line number in the source file that emitted the log. 
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Log level. Can not be <see cref="LogLevel.None"/>.
        /// If the log has been successfully filtered, the <see cref="LogLevel.IsFiltered"/> bit flag is set.
        /// </summary>
        public readonly LogLevel Level;

        /// <summary>
        /// The actual level (<see cref="LogLevel.Debug"/> to <see cref="LogLevel.Fatal"/>) associated to this group
        /// without <see cref="LogLevel.IsFiltered"/> bit flag.
        /// </summary>
        public readonly LogLevel MaskedLevel => Level & LogLevel.Mask;

        // Tags and LogTime can be changed.
        DateTimeStamp _logTime;

        /// <summary>
        /// Gets the time of the log.
        /// </summary>
        public readonly DateTimeStamp LogTime => _logTime;

        /// <summary>
        /// Gets whether this data has been handled by a monitor (<see cref="LogTime"/>'s <see cref="DateTimeStamp.IsKnown"/> is false).
        /// </summary>
        public readonly bool IsLogged => _logTime.IsKnown;

        /// <summary>
        /// Gets whether this log data has been successfully filtered (otherwise it is an unfiltered log).
        /// </summary>
        public readonly bool IsFilteredLog => (Level & LogLevel.IsFiltered) != 0;

        /// <summary>
        /// Explicitly sets the <see cref="LogTime"/>.
        /// This should obviously be used with care and cannot be called after <see cref="AcquireExternalData()"/> has been called.
        /// </summary>
        /// <param name="logTime">The time log.</param>
        public void SetExplicitLogTime( DateTimeStamp logTime )
        {
            Throw.CheckState( "Cannot be called once AcquireExternalData has been called.", _externalData == null );
            _logTime = logTime;
        }

        /// <summary>
        /// Explicitly sets the <see cref="Tags"/>.
        /// This should obviously be used with care and cannot be called after <see cref="AcquireExternalData()"/> has been called.
        /// </summary>
        /// <param name="tags">The tags.</param>
        public void SetExplicitTags( CKTrait tags )
        {
            Throw.CheckArgument( tags != null && tags.Context == ActivityMonitor.Tags.Context );
            Throw.CheckState( "Cannot be called once AcquireExternalData has been called.", _externalData == null );
            SetTags( tags  );
        }

        internal DateTimeStamp SetLogTime( DateTimeStamp logTime )
        {
            // This is called by ActivityMonitor.DoUnfilteredLog and ActivityMonitor.DoOpenGroup.
            // When an _externalData is acquired (or has initialized this struct), then the LogTime is known
            // and DoUnfilteredLog or DoOpenGroup don't call this.
            Debug.Assert( _externalData == null, "No external data must have been acquired." );
            return _logTime = logTime;
        }

        internal void SetMonitorId( string uniqueId )
        {
            // Only called when replaying logs from the InternalMonitor.
            _monitorId = uniqueId;
        }
    }
}
