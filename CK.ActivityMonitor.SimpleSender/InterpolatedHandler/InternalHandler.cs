using System;
using System.Text;

namespace CK.Core.LogHandler
{
    struct InternalHandler
    {
        StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;
        // The AppendInterpolatedStringHandler._stringBuilder is internal. We duplicate its reference here.
        StringBuilder? _builder;

        public CKTrait FinalTags;

        public InternalHandler( bool isGroup, int literalLength, int formattedCount, IActivityMonitor monitor, LogLevel level, CKTrait? traits, out bool shouldAppend )
        {
            if( isGroup ? monitor.ShouldLogGroup( level, traits, out FinalTags ) : monitor.ShouldLogLine( level, traits, out FinalTags ) )
            {
                _builder = StringBuilderCache.Acquire( literalLength, formattedCount );
                _stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler( literalLength, formattedCount, _builder );
                shouldAppend = true;
            }
            else
            {
                _stringBuilderHandler = default;
                _builder = null;
                shouldAppend = false;
            }
        }

        public string? ToStringAndClear()
        {
            _stringBuilderHandler = default;
            var b = _builder;
            _builder = null;
            return b != null ? StringBuilderCache.GetStringAndRelease( b ) : null;
        }

        public void AppendLiteral( string value ) => _stringBuilderHandler.AppendLiteral( value );

        public void AppendFormatted<T>( T value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted<T>( T value, string? format ) => _stringBuilderHandler.AppendFormatted( value, format );

        public void AppendFormatted<T>( T value, int alignment ) => _stringBuilderHandler.AppendFormatted( value, alignment );

        public void AppendFormatted<T>( T value, int alignment, string? format ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( ReadOnlySpan<char> value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted( ReadOnlySpan<char> value, int alignment = 0, string? format = null ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( string? value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted( string? value, int alignment = 0, string? format = null ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( object? value, int alignment = 0, string? format = null ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );
    }

}
