using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace CK.Core
{
    /// <summary>
    /// Helper that can configure any number of <see cref="StaticGate"/> that must have a <see cref="StaticGate.DisplayName"/>
    /// and can already exist or don't exist yet.
    /// </summary>
    public static class StaticGateConfigurator
    {
        static readonly object _lock = new object();
        static IDisposable? _current;

        /// <summary>
        /// Applies a new configuration to <see cref="StaticGate"/> that must have a real display name - <see cref="StaticGate.HasDisplayName"/>
        /// must be true - gates without real display name are ignored.
        /// <para>
        /// The <paramref name="configuration"/> is rather simple: <c>"AsyncLock;LowLevelStuff;VeryLowLevelStuff:!"</c> will
        /// open the first two and close the "VeryLowLevelStuff" gate.
        /// </para>
        /// <para>
        /// The configuration applies until a new one is applied (the creation of new gates is tracked thanks to <see cref="StaticGate.OnNewStaticGate"/>).
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor to use. When null, <see cref="ActivityMonitor.StaticLogger"/> is used.</param>
        /// <param name="configuration">The configuration string: semi colon separated display names to activate or suffixed with ":!" to deactivate them.</param>
        public static void ApplyConfiguration( IActivityMonitor? monitor, string configuration )
        {
            var (names, states) = CreateConfig( configuration );
            var logger = monitor ?? ActivityMonitor.StaticLogger;
            if( logger.ShouldLogLine( LogLevel.Info, null, out var finalTags ) )
            {
                logger.UnfilteredLog( LogLevel.Info | LogLevel.IsFiltered, finalTags, $"Applying StaticGate configuration: '{configuration}'.", null );
            }
            lock( _lock )
            {
                _current?.Dispose();
                _current = new Current( names, states );
            }
        }

        /// <summary>
        /// Gets a configuration string that can be applied later by calling <see cref="ApplyConfiguration(IActivityMonitor?, string)"/>.
        /// </summary>
        /// <param name="openedGates">
        /// By default both opened and closed gates are returned. True to only consider the opened gates and
        /// false to only return the closed ones (suffixed by ":!").</param>
        /// <returns>The configuration string.</returns>
        public static string GetConfiguration( bool? openedGates = null )
        {
            StringBuilder b = new StringBuilder();
            bool withOpen = openedGates ?? true;
            bool withClosed = !openedGates ?? true;
            foreach( var g in StaticGate.GetStaticGates() )
            {
                if( g.HasDisplayName )
                {
                    bool open = g.IsOpen;
                    if( (open && withOpen) || (!open && withClosed) )
                    {
                        if( b.Length > 0 ) b.Append( ';' );
                        b.Append( g.DisplayName );
                        if( !open ) b.Append( ":!" );
                    }
                }
            }
            return b.ToString();
        }

        static (string[] Names, bool[] States) CreateConfig( string configuration )
        {
            var e = configuration.Split( ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
            var names = new string[e.Length];
            var states = new bool[e.Length];
            for( int i = 0; i < e.Length; ++i )
            {
                var name = e[i];
                if( name[0] == '!' )
                {
                    // Allow "!Gate".
                    names[i] = name.Substring( 1 );
                }
                else if( name[name.Length - 1] == '!' )
                {
                    // Allow "Gate!", but the documented way of doing this is "Gate:!"
                    // to match the behavior of "EventSourceName:!" that disables an EventSource.
                    int iC = name.LastIndexOf( ':' );
                    if( iC >= 0 )
                    {
                        while( iC > 0 && char.IsWhiteSpace( name[iC - 1] ) ) --iC;
                    }
                    else iC = name.Length - 1;
                    names[i] = name.Substring( 0, iC );
                }
                else
                {
                    states[i] = true;
                    names[i] = name;
                }
            }
            return (names, states);
        }

        sealed class Current : IDisposable
        {

            // We need a lock here because the OnNewStaticGate is raised outside
            // of any lock. We use the _names array as the lock object.
            readonly string[] _names;
            readonly bool[] _states;

            public Current( string[] names, bool[] states )
            {
                _names = names;
                _states = states;
                lock( _names )
                {
                    StaticGate.OnNewStaticGate += Configure;
                    foreach( var g in StaticGate.GetStaticGates() )
                    {
                        DoConfigure( g );
                    }
                }
            }

            void Configure( StaticGate g )
            {
                lock( _names ) DoConfigure( g );
            }

            void DoConfigure( StaticGate g )
            {
                Debug.Assert( Monitor.IsEntered( _names ) );
                if( g.HasDisplayName )
                {
                    int idx = Array.IndexOf( _names, g.DisplayName );
                    if( idx >= 0 )
                    {
                        g.IsOpen = _states[idx];
                    }
                }
            }

            public void Dispose()
            {
                StaticGate.OnNewStaticGate -= Configure;
            }
        }

    }
}
