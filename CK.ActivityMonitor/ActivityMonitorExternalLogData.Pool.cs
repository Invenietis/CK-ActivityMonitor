using System.Collections.Concurrent;
using System.Threading;
using System;

namespace CK.Core
{
    public sealed partial class ActivityMonitorExternalLogData
    {
        /// <summary>
        /// Tags for warning and errors related to <see cref="CurrentPoolCapacity"/>.
        /// </summary>
        public static CKTrait LogDataPoolAlertTag = ActivityMonitor.Tags.Register( "ActivityMonitorLogDataPoolAlert" );

        /// <summary>
        /// Gets the current pool capacity. It starts at 200 and increases (with a warning) until <see cref="MaximalCapacity"/>
        /// is reached (where errors are emitted). Warnings and errors are tagged with <see cref="LogDataPoolAlertTag"/>.
        /// </summary>
        public static int CurrentPoolCapacity => _currentCapacity;

        /// <summary>
        /// The current pool capacity increment until <see cref="CurrentPoolCapacity"/> reaches <see cref="MaximalCapacity"/>.
        /// </summary>
        public const int PoolCapacityIncrement = 10;

        /// <summary>
        /// The maximal capacity. Once reached, newly acquired <see cref="ActivityMonitorExternalLogData"/> are garbage
        /// collected instead of returned to the pool.
        /// </summary>
        public const int MaximalCapacity = 2000;

        static readonly ConcurrentQueue<ActivityMonitorExternalLogData> _items = new();
        static ActivityMonitorExternalLogData? _fastItem;
        static int _numItems;
        static int _currentCapacity = 200;
        static DateTime _nextPoolError;

        internal static ActivityMonitorExternalLogData Acquire( ref ActivityMonitorLogData data )
        {
            var item = _fastItem;
            if( item == null || Interlocked.CompareExchange( ref _fastItem, null, item ) != item )
            {
                if( _items.TryDequeue( out item ) )
                {
                    Interlocked.Decrement( ref _numItems );
                }
                else
                {
                    item = new ActivityMonitorExternalLogData();
                }
            }
            item.Initialize( ref data );
            return item;
        }

        static void Release( ActivityMonitorExternalLogData c )
        {
            if( _fastItem != null || Interlocked.CompareExchange( ref _fastItem, c, null ) != null )
            {
                int poolCount = Interlocked.Increment( ref _numItems );
                // Strictly lower than to account for the _fastItem.
                if( poolCount < _currentCapacity )
                {
                    _items.Enqueue( c );
                    return;
                }
                // Current capacity is reached. Increasing it and emits a warning.
                // If the count reaches the MaximalCapacity, emits an error and don't increase the
                // limit anymore: log data will be garbage collected. If this error persists, it indicates a leak somewhere!
                if( poolCount >= MaximalCapacity )
                {
                    // Adjust the pool count.
                    Interlocked.Decrement( ref _numItems );
                    // Signals the error continuously once per second.
                    var now = DateTime.UtcNow;
                    if( _nextPoolError < now )
                    {
                        _nextPoolError = now.AddSeconds( 1 );
                        ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Error,
                                                                    LogDataPoolAlertTag,
                                                                    $"The log data pool reached its maximal capacity of {MaximalCapacity}. This may indicate a peak of activity " +
                                                                    $"or a leak (missing ActivityMonitorExternalLogData.Release() calls) if this error persists.", null );
                    }
                }
                else
                {
                    int newCapacity = Interlocked.Add( ref _currentCapacity, PoolCapacityIncrement );
                    ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Warn, LogDataPoolAlertTag, $"The log data pool has been increased to {newCapacity}.", null );
                }
            }
        }

    }
}