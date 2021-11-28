using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CK.Core
{

    public partial class ActivityMonitor
    {
        /// <summary>
        /// Manages source filtering.
        /// This default implementation (<see cref="DefaultFilter(ref string, ref int)"/>) handles file scope only.
        /// </summary>
        public static class TagFilter
        {
            static (CKTrait T, LogClamper F)[] _filters;

            static TagFilter()
            {
                _filters = Array.Empty<(CKTrait, LogClamper)>();
            }

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
            public static void SetFilters( IEnumerable<(CKTrait,LogClamper)> filters )
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
