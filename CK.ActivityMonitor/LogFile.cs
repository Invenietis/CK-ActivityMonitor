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
    /// Holds centralized directory <see cref="RootLogPath"/>.
    /// </summary>
    public static class LogFile
    {
        static string? _logPath;

        /// <summary>
        /// Gets or sets (it can be set only once) the log folder to use (setting multiple time the same path is accepted).
        /// This path MUST be rooted (see <see cref="Path.GetFullPath(string)"/> and <see cref="Path.IsPathRooted(string)"/>)
        /// otherwise a <see cref="ArgumentException"/> is thrown.
        /// Once set, the path is <see cref="FileUtil.NormalizePathSeparator">normalized and ends with a path separator</see>.
        /// See remarks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When setting it, the path must be valid AND rooted.
        /// </para>
        /// <para>
        /// At initialization, a test file is created (and deleted) inside it to ensure that (at least at configuration time), no security configuration prevents
        /// us to create log files.
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
            [MemberNotNull( nameof( _logPath ) )]
            set
            {
                Throw.CheckNotNullOrWhiteSpaceArgument( value );
                value = FileUtil.NormalizePathSeparator( value, true );
                if( _logPath != null && value != _logPath )
                {
                    Throw.InvalidOperationException( ActivityMonitorResources.LogFileRootLogPathSetOnlyOnce );
                }
                if( !Path.IsPathRooted( value ) )
                {
                    Throw.ArgumentException( nameof( value ), ActivityMonitorResources.InvalidRootLogPath ); ;
                }
                try
                {
                    Directory.CreateDirectory( value );
                    string testWriteFile = Path.Combine( value, Guid.NewGuid().ToString() );
                    File.AppendAllText( testWriteFile, testWriteFile );
                    File.Delete( testWriteFile );
                    _logPath = value;
                }
                catch( Exception ex )
                {
                    Throw.Exception( $"Provided RootLogPath '{value}' must be a writable folder.", ex );
                }
            }
        }

        /// <summary>
        /// Checks that <see cref="RootLogPath"/> is correctly configured by throwing a exception if not. 
        /// </summary>
        [MemberNotNull( nameof( RootLogPath ), nameof( _logPath ) )]
        public static void AssertRootLogPathIsSet()
        {
            if( RootLogPath == null ) Throw.Exception( ActivityMonitorResources.RootLogPathMustBeSet );
            Debug.Assert( _logPath != null );
        }

    }
}

