using FluentAssertions;
using Microsoft.Toolkit.Diagnostics;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class LogTextHandlerTests
    {
        [Test]
        public void LogTextHandler_skips_text_building()
        {
            int i = 0;
            var m = new ActivityMonitor( false );
            m.MinimalFilter = LogFilter.Verbose;
            m.Log( LogLevel.Info, "constant" );
            m.Log( LogLevel.Info, $"I'm computed {i++}." );
            i.Should().Be( 1 );
            m.Log( LogLevel.Trace, $"I'm NOT computed {i++}." );
            i.Should().Be( 1 );
        }

        class Gen<T>
        {
            public class Sub<T2> { }
            public List<(string?, DateTime?)> Prop = new();
        }

        [Test]
        public void Types_are_logged_with_clear_names_using_Toolkit_Diagnostics_ToTypeString()
        {
            var o = new Gen<Guid>();
            var m = new ActivityMonitor( false );

            var expected = o.Prop.GetType().ToTypeString();
            CheckWithAllTextHandlers( m, o.Prop.GetType(), expected );
            CheckWithAllTextHandlers( m, (object)o.Prop.GetType(), expected );

            var oG = new Gen<Guid>.Sub<string>();
            expected = oG.GetType().ToTypeString();
            CheckWithAllTextHandlers( m, oG.GetType(), expected );
            CheckWithAllTextHandlers( m, oG.GetType(), expected );
            CheckWithAllTextHandlers( m, typeof( Gen<Guid>.Sub<string> ), expected );
            CheckWithAllTextHandlers( m, typeof( Gen<Guid>.Sub<string> ), expected );
        }

        void CheckWithAllTextHandlers<T>( IActivityMonitor monitor, T value, string expectedText )
        {
            var text = new StupidStringClient();
            monitor.Output.RegisterClient( text );

            LogWithAllTextHandlers( monitor, "~~|", value, "|~~" );

            var logs = text.Writer.ToString();
            monitor.Output.UnregisterClient( text );

            var m = Regex.Matches( logs, "~~\\|(?<1>.*?)\\|~~", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture );
            var logged = m.OfType<Match>().Select( x => x.Groups[1].Value ).Concatenate();
            var expected = Enumerable.Repeat( expectedText, 8 ).Concatenate();
            logged.Should().Be( expected );
        }

        void LogWithAllTextHandlers<T>( IActivityMonitor monitor, string prefix, T value, string suffix )
        {
            monitor.Log( LogLevel.Info, $"Log {prefix}{value}{suffix}" );
            monitor.Info( $"Line {prefix}{value}{suffix}" );
            monitor.OpenGroup( LogLevel.Info, $"Log {prefix}{value}{suffix}" );
            monitor.OpenInfo( $"Group {prefix}{value}{suffix}" );

            monitor.Log( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value}{suffix}" );
            monitor.Info( TestHelper.Tag1, $"Line With Tags {prefix}{value}{suffix}" );
            monitor.OpenGroup( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value}{suffix}" );
            monitor.OpenInfo( TestHelper.Tag1, $"Group With Tags {prefix}{value}{suffix}" );
        }

        // FYI:
        void ILSpyLogWithAllTextHandlers<T>( IActivityMonitor monitor, string prefix, T value, string suffix )
        {
            LogLevel logLevel = LogLevel.Info;
            LogLevel level = logLevel;
            bool shouldAppend;
            LogHandler.LineLog text = new LogHandler.LineLog( 4, 3, monitor, logLevel, out shouldAppend );
            if( shouldAppend )
            {
                text.AppendLiteral( "Log " );
                text.AppendFormatted( prefix );
                text.AppendFormatted( value );
                text.AppendFormatted( suffix );
            }
            monitor.Log( level, text, 61, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            LogHandler.LineInfo text2 = new LogHandler.LineInfo( 5, 3, monitor, out shouldAppend );
            if( shouldAppend )
            {
                text2.AppendLiteral( "Line " );
                text2.AppendFormatted( prefix );
                text2.AppendFormatted( value );
                text2.AppendFormatted( suffix );
            }
            monitor.Info( text2, 62, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            LogLevel logLevel2 = LogLevel.Info;
            LogLevel level2 = logLevel2;
            LogHandler.GroupLog text3 = new LogHandler.GroupLog( 4, 3, monitor, logLevel2, out shouldAppend );
            if( shouldAppend )
            {
                text3.AppendLiteral( "Log " );
                text3.AppendFormatted( prefix );
                text3.AppendFormatted( value );
                text3.AppendFormatted( suffix );
            }
            monitor.OpenGroup( level2, text3, 63, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            LogHandler.GroupInfo text4 = new LogHandler.GroupInfo( 6, 3, monitor, out shouldAppend );
            if( shouldAppend )
            {
                text4.AppendLiteral( "Group " );
                text4.AppendFormatted( prefix );
                text4.AppendFormatted( value );
                text4.AppendFormatted( suffix );
            }
            monitor.OpenInfo( text4, 64, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            LogLevel logLevel3 = LogLevel.Info;
            LogLevel level3 = logLevel3;
            CKTrait tag = TestHelper.Tag1;
            CKTrait tags = tag;
            LogHandler.LineLogWithTags text5 = new LogHandler.LineLogWithTags( 14, 3, monitor, logLevel3, tag, out shouldAppend );
            if( shouldAppend )
            {
                text5.AppendLiteral( "Log With Tags " );
                text5.AppendFormatted( prefix );
                text5.AppendFormatted( value );
                text5.AppendFormatted( suffix );
            }
            monitor.Log( level3, tags, text5, 66, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            CKTrait tag2 = TestHelper.Tag1;
            CKTrait tags2 = tag2;
            LogHandler.LineInfoWithTags text6 = new LogHandler.LineInfoWithTags( 15, 3, monitor, tag2, out shouldAppend );
            if( shouldAppend )
            {
                text6.AppendLiteral( "Line With Tags " );
                text6.AppendFormatted( prefix );
                text6.AppendFormatted( value );
                text6.AppendFormatted( suffix );
            }
            monitor.Info( tags2, text6, 67, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            LogLevel logLevel4 = LogLevel.Info;
            LogLevel level4 = logLevel4;
            CKTrait tag3 = TestHelper.Tag1;
            CKTrait tags3 = tag3;
            LogHandler.GroupLogWithTags text7 = new LogHandler.GroupLogWithTags( 14, 3, monitor, logLevel4, tag3, out shouldAppend );
            if( shouldAppend )
            {
                text7.AppendLiteral( "Log With Tags " );
                text7.AppendFormatted( prefix );
                text7.AppendFormatted( value );
                text7.AppendFormatted( suffix );
            }
            monitor.OpenGroup( level4, tags3, text7, 68, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
            CKTrait tag4 = TestHelper.Tag1;
            CKTrait tags4 = tag4;
            LogHandler.GroupInfoWithTags text8 = new LogHandler.GroupInfoWithTags( 16, 3, monitor, tag4, out shouldAppend );
            if( shouldAppend )
            {
                text8.AppendLiteral( "Group With Tags " );
                text8.AppendFormatted( prefix );
                text8.AppendFormatted( value );
                text8.AppendFormatted( suffix );
            }
            monitor.OpenInfo( tags4, text8, 69, "C:\\Dev\\CK\\CK-Core-Projects\\CK-ActivityMonitor\\Tests\\CK.ActivityMonitor.Tests\\LogTextHandlerTests.cs" );
        }

    }
}
