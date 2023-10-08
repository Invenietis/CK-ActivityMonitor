using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
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
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            m.MinimalFilter = LogFilter.Verbose;
            m.Log( LogLevel.Info, "constant" );
            m.Log( LogLevel.Info, $"I'm computed {i++}." );
            i.Should().Be( 1 );
            m.Log( LogLevel.Trace, $"I'm NOT computed {i++}." );
            i.Should().Be( 1 );
        }

        class Nested<T> { }

        [Test]
        public void logging_types()
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );

            using( m.CollectTexts( out var messages ) )
            {
                // t.ToString()            "CK.Core.Tests.Monitoring.LogTextHandlerTests+Nested`1[System.Collections.Generic.Dictionary`2[System.Int32,System.ValueTuple`2[System.String,System.Nullable`1[System.Int32]]]]"
                // t.FullName              "CK.Core.Tests.Monitoring.LogTextHandlerTests+Nested`1[[System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.ValueTuple`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]"
                // t.AssemblyQualifiedName "CK.Core.Tests.Monitoring.LogTextHandlerTests+Nested`1[[System.Collections.Generic.Dictionary`2[[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.ValueTuple`2[[System.String, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=6.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], CK.ActivityMonitor.Tests, Version=0.0.0.0, Culture=neutral, PublicKeyToken=731c291b31fb8d27"
                var t = typeof( Nested<Dictionary<int, (string, int?)>> );
                var toString = t.ToString();
                var fullName = t.FullName;
                var qualifiedName = t.AssemblyQualifiedName;

                m.Info( $"Type: {t}" );
                messages[0].Should().Be( $"Type: {toString}" );

                m.Info( $"Type: {t:F}" );
                messages[1].Should().Be( $"Type: {fullName}" );

                m.Info( $"Type: {t:A}" );
                messages[2].Should().Be( $"Type: {qualifiedName}" );

                m.Info( $"Type: {t:C}" );
                messages[3].Should().Be( "Type: LogTextHandlerTests.Nested<Dictionary<int,(string,int?)>>" );

                m.Info( $"Type: {t:N}" );
                messages[4].Should().Be( "Type: CK.Core.Tests.Monitoring.LogTextHandlerTests.Nested<System.Collections.Generic.Dictionary<int,(string,int?)>>" );
            }
        }

        [Test]
        public void logging_null_type()
        {
            var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
            Type? type = null;
            using( monitor.CollectTexts( out var logs ) )
            {
                monitor.Log( LogLevel.Info, $"Type: {type}" );
                logs[0].Should().Be( "Type: " );

                monitor.OpenGroup( LogLevel.Info, $"Type: {type}" ).Dispose();
                logs[1].Should().Be( "Type: " );

                monitor.Info( $"Type: {type}" );
                logs[2].Should().Be( "Type: " );

                monitor.OpenInfo( $"Type: {type}" ).Dispose();
                logs[3].Should().Be( "Type: " );
            }
            using( monitor.CollectTexts( out var logs ) )
            {
                monitor.Log( LogLevel.Info, $"Type: {type:C}" );
                logs[0].Should().Be( "Type: null" );

                monitor.OpenGroup( LogLevel.Info, $"Type: {type:C}" ).Dispose();
                logs[1].Should().Be( "Type: null" );

                monitor.Info( $"Type: {type:C}" );
                logs[2].Should().Be( "Type: null" );

                monitor.OpenInfo( $"Type: {type:C}" ).Dispose();
                logs[3].Should().Be( "Type: null" );
            }
        }


        class Gen<T>
        {
            public class Sub<T2> { }
            public List<(string?, DateTime?)> Prop = new();
        }

        [Test]
        public void Types_are_logged_with_csharp_names_with_Type_Format_C_with_all_overloads()
        {
            var m = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );

            var o = new Gen<Guid>();

            var expected = o.Prop.GetType().ToCSharpName( withNamespace: false );
            // This test like an "object" (see below).
            CheckWithAllTextHandlers( m, o.Prop.GetType(), expected );
            // So this is the same as this one:
            CheckWithAllTextHandlers( m, (object)o.Prop.GetType(), expected );
            // To test with Type, we cannot use the generic sender but the explicitly typed one.
            CheckWithAllTextHandlersWithExplicitype( m, o.Prop.GetType(), expected );

            var oG = new Gen<Guid>.Sub<string>();
            expected = oG.GetType().ToCSharpName( withNamespace: false );
            CheckWithAllTextHandlers( m, oG.GetType(), expected );
            CheckWithAllTextHandlersWithExplicitype( m, oG.GetType(), expected );
            CheckWithAllTextHandlers( m, typeof( Gen<Guid>.Sub<string> ), expected );
            CheckWithAllTextHandlersWithExplicitype( m, typeof( Gen<Guid>.Sub<string> ), expected );

            // My first idea: this generic method should be routed to AppendFormatted( Type t, string? format )
            // when T is Type and to the AppendFormatted<T>( T value, string? format ) when Type is an "object".
            // But it's not! This is always routed to the AppendFormatted<T>( T value, string? format ).
            //
            // This is why the generic method handler on the InternalHandler has to match the T as a Type to reroute the call.
            // The AppendFormatted( Type t, string? format ) overload is correctly selected when
            // using a regular (ie. non generic) Type.

            // These 2 calls are (correctly) handled by the AppendFormatted( Type t, string? format ) overload.
            m.Info( $"{typeof( Gen<Guid>.Sub<string> ):C}" ); 
            m.Info( $"{oG.GetType():C}" ); 

            // This "generic" acts as the "object tester". I keep it but copy it with an explict Type
            // below to test all the overloads/interpolated handlers...
            static void CheckWithAllTextHandlers<T>( IActivityMonitor monitor, T t, string expectedText )
            {
                using( monitor.CollectTexts( out var messages ) )
                {
                    LogWithAllTextHandlers( monitor, "~~|", t, "|~~" );

                    var typeNames = messages.Select( t => Regex.Matches( t, "~~\\|(?<1>.*?)\\|~~", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )
                                               .OfType<Match>().Single().Groups[1].Value );

                    typeNames.Should().HaveCount( 8 )
                                      .And.OnlyContain( t => t == expectedText );
                }

                static void LogWithAllTextHandlers( IActivityMonitor monitor, string prefix, T value, string suffix )
                {
                    monitor.Log( LogLevel.Info, $"Log {prefix}{value:C}{suffix}" );
                    monitor.Info( $"Line {prefix}{value:C}{suffix}" );
                    monitor.OpenGroup( LogLevel.Info, $"Log {prefix}{value:C}{suffix}" );
                    monitor.OpenInfo( $"Group {prefix}{value:C}{suffix}" );

                    monitor.Log( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value:C}{suffix}" );
                    monitor.Info( TestHelper.Tag1, $"Line With Tags {prefix}{value:C}{suffix}" );
                    monitor.OpenGroup( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value:C}{suffix}" );
                    monitor.OpenInfo( TestHelper.Tag1, $"Group With Tags {prefix}{value:C}{suffix}" );
                }

            }

            static void CheckWithAllTextHandlersWithExplicitype( IActivityMonitor monitor, Type t, string expectedText )
            {
                using( monitor.CollectTexts( out var messages ) )
                {
                    LogWithAllTextHandlers( monitor, "~~|", t, "|~~" );

                    var typeNames = messages.Select( t => Regex.Matches( t, "~~\\|(?<1>.*?)\\|~~", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture )
                                               .OfType<Match>().Single().Groups[1].Value );

                    typeNames.Should().HaveCount( 8 )
                                      .And.OnlyContain( t => t == expectedText );
                }

                static void LogWithAllTextHandlers( IActivityMonitor monitor, string prefix, Type value, string suffix )
                {
                    monitor.Log( LogLevel.Info, $"Log {prefix}{value:C}{suffix}" );
                    monitor.Info( $"Line {prefix}{value:C}{suffix}" );
                    monitor.OpenGroup( LogLevel.Info, $"Log {prefix}{value:C}{suffix}" );
                    monitor.OpenInfo( $"Group {prefix}{value:C}{suffix}" );

                    monitor.Log( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value:C}{suffix}" );
                    monitor.Info( TestHelper.Tag1, $"Line With Tags {prefix}{value:C}{suffix}" );
                    monitor.OpenGroup( LogLevel.Info, TestHelper.Tag1, $"Log With Tags {prefix}{value:C}{suffix}" );
                    monitor.OpenInfo( TestHelper.Tag1, $"Group With Tags {prefix}{value:C}{suffix}" );
                }

            }

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
