using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace CK.Core;

public sealed partial class ActivityMonitor
{
    /// <summary>
    /// Describes the origin of a dependent activity: it is created by <see cref="CreateToken(string, string?, CKTrait?, string?, int)"/>.
    /// </summary>
    public sealed class Token : ICKSimpleBinarySerializable
    {
        readonly LogKey _key;
        readonly string? _message;
        readonly string? _topic;

        internal Token( LogKey key, string? message, string? topic )
        {
            _key = key;
            _message = message;
            _topic = topic;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="r">The reader.</param>
        public Token( ICKBinaryReader r )
        {
            int v = r.ReadByte();
            // Use the 0 version.
            _key = new LogKey( r, 0 );
            _message = r.ReadNullableString();
            _topic = r.ReadNullableString();
            if( v == 0 )
            {
                // Was IsMonitorTopic.
                r.ReadBoolean();
            }
        }

        /// <summary>
        /// Writes this token.
        /// </summary>
        /// <param name="w">The writer.</param>
        public void Write( ICKBinaryWriter w )
        {
            w.Write( (byte)1 );
            _key.WriteData( w );
            w.WriteNullableString( _message );
            w.WriteNullableString( _topic );
        }

        /// <summary>
        /// Gets this token's <see cref="LogKey"/>.
        /// </summary>
        public LogKey Key => _key;

        /// <summary>
        /// Unique identifier of the activity that created this dependent token.
        /// </summary>
        public string OriginatorId => _key.OriginatorId;

        /// <summary>
        /// Gets the creation date. This is the log time of the unfiltered Info log that has 
        /// been emitted in the originator monitor.
        /// </summary>
        public DateTimeStamp CreationDate => _key.CreationDate;

        /// <summary>
        /// Gets the token creation message.
        /// </summary>
        public string? Message => _message;

        /// <summary>
        /// Gets the topic that must be set on the dependent activity.
        /// When null, the current <see cref="IActivityMonitor.Topic"/> of the dependent monitor is not changed.
        /// </summary>
        public string? Topic => _topic;

        internal string ToString( string? prefix )
        {
            if( _topic == null )
                return _message == null
                        ? $"{prefix}{_key}"
                        : $"{prefix}{_key} - {_message}";
            return _message == null
                    ? $"{prefix}{_key} (With topic '{_topic}'.)"
                    : $"{prefix}{_key} - {_message} (With topic '{_topic}'.)";
        }

        /// <summary>
        /// Overridden to give a readable description of this token that can be <see cref="Parse"/>d (or <see cref="TryParse"/>) back:
        /// The format is "<see cref="OriginatorId"/>.<see cref="CreationDate"/> [- <see cref="Message"/>][ (with topic '...')].".
        /// </summary>
        /// <returns>A readable string.</returns>
        public override string ToString() => ToString( null );

        /// <summary>
        /// Tries to parse a <see cref="Token.ToString()"/> string.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="t">The resulting dependent token.</param>
        /// <returns>True on success, false otherwise.</returns>
        static public bool TryParse( ReadOnlySpan<char> s, [NotNullWhen( true )] out Token? t )
        {
            t = null;
            if( LogKey.TryMatch( ref s, out var key ) )
            {
                string? message, topic;
                if( s.IsEmpty )
                {
                    message = topic = null;
                }
                else
                {
                    // Remove message separator if any.
                    // If there is no message separator then there must be a space
                    // before the (With topic...).
                    if( !(s.TryMatch( " - " ) || s.TryMatch( ' ' ))
                        || !TryParseMessageAndTopic( s, out message, out topic ) )
                    {
                        return false;
                    }
                }
                t = new Token( key, message, topic );
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parses a <see cref="Token.ToString()"/> string or throws a <see cref="FormatException"/>
        /// on error.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The resulting dependent token.</returns>
        static public Token Parse( ReadOnlySpan<char> s )
        {
            if( !TryParse( s, out Token? t ) ) Throw.FormatException( $"Invalid Dependent token string: '{s}'." );
            return t;
        }

        /// <summary>
        /// Tries to parse a create message. 
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="message">The optional token creation message.</param>
        /// <param name="topic">The topic to set on the target monitor.</param>
        /// <returns>True on success, false otherwise.</returns>
        public static bool TryParseMessageAndTopic( ReadOnlySpan<char> s,
                                                  out string? message,
                                                  out string? topic )
        {
            message = null;
            topic = null;
            if( s.EndsWith( "\'.)", StringComparison.Ordinal ) )
            {
                int idx = s.LastIndexOf( "(With topic '", StringComparison.Ordinal );
                if( idx < 0 ) return false;
                Throw.DebugAssert( "(With topic '".Length == 13 );
                if( idx > 0 )
                {
                    // Trim any white space that may appear: normalized to null.
                    var ss = s.Slice( 0, idx ).Trim();
                    message = ss.Length > 0 ? ss.ToString() : null;
                }
                idx += 13;
                topic = s.Slice( idx, s.Length - idx - 3 ).ToString();
                return true;
            }
            if( !s.TryMatch( NoLogText, StringComparison.Ordinal ) )
            {
                s = s.Trim();
                if( s.Length > 0 ) message = s.ToString();
            }
            return true;
        }

        /// <summary>
        /// Attempts to parse the start message of a dependent activity (tagged with <see cref="ActivityMonitor.Tags.StartActivity"/>).
        /// </summary>
        /// <param name="startMessage">The start message to parse.</param>
        /// <param name="token">The token parsed.</param>
        /// <returns>True on success.</returns>
        static public bool TryParseStartMessage( ReadOnlySpan<char> startMessage, [NotNullWhen( true )] out Token? token )
        {
            token = null;
            if( !startMessage.StartsWith( "Starting: ", StringComparison.Ordinal ) ) return false;
            Throw.DebugAssert( "Starting: ".Length == 10 );
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

    /// <inheritdoc />
    public Token CreateToken( string? message = null, string? dependentTopic = null, CKTrait? createTags = null, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
    {
        if( string.IsNullOrWhiteSpace( message ) ) message = null;
        createTags |= _autoTags | Tags.CreateToken;
        var data = _logger.CreateLogLineData( false, LogLevel.Info | LogLevel.IsFiltered, createTags, message, null, fileName, lineNumber );
        Token t = CreateToken( ref data, message, dependentTopic );
        UnfilteredLog( ref data );
        return t;
    }

    Token CreateToken( ref ActivityMonitorLogData data, string? message, string? dependentTopic )
    {
        if( string.IsNullOrWhiteSpace( dependentTopic ) ) dependentTopic = null;
        if( dependentTopic != null )
        {
            var m = $"{(message != null ? message + ' ' : "")}(With topic '{dependentTopic}'.)";
            data.SetText( m );
        }
        return new Token( new LogKey( _uniqueId, data.LogTime ), message, dependentTopic );
    }

}
