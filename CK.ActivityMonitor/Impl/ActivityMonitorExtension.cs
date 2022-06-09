using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace CK.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="IActivityMonitor"/> and other types from the Activity monitor framework.
    /// </summary>
    public static partial class ActivityMonitorExtension
    {
        /// <summary>
        /// Returns a valid <see cref="DateTimeStamp"/> that will be used for a log: it is based on <see cref="DateTime.UtcNow"/> and has 
        /// a <see cref="DateTimeStamp.Uniquifier"/> that will not be changed when emitting the next log.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <returns>The next log time for the monitor.</returns>
        public static DateTimeStamp NextLogTime( this IActivityMonitor @this )
        {
            return new DateTimeStamp( @this.LastLogTime, DateTime.UtcNow );
        }

        /// <summary>
        /// Closes all opened groups and sends an unfiltered <see cref="ActivityMonitor.Tags.MonitorEnd"/> log.
        /// </summary>
        /// <param name="text">Optional log text (defaults to "Done.").</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        public static void MonitorEnd( this IActivityMonitor @this, string? text = null, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            while( @this.CloseGroup() ) ;
            @this.UnfilteredLog( LogLevel.Info, ActivityMonitor.Tags.MonitorEnd, text ?? "Done.", null, fileName, lineNumber );
        }

        /// <summary>
        /// Challenges tag minimal filters, <see cref="IActivityMonitor.ActualFilter">this monitors' actual filter</see> and application 
        /// domain's <see cref="ActivityMonitor.DefaultFilter"/> filters to test whether a log line should actually be emitted.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">Log level.</param>
        /// <param name="tags">Optional tags on the line.</param>
        /// <param name="finalTags">Combined monitor's and line's tag.</param>
        /// <returns>True if the log should be emitted.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]  
        public static bool ShouldLogLine( this IActivityMonitor @this, LogLevel level, CKTrait? tags, out CKTrait finalTags )
        {
            finalTags = @this.AutoTags + tags;
            return ActivityMonitor.Tags.ApplyForLine( (int)level, finalTags, (int)@this.ActualFilter.Line );
        }

        /// <summary>
        /// Challenges tag minimal filters, <see cref="IActivityMonitor.ActualFilter">this monitors' actual filter</see> and application 
        /// domain's <see cref="ActivityMonitor.DefaultFilter"/> filters to test whether a log line should actually be emitted.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">Log level.</param>
        /// <param name="tags">Optional tags on the group.</param>
        /// <param name="finalTags">Combined monitor's and group's tag.</param>
        /// <returns>True if the log should be emitted.</returns>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static bool ShouldLogGroup( this IActivityMonitor @this, LogLevel level, CKTrait? tags, out CKTrait finalTags )
        {
            finalTags = @this.AutoTags + tags;
            return ActivityMonitor.Tags.ApplyForGroup( finalTags, (int)@this.ActualFilter.Group, (int)level );
        }

        /// <summary>
        /// Logs a text regardless of <see cref="IActivityMonitor.ActualFilter">ActualFilter</see> level. 
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="tags">
        /// Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. 
        /// These tags will be union-ed with the current <see cref="IActivityMonitor.AutoTags">AutoTags</see>.
        /// </param>
        /// <param name="level">Log level. Must not be <see cref="LogLevel.None"/>.</param>
        /// <param name="text">Text to log. Must not be null or empty.</param>
        /// <param name="ex">Optional exception associated to the log. When not null, a Group is automatically created.</param>
        /// <param name="fileName">The source code file name from which the log is emitted.</param>
        /// <param name="lineNumber">The line number in the source from which the log is emitted.</param>
        /// <remarks>
        /// The <paramref name="text"/> can not be null or empty.
        /// <para>
        /// Each call to log is considered as a unit of text: depending on the rendering engine, a line or a 
        /// paragraph separator (or any appropriate separator) should be appended between each text if 
        /// the <paramref name="level"/> is the same as the previous one.
        /// </para>
        /// <para>If needed, the special text <see cref="ActivityMonitor.ParkLevel"/> ("PARK-LEVEL") can be used as a convention 
        /// to break the current <see cref="LogLevel"/> and resets it: the next log, even with the same LogLevel, should be 
        /// treated as if a different LogLevel is used.
        /// </para>
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void UnfilteredLog( this IActivityMonitor @this,
                                          LogLevel level,
                                          CKTrait? tags,
                                          string? text,
                                          Exception? ex,
                                          [CallerFilePath] string? fileName = null,
                                          [CallerLineNumber] int lineNumber = 0 )
        {
            var d = new ActivityMonitorLogData( level, @this.AutoTags | tags, text, ex, fileName, lineNumber );
            @this.UnfilteredLog( ref d );
        }

        /// <summary>
        /// Opens a group regardless of <see cref="IActivityMonitor.ActualFilter">ActualFilter</see> level. 
        /// <see cref="IActivityMonitor.CloseGroup"/> must be called in order to close the group, and/or the returned object must be disposed (both safely can be called: 
        /// the group is closed on the first action, the second one is ignored).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">Log level. The <see cref="LogLevel.None"/> level is used to open a filtered group. See remarks.</param>
        /// <param name="tags">Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. It will be union-ed with current <see cref="IActivityMonitor.AutoTags">AutoTags</see>.</param>
        /// <param name="text">Text to log (the title of the group). Null text is valid and considered as <see cref="String.Empty"/> or assigned to the <see cref="Exception.Message"/> if it exists.</param>
        /// <param name="ex">Optional exception associated to the group.</param>
        /// <param name="fileName">The source code file name from which the group is opened.</param>
        /// <param name="lineNumber">The line number in the source from which the group is opened.</param>
        /// <returns>A disposable object that can be used to close the group.</returns>
        /// <remarks>
        /// <para>
        /// Opening a group does not change the current <see cref="IActivityMonitor.MinimalFilter">MinimalFilter</see>, except when 
        /// opening a <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/> group: in such case, the Filter is automatically 
        /// sets to <see cref="LogFilter.Trace"/> to capture all potential information inside the error group.
        /// </para>
        /// <para>
        /// Changes to the monitor's current Filter or AutoTags that occur inside a group are automatically restored to their original values when the group is closed.
        /// This behavior guaranties that a local modification (deep inside unknown called code) does not impact caller code: groups are a way to easily isolate such 
        /// configuration changes.
        /// </para>
        /// <para>
        /// Note that this automatic configuration restoration works even if the group is filtered (when the <paramref name="level"/> is None).
        /// </para>
        /// </remarks>
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        static public IDisposable UnfilteredOpenGroup( this IActivityMonitor @this,
                                                       LogLevel level,
                                                       CKTrait? tags,
                                                       string text,
                                                       Exception? ex,
                                                       [CallerFilePath] string? fileName = null,
                                                       [CallerLineNumber] int lineNumber = 0 )
        {
            var d = new ActivityMonitorLogData( level, @this.AutoTags | tags, text, ex, fileName, lineNumber );
            return @this.UnfilteredOpenGroup( ref d );
        }
        
        #region Catch & CatchCounter

        /// <summary>
        /// Enables simple "using" syntax to easily collect any <paramref name="level"/> (or above) entries (defaults to <see cref="LogLevel.Error"/>) around operations.
        /// The callback is called when the returned IDisposable is disposed and there are at least one collected entry.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="errorHandler">An action that accepts a list of <see cref="ActivityMonitorSimpleCollector.Entry">entries</see>.</param>
        /// <param name="level">Defines the level of the collected entries (by default fatal or error entries).</param>
        /// <param name="capacity">Capacity of the collector defaults to 50.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable CollectEntries( this IActivityMonitor @this, Action<IReadOnlyList<ActivityMonitorSimpleCollector.Entry>> errorHandler, LogLevelFilter level = LogLevelFilter.Error, int capacity = 50 )
        {
            Throw.CheckNotNullArgument( errorHandler );
            ActivityMonitorSimpleCollector errorTracker = new ActivityMonitorSimpleCollector() { MinimalFilter = level, Capacity = capacity };
            @this.Output.RegisterClient( errorTracker );
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorTracker );
                if( errorTracker.Entries.Count > 0 ) errorHandler( errorTracker.Entries );
            } );
        }

        /// <summary>
        /// Enables simple "using" syntax to easily collect any <see cref="LogLevel"/> (or above) entries (defaults to <see cref="LogLevel.Error"/>) around operations.
        /// Entries are added in the <paramref name="entries"/> as soon as they appear.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="entries">Collector for <see cref="ActivityMonitorSimpleCollector.Entry">entries</see>.</param>
        /// <param name="level">Defines the level of the collected entries (by default fatal or error entries).</param>
        /// <param name="capacity">Capacity of the collector defaults to 50.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable CollectEntries( this IActivityMonitor @this, out IReadOnlyList<ActivityMonitorSimpleCollector.Entry> entries, LogLevelFilter level = LogLevelFilter.Error, int capacity = 50 )
        {
            ActivityMonitorSimpleCollector errorTracker = new ActivityMonitorSimpleCollector() { MinimalFilter = level, Capacity = capacity };
            @this.Output.RegisterClient( errorTracker );
            entries = errorTracker.Entries;
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorTracker );
            } );
        }


        class ErrorTracker : IActivityMonitorClient
        {
            readonly Action _onFatal;
            readonly Action _onError;

            public ErrorTracker( Action onFatal, Action onError )
            {
                _onFatal = onFatal;
                _onError = onError;
            }

            public void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                if( data.MaskedLevel == LogLevel.Error ) _onError();
                else if( data.MaskedLevel == LogLevel.Fatal ) _onFatal();
            }

            public void OnOpenGroup( IActivityLogGroup group )
            {
                if( group.Data.MaskedLevel == LogLevel.Error ) _onError();
                else if( group.Data.MaskedLevel == LogLevel.Fatal ) _onFatal();
            }

            public void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
            }

            public void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
            {
            }

            public void OnTopicChanged( string newTopic, string? fileName, int lineNumber )
            {
            }

            public void OnAutoTagsChanged( CKTrait newTrait )
            {
            }
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="onFatalOrError">An action that is called whenever an Error or Fatal error occurs.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable OnError( this IActivityMonitor @this, Action onFatalOrError )
        {
            Throw.CheckNotNullArgument( onFatalOrError );
            ErrorTracker tracker = new ErrorTracker( onFatalOrError, onFatalOrError );
            @this.Output.RegisterClient( tracker );
            return Util.CreateDisposableAction( () => @this.Output.UnregisterClient( tracker ) );
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="onFatal">An action that is called whenever a Fatal error occurs.</param>
        /// <param name="onError">An action that is called whenever an Error occurs.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable OnError( this IActivityMonitor @this, Action onFatal, Action onError )
        {
            Throw.CheckArgument( onFatal != null && onError != null );
            ErrorTracker tracker = new ErrorTracker( onFatal, onError );
            @this.Output.RegisterClient( tracker );
            return Util.CreateDisposableAction( () => @this.Output.UnregisterClient( tracker ) );
        }
        #endregion


        /// <summary>
        /// Sets the <see cref="IActivityMonitorFilteredClient.MinimalFilter"/> to all clients
        /// that are <see cref="IActivityMonitorInteractiveUserClient"/>.
        /// </summary>
        /// <param name="this">This monitor.</param>
        /// <param name="filter">The filter (typically <see cref="LogClamper.Clamp"/> is true).</param>
        public static void SetInteractiveUserFilter( this IActivityMonitor @this, LogClamper filter )
        {
            foreach( var c in @this.Output.Clients.OfType<IActivityMonitorInteractiveUserClient>() )
            {
                c.MinimalFilter = filter;
            }
        }

        /// <summary>
        /// Temporarily sets the <see cref="IActivityMonitorFilteredClient.MinimalFilter"/> to all clients
        /// that are <see cref="IActivityMonitorInteractiveUserClient"/> until the returned disposable is disposed.
        /// </summary>
        /// <param name="this">This monitor.</param>
        /// <param name="filter">The filter (typically <see cref="LogClamper.Clamp"/> is true).</param>
        /// <returns>A disposable to restore the original filters.</returns>
        public static IDisposable TemporarilySetInteractiveUserFilter( this IActivityMonitor @this, LogClamper filter )
        {
            Action? restore = null;
            foreach( var c in @this.Output.Clients.OfType<IActivityMonitorInteractiveUserClient>() )
            {
                var current = c.MinimalFilter;
                if( current != filter )
                {
                    var prevRestore = restore;
                    restore = prevRestore != null
                                ? () => { prevRestore(); c.MinimalFilter = current; }
                    : () => c.MinimalFilter = current;
                    c.MinimalFilter = filter;
                }
            }
            return restore == null ? Util.EmptyDisposable : Util.CreateDisposableAction( restore );
        }

        #region IActivityMonitor.TemporarilySetMinimalFilter( ... )

        class LogFilterSentinel : IDisposable
        {
            readonly IActivityMonitor _monitor;
            readonly LogFilter _prevLevel;

            public LogFilterSentinel( IActivityMonitor l, LogFilter filter )
            {
                _prevLevel = l.MinimalFilter;
                _monitor = l;
                l.MinimalFilter = filter;
            }

            public void Dispose()
            {
                _monitor.MinimalFilter = _prevLevel;
            }

        }

        /// <summary>
        /// Sets filter levels on this <see cref="IActivityMonitor"/>. The current <see cref="IActivityMonitor.MinimalFilter"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// <para>
        /// Note that even if closing a Group automatically restores the IActivityMonitor.MinimalFilter to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the filter level without bothering to restore the 
        /// initial value (this is what OpenGroup/CloseGroup do with both the Filter and the AutoTags).
        /// </para>
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="group">The new filter level for group.</param>
        /// <param name="line">The new filter level for log line.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current level.</returns>
        public static IDisposable TemporarilySetMinimalFilter( this IActivityMonitor @this, LogLevelFilter group, LogLevelFilter line )
        {
            return new LogFilterSentinel( @this, new LogFilter( group, line ) );
        }

        /// <summary>
        /// Sets a filter level on this <see cref="IActivityMonitor"/>. The current <see cref="IActivityMonitor.MinimalFilter"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// <para>
        /// Even if, when a Group is closed, the IActivityMonitor.Filter is automatically restored to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the filter level without bothering to restore the 
        /// initial value (this is what OpenGroup/CloseGroup do with both the Filter and the AutoTags).
        /// </para>
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="f">The new filter.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current level.</returns>
        public static IDisposable TemporarilySetMinimalFilter( this IActivityMonitor @this, LogFilter f )
        {
            return new LogFilterSentinel( @this, f );
        }

        #endregion IActivityMonitor.TemporarilySetMinimalFilter( ... )


        #region IActivityMonitor.TemporarilySetAutoTags( Tags, SetOperation )

        class TagsSentinel : IDisposable
        {
            readonly IActivityMonitor _monitor;
            readonly CKTrait _previous;

            public TagsSentinel( IActivityMonitor l, CKTrait t )
            {
                _previous = l.AutoTags;
                _monitor = l;
                l.AutoTags = t;
            }

            public void Dispose()
            {
                _monitor.AutoTags = _previous;
            }

        }

        /// <summary>
        /// Alter tags of this <see cref="IActivityMonitor"/>. Current <see cref="IActivityMonitor.AutoTags"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// Even if, when a Group is closed, the IActivityMonitor.AutoTags is automatically restored to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the tags level without bothering to restore the 
        /// initial value (this is close to what OpenGroup/CloseGroup do with both the MinimalFilter and the AutoTags).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="tags">Tags to combine with the current one.</param>
        /// <param name="operation">Defines the way the new <paramref name="tags"/> must be combined with current ones.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current tag when disposed.</returns>
        public static IDisposable TemporarilySetAutoTags( this IActivityMonitor @this, CKTrait tags, SetOperation operation = SetOperation.Union )
        {
            return new TagsSentinel( @this, @this.AutoTags.Apply( tags, operation ) );
        }
        
        #endregion


        #region RegisterClients

        /// <summary>
        /// Registers an <see cref="IActivityMonitorClient"/> to the <see cref="IActivityMonitorOutput.Clients">Clients</see> list.
        /// Duplicates IActivityMonitorClient are silently ignored.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="client">An <see cref="IActivityMonitorClient"/> implementation.</param>
        /// <returns>The registered client.</returns>
        public static IActivityMonitorClient RegisterClient( this IActivityMonitorOutput @this, IActivityMonitorClient client )
        {
            return @this.RegisterClient( client, out _ );
        }

        /// <summary>
        /// Registers a typed <see cref="IActivityMonitorClient"/>.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <typeparam name="T">Any type that specializes <see cref="IActivityMonitorClient"/>.</typeparam>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="client">Client to register.</param>
        /// <returns>The registered client.</returns>
        public static T RegisterClient<T>( this IActivityMonitorOutput @this, T client ) where T : IActivityMonitorClient
        {
            return @this.RegisterClient<T>( client, out _ );
        }

        /// <summary>
        /// Registers multiple <see cref="IActivityMonitorClient"/>.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="clients">Multiple clients to register.</param>
        /// <returns>This registrar to enable fluent syntax.</returns>
        public static IActivityMonitorOutput RegisterClients( this IActivityMonitorOutput @this, IEnumerable<IActivityMonitorClient> clients )
        {
            foreach( var c in clients ) @this.RegisterClient( c );
            return @this;
        }

        /// <summary>
        /// Registers multiple <see cref="IActivityMonitorClient"/>.
        /// Duplicate IActivityMonitorClient instances are ignored.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="clients">Multiple clients to register.</param>
        /// <returns>This registrar to enable fluent syntax.</returns>
        public static IActivityMonitorOutput RegisterClients( this IActivityMonitorOutput @this, params IActivityMonitorClient[] clients )
        {
            return RegisterClients( @this, (IEnumerable<IActivityMonitorClient>)clients );
        }

        /// <summary>
        /// Registers a unique client for a type that must have a public default constructor. 
        /// <see cref="Activator.CreateInstance{T}()"/> is called if necessary.
        /// </summary>
        /// <returns>The found or newly created client.</returns>
        public static T RegisterUniqueClient<T>( this IActivityMonitorOutput @this ) where T : IActivityMonitorClient, new()
        {
            return @this.RegisterUniqueClient( c => true, () => Activator.CreateInstance<T>() )!;
        }

        /// <summary>
        /// Unregisters the first <see cref="IActivityMonitorClient"/> from the <see cref="IActivityMonitorOutput.Clients"/> list
        /// that satisfies the predicate.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="predicate">A predicate that will be used to determine the first client to unregister.</param>
        /// <returns>The unregistered client, or null if no client has been found.</returns>
        public static T? UnregisterClient<T>( this IActivityMonitorOutput @this, Func<T, bool> predicate ) where T : IActivityMonitorClient
        {
            Throw.CheckNotNullArgument( predicate );
            T? c = @this.Clients.OfType<T>().Where( predicate ).FirstOrDefault();
            if( c != null ) @this.UnregisterClient( c );
            return c;
        }

        #endregion

        /// <summary>
        /// Gets this Group conclusions as a readable string.
        /// </summary>
        /// <param name="this">This group conclusion. Can be null.</param>
        /// <param name="conclusionSeparator">Conclusion separator.</param>
        /// <returns>A lovely concatenated string of conclusions.</returns>
        public static string ToStringGroupConclusion( this IEnumerable<ActivityLogGroupConclusion> @this, string conclusionSeparator = " - " )
        {
            if( @this == null ) return String.Empty;
            StringBuilder b = new StringBuilder();
            foreach( var e in @this )
            {
                if( b.Length > 0 ) b.Append( conclusionSeparator );
                b.Append( e.Text );
            }
            return b.ToString();
        }

        /// <summary>
        /// Gets the path as a readable string.
        /// </summary>
        /// <param name="this">This path. Can be null.</param>
        /// <param name="elementSeparator">Between elements.</param>
        /// <param name="withoutConclusionFormat">There must be 3 placeholders {0} for the level, {1} for the text and {2} for the conclusion.</param>
        /// <param name="withConclusionFormat">There must be 2 placeholders {0} for the level and {1} for the text.</param>
        /// <param name="conclusionSeparator">Conclusion separator.</param>
        /// <param name="fatal">For Fatal errors.</param>
        /// <param name="error">For Errors.</param>
        /// <param name="warn">For Warnings.</param>
        /// <param name="info">For Infos.</param>
        /// <param name="trace">For Traces.</param>
        /// <param name="debug">For Debugs.</param>
        /// <returns>A lovely path.</returns>
        public static string ToStringPath( this IEnumerable<ActivityMonitorPathCatcher.PathElement> @this,
            string elementSeparator = "> ",
            string withoutConclusionFormat = "{0}{1} ",
            string withConclusionFormat = "{0}{1} -{{ {2} }}",
            string conclusionSeparator = " - ",
            string fatal = "[Fatal]- ",
            string error = "[Error]- ",
            string warn = "[Warning]- ",
            string info = "[Info]- ",
            string trace = "",
            string debug = "[Debug]- " )
        {
            if( @this == null ) return String.Empty;
            StringBuilder b = new StringBuilder();
            foreach( var e in @this )
            {
                if( b.Length > 0 ) b.Append( elementSeparator );
                string prefix = trace;
                switch( e.MaskedLevel )
                {
                    case LogLevel.Fatal: prefix = fatal; break;
                    case LogLevel.Error: prefix = error; break;
                    case LogLevel.Warn: prefix = warn; break;
                    case LogLevel.Info: prefix = info; break;
                    case LogLevel.Debug: prefix = debug; break;
                }
                if ( e.GroupConclusion != null ) b.AppendFormat( withConclusionFormat, prefix, e.Text, e.GroupConclusion.ToStringGroupConclusion( conclusionSeparator ) );
                else b.AppendFormat( withoutConclusionFormat, prefix, e.Text );
            }
            return b.ToString();
        }

    }
}
