using System;
using System.Diagnostics.Tracing;

namespace CK.Core
{
    /// <summary>
    /// Helper that can configure the <see cref="DotNetEventSourceCollector"/> for EventSource
    /// that can already exist or don't exist yet (as long as the configurator is not disposed).
    /// <para>
    /// The configuration is applied at construction time and the creation of new sources is tracked
    /// thanks to <see cref="DotNetEventSourceCollector.OnNewEventSource"/>.
    /// </para>
    /// </summary>
    public sealed class DotNetEventSourceConfigurator : IDisposable
    {
        // Even if the EventSource has a global lock, we replicate here the
        // protection implemented in the STaticGatesConfigurator. We need it at least
        // in the constructor: if a new DotNetEventSourceConfigurator is built while a
        // new source appear then we may have a race condition.
        // It is safer and we have NO performance issue here.
        readonly string[] _names;
        readonly int[] _levels;

        public DotNetEventSourceConfigurator( string configuration )
        {
            (_names, _levels) = CreateConfig( configuration );
            lock( _names )
            {
                DotNetEventSourceCollector.OnNewEventSource += Configure;
                foreach( var g in DotNetEventSourceCollector.GetSources() )
                {
                    DoConfigure( g.Name );
                }
            }
        }

        void Configure( string s )
        {
            lock( _names ) DoConfigure( s );
        }

        // No risk here, reuse the public by name API: we never manipulate
        // The event source itself here, only its name.
        void DoConfigure( string s )
        {
            int idx = Array.IndexOf( _names, s );
            if( idx >= 0 )
            {
                int l = _levels[idx];
                if( l >= 0 ) DotNetEventSourceCollector.Enable( s, (EventLevel)l );
                else DotNetEventSourceCollector.Disable( s );
            }
        }

        static (string[] Names, int[] Levels) CreateConfig( string configuration )
        {
            var e = configuration.Split( ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
            var names = new string[e.Length];
            var levels = new int[e.Length];
            for( int i = 0; i < e.Length; ++i )
            {
                var name = e[i];
                int iC = name.LastIndexOf( ':' );
                int level;
                if( iC < 0 || iC == name.Length - 1 )
                {
                    // No level defaults to Informational (Verbose is too much!).
                    level = (int)EventLevel.Informational;
                }
                else
                {
                    int iL = iC + 1;
                    while( iL < name.Length && char.IsWhiteSpace( name, iL ) ) ++iL;
                    if( iL == name.Length ) level = (int)EventLevel.Informational;
                    else level = name[iL] switch
                    {
                        '!' => -1,
                        'C' or 'c' => (int)EventLevel.Critical,
                        'E' or 'e' => (int)EventLevel.Error,
                        'W' or 'w' => (int)EventLevel.Warning,
                        'L' or 'l' => (int)EventLevel.LogAlways,
                        'V' or 'v' => (int)EventLevel.Verbose,
                        _ => (int)EventLevel.Informational
                    };
                }
                while( iC > 0 && char.IsWhiteSpace( name[iC - 1] ) ) --iC;
                // Don't car if an empty or invalid name happens, we'll never
                // match it and this is fine.
                names[i] = iC >= 0 ? name.Substring( 0, iC ) : name;
                levels[i] = level;
            }
            return (names, levels);
        }

        public void Dispose()
        {
            DotNetEventSourceCollector.OnNewEventSource -= Configure;
        }
    }

}
