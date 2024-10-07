using System;

namespace CK.Core;

/// <summary>
/// Simple <see cref="EventArgs"/> that captures and exposes a <see cref="IActivityMonitor"/>.
/// This is often used as a base class that exposes more contextual information.
/// </summary>
public class EventMonitoredArgs : EventArgs
{
    /// <summary>
    /// Initializes a new <see cref="EventMonitoredArgs"/>.
    /// </summary>
    /// <param name="monitor">The activity monitor to use. Must not be null.</param>
    public EventMonitoredArgs( IActivityMonitor monitor )
    {
        Throw.CheckNotNullArgument( monitor );
        Monitor = monitor;
    }

    /// <summary>
    /// Initializes a new <see cref="EventMonitoredArgs"/> with no monitor.
    /// The <see cref="Monitor"/> must be set by the specialization.
    /// </summary>
#pragma warning disable CS8618 // Monitor must be set by specialization.
    protected EventMonitoredArgs()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets the monitor that should be used while processing the event.
    /// Specialized classes may need to set this monitor when implementing shared, pooled, reusable event arguments
    /// or to override this default getter implementation.
    /// </summary>
    public virtual IActivityMonitor Monitor { get; protected set; }
}
