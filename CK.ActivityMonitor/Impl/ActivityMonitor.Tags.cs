using CK.Core.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CK.Core
{
    public partial class ActivityMonitor
    {
        /// <summary>
        /// Thread-safe context for tags used to categorize log entries (and group conclusions).
        /// All tags used in monitoring must be <see cref="Register"/>ed here.
        /// <para>
        /// This nested static class also manages tags filtering.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Tags used for conclusions should start with "c:".
        /// </remarks>
        public static class Tags
        {
            /// <summary>
            /// The central, unique, context of all monitoring related tags used in the application domain.
            /// </summary>
            public static readonly CKTraitContext Context;

            /// <summary>
            /// Shortcut to <see cref="CKTraitContext.EmptyTrait">Context.EmptyTrait</see>.
            /// </summary>
            static public readonly CKTrait Empty;

            /// <summary>
            /// Creation of dependent activities are marked with "dep:CreateActivity".
            /// </summary>
            static public readonly CKTrait CreateDependentActivity;

            /// <summary>
            /// Start of dependent activities are marked with "dep:StartActivity".
            /// </summary>
            static public readonly CKTrait StartDependentActivity;

            /// <summary>
            /// Conclusions provided to IActivityMonitor.Close(string) are marked with "c:User".
            /// </summary>
            static public readonly CKTrait UserConclusion;

            /// <summary>
            /// Conclusions returned by the optional function when a group is opened (see <see cref="IActivityMonitor.UnfilteredOpenGroup"/>) are marked with "c:GetText".
            /// </summary>
            static public readonly CKTrait GetTextConclusion;

            /// <summary>
            /// Whenever <see cref="Topic"/> changed, a <see cref="LogLevel.Info"/> is emitted marked with "MonitorTopicChanged".
            /// </summary>
            static public readonly CKTrait MonitorTopicChanged;

            /// <summary>
            /// A "MonitorEnd" tag is emitted by <see cref="ActivityMonitorExtension.MonitorEnd"/>.
            /// This indicates the logical end of life of the monitor. It should not be used anymore (but technically can
            /// be used).
            /// </summary>
            static public readonly CKTrait MonitorEnd;

            /// <summary>
            /// A "m:Internal" tag is used while replaying <see cref="IActivityMonitorImpl.InternalMonitor"/>
            /// logs.
            /// </summary>
            static public readonly CKTrait InternalMonitor;

            /// <summary>
            /// A "StackTrace" tag activates stack trace tracking and dumping when a concurrent access is detected.
            /// logs.
            /// </summary>
            static public readonly CKTrait StackTrace;

            /// <summary>
            /// Simple shortcut to <see cref="CKTraitContext.FindOrCreate(string)"/>.
            /// </summary>
            /// <param name="tags">Atomic tag or multiple tags separated by pipes (|).</param>
            /// <returns>Registered tags.</returns>
            static public CKTrait Register( string tags ) => Context.FindOrCreate( tags );

            static Tags()
            {
                Context = CKTraitContext.Create( "ActivityMonitor" );
                Empty = Context.EmptyTrait;
                UserConclusion = Context.FindOrCreate( "c:User" );
                GetTextConclusion = Context.FindOrCreate( "c:GetText" );
                MonitorTopicChanged = Context.FindOrCreate( "MonitorTopicChanged" );
                CreateDependentActivity = Context.FindOrCreate( "dep:CreateActivity" );
                StartDependentActivity = Context.FindOrCreate( "dep:StartActivity" );
                MonitorEnd = Context.FindOrCreate( "MonitorEnd" );
                InternalMonitor = Context.FindOrCreate( "m:Internal" );
                StackTrace = Context.FindOrCreate( "StackTrace" );
                _filters = Array.Empty<(CKTrait, LogClamper)>();
            }

            static (CKTrait T, LogClamper F)[] _filters;

            /// <summary>
            /// Clears all existing filters.
            /// </summary>
            public static void ClearAll()
            {
                Interlocked.Exchange( ref _filters, Array.Empty<(CKTrait, LogClamper)>() );
            }

            /// <summary>
            /// Updates filters.
            /// </summary>
            /// <param name="filters">Ordered set of tags and associated clamper.</param>
            public static void SetFilters( IEnumerable<(CKTrait, LogClamper)> filters )
            {
                var t = filters.Where( f => f.Item2.Filter != default ).ToArray();
                Interlocked.Exchange( ref _filters, t );
            }

            /// <summary>
            /// Finds a <see cref="LogClamper"/> to consider for a line that has tags and a current filter
            /// and updates the filter.
            /// </summary>
            /// <param name="tags">The log's tags.</param>
            /// <param name="filter">The current filter applied to the line.</param>
            /// <param name="logLevel">The current log level.</param>
            /// <returns>Whether the log must be emitted or not.</returns>
            internal static bool ApplyForLine( CKTrait tags, int filter, int logLevel )
            {
                var filters = _filters;
                if( !tags.IsEmpty )
                {
                    foreach( var (T, F) in filters )
                    {
                        int iTag;
                        if( tags.Overlaps( T ) && (iTag = (int)F.Filter.Line) > 0 )
                        {
                            if( iTag != filter )
                            {
                                if( F.Clamp )
                                {
                                    filter = iTag;
                                }
                                else
                                {
                                    if( filter <= 0 ) filter = (int)ActivityMonitor.DefaultFilter.Line;
                                    filter = Math.Min( filter, iTag );
                                }
                            }
                            return (logLevel & (int)LogLevel.Mask) >= filter;
                        }
                    }
                }
                if( filter <= 0 ) filter = (int)ActivityMonitor.DefaultFilter.Line;
                return (logLevel & (int)LogLevel.Mask) >= filter;
            }

            internal static bool ApplyForGroup( CKTrait tags, int filter, int logLevel )
            {
                var filters = _filters;
                foreach( var (T, F) in filters )
                {
                    int iTag;
                    if( tags.Overlaps( T ) && (iTag = (int)F.Filter.Group) > 0 )
                    {
                        if( iTag != filter )
                        {
                            if( iTag != filter )
                            {
                                if( F.Clamp )
                                {
                                    filter = iTag;
                                }
                                else
                                {
                                    if( filter <= 0 ) filter = (int)ActivityMonitor.DefaultFilter.Group;
                                    filter = Math.Min( filter, iTag );
                                }
                            }
                        }
                        return (logLevel & (int)LogLevel.Mask) >= filter;
                    }
                }
                if( filter <= 0 ) filter = (int)ActivityMonitor.DefaultFilter.Line;
                return (logLevel & (int)LogLevel.Mask) >= filter;
            }
        }
    }

}
