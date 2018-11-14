using System;

namespace CK.Core
{
    /// <summary>
    /// Simple <see cref="EventArgs"/> that captures and exposes a <see cref="IActivityMonitor"/>.
    /// </summary>
    public class EventMonitoredArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new <see cref="EventMonitoredArgs"/>.
        /// </summary>
        /// <param name="monitor">The activity monitor to use. Must not be null.</param>
        public EventMonitoredArgs( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            Monitor = monitor;
        }

        /// <summary>
        /// Gets the monitor that should be used while processing the event.
        /// </summary>
        public IActivityMonitor Monitor { get; }
    }
}
