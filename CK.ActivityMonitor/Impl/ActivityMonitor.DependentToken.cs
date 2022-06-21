using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Describes the origin of a dependent activity: it is created by <see cref="ActivityMonitorExtension.CreateDependentToken(IActivityMonitor, string, string?, string?, int)">IActivityMonitor.CreateDependentToken</see> 
        /// (extension method).
        /// </summary>
        public sealed class DependentToken : ICKSimpleBinarySerializable
        {
            readonly string _originatorId;
            readonly DateTimeStamp _creationDate;
            readonly string? _message;
            readonly string? _topic;
            readonly bool _isMonitorTopic;

            internal DependentToken( string monitorId, DateTimeStamp logTime, string? message, string? topic, bool isMonitorTopic )
            {
                _originatorId = monitorId;
                _creationDate = logTime;
                _message = message;
                _topic = topic;
                _isMonitorTopic = isMonitorTopic;
            }

            /// <summary>
            /// Deserialization constructor.
            /// </summary>
            /// <param name="r">The reader.</param>
            public DependentToken( ICKBinaryReader r )
            {
                r.ReadByte();
                _originatorId = r.ReadString();
                _creationDate = new DateTimeStamp( r );
                _message = r.ReadNullableString();
                _topic = r.ReadNullableString();
                _isMonitorTopic = r.ReadBoolean();
            }

            /// <summary>
            /// Writes this token.
            /// </summary>
            /// <param name="w">The writer.</param>
            public void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)0 );
                w.Write( _originatorId );
                _creationDate.Write( w );
                w.WriteNullableString( _message );
                w.WriteNullableString( _topic );
                w.Write( _isMonitorTopic );
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
            /// Gets the token creation message.
            /// </summary>
            public string? Message => _message;

            /// <summary>
            /// Gets the topic that must be set on the dependent activity.
            /// When null, the current <see cref="IActivityMonitor.Topic"/> of the dependent monitor is not changed.
            /// </summary>
            public string? Topic => _topic;

            /// <summary>
            /// Gets whether the <see cref="Topic"/> to set on the target monitor is the
            /// same as the originating monitor.
            /// </summary>
            public bool IsMonitorTopic => _isMonitorTopic;

            internal string ToString( string? prefix )
            {
                if( _topic == null )
                    return $"{prefix}{_originatorId} at {_creationDate} - {_message} (Without topic.)";
                if( _isMonitorTopic )
                    return $"{prefix}{_originatorId} at {_creationDate} - {_message} (With monitor's topic '{_topic}'.)";
                return $"{prefix}{_originatorId} at {_creationDate} - {_message} (With topic '{_topic}'.)";
            }

            /// <summary>
            /// Overridden to give a readable description of this token that can be <see cref="Parse"/>d (or <see cref="TryParse"/>) back:
            /// The format is "<see cref="OriginatorId"/> at <see cref="CreationDate"/> with topic '...'|without topic|with monitor's topic '...'.".
            /// </summary>
            /// <returns>A readable string.</returns>
            public override string ToString() => ToString( null );

            /// <summary>
            /// Tries to parse a <see cref="DependentToken.ToString()"/> string.
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <param name="t">The resulting dependent token.</param>
            /// <returns>True on success, false otherwise.</returns>
            static public bool TryParse( ReadOnlySpan<char> s, [NotNullWhen( true )] out DependentToken? t )
            {
                t = null;
                var m = new ROSpanCharMatcher( s ) { SingleExpectationMode = true };
                if( MatchOriginatorAndTime( ref m, out var id, out DateTimeStamp time )
                    && m.TryMatch( " - " )
                    && TryParseCreateMessage( m.Head, out var message, out var topic, out var isMonitorTopic ) )
                {
                    t = new DependentToken( id, time, message, topic, isMonitorTopic );
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Parses a <see cref="DependentToken.ToString()"/> string or throws a <see cref="FormatException"/>
            /// on error.
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <returns>The resulting dependent token.</returns>
            static public DependentToken Parse( ReadOnlySpan<char> s )
            {
                if( !TryParse( s, out DependentToken? t ) ) Throw.FormatException( $"Invalid Dependent token string: '{s}'." );
                return t;
            }

            /// <summary>
            /// Tries to parse a create message. 
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <param name="message">The optional token creation message.</param>
            /// <param name="topic">The topic to set on the target monitor.</param>
            /// <param name="isMonitorTopic">True the topic is the one of the originating monitor.</param>
            /// <returns>True on success, false otherwise.</returns>
            public static bool TryParseCreateMessage( ReadOnlySpan<char> s,
                                                      out string? message,
                                                      out string? topic,
                                                      out bool isMonitorTopic )
            {
                message = null;
                topic = null;
                isMonitorTopic = false;
                if( s.EndsWith( "\'.)", StringComparison.Ordinal ) )
                {
                    int idx = s.IndexOf( " (With monitor's topic '", StringComparison.Ordinal );
                    if( idx >= 0 ) isMonitorTopic = true;
                    else if( (idx = s.IndexOf( " (With topic '", StringComparison.Ordinal )) < 0 ) return false;
                    Debug.Assert( " (With monitor's topic '".Length == 24 );
                    Debug.Assert( " (With topic '".Length == 14 );
                    if( idx > 0 ) message = s.Slice( 0, idx ).ToString();
                    idx += isMonitorTopic ? 24 : 14;
                    topic = s.Slice( idx, s.Length - idx - 3 ).ToString();
                    return true;
                }
                if( s.EndsWith( " (Without topic.)", StringComparison.Ordinal ) )
                {
                    Debug.Assert( " (Without topic.)".Length == 17 );
                    int len = s.Length - 17;
                    if( len > 0 ) message = s.Slice( 0, len ).ToString();
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Attempts to parse the start message of a dependent activity (tagged with <see cref="ActivityMonitor.Tags.StartDependentActivity"/>).
            /// </summary>
            /// <param name="startMessage">The start message to parse.</param>
            /// <param name="token">The token parsed.</param>
            /// <returns>True on success.</returns>
            static public bool TryParseStartMessage( ReadOnlySpan<char> startMessage, [NotNullWhen(true)]out DependentToken? token )
            {
                token = null;
                if( !startMessage.StartsWith( "Starting: ", StringComparison.Ordinal ) ) return false;
                Debug.Assert( "Starting: ".Length == 10 );
                return TryParse( startMessage.Slice( 10 ), out token );
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

        }
    }
}
