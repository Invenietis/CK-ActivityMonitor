using System;
using System.Diagnostics;
using System.Threading;

namespace CK.Core
{
    /// <summary>
    /// Helper that can configure any number of <see cref="StaticGate"/> that must have a <see cref="StaticGate.DisplayName"/>
    /// and can already exist or don't exist yet (as long as the configurator is not disposed).
    /// <para>
    /// The configuration is applied at construction time and the creation of new gates is tracked
    /// thanks to <see cref="StaticGate.OnNewStaticGate"/>.
    /// </para>
    /// </summary>
    public sealed class StaticGatesConfigurator : IDisposable
    {
        // We need a lock here because the OnNewStaticGate is raised outside
        // of any lock. We use the _names array as the lock object.
        readonly string[] _names;
        readonly bool[] _states;

        /// <summary>
        /// Applies a new configuration to <see cref="StaticGate"/> that must have a real display name - <see cref="StaticGate.HasDisplayName"/>
        /// must be true - gates without real display name are ignored.
        /// <para>
        /// The <paramref name="configuration"/> is rather simple: "Archive.Manager.TraceAll;LowLevelStuff;VeryLowLevelStuff:!" will
        /// open the first two and close the "VeryLowLevelStuff" gate.
        /// </para>
        /// </summary>
        /// <param name="configuration">The configuration string: semi colon separated display name to activate or suffixed with ":!" to deactivate them.</param>
        public StaticGatesConfigurator( string configuration )
        {
            (_names, _states) = CreateConfig( configuration );
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

        static (string[] Names, bool[] States) CreateConfig( string configuration )
        {
            var e = configuration.Split( ';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries );
            var n = new string[e.Length];
            var s = new bool[e.Length];
            for( int i = 0; i < e.Length; ++i )
            {
                var name = e[i];
                if( name[0] == '!' )
                {
                    // Allow "!Gate".
                    n[i] = name.Substring( 1 );
                }
                else if( name[name.Length-1] == '!' )
                {
                    // Allow "Gate!", but the documented way of doing this is "Gate:!"
                    // to match the behavior of "EventSourceName:!" that disables an EventSource.
                    int iC = name.LastIndexOf( ':' );
                    if( iC >= 0 )
                    {
                        while( iC > 0 && char.IsWhiteSpace( name[iC - 1] ) ) --iC;
                    }
                    else iC = name.Length - 1;
                    n[i] = name.Substring( 0, iC );
                }
                else
                {
                    s[i] = true;
                    n[i] = name;
                }
            }
            return (n, s);
        }

        public void Dispose()
        {
            StaticGate.OnNewStaticGate -= Configure;
        }
    }

}
