using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core
{
    public sealed partial class ActivityMonitor
    {
        /// <summary>
        /// Identifies a <see cref="Token"/> creation point but can be used to identify any log entry
        /// creation point. This class implements full value equality semantics and is comparable: <see cref="CreationDate"/>
        /// comes first, then <see cref="OriginatorId"/> is used.
        /// <para>
        /// Equality and comparison operators are supported.
        /// </para>
        /// </summary>
        [SerializationVersion( SerializationVersion )]
        public sealed class LogKey : IEquatable<LogKey>, IComparable<LogKey>, ICKVersionedBinarySerializable, ICKSimpleBinarySerializable
        {
            const byte SerializationVersion = 0;

            readonly string _originatorId;
            readonly DateTimeStamp _creationDate;

            /// <summary>
            /// Initializes a new <see cref="LogKey"/>.
            /// </summary>
            /// <param name="originatorId">The origin monitor identifier.</param>
            /// <param name="creationDate">The log creation date.</param>
            public LogKey( string originatorId, DateTimeStamp creationDate )
            {
                Throw.CheckNotNullOrEmptyArgument( originatorId );
                _originatorId = originatorId;
                _creationDate = creationDate;
            }

            /// <summary>
            /// <see cref="ICKSimpleBinarySerializable"/> constructor.
            /// </summary>
            /// <param name="r">The reader.</param>
            public LogKey( ICKBinaryReader r )
            {
                r.ReadByte(); // Version.
                _originatorId = r.ReadString();
                _creationDate = new DateTimeStamp( r );
            }

            /// <summary>
            /// The <see cref="ICKVersionedBinarySerializable"/> constructor.
            /// </summary>
            /// <param name="r">The reader.</param>
            /// <param name="version">The version.</param>
            public LogKey( ICKBinaryReader r, int version )
            {
                _originatorId = r.ReadString();
                _creationDate = new DateTimeStamp( r );
            }

            /// <inheritdoc />
            public void WriteData( ICKBinaryWriter w )
            {
                w.Write( _originatorId );
                _creationDate.Write( w );
            }

            /// <inheritdoc />
            public void Write( ICKBinaryWriter w )
            {
                w.Write( (byte)SerializationVersion );
                w.Write( _originatorId );
                _creationDate.Write( w );
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
            /// Implements value equality semantics.
            /// </summary>
            /// <param name="other">The other token key.</param>
            /// <returns>Whether <see cref="OriginatorId"/> and <see cref="CreationDate"/> are the same.</returns>
            public bool Equals( LogKey? other ) => !ReferenceEquals( other, null ) && other._originatorId == _originatorId && other._creationDate == _creationDate;

            /// <summary>
            /// Overridden for value equality semantics. 
            /// </summary>
            /// <param name="obj">The other object.</param>
            /// <returns>Whether this is a token key and <see cref="OriginatorId"/> and <see cref="CreationDate"/> are the same.</returns>
            public override bool Equals( object? obj ) => Equals( obj as LogKey );

            /// <inheritdoc />
            public override int GetHashCode() => HashCode.Combine( _originatorId, _creationDate );

            /// <summary>
            /// <see cref="CreationDate"/> is the primary sort key, then comes the <see cref="OriginatorId"/>.
            /// </summary>
            /// <param name="other">The other key to compare to.</param>
            /// <returns>Standard comparison result.</returns>
            public int CompareTo( LogKey? other )
            {
                if( ReferenceEquals( other, null ) ) return -1;
                int cmp = _creationDate.CompareTo( other._creationDate );
                if( cmp == 0 ) cmp = _originatorId.CompareTo( other._originatorId );
                return cmp;
            }

            /// <summary>
            /// Gets "<see cref="OriginatorId"/>.<see cref="CreationDate"/>" string.
            /// </summary>
            /// <returns>String with the OriginatorId and CreationDate.</returns>
            public override string ToString() => $"{_originatorId}.{_creationDate}";

            /// <summary>
            /// Implict cast into string.
            /// </summary>
            /// <param name="key">The key. A null logkey is the empty string.</param>
            public static implicit operator string( LogKey? key ) => key?.ToString() ?? string.Empty;

            /// <summary>
            /// Tries to match a <see cref="LogKey.ToString()"/> string and forwards the <paramref name="head"/> on success.
            /// </summary>
            /// <param name="head">The parsing head.</param>
            /// <param name="t">The resulting log key.</param>
            /// <returns>True on success, false otherwise.</returns>
            static public bool TryMatch( ref ReadOnlySpan<char> head, [NotNullWhen( true )] out LogKey? t ) => TryMatch( ref head, out t, false );

            /// <summary>
            /// Tries to parse a <see cref="LogKey.ToString()"/>.
            /// </summary>
            /// <param name="head">The string to parse.</param>
            /// <param name="t">The resulting log key on success, null otherwise.</param>
            /// <returns>True on success, false otherwise.</returns>
            static public bool TryParse( ReadOnlySpan<char> head, [NotNullWhen( true )] out LogKey? t ) => TryMatch( ref head, out t, true );

            static bool TryMatch( ref ReadOnlySpan<char> head, [NotNullWhen( true )] out LogKey? t, bool parse )
            {
                Debug.Assert( head.Length == 1 );
                t = null;
                int idx = head.IndexOf( '.' );
                // MonitorId must contain at least one character.
                // DateTimeStamp string contains between 27 and 32 characters.
                if( idx < 1 || idx + 28 < head.Length ) return false;
                string monitorId = new string( head.Slice( 0, idx ) );
                var savedHead = head;
                head = head.Slice( idx + 1 );
                if( DateTimeStamp.TryMatch( ref head, out var stamp )
                    && (!parse || head.IsEmpty) )
                {
                    t = new LogKey( monitorId, stamp );
                    return true;
                }
                head = savedHead;
                return false;
            }


            /// <summary>
            /// Parses a <see cref="LogKey.ToString()"/> string or throws a <see cref="FormatException"/>
            /// on error.
            /// </summary>
            /// <param name="s">The string to parse.</param>
            /// <returns>The resulting LogKey.</returns>
            static public LogKey Parse( ReadOnlySpan<char> s )
            {
                if( !TryParse( s, out var t ) ) Throw.FormatException( $"Invalid LogKey string: '{s}'." );
                return t;
            }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            public static bool operator ==( LogKey left, LogKey right )
            {
                if( ReferenceEquals( left, null ) )
                {
                    return ReferenceEquals( right, null );
                }

                return left.Equals( right );
            }

            public static bool operator !=( LogKey left, LogKey right )
            {
                return !(left == right);
            }

            public static bool operator <( LogKey left, LogKey right )
            {
                return ReferenceEquals( left, null ) ? !ReferenceEquals( right, null ) : left.CompareTo( right ) < 0;
            }

            public static bool operator <=( LogKey left, LogKey right )
            {
                return ReferenceEquals( left, null ) || left.CompareTo( right ) <= 0;
            }

            public static bool operator >( LogKey left, LogKey right )
            {
                return !ReferenceEquals( left, null ) && left.CompareTo( right ) > 0;
            }

            public static bool operator >=( LogKey left, LogKey right )
            {
                return ReferenceEquals( left, null ) ? ReferenceEquals( right, null ) : left.CompareTo( right ) >= 0;
            }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        }
    }
}
