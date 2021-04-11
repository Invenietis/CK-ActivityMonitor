#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\ActivityMonitorLogData.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2015, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Data required by <see cref="IActivityMonitor.UnfilteredLog"/>.
    /// This is also the base class for <see cref="ActivityMonitorGroupData"/>.
    /// </summary>
    public class ActivityMonitorLogData
    {
        string? _text;
        CKTrait? _tags;
        DateTimeStamp _logTime;
        Exception? _exception;
        CKExceptionData? _exceptionData;

        /// <summary>
        /// Log level. Can not be <see cref="LogLevel.None"/>.
        /// If the log has been successfully filtered, the <see cref="LogLevel.IsFiltered"/> bit flag is set.
        /// </summary>
        public readonly LogLevel Level;

        /// <summary>
        /// The actual level (<see cref="LogLevel.Debug"/> to <see cref="LogLevel.Fatal"/>) associated to this group
        /// without <see cref="LogLevel.IsFiltered"/> bit flag.
        /// </summary>
        public readonly LogLevel MaskedLevel;

        /// <summary>
        /// Name of the source file that emitted the log. Cannot be null.
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// Line number in the source file that emitted the log. Can be null.
        /// </summary>
        public readonly int LineNumber;

        /// <summary>
        /// Gets whether this log data has been successfully filtered (otherwise it is an unfiltered log).
        /// </summary>
        public bool IsFilteredLog => (Level & LogLevel.IsFiltered) != 0;

        /// <summary>
        /// Tags (from <see cref="ActivityMonitor.Tags"/>) associated to the log. 
        /// It will be union-ed with the current <see cref="IActivityMonitor.AutoTags"/>.
        /// </summary>
        public CKTrait Tags
        {
            get
            {
                if( _tags == null ) ActivityMonitor.ThrowOnGroupOrDataNotInitialized();
                return _tags;
            }
        }

        /// <summary>
        /// Text of the log. Can not be null.
        /// </summary>
        public string Text
        {
            get
            {
                if( _text == null ) ActivityMonitor.ThrowOnGroupOrDataNotInitialized();
                return _text;
            }
        }

        /// <summary>
        /// Gets the time of the log.
        /// </summary>
        public DateTimeStamp LogTime => _logTime;

        /// <summary>
        /// Exception of the log. Can be null.
        /// </summary>
        public Exception? Exception => _exception;

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
                if( _exceptionData == null && _exception != null )
                {
                    _exceptionData = CKExceptionData.CreateFrom( _exception );
                }
                return _exceptionData;
            }
        }

        /// <summary>
        /// Gets whether the <see cref="Text"/> is actually the <see cref="P:Exception"/> message.
        /// </summary>
        public bool IsTextTheExceptionMessage => _exception != null && ReferenceEquals( _exception.Message, _text );

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorLogData"/>.
        /// </summary>
        /// <param name="level">Log level. Can not be <see cref="LogLevel.None"/>.</param>
        /// <param name="exception">Exception of the log. Can be null.</param>
        /// <param name="tags">Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. It will be union-ed with the current <see cref="IActivityMonitor.AutoTags"/>.</param>
        /// <param name="text">Text of the log. Can be null or empty only if <paramref name="exception"/> is not null: the <see cref="T:Exception.Message"/> is the text.</param>
        /// <param name="logTime">
        /// Time of the log. 
        /// You can use <see cref="DateTimeStamp.UtcNow"/> or <see cref="ActivityMonitorExtension.NextLogTime">IActivityMonitor.NextLogTime()</see> extension method.
        /// </param>
        /// <param name="fileName">Name of the source file that emitted the log. Can be null.</param>
        /// <param name="lineNumber">Line number in the source file that emitted the log.</param>
        public ActivityMonitorLogData( LogLevel level, Exception? exception, CKTrait? tags, string? text, DateTimeStamp logTime, [CallerFilePath] string fileName = null, [CallerLineNumber] int lineNumber = 0 )
            : this( level, fileName, lineNumber )
        {
            if( MaskedLevel == LogLevel.None || MaskedLevel == LogLevel.Mask )
            {
                ThrowInvalidLogLevel();
            }
            Initialize( text, exception, tags, logTime );
        }

        [DoesNotReturn]
        static void ThrowInvalidLogLevel()
        {
            throw new ArgumentException( Impl.ActivityMonitorResources.ActivityMonitorInvalidLogLevel, "level" );
        }

        /// <summary>
        /// Preinitializes a new <see cref="ActivityMonitorLogData"/>: <see cref="Initialize"/> has yet to be called.
        /// </summary>
        /// <param name="level">Log level. Can be <see cref="LogLevel.None"/> (the log will be ignored).</param>
        /// <param name="fileName">Name of the source file that emitted the log. Cannot be null.</param>
        /// <param name="lineNumber">Line number in the source file that emitted the log.</param>
        public ActivityMonitorLogData( LogLevel level, string fileName, int lineNumber )
        {
            Level = level;
            MaskedLevel = level & LogLevel.Mask;
            FileName = fileName;
            LineNumber = lineNumber;
        }

        /// <summary>
        /// Used only to initialize a ActivityMonitorGroupSender for rejected opened group.
        /// </summary>
        internal ActivityMonitorLogData()
        {
            Debug.Assert( Level == LogLevel.None );
            FileName = String.Empty;
        }

        /// <summary>
        /// Initializes this data.
        /// </summary>
        /// <param name="text">
        /// Text of the log. Can be null or empty: if <paramref name="exception"/> is not null, 
        /// the <see cref="Exception.Message"/> becomes the text otherwise <see cref="ActivityMonitor.NoLogText"/> is used.
        /// </param>
        /// <param name="exception">Exception of the log. Can be null.</param>
        /// <param name="tags">
        /// Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. 
        /// It will be union-ed with the current <see cref="IActivityMonitor.AutoTags"/>.</param>
        /// <param name="logTime">
        /// Time of the log. 
        /// You can use <see cref="DateTimeStamp.UtcNow"/> or <see cref="ActivityMonitorExtension.NextLogTime">IActivityMonitor.NextLogTime()</see> extension method.
        /// </param>
        public void Initialize( string? text, Exception? exception, CKTrait? tags, DateTimeStamp logTime )
        {
            if( string.IsNullOrEmpty( _text = text ) )
            {
                _text = exception == null || exception.Message.Length == 0
                        ? ActivityMonitor.NoLogText
                        : exception.Message;
            }
            _exception = exception;
            _tags = tags ?? ActivityMonitor.Tags.Empty;
            _logTime = logTime;
        }

        internal void CombineTags( CKTrait tags )
        {
            if( Tags.IsEmpty ) _tags = tags;
            else _tags = Tags.Union( tags );
        }

        internal DateTimeStamp CombineTagsAndAdjustLogTime( CKTrait tags, DateTimeStamp lastLogTime )
        {
            if( Tags.IsEmpty ) _tags = tags;
            else _tags = Tags.Union( tags );
            return _logTime = new DateTimeStamp( lastLogTime, _logTime.IsKnown ? _logTime : DateTimeStamp.UtcNow );
        }
    }
}
