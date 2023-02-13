using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
        /// <param name="level">The log level that may be flagged with <see cref="LogLevel.IsFiltered"/> or not.</param>
        /// <param name="finalTags">The final tags (combines the monitors and the line's ones).</param>
        /// <param name="text">The text.</param>
        /// <param name="exception">Optional exception.</param>
        /// <param name="fileName">Optional file name.</param>
        /// <param name="lineNumber">Line number.</param>
        public ActivityMonitorLogData( LogLevel level,
                                       CKTrait finalTags,
                                       string? text,
                                       Exception? exception,
                                       [CallerFilePath] string? fileName = null,
                                       [CallerLineNumber] int lineNumber = 0 )
        {
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
            Level = level;
        }

        /// <summary>
        /// Text of the log.
        /// </summary>
        public readonly string Text;

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
        /// <param name="tags"></param>
        internal void SetTags( CKTrait tags )
        {
            _tags = tags;
        }

        /// <summary>
        /// Exception of the log.
        /// </summary>
        public readonly Exception? Exception;

        internal CKExceptionData? _exceptionData;

        /// <summary>
        /// Gets the <see cref="CKExceptionData"/> that captures exception information 
        /// if it exists.
        /// If this log data has not been built on CKExceptionData and if <see cref="P:Exception"/>
        /// is not null, <see cref="CKExceptionData.CreateFrom(Exception)"/> is automatically called.
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
        /// Acquires a cached data from this one (this locks this data).
        /// The acquired object MUST be <see cref="ActivityMonitorExternalLogData.Release()"/>.
        /// </summary>
        /// <returns>A cached log data for this.</returns>
        public ActivityMonitorExternalLogData AcquireExternalData()
        {
            var e = _externalData;
            if( e == null ) return ActivityMonitorExternalLogData.Acquire( ref this );
            e.AddRef();
            return e;
        }

        /// <summary>
        /// Gets whether the <see cref="Text"/> is actually the <see cref="P:Exception"/> message.
        /// </summary>
        public readonly bool IsTextTheExceptionMessage => Exception != null && ReferenceEquals( Exception.Message, Text );

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
        /// This should obviously be used with care and cannot be called after <see cref="AcquireExternalData"/> has been called.
        /// </summary>
        /// <param name="logTime">The time log.</param>
        public void SetExplicitLogTime( DateTimeStamp logTime )
        {
            Throw.CheckState( "Cannot be called once AcquireExternalData has been called.", _externalData == null );
            _logTime = logTime;
        }

        /// <summary>
        /// Explicitly sets the <see cref="Tags"/>.
        /// This should obviously be used with care and cannot be called after <see cref="AcquireExternalData"/> has been called.
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
            Debug.Assert( _exceptionData == null, "No external data must have been acquired." );
            return _logTime = logTime;
        }
    }
}
