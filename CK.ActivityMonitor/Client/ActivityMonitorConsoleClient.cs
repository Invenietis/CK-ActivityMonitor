using System;

namespace CK.Core
{
    /// <summary>
    /// Displays the activity to the console.
    /// </summary>
    public sealed class ActivityMonitorConsoleClient : ActivityMonitorTextWriterClient, IActivityMonitorInteractiveUserClient
    {
        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/>.
        /// </summary>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ActivityMonitorConsoleClient( char depthInitial = '|' )
            : base( ConsoleWrite, LogClamper.Undefined, depthInitial )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/>.
        /// </summary>
        /// <param name="filter">Filter to apply.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ActivityMonitorConsoleClient( LogClamper filter, char depthInitial = '|' )
            : base( ConsoleWrite, filter, depthInitial )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/> that outputs
        /// to the standard output or the error output.
        /// </summary>
        /// <param name="useErrorStream">True to output logs to standard error stream, false for standard output.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ActivityMonitorConsoleClient( bool useErrorStream, char depthInitial = '|' )
            : base( useErrorStream ? (Action<string>)ErrorWrite : ConsoleWrite, LogClamper.Undefined, depthInitial )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/> that outputs
        /// to the standard output or the error output.
        /// </summary>
        /// <param name="useErrorStream">True to output logs to standard error stream, false for standard output.</param>
        /// <param name="filter">Filter to apply.</param>
        /// <param name="depthInitial">A character that starts the "depth padding".</param>
        public ActivityMonitorConsoleClient( bool useErrorStream, LogClamper filter, char depthInitial = '|' )
            : base( useErrorStream ? (Action<string>)ErrorWrite : ConsoleWrite, filter, depthInitial )
        {
        }

        /// <summary>
        /// Static relay required to be resilient to calls to <see cref="Console.SetOut(System.IO.TextWriter)"/>.
        /// </summary>
        /// <param name="text">The text to write.</param>
        static public void ConsoleWrite( string text ) => Console.Out.Write( text );

        /// <summary>
        /// Same as <see cref="ConsoleWrite(string)"/> for the error stream.
        /// </summary>
        /// <param name="text">The text to write.</param>
        static public void ErrorWrite( string text ) => Console.Error.Write( text );

    }

}
