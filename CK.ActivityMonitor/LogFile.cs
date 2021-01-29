using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using CK.Core.Impl;

namespace CK.Core
{
    /// <summary>
    /// Holds centralized directory <see cref="RootLogPath"/> and by default handles
    /// errors emitted by <see cref="ActivityMonitor.CriticalErrorCollector"/>.
    /// Critical errors will be logged in <see cref="CriticalErrorsPath"/> only if <see cref="RootLogPath"/>
    /// is set and <see cref="TrackActivityMonitorLoggingError"/> is true (that is the default).
    /// </summary>
    public static class LogFile
    {
        static string? _logPath;
        static string? _criticalErrorsPath;
        static int _activityMonitorErrorTracked;
        const string CriticalErrorsSubPath = "CriticalErrors";

        static LogFile()
        {
            _activityMonitorErrorTracked = 1;
            ActivityMonitor.CriticalErrorCollector.OnErrorFromBackgroundThreads += OnTrackActivityMonitorLoggingError;
        }

        static void OnTrackActivityMonitorLoggingError( object? sender, CriticalErrorCollector.ErrorEventArgs e )
        {
            string? logPath = _logPath;
            if( logPath != null )
            {
                foreach( var error in e.Errors )
                {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append( "Error#" ).Append( error.SequenceNumber ).Append( "@" ).Append( error.ErrorCreationTimeUtc.ToString( FileUtil.FileNameUniqueTimeUtcFormat ) );
                    if( error.LostErrorCount != 0 ) buffer.Append( " !! " ).Append( error.LostErrorCount ).Append( " lost critical error messages." );
                    buffer.AppendLine();
                    if( !String.IsNullOrEmpty( error.Comment ) ) buffer.Append( error.Comment ).AppendLine();
                    if( error.Exception != null ) ActivityMonitorTextWriterClient.DumpException( buffer, String.Empty, !ReferenceEquals( error.Comment, error.Exception.Message ), error.Exception );
                    FileUtil.WriteUniqueTimedFile( _criticalErrorsPath, ".txt", DateTime.UtcNow, Encoding.UTF8.GetBytes( buffer.ToString() ), withUTF8Bom: true );
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="RootLogPath"/>CriticalErrors/" path.
        /// RootLogPath must be set otherwise a exception is thrown.
        /// </summary>
        static public string CriticalErrorsPath
        {
            get
            {
                AssertRootLogPathIsSet();
                return _criticalErrorsPath;
            }
        }

        /// <summary>
        /// Gets or sets whether <see cref="ActivityMonitor.CriticalErrorCollector"/> are tracked (this is thread safe).
        /// When true, <see cref="CriticalErrorCollector.ErrorEventArgs"/> are tracked and written to a file (if <see cref="RootLogPath"/> is not null).
        /// Defaults to true.
        /// </summary>
        static public bool TrackActivityMonitorLoggingError
        {
            get { return _activityMonitorErrorTracked == 1; }
            set
            {
                if( value )
                {
                    if( Interlocked.CompareExchange( ref _activityMonitorErrorTracked, 1, 0 ) == 0 )
                    {
                        ActivityMonitor.CriticalErrorCollector.OnErrorFromBackgroundThreads += OnTrackActivityMonitorLoggingError;
                    }
                }
                else if( Interlocked.CompareExchange( ref _activityMonitorErrorTracked, 0, 1 ) == 1 )
                {
                    ActivityMonitor.CriticalErrorCollector.OnErrorFromBackgroundThreads -= OnTrackActivityMonitorLoggingError;
                }
            }
        }

        /// <summary>
        /// Gets or sets (it can be set only once) the log folder to use (setting multiple time the same path is accepted).
        /// Thie path MUST be rooted (see <see cref="Path.GetFullPath(string)"/> and <see cref="Path.IsPathRooted(string)"/>)
        /// otherwise a <see cref="ArgumentException"/> is thrown.
        /// Once set, the path is <see cref="FileUtil.NormalizePathSeparator">normalized and ends with a path separator</see>.
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When setting it, the path must be valid AND rooted.
        /// </para>
        /// <para>
        /// The subordinate directory "CriticalErrors" is created (if not already here) and a test file is created (and deleted) inside it 
        /// to ensure that (at least at configuration time), no security configuration prevents us to create log files: all errors files will be created in this sub directory.
        /// </para>
        /// <para>
        /// It is recommended to use this directory to store all other logs and/or files related to activity tracking.
        /// </para>
        /// <para>
        /// When not null, it necessarily ends with a <see cref="Path.DirectorySeparatorChar"/>.
        /// </para>
        /// </remarks>
        [DisallowNull]
        static public string? RootLogPath
        {
            get { return _logPath; }
            [MemberNotNull( nameof( _criticalErrorsPath ), nameof( _logPath ) )]
            set
            {
                if( string.IsNullOrWhiteSpace( value ) ) throw new ArgumentNullException();
                value = FileUtil.NormalizePathSeparator( value, true );
                if( _logPath != null && value != _logPath )
                {
                    throw new InvalidOperationException( ActivityMonitorResources.LogFileRootLogPathSetOnlyOnce );
                }
                if( !Path.IsPathRooted( value ) )
                {
                    throw new ArgumentException( ActivityMonitorResources.InvalidRootLogPath );
                }
                try
                {
                    string dirName = value + CriticalErrorsSubPath + Path.DirectorySeparatorChar;
                    if( !Directory.Exists( dirName ) ) Directory.CreateDirectory( dirName );
                    string testWriteFile = Path.Combine( dirName, Guid.NewGuid().ToString() );
                    File.AppendAllText( testWriteFile, testWriteFile );
                    File.Delete( testWriteFile );
                    _logPath = value;
                    _criticalErrorsPath = dirName;
                }
                catch( Exception ex )
                {
                    throw new Exception( ActivityMonitorResources.InvalidRootLogPath, ex );
                }
            }
        }

        /// <summary>
        /// Checks that <see cref="RootLogPath"/> is correctly configured by throwing a exception if not. 
        /// </summary>
        [MemberNotNull( nameof( RootLogPath ), nameof( _criticalErrorsPath ), nameof( _logPath ) )]
        public static void AssertRootLogPathIsSet()
        {
            if( RootLogPath == null ) throw new Exception( ActivityMonitorResources.RootLogPathMustBeSet );
            Debug.Assert( _criticalErrorsPath != null );
            Debug.Assert( _logPath != null );
        }

    }
}

