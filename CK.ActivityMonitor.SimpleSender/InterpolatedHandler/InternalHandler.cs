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

        public InternalHandler( int literalLength, int formattedCount, IActivityLineEmitter logger, LogLevel level, CKTrait? traits, out bool shouldAppend )
        {
            if( logger.ShouldLogLine( level, traits, out FinalTags ) )
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

        public InternalHandler( int literalLength, int formattedCount, IActivityMonitor monitor, LogLevel level, CKTrait? traits, out bool shouldAppend )
        {
            if( monitor.ShouldLogGroup( level, traits, out FinalTags ) )
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

        public void AppendFormatted( Type? t, string? format )
        {
            switch( format )
            {
                case "C": _stringBuilderHandler.AppendLiteral( t.ToCSharpName( withNamespace: false, true, true ) ); break;
                case "N": _stringBuilderHandler.AppendLiteral( t.ToCSharpName( withNamespace: true, true, true ) ); break;
                case "A": _stringBuilderHandler.AppendLiteral( t?.AssemblyQualifiedName ?? "null" ); break;
                case "F": _stringBuilderHandler.AppendLiteral( t?.FullName ?? "null" ); break;

                default:
                    if( t != null ) _stringBuilderHandler.AppendFormatted( t );
                    else _stringBuilderHandler.AppendLiteral( "null" );
                    _stringBuilderHandler.AppendLiteral( "(Invalid Type Format. Must be \"F\" for FullName,`\"A\" for AssemblyQualifiedName, \"C\" for compact C# name and \"N\" for C# name with namespace)" );
                    break;
            }
        }

        public void AppendLiteral( string value ) => _stringBuilderHandler.AppendLiteral( value );

        public void AppendFormatted<T>( T value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted<T>( T value, string? format )
        {
            if( format != null && value is Type t ) AppendFormatted( t, format );
            else _stringBuilderHandler.AppendFormatted( value, format );
        }

        public void AppendFormatted<T>( T value, int alignment ) => _stringBuilderHandler.AppendFormatted( value, alignment );

        public void AppendFormatted<T>( T value, int alignment, string? format ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( ReadOnlySpan<char> value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted( ReadOnlySpan<char> value, int alignment = 0, string? format = null ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( string? value ) => _stringBuilderHandler.AppendFormatted( value );

        public void AppendFormatted( string? value, int alignment = 0, string? format = null ) => _stringBuilderHandler.AppendFormatted( value, alignment, format );

        public void AppendFormatted( object? value, int alignment = 0, string? format = null )
        {
            if( value is Type type )
            {
                _stringBuilderHandler.AppendLiteral( type.ToCSharpName( withNamespace: false ) );
            }
            else
            {
                _stringBuilderHandler.AppendFormatted( value, alignment, format );
            }
        }
    }

}
