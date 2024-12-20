using System.Collections.Generic;
using CK.Core.Impl;

namespace CK.Core;

/// <summary>
/// Base class for <see cref="IActivityMonitorClient"/> that tracks groups and level changes in order
/// to ease text-based renderer implementations.
/// </summary>
public abstract class ActivityMonitorTextHelperClient : IActivityMonitorFilteredClient
{
    int _curLevel;
    LogClamper _filter;
    readonly Stack<bool> _openGroups;
    IActivityMonitorImpl? _source;
    static string[] _prefixGroupDepthCache;
    const string _emptyLinePrefix = "| ";

    static ActivityMonitorTextHelperClient()
    {
        // Preload cache with 20 depths.
        _prefixGroupDepthCache = new string[20];
        _prefixGroupDepthCache[0] = "";
        for( int i = 1; i < _prefixGroupDepthCache.Length; i++ )
        {
            _prefixGroupDepthCache[i] = _prefixGroupDepthCache[i - 1] + _emptyLinePrefix;
        }
    }

    /// <summary>
    /// Gets the group prefix (cached) for a certain depth.
    /// </summary>
    /// <param name="depth">Depth of the group. Must be in [0, 1024[.</param>
    /// <returns>The string to display the indented group.</returns>
    public static string GetMultilinePrefixWithDepth( int depth )
    {
        Throw.CheckArgument( depth >= 0 && depth <= 1024 );
        if( _prefixGroupDepthCache.Length < depth + 1 )
        {
            string previousPrefix = GetMultilinePrefixWithDepth( depth - 1 );
            Util.InterlockedAdd( ref _prefixGroupDepthCache, previousPrefix + _emptyLinePrefix );
        }
        return _prefixGroupDepthCache[depth];
    }

    /// <summary>
    /// Initialize a new <see cref="ActivityMonitorTextHelperClient"/> with a filter.
    /// </summary>
    protected ActivityMonitorTextHelperClient( LogClamper filter )
    {
        _curLevel = -1;
        _openGroups = new Stack<bool>();
        _filter = filter;
    }

    /// <summary>
    /// Initialize a new <see cref="ActivityMonitorTextHelperClient"/> without any filter.
    /// </summary>
    protected ActivityMonitorTextHelperClient()
        : this( LogClamper.Undefined )
    {
    }

    void IActivityMonitorClient.OnUnfilteredLog( ref ActivityMonitorLogData data )
    {
        var level = data.MaskedLevel;

        if( !CanOutputLine( level ) )
        {
            return;
        }

        if( data.Text == ActivityMonitor.ParkLevel )
        {
            if( _curLevel != -1 )
            {
                OnLeaveLevel( (LogLevel)_curLevel );
            }
            _curLevel = -1;
        }
        else
        {
            if( _curLevel == (int)level )
            {
                OnContinueOnSameLevel( ref data );
            }
            else
            {
                if( _curLevel != -1 )
                {
                    OnLeaveLevel( (LogLevel)_curLevel );
                }
                OnEnterLevel( ref data );
                _curLevel = (int)level;
            }
        }
    }

    void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
    {
        if( _curLevel != -1 )
        {
            OnLeaveLevel( (LogLevel)_curLevel );
            _curLevel = -1;
        }
        if( !CanOutputGroup( group.Data.MaskedLevel ) )
        {
            _openGroups.Push( false );
            return;
        }
        _openGroups.Push( true );
        OnGroupOpen( group );
    }

    void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
    {
    }

    void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
    {
        if( _curLevel != -1 )
        {
            OnLeaveLevel( (LogLevel)_curLevel );
            _curLevel = -1;
        }
        // Has the Group actually been opened?
        if( _openGroups.Count > 0 && _openGroups.Pop() ) OnGroupClose( group, conclusions );
    }

    /// <summary>
    /// Called for the first text of a <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="data">Log data.</param>
    protected abstract void OnEnterLevel( ref ActivityMonitorLogData data );

    /// <summary>
    /// Called for text with the same <see cref="LogLevel"/> as the previous ones.
    /// </summary>
    /// <param name="data">Log data.</param>
    protected abstract void OnContinueOnSameLevel( ref ActivityMonitorLogData data );

    /// <summary>
    /// Called when current log level changes.
    /// </summary>
    /// <param name="level">The previous log level (without <see cref="LogLevel.IsFiltered"/>).</param>
    protected abstract void OnLeaveLevel( LogLevel level );

    /// <summary>
    /// Called whenever a group is opened.
    /// </summary>
    /// <param name="group">The newly opened group.</param>
    protected abstract void OnGroupOpen( IActivityLogGroup group );

    /// <summary>
    /// Called when the group is actually closed.
    /// </summary>
    /// <param name="group">The closing group.</param>
    /// <param name="conclusions">Texts that concludes the group. Never null but can be empty.</param>
    protected abstract void OnGroupClose( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions );


    void IActivityMonitorClient.OnTopicChanged( string newTopic, string? fileName, int lineNumber )
    {
    }

    void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
    {
    }

    /// <inheritdoc />
    public LogClamper MinimalFilter
    {
        get { return _filter; }
        set
        {
            _filter = value;
            _source?.SignalChange();
        }
    }

    bool CanOutputLine( LogLevel logLevel )
    {
        Throw.DebugAssert( (logLevel & LogLevel.IsFiltered) == 0, "The level must already be masked." );
        return !_filter.Clamp || (int)logLevel >= (int)_filter.Filter.Line;
    }

    bool CanOutputGroup( LogLevel logLevel )
    {
        Throw.DebugAssert( (logLevel & LogLevel.IsFiltered) == 0, "The level must already be masked." );
        return !_filter.Clamp || (int)logLevel >= (int)_filter.Filter.Group;
    }

    #region IActivityMonitorBoundClient Members

    LogFilter IActivityMonitorBoundClient.MinimalFilter => _filter.Filter;

    void IActivityMonitorBoundClient.SetMonitor( Impl.IActivityMonitorImpl? source, bool forceBuggyRemove )
    {
        if( !forceBuggyRemove )
        {
            if( source != null && _source != null ) ActivityMonitorClient.ThrowMultipleRegisterOnBoundClientException( this );
        }
        _openGroups.Clear();
        _source = source;
    }

    bool IActivityMonitorBoundClient.IsDead => false;

    #endregion
}
