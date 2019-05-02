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
        public ActivityMonitorConsoleClient( bool useErrorStream = false )
            : base( useErrorStream ? (Action<string>)Console.Error.Write : Console.Out.Write )
        {
        }

    }

}
