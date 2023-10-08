using System;
using System.Diagnostics.Tracing;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Helper that can configure the <see cref="DotNetEventSourceCollector"/> for EventSource
    /// that can already exist or don't exist yet.
    /// </summary>
    public static class DotNetEventSourceConfigurator
    {
        static readonly object _lock = new object();
        static IDisposable? _current;

        /// <summary>
        /// Applies a new configuration to <see cref="DotNetEventSourceCollector"/>.
        /// <para>
        /// The <paramref name="configuration"/> is rather simple: <c>"System.Threading.Tasks.TplEventSource:C[ritical];System.Net.Sockets:!"</c>.
        /// It is enough for the <see cref="EventLevel"/> be the first character:
        /// <list type="bullet">
        ///     <item>'L' or 'l' for <see cref="EventLevel.LogAlways"/>.</item>
        ///     <item>'C' or 'c' for <see cref="EventLevel.Critical"/>.</item>
        ///     <item>'E' or 'e' for <see cref="EventLevel.Error"/>.</item>
        ///     <item>'W' or 'w' for <see cref="EventLevel.Warning"/>.</item>
        ///     <item>'I' or 'i' for <see cref="EventLevel.Informational"/>.</item>
        ///     <item>'V' or 'v' for <see cref="EventLevel.Verbose"/>.</item>
        ///     <item>'!' to disable the EventSource.</item>
        /// </list>
        /// If the level is not specified or is not one of these characters, <see cref="EventLevel.Informational"/> is assumed.
        /// </para>
        /// <para>
        /// The configuration applies until a new one is applied (the creation of new EventSources is tracked thanks to <see cref="DotNetEventSourceCollector.OnNewEventSource"/>).
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use. When null, <see cref="ActivityMonitor.StaticLogger"/> is used.</param>
        /// <param name="configuration">The configuration string: semi colon separated EventSource names suffixed by ":<see cref="EventLevel"/>" or ":!" for disabled ones.</param>
        public static void ApplyConfiguration( IActivityMonitor? monitor, string configuration )
        {
            var logger = (IActivityLineEmitter?)monitor ?? ActivityMonitor.StaticLogger;
            if( logger.ShouldLogLine( LogLevel.Info, null, out var finalTags ) )
            {
                logger.UnfilteredLog( LogLevel.Info | LogLevel.IsFiltered, finalTags, $"Applying .Net EventSource configuration: '{configuration}'.", null );
            }
            var (names, levels) = CreateConfig( monitor, configuration );
            lock( _lock )
            {
                _current?.Dispose();
                _current = new Current( names, levels );
            }
        }

        /// <summary>
        /// Gets a configuration string that can be applied later by calling <see cref="ApplyConfiguration(IActivityMonitor?, string)"/>.
        /// </summary>
        /// <param name="enabled">
        /// By default both enabled and disabled EventSources are returned. True to only consider the enabled ones and
        /// false to only return the disabled ones.</param>
        /// <returns>The configuration string.</returns>
        public static string GetConfiguration( bool? enabled = null )
        {
            StringBuilder b = new StringBuilder();
            bool withEnabled = enabled ?? true;
            bool withDisabled = !enabled ?? true;
            foreach( var (name,level) in DotNetEventSourceCollector.GetSources() )
            {
                if( (level.HasValue && withEnabled) || (!level.HasValue && withDisabled) )
                {
                    if( b.Length > 0 ) b.Append( ';' );
                    b.Append( name ).Append( ':' );
                    b.Append( level switch
                    {
                        null => '!',
                        EventLevel.LogAlways => 'L',
                        EventLevel.Critical => 'C',
                        EventLevel.Error => 'E',
                        EventLevel.Warning => 'W',
                        EventLevel.Informational => 'I',
                        _ => 'V'
                    } );
                }
            }
            return b.ToString();
        }


        static (string[] Names, int[] Levels) CreateConfig( IActivityMonitor? monitor, string configuration )
        {
            var e = configuration.Split( ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
            var names = new string[e.Length];
            var levels = new int[e.Length];
            for( int i = 0; i < e.Length; ++i )
            {
                var name = e[i];
                int iC = name.LastIndexOf( ':' );
                int level;
                // No level defaults to Informational (Verbose is too much!).
                if( iC < 0 || iC == name.Length - 1 )
                {
                    // Silently allow '!' only.
                    if( name[name.Length - 1] == '!' )
                    {
                        level = -1;
                        iC = name.Length - 1;
                    }
                    else
                    {
                        level = -2;
                    }
                }
                else
                {
                    int iL = iC + 1;
                    while( iL < name.Length && char.IsWhiteSpace( name, iL ) ) ++iL;
                    if( iL == name.Length ) level = -2;
                    else level = name[iL] switch
                    {
                        '!' => -1,
                        'L' or 'l' => (int)EventLevel.LogAlways,
                        'C' or 'c' => (int)EventLevel.Critical,
                        'E' or 'e' => (int)EventLevel.Error,
                        'W' or 'w' => (int)EventLevel.Warning,
                        'I' or 'i' => (int)EventLevel.Informational,
                        'V' or 'v' => (int)EventLevel.Verbose,
                        _ => -3
                    };
                }
                while( iC > 0 && char.IsWhiteSpace( name[iC - 1] ) ) --iC;
                // Don't care if an empty or invalid name happens, we'll never
                // match it and this is fine.
                names[i] = iC >= 0 ? name.Substring( 0, iC ) : name;
                if( level < -1 || name.Length == 0 )
                {
                    string warn;
                    if( name.Length == 0 ) warn = "Missing EventSource name in configuration. This is ignored.";
                    else
                    {
                        warn = $"{(level == -2 ? "Missing" : "Unrecognized")} level specification for EventSource '{name}', using Informational by default. "
                               + $"Levels can be: \"{name}:L[ogAlways]\" or :C[ritical], :E[rror], :W[arning], :I[nformational], !V[erbose] or :! (disabled).";
                    }
                    var logger = (IActivityLineEmitter?)monitor ?? ActivityMonitor.StaticLogger;
                    if( logger.ShouldLogLine( LogLevel.Warn, null, out var finalTags ) )
                    {
                        logger.UnfilteredLog( LogLevel.Warn | LogLevel.IsFiltered, finalTags, warn, null );
                    }
                }
                levels[i] = level;
            }
            return (names, levels);
        }


        sealed class Current : IDisposable
        {
            // Even if the EventSource has a global lock, we replicate here the
            // protection implemented in the STaticGatesConfigurator. We need it at least
            // in the constructor: if a new DotNetEventSourceConfigurator is built while a
            // new source appear then we may have a race condition.
            // It is safer and we have NO performance issue here.
            readonly string[] _names;
            readonly int[] _levels;

            public Current( string[] names, int[] levels )
            {
                _names = names;
                _levels = levels;
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

            public void Dispose()
            {
                DotNetEventSourceCollector.OnNewEventSource -= Configure;
            }
        }
    }
}
