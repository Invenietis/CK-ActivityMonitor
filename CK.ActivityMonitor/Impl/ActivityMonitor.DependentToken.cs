using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Concrete implementation of <see cref="IActivityMonitor"/>.
    /// </summary>
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Describes the origin of a dependent activity: it is created by <see cref="ActivityMonitorExtension.DependentActivity">IActivityMonitor.DependentActivity</see> 
        /// (extension methods).
        /// </summary>
        [Serializable]
        public class DependentToken
        {
            readonly string _originatorId;
            readonly DateTimeStamp _creationDate;
            readonly string? _topic;
            [NonSerialized]
            string? _delayedLaunchMessage;

            internal DependentToken( string monitorId, DateTimeStamp logTime, string? topic )
            {
                _originatorId = monitorId;
                _creationDate = logTime;
                _topic = topic;
            }

            /// <summary>
            /// Unique identifier of the activity that created this dependent token.
            /// </summary>
            public string OriginatorId => _originatorId;

            /// <summary>
            /// Gets the creation date. This is the log time of the unfiltered Info log that has 
            /// been emitted in the originator monitor.
            /// </summary>
            public DateTimeStamp CreationDate => _creationDate;

            /// <summary>
            /// Gets the topic that must be set on the dependent activity.
            /// When null, the current <see cref="IActivityMonitor.Topic"/> of the dependent monitor is not changed.
            /// </summary>
            public string? Topic => _topic;

            /// <summary>
            /// Overridden to give a readable description of this token that can be <see cref="Parse"/>d (or <see cref="TryParse"/>) back:
            /// The format is "<see cref="OriginatorId"/> at <see cref="CreationDate"/> (with topic '...'|without topic).".
            /// </summary>
            /// <returns>A readable string.</returns>
            public override string ToString()
            {
                return AppendTopic( $"{_originatorId} at {_creationDate} with", _topic );
            }

            /// <summary>
            /// Tries to parse a <see cref="DependentToken.ToString()"/> string.
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <param name="t">The resulting dependent token.</param>
            /// <returns>True on success, false otherwise.</returns>
            static public bool TryParse( string s, [MaybeNullWhen( false )] out DependentToken t )
            {
                t = null;
                var m = new ROSpanCharMatcher( s ) { SingleExpectationMode = true };
                if( MatchOriginatorAndTime( ref m, out var id, out DateTimeStamp time ) && m.TryMatch( " with" ) )
                {
                    if( TryExtractTopic( m.Head, out string? topic ) )
                    {
                        t = new DependentToken( id, time, topic );
                        return true;
                    }
                }
                return false;
            }

            /// <summary>
            /// Parses a <see cref="DependentToken.ToString()"/> string or throws a <see cref="FormatException"/>
            /// on error.
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <returns>The resulting dependent token.</returns>
            static public DependentToken Parse( string s )
            {
                if( !TryParse( s, out DependentToken? t ) ) Throw.FormatException( $"Invalid Dependent token string: '{s}'." );
                return t;
            }

            /// <summary>
            /// Tries to parse a launch or create message. 
            /// </summary>
            /// <param name="message">The message to parse.</param>
            /// <param name="launched">True if the activity has been launched or the token has only be created.</param>
            /// <param name="withTopic">True if an explicit topic has been associated to the dependent activity.</param>
            /// <param name="dependentTopic">When <paramref name="withTopic"/> is true, this contains the explicitly set topic.</param>
            /// <returns>True on success.</returns>
            public static bool TryParseLaunchOrCreateMessage( string message, out bool launched, out bool withTopic, out string? dependentTopic )
            {
                Throw.CheckNotNullArgument( message );
                launched = false;
                withTopic = false;
                dependentTopic = null;

                if( message.Length < 10 ) return false;
                if( message.StartsWith( _prefixLaunchWithTopic ) )
                {
                    launched = true;
                    withTopic = true;
                    Debug.Assert( _prefixLaunchWithTopic.Length == 33 );
                    if( !TryExtractTopic( message.AsSpan( 33 ), out dependentTopic ) ) return false;
                }
                else if( message.StartsWith( _prefixCreateWithTopic ) )
                {
                    withTopic = true;
                    Debug.Assert( _prefixCreateWithTopic.Length == 37 );
                    if( !TryExtractTopic( message.AsSpan( 37 ), out dependentTopic ) ) return false;
                }
                else if( message.StartsWith( _prefixLaunch ) )
                {
                    launched = true;
                }
                else if( !message.StartsWith( _prefixCreate ) ) return false;
                return true;
            }

            /// <summary>
            /// Captures the log message when created with a delayed launch so that DependentSender.Launch( token ) can log it.
            /// </summary>
            internal string? DelayedLaunchMessage
            {
                get { return _delayedLaunchMessage; }
                set { _delayedLaunchMessage = value; }
            }

            static bool TryExtractTopic( ReadOnlySpan<char> message, out string? dependentTopic )
            {
                Debug.Assert( _suffixWithoutTopic.Length == 9 );
                Debug.Assert( _suffixWithTopic.Length == 8 );

                dependentTopic = null;

                if( message.Length < 8 + 1 ) return false;
                if( message.TryMatch( _suffixWithTopic ) )
                {
                    int idxEndQuote = message.LastIndexOf( '\'' );
                    if( idxEndQuote < 0 ) return false;
                    dependentTopic = new string( message.Slice( 0, idxEndQuote ) );
                    return true;
                }
                if( message.StartsWith( _suffixWithoutTopic ) )
                {
                    // We exit with true and a null dependentTopic since there is no topic.
                    return true; 
                }
                return false;
            }

            /// <summary>
            /// Attempts to parse the start message of a dependent activity (tagged with <see cref="ActivityMonitor.Tags.StartDependentActivity"/>).
            /// </summary>
            /// <param name="startMessage">The start message to parse.</param>
            /// <param name="id">The originator monitor identifier.</param>
            /// <param name="time">The creation time of the dependent activity.</param>
            /// <returns>True on success.</returns>
            static public bool TryParseStartMessage( string startMessage, out string id, out DateTimeStamp time )
            {
                Debug.Assert( "Starting dependent activity issued by ".Length == 38 );

                if( startMessage.Length < 38 + ActivityMonitor.MinMonitorUniqueIdLength + 4 + 27
                    || !startMessage.StartsWith( "Starting dependent activity issued by " ) )
                {
                    id = string.Empty;
                    time = DateTimeStamp.MinValue;
                    return false;
                }
                var m = new ROSpanCharMatcher( startMessage.AsSpan( 38 ) ) { SingleExpectationMode = true };
                return MatchOriginatorAndTime( ref m, out id, out time );
            }

            static bool MatchOriginatorAndTime( ref ROSpanCharMatcher m, out string id, out DateTimeStamp time )
            {
                time = DateTimeStamp.MinValue;
                id = string.Empty;
                int i = 0;
                int len = m.Head.Length;
                while( --len >= 0 )
                {
                    char c = m.Head[i];
                    if( char.IsWhiteSpace( c ) ) break;
                    ++i;
                }
                if( i < MinMonitorUniqueIdLength ) return m.AddExpectation( "Monitor Id" );
                id = new string( m.Head.Slice( 0, i ) );
                m.Head = m.Head.Slice( i );
                if( !m.TryMatch( " at " ) ) return false;
                return m.TryMatchDateTimeStamp( out time );
            }

            internal string FormatStartMessage()
            {
                return string.Format( "Starting dependent activity issued by {0} at {1}.", _originatorId, _creationDate );
            }

            const string _prefixLaunch = "Launching dependent activity";
            const string _prefixCreate = "Activity dependent token created";
            const string _prefixLaunchWithTopic = "Launching dependent activity with";
            const string _prefixCreateWithTopic = "Activity dependent token created with";
            const string _suffixWithoutTopic = "out topic";
            const string _suffixWithTopic = " topic '";

            internal static DependentToken CreateWithMonitorTopic( IActivityMonitor m, bool launchActivity, out string msg )
            {
                msg = launchActivity ? _prefixLaunch : _prefixCreate;
                DependentToken t = new DependentToken( m.UniqueId, m.NextLogTime(), m.Topic );
                msg += '.';
                return t;
            }

            internal static DependentToken CreateWithDependentTopic( IActivityMonitor m, bool launchActivity, string dependentTopic, out string msg )
            {
                msg = AppendTopic( launchActivity ? _prefixLaunchWithTopic : _prefixCreateWithTopic, dependentTopic );
                return new DependentToken( m.UniqueId, m.NextLogTime(), dependentTopic );
            }

            static string AppendTopic( string msg, string? dependentTopic )
            {
                Debug.Assert( msg.EndsWith( " with" ) );
                if( dependentTopic == null ) msg += _suffixWithoutTopic;
                else msg += _suffixWithTopic + dependentTopic + '\'';
                return msg + '.';
            }

            static internal IDisposable Start( DependentToken token, IActivityMonitor monitor, string? fileName, int lineNumber )
            {
                string msg = token.FormatStartMessage();
                if( token.Topic != null )
                {
                    string currentTopic = token.Topic;
                    monitor.SetTopic( token.Topic, fileName, lineNumber );
                    var g = monitor.UnfilteredOpenGroup( LogLevel.Info, Tags.StartDependentActivity, msg, null, fileName, lineNumber );
                    return Util.CreateDisposableAction( () => { g.Dispose(); monitor.SetTopic( currentTopic, fileName, lineNumber ); } );
                }
                return monitor.UnfilteredOpenGroup( LogLevel.Info, Tags.StartDependentActivity, msg, null, fileName, lineNumber );
            }
        }
    }
}
