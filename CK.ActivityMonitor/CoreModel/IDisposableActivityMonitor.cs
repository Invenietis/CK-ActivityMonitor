using System;

namespace CK.Core;


/// <summary>
/// A diposable <see cref="IActivityMonitor"/>.
/// This is typically used when monitors must be reusable from a pool: the Dispose method
/// returns the object to the pool.
/// </summary>
public interface IDisposableActivityMonitor : IActivityMonitor, IDisposable
{
}



