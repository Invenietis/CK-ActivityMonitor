using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace CK.Core.Tests.Monitoring
{

    [TestFixture]
    public class LogGateTests
    {
        [SetUp]
        protected void ResetGates()
        {
            typeof( LogGate ).GetMethod( "Reset", BindingFlags.NonPublic | BindingFlags.Static )!
                             .Invoke( null, Array.Empty<object>() );
        }

        [Test]
        public void finding_gate_by_index()
        {
            LogGate.Find( 0 ).Should().BeNull();
            LogGate.Find( 1 ).Should().BeNull();

            var g0 = new LogGate( true );
            LogGate.Find( 0 ).Should().BeSameAs( g0 );
            LogGate.Find( 1 ).Should().BeNull();

            var g1 = new LogGate( false );
            LogGate.Find( 0 ).Should().BeSameAs( g0 );
            LogGate.Find( 1 ).Should().BeSameAs( g1 );
            LogGate.Find( 2 ).Should().BeNull();

            var g2 = new LogGate( false );
            LogGate.Find( 0 ).Should().BeSameAs( g0 );
            LogGate.Find( 1 ).Should().BeSameAs( g1 );
            LogGate.Find( 2 ).Should().BeSameAs( g2 );
            LogGate.Find( 3 ).Should().BeNull();
        }

        [Test]
        public void log_method_is_not_called_at_all_when_IsOpen_is_false()
        {
            var monitor = new ActivityMonitor( false );
            var g = new LogGate( false );

            g.O( monitor )?.UnfilteredLog( ThrowingLogLevel(), null, null, null );
            g.O( monitor )?.Error( ThrowingMessage() );

            g.IsOpen = true;
            FluentActions.Invoking( () => g.O( monitor )?.UnfilteredLog( ThrowingLogLevel(), null, null, null ) )
                         .Should().Throw<CKException>().WithMessage( "Called!" );
            FluentActions.Invoking( () => g.O( monitor )?.Error( ThrowingMessage() ) )
                         .Should().Throw<CKException>().WithMessage( "Called!" );

            static LogLevel ThrowingLogLevel() => throw new CKException( "Called!" );
        }

        [Test]
        public void enemurating_gates()
        {
            LogGate.GetLogGates().Should().BeEmpty();
            var g0 = new LogGate( false );
            LogGate.GetLogGates().SequenceEqual( new[] { g0 } ).Should().BeTrue();
            var g1 = new LogGate( false );
            LogGate.GetLogGates().SequenceEqual( new[] { g0, g1 } ).Should().BeTrue();
            var g2 = new LogGate( false );
            LogGate.GetLogGates().SequenceEqual( new[] { g0, g1, g2 } ).Should().BeTrue();
            var g3 = new LogGate( false );
            LogGate.GetLogGates().SequenceEqual( new[] { g0, g1, g2, g3 } ).Should().BeTrue();
        }

        [Test]
        public void OpenedCount_and_TotalCount_are_available()
        {
            LogGate.TotalCount.Should().Be( 0 );
            LogGate.OpenedCount.Should().Be( 0 );
            var g0 = new LogGate( false );
            LogGate.TotalCount.Should().Be( 1 );
            LogGate.OpenedCount.Should().Be( 0 );
            var g1 = new LogGate( true );
            LogGate.TotalCount.Should().Be( 2 );
            LogGate.OpenedCount.Should().Be( 1 );
            var g2 = new LogGate( false );
            var g3 = new LogGate( false );
            LogGate.TotalCount.Should().Be( 4 );
            LogGate.OpenedCount.Should().Be( 1 );
            g1.IsOpen = false;
            LogGate.OpenedCount.Should().Be( 0 );
            g2.IsOpen = true;
            LogGate.OpenedCount.Should().Be( 1 );
            g1.IsOpen = g2.IsOpen = g3.IsOpen = true;
            LogGate.OpenedCount.Should().Be( 3 );
            g0.IsOpen = true;
            LogGate.OpenedCount.Should().Be( 4 );
            g0.IsOpen = g1.IsOpen = g2.IsOpen = g3.IsOpen = g3.IsOpen = false;
            LogGate.OpenedCount.Should().Be( 0 );
        }

        [Test]
        public void Open_requires_a_valid_index_and_CoreApplicationIdentity_InstanceId()
        {
            var g = new LogGate( false );
            LogGate.Open( 0, "not the instanceId", true ).Should().BeFalse();
            g.IsOpen.Should().BeFalse();

            LogGate.Open( 3712, CoreApplicationIdentity.InstanceId, true ).Should().BeFalse();
            g.IsOpen.Should().BeFalse();

            LogGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).Should().BeTrue();
            g.IsOpen.Should().BeTrue();
            LogGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).Should().BeTrue();
            g.IsOpen.Should().BeTrue();

            LogGate.Open( 0, CoreApplicationIdentity.InstanceId, false ).Should().BeTrue();
            g.IsOpen.Should().BeFalse();
        }

        [Test]
        public void StaticLogger_methods_are_not_called_at_all_when_IsOpen_is_false()
        {
            var monitor = new ActivityMonitor( false );
            var g = new LogGate( false );

            g.StaticLogger?.Fatal( ThrowingMessage() );

            g.IsOpen = true;
            FluentActions.Invoking( () => g.O( monitor )?.Fatal( ThrowingMessage() ) )
                         .Should().Throw<CKException>().WithMessage( "Called!" );

        }

        static string ThrowingMessage() => throw new CKException( "Called!" );

    }
}
