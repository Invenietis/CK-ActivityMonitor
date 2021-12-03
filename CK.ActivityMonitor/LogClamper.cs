using Microsoft.Toolkit.Diagnostics;
using System;

namespace CK.Core
{
    /// <summary>
    /// Encapsulates a <see cref="Filter"/> and a <see cref="Clamp"/> flag that can optionally restricts the level of the logs.
    /// </summary>
    [System.ComponentModel.TypeConverter( typeof( LogClamperTypeConverter ) )]
    [Serializable]
    public readonly struct LogClamper : IEquatable<LogClamper>
    {
        /// <summary>
        /// Undefined clamper is <see cref="LogFilter.Undefined"/> with a false <see cref="Clamp"/>.
        /// This is the same as using the default constructor for this structure (it is exposed here for clarity).
        /// </summary>
        static public readonly LogClamper Undefined = default;

        /// <summary>
        /// Gets the filter to apply. It is generally <see cref="LogFilter.Combine(LogFilter)"/> with other
        /// participant filters to satisfy all of them.
        /// </summary>
        public LogFilter Filter { get; }

        /// <summary>
        /// Whether the <see cref="Filter"/> must also "cut" the logs or only be combined with others.
        /// <para>
        /// When exposed and used by a <see cref="IActivityMonitorBoundClient"/>, a true value applies to its own log handling
        /// (whether others request more detailed logs is not its business).
        /// </para>
        /// <para>
        /// When used for tags filtering (or other "Log Gates"), a true value clamps the final filter computed by
        /// <see cref="IActivityMonitor.ActualFilter"/> (and <see cref="ActivityMonitor.DefaultFilter"/> if the actual filter is undefined).
        /// </para>
        /// </summary>
        public bool Clamp { get; }

        /// <summary>
        /// Initializes a new clamper.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <param name="clamp">See <see cref="Clamp"/>.</param>
        public LogClamper( LogFilter filter, bool clamp )
        {
            Filter = filter;
            Clamp = clamp;
        }

        /// <summary>
        /// Overridden to compare Clamp and Filter.
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns>True if equal.</returns>
        public override bool Equals( object? obj ) => obj is LogClamper c && Equals( c );

        /// <summary>
        /// <see cref="Filter"/> and <see cref="Clamp"/> must be equal.
        /// </summary>
        /// <param name="x">Other filter.</param>
        /// <returns>True if equal.</returns>
        public bool Equals( LogClamper x ) => Filter.Equals( x.Filter ) && Clamp == x.Clamp;

        /// <summary>
        /// Overridden to compute hash based on <see cref="Filter"/> and <see cref="Clamp"/> values.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode() => Clamp ? -Filter.GetHashCode() : Filter.GetHashCode();

        /// <summary>
        /// Overridden to show the Filter and a '!' if <see cref="Clamp"/> is true.
        /// </summary>
        /// <returns>A {group,line} string optionally followed by a '!'.</returns>
        public override string ToString()
        {
            var s = Filter.ToString();
            return Clamp ? s + '!' : s;
        }

        /// <summary>
        /// Parses a clamper: the <see cref="LogFilter.Parse(string)"/> and optional '!' suffix for the <see cref="Clamp"/>.
        /// Can be "{GroupLogLevelFilter,LineLogLevelFilter}" like "{Error,Trace}" or predefined <see cref="LogFilter"/> like
        /// "Verbose!" (same as "{Trace,Info}!").
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The filter.</returns>
        public static LogClamper Parse( string s )
        {
            if( !TryParse( s, out var c ) ) ThrowHelper.ThrowArgumentException( $"Invalid LogClamper: '{s}'.", nameof( s ) );
            return c;
        }

        /// <summary>
        /// Tries to parse a <see cref="LogClamper"/>: a <see cref="LogFilter.TryParse(string, out LogFilter)"/> and an
        /// optional '!' suffix for the <see cref="Clamp"/>.
        /// Can be "{GroupLogLevelFilter,LineLogLevelFilter}" like "{Error,Trace}" or predefined <see cref="LogFilter"/> like
        /// "Verbose!" (same as "{Trace,Info}!").
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <param name="c">Resulting clamper.</param>
        /// <returns>True on success, false on error.</returns>
        public static bool TryParse( string s, out LogClamper c )
        {
            Guard.IsNotNull( s, nameof( s ) );
            var m = new StringMatcher( s );
            return m.MatchLogClamper( out c ) && m.IsEnd;
        }


    }
}
