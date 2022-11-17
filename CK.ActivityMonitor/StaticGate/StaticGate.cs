using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Simple boolean gate that can control log emission or any access to object reference
    /// with minimal overhead.
    /// This class is totally thread safe.
    /// <para>
    /// Gates must be static, they cannot be removed or destroyed and live for the lifetime of the application.
    /// </para>
    /// </summary>
    public sealed partial class StaticGate
    {
        readonly static Logger _gateLogger = new Logger();
        static object _registerLock;
        static StaticGate? _first;
        static StaticGate? _last;
        static int _count;
        static int _activeCount;

        StaticGate? _prev;
        bool _isOpen;
        bool _hasDisplayName;

        /// <summary>
        /// Creates a gate with no display name (it will be the <see cref="FilePath"/>).
        /// </summary>
        /// <param name="open">Whether to initially open this gate or not.</param>
        /// <param name="fileName">Source file name of the instantiation (automatically injected by C# compiler).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        public StaticGate( bool open, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
            : this( fileName!, open, fileName, lineNumber )
        {
        }

        /// <summary>
        /// Creates a gate with a display name.
        /// </summary>
        /// <param name="displayName">
        /// The display name. Must not be empty or whitespace nor contain
        /// comma ',', colon ':', semicolon ';', exclamation mark '!', pipe '|', ampersand '&amp;', double quotes '"', and parentheses '(', ')'.
        /// </param>
        /// <param name="open">Whether to initially open this gate or not.</param>
        /// <param name="fileName">Source file name of the instantiation (automatically injected by C# compiler).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler).</param>
        public StaticGate( string displayName, bool open, [CallerFilePath] string? fileName = null, [CallerLineNumber] int lineNumber = 0 )
        {
            Throw.CheckNotNullOrWhiteSpaceArgument( fileName );
            Throw.CheckNotNullOrWhiteSpaceArgument( displayName );
            FilePath = fileName;
            LineNumber = lineNumber;
            displayName = displayName.Trim();
            if( _hasDisplayName = displayName != fileName )
            {
                Throw.CheckArgument( displayName.AsSpan().IndexOfAny( ",:;!|&\",()" ) < 0 );
                DisplayName = displayName;
            }
            else
            {
                DisplayName = FilePath.LastPart;
            }
            Key = Interlocked.Increment( ref _count ) - 1;
            if( _isOpen = open ) Interlocked.Increment( ref _activeCount );
            OnNewStaticGate?.Invoke( this );
            lock( _registerLock )
            {
                if( _last == null ) _last = _first = this;
                else _last._prev = this;
                _last = this;
            }
        }

        static StaticGate()
        {
            _registerLock = new object();
        }

        /// <summary>
        /// This is only called by tests (with crappy reflection).
        /// </summary>
        static void Reset()
        {
            lock( _registerLock )
            {
                _last = _first = null;
                _activeCount = _count = 0;
            }
        }

        /// <summary>
        /// Provides a hook that is called when a new StaticGate is created.
        /// This is called before the StaticGate is registered and appears in
        /// <see cref="GetStaticGates()"/> enumeration.
        /// </summary>
        public static event Action<StaticGate>? OnNewStaticGate;

        /// <summary>
        /// Gets the number of log gates.
        /// </summary>
        public static int TotalCount => _count;

        /// <summary>
        /// Gets the number of opened log gates.
        /// </summary>
        public static int OpenedCount => _activeCount;

        /// <summary>
        /// Sets the <see cref="IsOpen"/> property of a gate by its key, ensuring that
        /// <see cref="CoreApplicationIdentity.InstanceId"/> is known by the caller.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="instanceId">Must be <see cref="CoreApplicationIdentity.InstanceId"/>, otherwise nothing is done.</param>
        /// <param name="open">True to open, false to close.</param>
        /// <returns>True if the operation has been applied, false otherwise.</returns>
        public static bool Open( int key, string instanceId, bool open )
        {
            if( instanceId == CoreApplicationIdentity.InstanceId )
            {
                StaticGate? g = Find( key );
                if( g != null )
                {
                    g.IsOpen = open;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets all the registered gates, order by their <see cref="Key"/>.
        /// </summary>
        /// <returns>All registered gates.</returns>
        public static IEnumerable<StaticGate> GetStaticGates()
        {
            var g = Volatile.Read( ref _first );
            while( g != null )
            {
                yield return g;
                g = g._prev;
            }
        }

        /// <summary>
        /// Finds a gate by its key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>The gate or null.</returns>
        public static StaticGate? Find( int key )
        {
            var g = _first;
            if( key >= _count ) return null;
            while( --key >= 0 )
            {
                Debug.Assert( g != null );
                g = g._prev;
            }
            return g;
        }

        /// <summary>
        /// Gets whether this gate has an actual <see cref="DisplayName"/>
        /// or its display name is the <see cref="FilePath"/>.
        /// </summary>
        public bool HasDisplayName => _hasDisplayName;

        /// <summary>
        /// Gets the display name of this gate.
        /// <para>
        /// When <see cref="HasDisplayName"/> is true, this name cannot contain
        /// comma ',', colon ':', semicolon ';', exclamation mark '!', pipe '|', ampersand '&amp;', double quotes '"', and parentheses '(', ')'.
        /// </para>
        /// <para>
        /// When <see cref="HasDisplayName"/> is false, this is the <see cref="FilePath"/>'s <see cref="NormalizedPath.LastPart"/>.
        /// </para>
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the file name where this gate has been instantiated.
        /// </summary>
        public NormalizedPath FilePath { get; }

        /// <summary>
        /// Gets the file number of the <see cref="FilePath"/> where this gate has been instantiated.
        /// </summary>
        public int LineNumber { get; }

        /// <summary>
        /// Gets a unique incremented key that identifies this gate.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Gets or sets whether this gate is opened.
        /// </summary>
        public bool IsOpen
        {
            get => _isOpen;
            set
            {
                var a = _isOpen;
                if( a != value )
                {
                    if( _isOpen = value )
                    {
                        Interlocked.Increment( ref _activeCount );
                    }
                    else
                    {
                        Interlocked.Decrement( ref _activeCount );
                    }
                }
            }
        }

        /// <summary>
        /// Overridden to return all the attributes in a readable string.
        /// </summary>
        /// <returns>Detailed current information about this gate.</returns>
        public override string ToString() => $"{DisplayName} [{(_isOpen ? "Open" : "Clos" )}ed] @{FilePath};{LineNumber} - Key: {Key}";

        /// <summary>
        /// Returns the <paramref name="instance"/> if this gate is opened, null otherwise.
        /// </summary>
        /// <param name="instance">A non null object.</param>
        /// <returns>The <paramref name="instance"/> if this gate is opened, null otherwise.</returns>
        public T? O<T>( T instance ) where T : class => _isOpen ? instance : null;

        /// <summary>
        /// Gets a <see cref="ActivityMonitor.StaticLogger"/> relay if this gate is opened, null otherwise.
        /// </summary>
        public Logger? StaticLogger => _isOpen ? _gateLogger : null;
    }

}
