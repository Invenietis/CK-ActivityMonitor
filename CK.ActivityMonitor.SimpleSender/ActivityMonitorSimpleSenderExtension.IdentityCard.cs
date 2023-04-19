using System.Collections.Generic;
using System;
using System.Reflection.Emit;

namespace CK.Core
{
    public static partial class ActivityMonitorSimpleSenderExtension
    {
        /// <summary>
        /// Adds an identity information for this application domain.
        /// This sends an unfiltered information line tagged with <see cref="IdentityCard.IdentityCardUpdate"/>.
        /// <see cref="IdentityCard.Pack(string, string)"/> is used to pack the text.
        /// <para>
        /// The characters 0 to 8 (NUl, SOH, STX, ETX, EOT, ENQ, ACK, BEL, BSP) are invalid in a key and in a value.
        /// </para> 
        /// <para>
        /// A key cannot contain newlines: the Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2 state
        /// that the CR, LF, CRLF, NEL, LS, FF, and PS sequences are considered newline functions.
        /// </para>
        /// </summary>
        /// <param name="logger">This logger.</param>
        /// <param name="key">The identity key. Must not be null, empty or white space or contain any line delimiter.</param>
        /// <param name="value">The identity information. Must not be empty.</param>
        public static void AddIdentityInformation( this IActivityLineEmitter logger, string key, string value )
        {
            logger.UnfilteredLog( LogLevel.Info | LogLevel.IsFiltered, IdentityCard.IdentityCardUpdate, IdentityCard.Pack( key, value ), null );
        }

        /// <summary>
        /// Encapsulates helpers for identity information.
        /// </summary>
        public static class IdentityCard
        {
            /// <summary>
            /// The key/value separator used by <see cref="Pack(string, string)"/>.
            /// </summary>
            public const char KeySeparator = '\u0002';

            /// <summary>
            /// Gets the tag that identify an identity information dent by <see cref="AddIdentityInformation"/>.
            /// The log text is packed with <see cref="Pack"/>.
            /// </summary>
            /// <remarks>
            /// Current implementation uses string packing to exchange identity card updates.
            /// One day, it should use a more efficient binary representation. To handle this new
            /// representation, This tag should be deprecated, internalized, and "IdentityCardAdd"
            /// should replace it.
            /// </remarks>
            public static readonly CKTrait IdentityCardUpdate = ActivityMonitor.Tags.Context.FindOrCreate( nameof( IdentityCardUpdate ) );

            /// <summary>
            /// Packs a (name, value) in a single string separated by STX (0x02) character
            /// that can be sent with the <see cref="IdentityCardUpdate"/> tag. The key and value must be valid:
            /// see <see cref="CkeckIdentityInformation"/>
            /// </summary>
            /// <param name="key">The identity key. Must not be null, empty or white space or contain any line delimiter.</param>
            /// <param name="value">The identity information. Must not be empty.</param>
            /// <returns>The packed string.</returns>
            public static string Pack( string key, string value )
            {
                CkeckIdentityInformation( key, value );
                return String.Create( key.Length + 1 + value.Length, (key, value), ( s, i ) =>
                {
                    i.key.CopyTo( s );
                    s[i.key.Length] = KeySeparator;
                    s = s.Slice( i.key.Length + 1 );
                    i.value.CopyTo( s );
                } );
            }

            /// <summary>
            /// Throws argument exceptions if key or value of the identity information are invalid.
            /// <para>
            /// The characters 0 to 8 (NUl, SOH, STX, ETX, EOT, ENQ, ACK, BEL, BSP) are invalid in a key and in a value.
            /// </para> 
            /// <para>
            /// A key cannot contain newlines: the Unicode Standard, Sec. 5.8, Recommendation R4 and Table 5-2 state
            /// that the CR, LF, CRLF, NEL, LS, FF, and PS sequences are considered newline functions.
            /// </para>
            /// </summary>
            /// <param name="key">The identity key. Must not be null, empty or white space or contain any line delimiter.</param>
            /// <param name="value">The identity information. Must not be empty.</param>
            public static void CkeckIdentityInformation( string key, string value )
            {
                Throw.CheckNotNullOrEmptyArgument( key );
                Throw.CheckNotNullOrEmptyArgument( value );
                Throw.CheckArgument( "Invalid newline character in identity key.", key.AsSpan().IndexOfAny( "\r\n\f\u0085\u2028\u2029" ) < 0 );
                Throw.CheckArgument( "First 8 characters (NUl, SOH, STX, ETX, EOT, ENQ, ACK, BEL, BSP) cannot appear.",
                                        key.AsSpan().IndexOfAny( "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008" ) < 0 );
                Throw.CheckArgument( "First 8 characters (NUl, SOH, STX, ETX, EOT, ENQ, ACK, BEL, BSP) cannot appear.",
                                        value.AsSpan().IndexOfAny( "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008" ) < 0 );
            }
        }
    }
}
