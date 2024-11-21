using System.Collections.Concurrent;
using System.Threading;
using System;

namespace CK.Core;

public sealed partial class ActivityMonitorExternalLogData
{
    /// <summary>
    /// Gets the current pool capacity. It starts at 200 and increases (with a warning) until <see cref="MaximalCapacity"/>
    /// is reached (where errors are emitted).
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

    /// <summary>
    /// Gets the current number of <see cref="ActivityMonitorExternalLogData"/> that are alive (not yet released).
    /// When there is no log activity and no entries have been cached, this must be 0.
    /// </summary>
    public static int AliveCount => _aliveItems;

    /// <summary>
    /// Gets the current number of cached entries.
    /// This is an approximate value because of concurency.
    /// </summary>
    public static int PooledEntryCount => _numItems + (_fastItem != null ? 1 : 0);

    static readonly ConcurrentQueue<ActivityMonitorExternalLogData> _items = new();
    static ActivityMonitorExternalLogData? _fastItem;
    static int _numItems;
    static int _currentCapacity = 200;
    static int _aliveItems;
    static long _nextPoolError;

    internal static ActivityMonitorExternalLogData CreateNonPooled( ref ActivityMonitorLogData data )
    {
        var e = new ActivityMonitorExternalLogData();
        e.Initialize( ref data, false );
        return e;
    }

    internal static ActivityMonitorExternalLogData Acquire( ref ActivityMonitorLogData data )
    {
        Interlocked.Increment( ref _aliveItems );
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
        item.Initialize( ref data, true );
        return item;
    }

    static void Release( ActivityMonitorExternalLogData c )
    {
        Interlocked.Decrement( ref _aliveItems );
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
                // Signals the error but no more than once per second.
                var next = _nextPoolError;
                var nextNext = Environment.TickCount64;
                if( next < nextNext && Interlocked.CompareExchange( ref _nextPoolError, nextNext + 1000, next ) == next )
                {
                    ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Error | LogLevel.IsFiltered,
                                                                ActivityMonitor.Tags.ToBeInvestigated,
                                                                $"The log data pool reached its maximal capacity of {MaximalCapacity}. This may indicate a peak of activity " +
                                                                $"or a leak (missing ActivityMonitorExternalLogData.Release() calls) if this error persists.", null );
                }
            }
            else
            {
                int newCapacity = Interlocked.Add( ref _currentCapacity, PoolCapacityIncrement );
                ActivityMonitor.StaticLogger.UnfilteredLog( LogLevel.Warn, null, $"The log data pool has been increased to {newCapacity}.", null );
                _items.Enqueue( c );
            }
        }
    }

}
