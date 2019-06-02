using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CK.Core
{
    /// <summary>
    /// Displays the activity to the console.
    /// </summary>
    public sealed class ActivityMonitorConsoleClient : ActivityMonitorTextWriterClient
    {
        
        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/>.
        /// </summary>
        public ActivityMonitorConsoleClient()
            : base( ConsoleWrite )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/> that outputs
        /// to the standard output or the error output.
        /// </summary>
        /// <param name="useErrorStream">True to output logs to standard error stream, false for standard output.</param>
        public ActivityMonitorConsoleClient( bool useErrorStream )
            : base( useErrorStream ? (Action<string>)ErrorWrite : ConsoleWrite )
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
