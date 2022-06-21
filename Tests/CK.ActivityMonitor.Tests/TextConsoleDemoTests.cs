using System;
using NUnit.Framework;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class TextConsoleDemoTests
    {
        static readonly Exception _exceptionWithInner;
        static readonly Exception _exceptionWithInnerLoader;
        static int _toggleConsoleCount = 0;

        #region static initialization of exceptions
        static TextConsoleDemoTests()
        {
            _exceptionWithInner = ThrowExceptionWithInner( false );
            _exceptionWithInnerLoader = ThrowExceptionWithInner( true );
        }

        static Exception ThrowExceptionWithInner( bool loaderException = false )
        {
            Exception e;
            try { throw new Exception( "Outer", loaderException ? ThrowLoaderException() : ThrowSimpleException( "Inner" ) ); }
            catch( Exception ex ) { e = ex; }
            return e;
        }

        static Exception ThrowSimpleException( string message )
        {
            Exception e;
            try { throw new Exception( message ); }
            catch( Exception ex ) { e = ex; }
            return e;
        }

        static Exception ThrowLoaderException()
        {
            Exception? e = null;
            try { Type.GetType( "A.Type, An.Unexisting.Assembly", true ); }
            catch( Exception ex ) { e = ex; }
            return e!;
        }
        #endregion

        [Test]
        [Explicit]
        public void Console_toggle()
        {
            TestHelper.Monitor.Info( $"Before toggle console: {++_toggleConsoleCount}" );
            TestHelper.LogsToConsole = !TestHelper.LogsToConsole;
            TestHelper.Monitor.Info( $"After toggle console: {_toggleConsoleCount}" );
        }

        [Test]
        [Explicit]
        public void Console_sample()
        {
            var m = new ActivityMonitor( false );
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            DumpSampleLogs1( m );
            DumpSampleLogs2( m );
        }

        static void DumpSampleLogs1( IActivityMonitor m )
        {
            m.SetTopic( "First Activity..." );
            using( m.OpenTrace( "Opening trace" ) )
            {
                m.Trace( "A trace in group." );
                m.Info( "An info in group." );
                m.Warn( "A warning in group." );
                m.Error( "An error in group." );
                m.Fatal( "A fatal in group." );
            }
            m.Trace( "End of first activity." );
        }

        static void DumpSampleLogs2( IActivityMonitor m )
        {
            m.Fatal( "An error occured", _exceptionWithInner );
            m.Fatal( "Same error occured (wrapped in CKException)", new CKException( CKExceptionData.CreateFrom( _exceptionWithInner ) ) );
            m.SetTopic( "This is a topic..." );
            m.Trace( "a trace" );
            m.Trace( "another one" );
            m.SetTopic( "Please, show this topic!" );
            m.Trace( "Anotther trace." );
            using( m.OpenTrace( "A group trace." ) )
            {
                m.Trace( "A trace in group." );
                m.Info( "An info..." );
                using( m.OpenInfo( @"A group information... with a 
multi
-line
message. 
This MUST be correctly indented!" ) )
                {
                    m.Info( "Info in info group." );
                    m.Info( "Another info in info group." );
                    m.Error( "An error.", _exceptionWithInnerLoader );
                    m.Error( "Same error occured (wrapped in CKException)", new CKException( CKExceptionData.CreateFrom( _exceptionWithInnerLoader ) ) );
                    m.Warn( "A warning." );
                    m.Trace( "Something must be said (1/3)." );
                    m.Trace( "Something must be said (2/3)." );
                    m.Trace( "Something must be said (3/3)." );
                    m.CloseGroup( "Everything is in place." );
                }
            }
            m.SetTopic( null! );
            using( m.OpenTrace( "A group with multiple conclusions." ) )
            {
                using( m.OpenTrace( "A group with no conclusion." ) )
                {
                    m.Trace( "Something must be said." );
                }
                m.CloseGroup( new[] {
                        new ActivityLogGroupConclusion( "My very first conclusion." ),
                        new ActivityLogGroupConclusion( "My second conclusion." ),
                        new ActivityLogGroupConclusion( @"My very last conclusion
is a multi line one.
and this is fine!" )
                    } );
            }
            m.Trace( "This is the final trace." );
        }

    }
}
