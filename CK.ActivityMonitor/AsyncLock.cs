using CK.Core;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Asynchronous/synchronous lock with recursion support based on a <see cref="SemaphoreSlim"/>.
    /// Recursion support relies on the <see cref="IActivityMonitor"/> that is a required parameter
    /// of all the methods: it is this monitor that acts as the "acquisition context".
    /// </summary>
    /// <remarks>
    /// This lock not disposable and this is intentional because unnecessary: a SemaphoreSlim must be
    /// disposed only if its <see cref="SemaphoreSlim.AvailableWaitHandle"/> has been used and since we
    /// encapsulate the semaphore and don't use it, we can avoid the IDisposable burden.
    /// </remarks>
    public class AsyncLock
    {
        readonly SemaphoreSlim _semaphore;
        readonly LockRecursionPolicy _policy;
        IActivityMonitorOutput? _current;
        int _recCount;
        readonly string _name;

        /// <summary>
        /// Initializes a new lock with an explicit name.
        /// </summary>
        /// <param name="recursionPolicy">the recursion policy to use.</param>
        /// <param name="name">A name for this lock.</param>
        public AsyncLock( LockRecursionPolicy recursionPolicy, string name )
        {
            _semaphore = new SemaphoreSlim( 1, 1 );
            _policy = recursionPolicy;
            _name = name;
        }

        /// <summary>
        /// Initializes a new lock with an automatic name (source file and line number).
        /// </summary>
        /// <param name="recursionPolicy">the recursion policy to use.</param>
        /// <param name="filePath">The path of the file that instantiates this lock. Automatically set by the compiler.</param>
        /// <param name="lineNmber">The line number in the source file where this lock has been instantiated. Automatically set by the compiler.</param>
        public AsyncLock( LockRecursionPolicy recursionPolicy, [CallerFilePath] string? filePath = null, [CallerLineNumber] int lineNmber = 0 )
        {
            _semaphore = new SemaphoreSlim( 1, 1 );
            _policy = recursionPolicy;
            _name = filePath + '@' + lineNmber.ToString( CultureInfo.InvariantCulture );
        }

        /// <summary>
        /// Gets the name of this lock.
        /// </summary>
        public string Name => _name;

        /// <summary>
        /// Helper to support using statement.
        /// <see cref="Enter(IActivityMonitor)"/> this lock and returns a <see cref="IDisposable"/> that will <see cref="Leave(IActivityMonitor)"/> this lock.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <returns>The disposable to release the lock.</returns>
        public Releaser Lock( IActivityMonitor monitor )
        {
            Enter( monitor );
            return new Releaser( this, monitor );
        }

        /// <summary>
        /// Helper to support using statement.
        /// <see cref="EnterAsync(IActivityMonitor)"/> this lock and returns an awaitable <see cref="IDisposable"/> that
        /// will <see cref="Leave(IActivityMonitor)"/> this lock.
        /// <para>
        /// This returns a ValueTask (that is not IDisposable): forgetting the await in the <c>using( await _lock.LockAsync() )</c> is not possible
        /// since this doesn't compile.
        /// </para>
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <returns>The disposable to release the lock.</returns>
        public ValueTask<Releaser> LockAsync( IActivityMonitor monitor ) => new ValueTask<Releaser>( DoLockAsync( monitor ) );

        async Task<Releaser> DoLockAsync( IActivityMonitor monitor )
        {
            await EnterAsync( monitor );
            return new Releaser( this, monitor );
        }

        /// <summary>
        /// Disposabe value type.
        /// Note that the Dispose explicit implementation must not be called more
        /// than once. (Using an explicit implementation here and exposing this Releaser
        /// type should avoid any misuse.)
        /// </summary>
        public readonly struct Releaser : IDisposable
        {
            readonly AsyncLock _lock;
            readonly IActivityMonitor _monitor;

            internal Releaser( AsyncLock l, IActivityMonitor m )
            {
                _lock = l;
                _monitor = m;
            }

            void IDisposable.Dispose()
            {
                _lock.Leave( _monitor );
            }
        }

        /// <summary>
        /// Gets whether this lock is currently enter by the <paramref name="monitor"/>.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <returns>True if the monitor has entered this lock.</returns>
        /// <exception cref="ArgumentNullException">The monitor is null.</exception>
        public bool IsEnteredBy( IActivityMonitor monitor )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            return _current == monitor.Output;
        }

        /// <summary>
        /// Asynchronously waits to enter this <see cref="AsyncLock"/>.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <returns>A task that will complete when the lock has been entered.</returns>
        /// <exception cref="ArgumentNullException">The monitor is null.</exception>
        /// <exception cref="LockRecursionException">Recursion detected and <see cref="LockRecursionPolicy.NoRecursion"/> has been configured.</exception>
        public Task EnterAsync( IActivityMonitor monitor ) => EnterAsync( monitor, Timeout.Infinite, default );

        /// <summary>
        /// Asynchronously waits to enter this <see cref="AsyncLock"/>, using a 32-bit signed integer to measure the time interval,
        /// while observing a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <param name="millisecondsTimeout">
        /// The number of milliseconds to wait, or <see cref="Timeout.Infinite"/>(-1) to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe.</param>
        /// <returns>
        /// A task that will complete with a result of true if the current thread successfully entered
        /// the <see cref="AsyncLock"/>, otherwise with a result of false.
        /// </returns>
        /// <exception cref="ArgumentNullException">The monitor is null.</exception>
        /// <exception cref="System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1,
        /// which represents an infinite time-out.
        /// </exception>
        /// <exception cref="LockRecursionException">Recursion detected and <see cref="LockRecursionPolicy.NoRecursion"/> has been configured.</exception>
        public async Task<bool> EnterAsync( IActivityMonitor monitor, int millisecondsTimeout, CancellationToken cancellationToken )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( _current == monitor.Output )
            {
                if( _policy == LockRecursionPolicy.NoRecursion ) throw new LockRecursionException( Name );
                ++_recCount;
                return true;
            }
            if( await _semaphore.WaitAsync( millisecondsTimeout, cancellationToken ).ConfigureAwait( false ) )
            {
                Debug.Assert( _recCount == 0 );
                if( ShouldLog( monitor ) ) SendLine( monitor, $"Entered AsyncLock '{_name}' (async)." );
                _current = monitor.Output;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Blocks the current thread until it can enter the <see cref="AsyncLock"/>.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <exception cref="ArgumentNullException">The monitor is null.</exception>
        /// <exception cref="System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="LockRecursionException">Recursion detected and <see cref="LockRecursionPolicy.NoRecursion"/> has been configured.</exception>
        public void Enter( IActivityMonitor monitor ) => Enter( monitor, Timeout.Infinite, CancellationToken.None );

        /// <summary>
        /// Blocks the current thread until it can enter this <see cref="AsyncLock"/>, using a 32-bit signed integer to measure the time interval,
        /// while observing a <see cref="System.Threading.CancellationToken"/>.
        /// </summary>
        /// <param name="monitor">The monitor that identifies the activity.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or <see cref="Timeout.Infinite"/>(-1) to
        /// wait indefinitely.</param>
        /// <param name="cancellationToken">The <see cref="System.Threading.CancellationToken"/> to observe.</param>
        /// <returns>true if the current thread successfully entered the <see cref="AsyncLock"/>; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">The monitor is null.</exception>
        /// <exception cref="System.ObjectDisposedException">The current instance has already been
        /// disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="millisecondsTimeout"/> is a negative number other than -1,
        /// which represents an infinite time-out.
        /// </exception>
        /// <exception cref="LockRecursionException">Recursion detected and <see cref="LockRecursionPolicy.NoRecursion"/> has been configured.</exception>
        public bool Enter( IActivityMonitor monitor, int millisecondsTimeout, CancellationToken cancellationToken )
        {
            if( monitor == null ) throw new ArgumentNullException( nameof( monitor ) );
            if( _current == monitor.Output )
            {
                if( _policy == LockRecursionPolicy.NoRecursion ) throw new LockRecursionException( Name );
                ++_recCount;
                if( ShouldLog( monitor ) ) SendLine( monitor, $"Incremented AsyncLock '{_name}' recursion count ({_recCount})." );
                return true;
            }
            if( _semaphore.Wait( millisecondsTimeout, cancellationToken ) )
            {
                Debug.Assert( _recCount == 0 );
                if( ShouldLog( monitor ) ) SendLine( monitor, $"Synchronously entered AsyncLock '{_name}'." );
                _current = monitor.Output;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Leaves the lock that must have been previously entered by the <paramref name="monitor"/>.
        /// </summary>
        /// <param name="monitor">The monitor that currently holds this lock.</param>
        public void Leave( IActivityMonitor monitor )
        {
            if( _current != monitor.Output )
            {
                var msg = $"Attempt to Release AsyncLock '{_name}' that has {(_current == null ? "never been acquired" : $"been aquired by another monitor")}.";
                throw new SynchronizationLockException( msg );
            }
            Debug.Assert( _recCount >= 0 );
            if( _recCount == 0 )
            {
                if( ShouldLog( monitor ) ) SendLine( monitor, $"Released AsyncLock '{_name}'." );
                _current = null;
                _semaphore.Release();
            }
            else
            {
                if( ShouldLog( monitor ) ) SendLine( monitor, $"Decremented AsyncLock '{_name}' recursion count ({_recCount})." );
                --_recCount;
            }
        }

        static bool ShouldLog( IActivityMonitor monitor ) => monitor.ShouldLogLine( LogLevel.Debug );

        static void SendLine( IActivityMonitor monitor, string text ) => monitor.UnfilteredLog( null, LogLevel.Debug | LogLevel.IsFiltered, text, monitor.NextLogTime(), null );

        /// <summary>
        /// Overridden to return the name of this lock.
        /// </summary>
        /// <returns>The nam of this lock.</returns>
        public override string ToString() => _name;
    }

}

