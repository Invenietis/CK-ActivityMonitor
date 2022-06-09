using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Formats the activity and pushes piece of texts to an <see cref="Action{T}"/> where T is a string.
    /// </summary>
    public class ActivityMonitorTextWriterClient : ActivityMonitorTextHelperClient
    {
        readonly string _depthPadding;
        readonly StringBuilder _buffer;
        Action<string> _writer;
        string _prefix;

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorTextWriterClient"/> bound to a 
        /// function that must write a string, with a filter and a character that starts
        /// the "depth padding" that defaults to '|'.
        /// </summary>
        /// <param name="writer">Function that writes the content.</param>
        /// <param name="filter">Filter to apply.</param>
        /// <param name="depthInitial">The character to use in front of the "depth padding".</param>
        public ActivityMonitorTextWriterClient( Action<string> writer, LogClamper filter, char depthInitial = '|' )
            : this( filter, depthInitial )
        {
            Throw.CheckNotNullArgument( writer );
            Writer = writer;
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorTextWriterClient"/> bound to a 
        /// function that must write a string.
        /// </summary>
        /// <param name="writer">Function that writes the content.</param>
        /// <param name="depthInitial">The character to use in front of the "depth padding".</param>
        public ActivityMonitorTextWriterClient( Action<string> writer, char depthInitial = '|' )
            : this( writer, LogClamper.Undefined, depthInitial )
        {
        }

        /// <summary>
        /// Initialize a new <see cref="ActivityMonitorTextWriterClient"/> with a 
        /// <see cref="Writer"/> sets to <see cref="Util.ActionVoid{T}"/>.
        /// Unless explicitly initialized, this will not write anything anywhere.
        /// </summary>
        /// <param name="filter">Filter to apply.</param>
        /// <param name="depthInitial">The character to use in front of the "depth padding".</param>
        public ActivityMonitorTextWriterClient( LogClamper filter, char depthInitial = '|' )
            : base( filter )
        {
            _depthPadding = depthInitial != ' ' ? depthInitial + " " : "  ";
            _buffer = new StringBuilder();
            _prefix = String.Empty;
            _writer = Util.ActionVoid<string>;
        }

        /// <summary>
        /// Initialize a new <see cref="ActivityMonitorTextWriterClient"/> with a 
        /// <see cref="Writer"/> sets to <see cref="Util.ActionVoid{T}"/>.
        /// Unless explicitly initialized, this will not write anything anywhere.
        /// </summary>
        /// <param name="depthInitial">The character to use in front of the "depth padding".</param>
        public ActivityMonitorTextWriterClient( char depthInitial = '|' )
            : this( LogClamper.Undefined, depthInitial )
        {
        }

        /// <summary>
        /// Gets or sets the actual writer function.
        /// When set to null, the empty <see cref="Util.ActionVoid{T}"/> is set.
        /// </summary>
        [AllowNull]
        protected Action<string> Writer { get => _writer; set => _writer = value ?? Util.ActionVoid<string>; }

        /// <summary>
        /// Writes all the information.
        /// </summary>
        /// <param name="data">Log data.</param>
        protected override void OnEnterLevel( ref ActivityMonitorLogData data )
        {
            var w = _buffer.Clear();
            w.Append( _prefix )
                .Append( data.Level.ToChar() )
                .Append( " [" ).Append( data.Tags ).Append( "] " )
                .AppendMultiLine( _prefix + "  ", data.Text, false )
                .AppendLine();
            if( data.Exception != null )
            {
                DumpException( w, _prefix + ' ', !data.IsTextTheExceptionMessage, data.Exception );
            }
            Writer( w.ToString() );
        }

        /// <summary>
        /// Writes all information.
        /// </summary>
        /// <param name="data">Log data.</param>
        protected override void OnContinueOnSameLevel( ref ActivityMonitorLogData data )
        {
            var w = _buffer.Clear();
            w.Append( _prefix ).Append( "  [" ).Append( data.Tags ).Append( "] " )
             .AppendMultiLine( _prefix + "  ", data.Text, false )
             .AppendLine();
            if( data.Exception != null )
            {
                DumpException( w, _prefix + ' ', !data.IsTextTheExceptionMessage, data.Exception );
            }
            Writer( _buffer.ToString() );
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        /// <param name="level">The previous log level (without <see cref="LogLevel.IsFiltered"/>).</param>
        protected override void OnLeaveLevel( LogLevel level )
        {
        }

        /// <summary>
        /// Writes a group opening.
        /// </summary>
        /// <param name="g">Group information.</param>
        protected override void OnGroupOpen( IActivityLogGroup g )
        {
            var w = _buffer.Clear();
            string start = string.Format( "{0}> {1} ", _prefix, g.Data.Level.ToChar() );
            _prefix += _depthPadding;

            w.Append( start )
             .Append( '[' ).Append( g.Data.Tags ).Append( "] " )
             .AppendMultiLine( _prefix + "  ", g.Data.Text, false )
             .AppendLine();
            if( g.Data.Exception != null )
            {
                DumpException( w, _prefix, !g.Data.IsTextTheExceptionMessage, g.Data.Exception );
            }
            Writer( _buffer.ToString() );
        }

        /// <summary>
        /// Writes group conclusion and updates internally managed line prefix.
        /// </summary>
        /// <param name="g">Group that must be closed.</param>
        /// <param name="conclusions">Conclusions for the group.</param>
        protected override void OnGroupClose( IActivityLogGroup g, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
        {
            Debug.Assert( _depthPadding.Length == 2 );
            _prefix = _prefix.Remove( _prefix.Length - 2 );
            if( conclusions is null || conclusions.Count == 0 ) return;
            var w = _buffer.Clear();
            bool one = false;
            List<ActivityLogGroupConclusion>? withMultiLines = null;
            foreach( var c in conclusions )
            {
                if( c.Text.Contains( '\n' ) )
                {
                    if( withMultiLines == null ) withMultiLines = new List<ActivityLogGroupConclusion>();
                    withMultiLines.Add( c );
                }
                else
                {
                    if( !one )
                    {
                        w.Append( _prefix ).Append( "< " );
                        one = true;
                    }
                    else
                    {
                        w.Append( " - " );
                    }
                    w.Append( c.Text );
                }
            }
            if( one ) w.AppendLine();
            if( withMultiLines != null )
            {
                foreach( var c in withMultiLines )
                {
                    w.Append( _prefix ).Append( "- " );
                    w.AppendMultiLine( _prefix + "  ", c.Text, false );
                    w.AppendLine();
                }
            }
            Writer( _buffer.ToString() );
        }

        /// <summary>
        /// Recursively dumps an <see cref="Exception"/> as readable text.
        /// </summary>
        /// <param name="w">The TextWriter to write to.</param>
        /// <param name="prefix">Prefix that will start all lines.</param>
        /// <param name="displayMessage">Whether the exception message must be displayed or skip.</param>
        /// <param name="ex">The exception to display.</param>
        static public void DumpException( StringBuilder w, string prefix, bool displayMessage, Exception ex )
        {
            if( ex is CKException ckEx && ckEx.ExceptionData != null )
            {
                ckEx.ExceptionData.ToStringBuilder( w, prefix );
                return;
            }
            string header = String.Format( " ┌──────────────────────────■ Exception : {0} ■──────────────────────────", ex.GetType().Name );

            string p;
            w.AppendLine( prefix + header );
            string localPrefix = prefix + " | ";
            if( displayMessage && ex.Message != null )
            {
                w.Append( localPrefix + "Message: " );
                w.AppendMultiLine( localPrefix + "         ", ex.Message, false );
                w.AppendLine();
            }
            if( ex.StackTrace != null )
            {
                w.Append( localPrefix + "Stack: " );
                w.AppendMultiLine( localPrefix + "       ", ex.StackTrace, false );
                w.AppendLine();
            }
            if( ex is System.IO.FileNotFoundException fileNFEx )
            {
                if( !String.IsNullOrEmpty( fileNFEx.FileName ) ) w.AppendLine( localPrefix + "FileName: " + fileNFEx.FileName );
                if( fileNFEx.FusionLog != null )
                {
                    w.Append( localPrefix + "FusionLog: " );
                    w.AppendMultiLine( localPrefix + "         ", fileNFEx.FusionLog, false );
                    w.AppendLine();
                }
            }
            else
            {
                if( ex is System.IO.FileLoadException loadFileEx )
                {
                    if( !String.IsNullOrEmpty( loadFileEx.FileName ) ) w.AppendLine( localPrefix + "FileName: " + loadFileEx.FileName );
                    if( loadFileEx.FusionLog != null )
                    {
                        w.Append( localPrefix + "FusionLog: " );
                        w.AppendMultiLine( localPrefix + "         ", loadFileEx.FusionLog, false );
                        w.AppendLine();
                    }
                }
                else
                {
                    if( ex is ReflectionTypeLoadException typeLoadEx )
                    {
                        w.AppendLine( localPrefix + " ┌──────────────────────────■ [Loader Exceptions] ■──────────────────────────" );
                        p = localPrefix + " | ";
                        for( int i = 0; i < typeLoadEx.Types.Length; i++ )
                        {
                            // apparently, Types/LoaderExceptions are parallel array.
                            // A null in Types[i] mean there is an exception in LoaderException[i]
                            // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.reflectiontypeloadexception.loaderexceptions?view=netstandard-2.0#property-value
                            if( typeLoadEx.Types[i] != null )
                            {
                                Debug.Assert( typeLoadEx.LoaderExceptions[i] == null );
                                continue;
                            }
                            DumpException( w, p, true, typeLoadEx.LoaderExceptions[i]! );
                        }
                        w.AppendLine( localPrefix + " └─────────────────────────────────────────────────────────────────────────" );
                    }
                }
            }
            // The InnerException of an aggregated exception is the same as the first of it InnerExceptionS.
            // (The InnerExceptionS are the contained/aggregated exceptions of the AggregatedException object.)
            // This is why, if we are on an AggregatedException we do not follow its InnerException.
            if( ex is AggregateException aggrex && aggrex.InnerExceptions.Count > 0 )
            {
                w.AppendLine( localPrefix + " ┌──────────────────────────■ [Aggregated Exceptions] ■──────────────────────────" );
                p = localPrefix + " | ";
                foreach( var item in aggrex.InnerExceptions )
                {
                    DumpException( w, p, true, item );
                }
                w.AppendLine( localPrefix + " └─────────────────────────────────────────────────────────────────────────" );
            }
            else if( ex.InnerException != null )
            {
                w.AppendLine( localPrefix + " ┌──────────────────────────■ [Inner Exception] ■──────────────────────────" );
                p = localPrefix + " | ";
                DumpException( w, p, true, ex.InnerException );
                w.AppendLine( localPrefix + " └─────────────────────────────────────────────────────────────────────────" );
            }
            w.AppendLine( prefix + " └" + new string( '─', header.Length - 2 ) );
        }

    }

}
