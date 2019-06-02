using System;
using System.Collections.Generic;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Displays the activity to the console with colors.
    /// </summary>
    public class ColoredActivityMonitorConsoleClient : ActivityMonitorTextWriterClient
    {
        LogLevel _currentLogLevel;
        ConsoleColor _backgroundColor;

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with the filter set to <see cref="LogFilter.Undefined"/>.
        /// </summary>
        /// <param name="background">Background color used to log.</param>
        public ColoredActivityMonitorConsoleClient( ConsoleColor background )
            : this( LogFilter.Undefined, background )
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with the filter set to <see cref="LogFilter.Undefined"/>.
        /// The background color is unchanged.
        /// </summary>
        public ColoredActivityMonitorConsoleClient()
            : this( LogFilter.Undefined )
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with a filter initially set.
        /// </summary>
        /// <param name="filter"><see cref="LogFilter"/> to set on this monitor</param>
        /// <param name="background">Background color used to log.</param>
        public ColoredActivityMonitorConsoleClient( LogFilter filter, ConsoleColor background )
            : base( filter )
        {
            _backgroundColor = background;
            Writer = WriteConsole;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with a filter initially set.
        /// </summary>
        /// <param name="filter"><see cref="LogFilter"/> to set on this monitor</param>
        public ColoredActivityMonitorConsoleClient( LogFilter filter )
            : this( filter, Console.BackgroundColor )
        {
        }
        /// <summary>
        /// Gets or Sets the background color used to log to the console.
        /// </summary>
        public ConsoleColor BackgroundColor { get => _backgroundColor; set => _backgroundColor = value; }


        /// <summary>
        /// Sets the color of the logged text. 
        /// </summary>
        /// <param name="logLevel">Current log level.</param>
        protected virtual void SetColor( LogLevel logLevel )
        {
            DefaultSetColor( _backgroundColor, logLevel );
        }

        /// <summary>
        /// Sets the color of the logged text. 
        /// </summary>
        /// <param name="backgroundColor">Background color to set.</param>
        /// <param name="logLevel">Current log level.</param>
        public static void DefaultSetColor(ConsoleColor backgroundColor, LogLevel logLevel )
        {
            switch( logLevel )
            {
                case LogLevel.Fatal:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Warn:
                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Info:
                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LogLevel.Trace:
                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.Debug:
                    Console.BackgroundColor = backgroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
            }
        }

        void WriteConsole( string s )
        {
            ConsoleColor prevForegroundColor = Console.ForegroundColor;
            ConsoleColor prevBackgroundColor = Console.BackgroundColor;
            SetColor( _currentLogLevel );
            Console.Out.Write( s );
            Console.ForegroundColor = prevForegroundColor;
            Console.BackgroundColor = prevBackgroundColor;
        }

        /// <summary>
        /// Writes all the information after having captured the log level.
        /// </summary>
        /// <param name="data">Log data.</param>
        protected override void OnEnterLevel( ActivityMonitorLogData data )
        {
            _currentLogLevel = data.MaskedLevel;
            base.OnEnterLevel( data );
        }

        /// <summary>
        /// Writes a group opening.
        /// </summary>
        /// <param name="g">Group information.</param>
        protected override void OnGroupOpen( IActivityLogGroup g )
        {
            _currentLogLevel = g.MaskedGroupLevel;
            base.OnGroupOpen( g );
        }

        /// <summary>
        /// Writes group conclusion and updates internally managed line prefix.
        /// </summary>
        /// <param name="g">Group that must be closed.</param>
        /// <param name="conclusions">Conclusions for the group.</param>
        protected override void OnGroupClose( IActivityLogGroup g, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            _currentLogLevel = g.MaskedGroupLevel;
            base.OnGroupClose( g, conclusions );
        }

    }
}
