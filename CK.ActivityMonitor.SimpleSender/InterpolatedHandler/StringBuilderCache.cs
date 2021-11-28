using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CK.Core.LogHandler
{
    class StringBuilderCache
    {
        // The StringBuilderCache that uses a ThreadStatic reusable instance with a small (360) buffer
        // used by formatting functions is internal.
        // We have 2 options here:
        //  - Using reflections to resolve 2 delegates (on Acquire/Release static methods) and use them.
        //  - Duplicating the code.
        // We choose here the first to avoid a second cached instance per thread.

        static readonly Func<int, StringBuilder> _acquireBuilder;
        static readonly Func<StringBuilder, string> _releaseBuilder;

        // This one is also internal (in DefaultInterpolatedStringHandler). We reproduce it.
        const int _guessedLengthPerHole = 11;
        const int _minimumArrayPoolLength = 256;

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static int GetDefaultLength( int literalLength, int formattedCount ) => Math.Max( _minimumArrayPoolLength, literalLength + (formattedCount * _guessedLengthPerHole) );

        static StringBuilderCache()
        {
            Type t = typeof( StringBuilder ).Assembly.GetType( "System.Text.StringBuilderCache", true )!;
            _acquireBuilder = t.GetMethod( "Acquire", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static )!
                               .CreateDelegate<Func<int, StringBuilder>>();
            _releaseBuilder = t.GetMethod( "GetStringAndRelease", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static )!
                               .CreateDelegate<Func<StringBuilder, string>>();

            IActivityMonitor m = new ActivityMonitor();
            m.Debug( ActivityMonitor.Tags.InternalMonitor, $"{t}" );

        }

        public static StringBuilder Acquire( int literalLength, int formattedCount ) => _acquireBuilder( GetDefaultLength( literalLength, formattedCount ) );
        public static string GetStringAndRelease( StringBuilder b ) => _releaseBuilder( b );
    }
}
