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
            : base( Console.Out.Write )
        {
        }

        /// <summary>
        /// Initializes a new <see cref="ActivityMonitorConsoleClient"/> that outputs
        /// to the standard output or the error output.
        /// </summary>
        /// <param name="useErrorStream">True to output logs to standard error stream, false for standard output.</param>
        public ActivityMonitorConsoleClient( bool useErrorStream )
            : base( useErrorStream ? (Action<string>)Console.Error.Write : Console.Out.Write )
        {
        }

    }

}
