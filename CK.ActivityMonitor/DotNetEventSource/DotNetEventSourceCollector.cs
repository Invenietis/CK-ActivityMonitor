using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using static CK.Core.CheckedWriteStream;

namespace CK.Core
{
    /// <summary>
    /// Enables logging to the <see cref="ActivityMonitor.StaticLogger"/> of <see cref="EventSource"/> events.
    /// Available registered sources can be listed and each of them can be enabled or disabled.
    /// <para>
    /// This class intentionally totally hides the EventSource object (and the private EventListener): only
    /// source's names are relevant and are exposed.
    /// </para>
    /// </summary>
    public static class DotNetEventSourceCollector
    {
        // Basic concurrency handling here: the Listener is used as a lock.
        static (WeakReference<EventSource>? S, int L)[] _allEventSources;
        static readonly Listener _listener;

        /// <summary>
        /// Gets the tag that will be set on all logs from <see cref="EventSource"/>.
        /// </summary>
        public static readonly CKTrait EventSourceTag;

        static DotNetEventSourceCollector()
        {
            EventSourceTag = ActivityMonitor.Tags.Register( nameof( EventSource ) );

            // This takes the global internal EventListener.EventListenersLock
            // and creates a List<EventSource> snapshot.
            _allEventSources = EventSource.GetSources()
                                          .Select( e => (new WeakReference<EventSource>( e ), -1) )
                                          .ToArray()!;
            
            // This takes again the global internal EventListener.EventListenersLock
            // and calls the OnEventSourceCreated with the sources. We may here skip
            // the update to the _allEventSources (with a toggle) but this would introduce
            // a race condition: it is safer to re-update the initial array. 
            _listener = new Listener();
        }

        /// <summary>
        /// Provides a hook that is called when a new EventSource is available.
        /// This is called before its registration in <see cref="GetSources()"/> list.
        /// </summary>
        public static event Action<string>? OnNewEventSource;

        /// <summary>
        /// Disables an EventSource by its name.
        /// </summary>
        /// <returns>True if the operation has been applied, false otherwise.</returns>
        public static bool Disable( string eventSourceName )
        {
            lock( _listener )
            {
                for( int i = 0; i < _allEventSources.Length; ++i )
                {
                    ref var wS = ref _allEventSources[i];
                    if( wS.S != null && wS.S.TryGetTarget( out var s ) && s.Name == eventSourceName )
                    {
                        wS.L = -1;
                        _listener.DisableEvents( s );
                        return true;
                    };
                }
                return false;
            }
        }

        /// <summary>
        /// Enables an EventSource by its name at a given level.
        /// </summary>
        /// <param name="level">The level of events to enable.</param>
        /// <returns>True if the operation has been applied, false otherwise.</returns>
        public static bool Enable( string eventSourceName, EventLevel level )
        {
            lock( _listener )
            {
                for( int i = 0; i < _allEventSources.Length; ++i )
                {
                    ref var wS = ref _allEventSources[i];
                    if( wS.S != null && wS.S.TryGetTarget( out var s ) && s.Name == eventSourceName )
                    {
                        wS.L = (int)level;
                        _listener.EnableEvents( s, level );
                        return true;
                    };
                }
                return false;
            }
        }

        /// <summary>
        /// Disables all existing sources at once.
        /// </summary>
        /// <returns>The number of sources that were enabled and have been disabled.</returns>
        public static int DisableAll()
        {
            int num = 0;
            lock( _listener )
            {
                for( int i = 0; i < _allEventSources.Length; ++i )
                {
                    ref var wS = ref _allEventSources[i];
                    if( wS.S != null && wS.S.TryGetTarget( out var s ) && wS.L >= 0 )
                    {
                        ++num;
                        wS.L = -1;
                        _listener.DisableEvents( s );
                    };
                }
            }
            return num;
        }

        /// <summary>
        /// Gets the list of all <see cref="EventSource"/> names and their current
        /// enabled level.
        /// </summary>
        /// <returns>A list of source name and their level.</returns>
        public static IReadOnlyList<(string Name, EventLevel? Level)> GetSources()
        {
            List<(string, EventLevel?)> result = new List<(string, EventLevel?)>();
            // No need to lock here. The capture is enough, the resulting Level
            // is what it is.
            foreach( var (S, L) in _allEventSources )
            {
                if( S != null && S.TryGetTarget( out var s ) )
                {
                    result.Add( (s.Name, L >= 0 ? (EventLevel)L : null) );
                }
            }
            return result;
        }

        /// <summary>
        /// Gets the <see cref="EventLevel"/> for a source.
        /// </summary>
        /// <param name="name">The EventSource name.</param>
        /// <param name="found">True if the source name exists, false otherwise.</param>
        /// <returns>The level or null if the source is disabled or not found.</returns>
        public static EventLevel? GetLevel( string name, out bool found )
        {
            found = false;  
            foreach( var (S, L) in _allEventSources )
            {
                if( S != null && S.TryGetTarget( out var s ) && s.Name == name )
                {
                    found = true;
                    return L >= 0 ? (EventLevel)L : null;
                }
            }
            return null;
        }

