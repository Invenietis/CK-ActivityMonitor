using System;
using System.Collections.Generic;
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
