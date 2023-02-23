using System;
using System.Collections.Generic;

namespace CK.Core
{
    /// <summary>
    /// Displays the activity to the console with colors.
    /// </summary>
    public class ColoredActivityMonitorConsoleClient : ActivityMonitorTextWriterClient, IActivityMonitorInteractiveUserClient
    {
        LogLevel _currentLogLevel;
        ConsoleColor _backgroundColor;

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with the filter set to <see cref="LogFilter.Undefined"/>.
        /// </summary>
        /// <param name="background">Background color used to log.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ColoredActivityMonitorConsoleClient( ConsoleColor background, char depthInitial = '|' )
            : this( LogClamper.Undefined, background, depthInitial )
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with the filter set to <see cref="LogFilter.Undefined"/>.
        /// The console's current background color is unchanged.
        /// </summary>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ColoredActivityMonitorConsoleClient( char depthInitial = '|' )
            : this( LogClamper.Undefined, depthInitial )
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with a filter initially set
        /// and a specific background color.
        /// </summary>
        /// <param name="filter"><see cref="LogClamper"/> to set on this client.</param>
        /// <param name="background">Background color used to log.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ColoredActivityMonitorConsoleClient( LogClamper filter, ConsoleColor background, char depthInitial = '|' )
            : base( filter, depthInitial )
        {
            _backgroundColor = background;
            Writer = WriteConsole;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ColoredActivityMonitorConsoleClient"/> with a filter initially set.
        /// The console's current background color is unchanged.
        /// </summary>
        /// <param name="filter"><see cref="LogClamper"/> to set on this client.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ColoredActivityMonitorConsoleClient( LogClamper filter, char depthInitial = '|' )
            : this( filter, Console.BackgroundColor, depthInitial )
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
            (ConsoleColor background, ConsoleColor foreground) = DefaultColorTheme( _backgroundColor, logLevel );
            Console.BackgroundColor = background;
            Console.ForegroundColor = foreground;
        }

        /// <summary>
        /// Sets the color of the logged text. 
        /// </summary>
        /// <param name="backgroundColor">Background color to set.</param>
        /// <param name="logLevel">Current log level.</param>
        public static (ConsoleColor background, ConsoleColor foreground) DefaultColorTheme( ConsoleColor backgroundColor, LogLevel logLevel )
        {
            return logLevel switch
            {
                LogLevel.Fatal => (ConsoleColor.DarkRed, ConsoleColor.Yellow),
                LogLevel.Error => (backgroundColor, ConsoleColor.Red),
                LogLevel.Warn => (backgroundColor, ConsoleColor.Yellow),
                LogLevel.Info => (backgroundColor, ConsoleColor.Cyan),
                LogLevel.Trace => (backgroundColor, ConsoleColor.Gray),
                LogLevel.Debug => (backgroundColor, ConsoleColor.DarkGray),
                _ => (ConsoleColor.Red, ConsoleColor.Green), //Awful so people may think "something is not right"
            };
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
        protected override void OnEnterLevel( ref ActivityMonitorLogData data )
        {
            _currentLogLevel = data.MaskedLevel;
            base.OnEnterLevel( ref data );
        }

        /// <summary>
        /// Writes a group opening.
        /// </summary>
        /// <param name="g">Group information.</param>
        protected override void OnGroupOpen( IActivityLogGroup g )
        {
            _currentLogLevel = g.Data.MaskedLevel;
            base.OnGroupOpen( g );
        }

        /// <summary>
        /// Writes group conclusion and updates internally managed line prefix.
        /// </summary>
        /// <param name="g">Group that must be closed.</param>
        /// <param name="conclusions">Conclusions for the group. Never null but can be empty.</param>
        protected override void OnGroupClose( IActivityLogGroup g, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            _currentLogLevel = g.Data.MaskedLevel;
            base.OnGroupClose( g, conclusions );
        }

    }
}