        sealed class Listener : EventListener
        {
            protected override void OnEventSourceCreated( EventSource eventSource )
            {
                // Don't call the base since we don't use/need the EventSourceCreated public event.
                var idx = EventSourceIndex( eventSource );
                if( _listener != null )
                {
                    OnNewEventSource?.Invoke( eventSource.Name );
                    lock( this ) AddEventSource( eventSource, idx );
                }
                else
                {
                    AddEventSource( eventSource, idx );
                }

                static void AddEventSource( EventSource eventSource, int idx )
                {
                    if( idx >= _allEventSources.Length )
                    {
                        var newArray = new (WeakReference<EventSource>?, int)[idx + 2];
                        Array.Copy( _allEventSources, 0, newArray, 0, _allEventSources.Length );
                        newArray[idx] = (new WeakReference<EventSource>( eventSource ), 0);
                        _allEventSources = newArray;
                    }
                    else
                    {
                        var wRef = _allEventSources[idx].S;
                        if( wRef == null ) _allEventSources[idx].S = new WeakReference<EventSource>( eventSource );
                        else wRef.SetTarget( eventSource );
                    }
                }
            }

            protected override void OnEventWritten( EventWrittenEventArgs eventData )
            {
                // Don't call the base since we don't use/need the EventWritten public event.

                // We consider the LogLevel to already be filtered since we have been called.
                var level = eventData.Level switch
                {
                    EventLevel.Critical => LogLevel.Fatal | LogLevel.IsFiltered,
                    EventLevel.Error => LogLevel.Error | LogLevel.IsFiltered,
                    EventLevel.Warning => LogLevel.Warn | LogLevel.IsFiltered,
                    EventLevel.Informational => LogLevel.Info | LogLevel.IsFiltered,
                    EventLevel.Verbose => LogLevel.Trace | LogLevel.IsFiltered,
                    // EventLevel.LogAlways is a "no level" (it is stronger than Critical) that indicates
                    // that the entry must always be logged (but without any "level"!).
                    // This (badly!) mirrors our LogLevel.IsFiltered bit but since there is no level, we
                    // have to choose one. We consider it as informational.
                    EventLevel.LogAlways => LogLevel.Info | LogLevel.IsFiltered,
                    // To be safe, if level is not valid, use Debug level.
                    _ => LogLevel.Debug | LogLevel.IsFiltered
                };
                var b = new StringBuilder();
                b.Append('[').Append( eventData.EventSource.Name ).Append(':').Append( eventData.EventId ).Append(']');
                if( !string.IsNullOrEmpty( eventData.EventName ) )
                {
                    b.Append( " EventName='" ).Append( eventData.EventName ).Append( '\'' );
                }
                if( !string.IsNullOrEmpty( eventData.Message ) )
                {
                    b.Append( " Message='" ).Append( eventData.Message.Replace( "'", "''" ) ).Append( '\'' );
                }
                // The payload can be totally buggy (more or less data than names). This it reported to the Debugger.Log (a debugger
                // must be attached), but the payload is emitted as-is.
                if( eventData.PayloadNames != null && eventData.Payload != null )
                {
                    var len = Math.Min( eventData.Payload.Count, eventData.PayloadNames.Count );
                    if( len > 0 )
                    {
                        b.Append( " Payload={ " );
                        for( int i = 0; i < len; i++ )
                        {
                            b.Append( eventData.PayloadNames[i] ).Append( '=' );
                            var p = eventData.Payload[i];
                            if( p == null ) b.Append( "null" );
                            else
                            {
                                try
                                {
                                    var pS = p.ToString();
                                    b.Append( '\'' ).Append( pS ).Append( '\'' );
                                }
                                catch
                                {
                                    b.Append( "ToStringFailure(" ).Append( p.GetType().ToCSharpName() ).Append( ')' );
                                }
                            }
                            b.Append( ' ' );
                        }
                        b.Append( '}' );
                    }
                }
                if( eventData.Keywords != EventKeywords.None ) b.Append( " Keywords='" ).Append( eventData.Keywords ).Append( '\'' );
                if( eventData.Channel != EventChannel.None ) b.Append( " Channel='" ).Append( eventData.Channel ).Append( '\'' );
                b.Append( " OpCode='" ).Append( eventData.Opcode ).Append( '\'' );
                if( eventData.Task == EventTask.None ) b.Append( " Task='" ).Append( eventData.Task ).Append( '\'' );

                ActivityMonitor.StaticLogger.UnfilteredLog( level, EventSourceTag, b.ToString() );
            }
        }
    }
}
