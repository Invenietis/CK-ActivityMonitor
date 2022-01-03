using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core.LogHandler
{

    /// <summary>
    /// Provides an interpolated string handler for log lines that only performs formatting if the log must be emitted.
    /// This supports the logging framework and must not be used directly.
    /// </summary>
    [EditorBrowsable( EditorBrowsableState.Never )]
    [InterpolatedStringHandler]
    public ref struct GroupLogWithTags
    {
        internal InternalHandler _handler;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public GroupLogWithTags( int literalLength, int formattedCount, IActivityMonitor monitor, LogLevel level, CKTrait tags, out bool shouldAppend )
        {
            _handler = new InternalHandler( true, literalLength, formattedCount, monitor, level, tags, out shouldAppend );
        }

        public void AppendLiteral( string value ) => _handler.AppendLiteral( value );

        public void AppendFormatted<T>( T value ) => _handler.AppendFormatted( value );

        public void AppendFormatted<T>( T value, string? format ) => _handler.AppendFormatted( value, format );

        public void AppendFormatted<T>( T value, int alignment ) => _handler.AppendFormatted( value, alignment );

        public void AppendFormatted<T>( T value, int alignment, string? format ) => _handler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( ReadOnlySpan<char> value ) => _handler.AppendFormatted( value );

        public void AppendFormatted( ReadOnlySpan<char> value, int alignment = 0, string? format = null ) => _handler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( string? value ) => _handler.AppendFormatted( value );

        public void AppendFormatted( string? value, int alignment = 0, string? format = null ) => _handler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( object? value, int alignment = 0, string? format = null ) => _handler.AppendFormatted( value, alignment, format );
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

}