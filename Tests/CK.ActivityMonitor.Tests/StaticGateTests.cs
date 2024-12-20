using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;

namespace CK.Core.Tests.Monitoring;

[TestFixture]
public partial class StaticGateTests
{
    [SetUp]
    protected void ResetGates()
    {
        typeof( StaticGate ).GetMethod( "Reset", BindingFlags.NonPublic | BindingFlags.Static )!
                         .Invoke( null, Array.Empty<object>() );
    }

    [Test]
    public void gates_ToString_gives_all_the_details()
    {
        var g = new StaticGate( false );
        g.ToString().Should().MatchEquivalentOf( "StaticGateTests.cs [Closed] @*/Tests/CK.ActivityMonitor.Tests/StaticGateTests.cs;* - Key: 0" );

        var gN = new StaticGate( "Hop", true );
        gN.ToString().Should().MatchEquivalentOf( "Hop [Opened] @*/Tests/CK.ActivityMonitor.Tests/StaticGateTests.cs;* - Key: 1" );
    }

    [Test]
    public void finding_gate_by_index()
    {
        StaticGate.Find( 0 ).Should().BeNull();
        StaticGate.Find( 1 ).Should().BeNull();

        var g0 = new StaticGate( true );
        StaticGate.Find( 0 ).Should().BeSameAs( g0 );
        StaticGate.Find( 1 ).Should().BeNull();

        var g1 = new StaticGate( false );
        StaticGate.Find( 0 ).Should().BeSameAs( g0 );
        StaticGate.Find( 1 ).Should().BeSameAs( g1 );
        StaticGate.Find( 2 ).Should().BeNull();

        var g2 = new StaticGate( false );
        StaticGate.Find( 0 ).Should().BeSameAs( g0 );
        StaticGate.Find( 1 ).Should().BeSameAs( g1 );
        StaticGate.Find( 2 ).Should().BeSameAs( g2 );
        StaticGate.Find( 3 ).Should().BeNull();
    }

    [Test]
    public void log_method_is_not_called_at_all_when_IsOpen_is_false()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var g = new StaticGate( false );

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
        StaticGate.GetStaticGates().Should().BeEmpty();
        var g0 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0 } ).Should().BeTrue();
        var g1 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1 } ).Should().BeTrue();
        var g2 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1, g2 } ).Should().BeTrue();
        var g3 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1, g2, g3 } ).Should().BeTrue();
    }

    [Test]
    public void OpenedCount_and_TotalCount_are_available()
    {
        StaticGate.TotalCount.Should().Be( 0 );
        StaticGate.OpenedCount.Should().Be( 0 );
        var g0 = new StaticGate( false );
        StaticGate.TotalCount.Should().Be( 1 );
        StaticGate.OpenedCount.Should().Be( 0 );
        var g1 = new StaticGate( true );
        StaticGate.TotalCount.Should().Be( 2 );
        StaticGate.OpenedCount.Should().Be( 1 );
        var g2 = new StaticGate( false );
        var g3 = new StaticGate( false );
        StaticGate.TotalCount.Should().Be( 4 );
        StaticGate.OpenedCount.Should().Be( 1 );
        g1.IsOpen = false;
        StaticGate.OpenedCount.Should().Be( 0 );
        g2.IsOpen = true;
        StaticGate.OpenedCount.Should().Be( 1 );
        g1.IsOpen = g2.IsOpen = g3.IsOpen = true;
        StaticGate.OpenedCount.Should().Be( 3 );
        g0.IsOpen = true;
        StaticGate.OpenedCount.Should().Be( 4 );
        g0.IsOpen = g1.IsOpen = g2.IsOpen = g3.IsOpen = g3.IsOpen = false;
        StaticGate.OpenedCount.Should().Be( 0 );
    }

    [Test]
    public void Open_requires_a_valid_index_and_CoreApplicationIdentity_InstanceId()
    {
        var g = new StaticGate( false );
        StaticGate.Open( 0, "not the instanceId", true ).Should().BeFalse();
        g.IsOpen.Should().BeFalse();

        StaticGate.Open( 3712, CoreApplicationIdentity.InstanceId, true ).Should().BeFalse();
        g.IsOpen.Should().BeFalse();

        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).Should().BeTrue();
        g.IsOpen.Should().BeTrue();
        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).Should().BeTrue();
        g.IsOpen.Should().BeTrue();

        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, false ).Should().BeTrue();
        g.IsOpen.Should().BeFalse();
    }

    [Test]
    public void StaticLogger_methods_are_not_called_at_all_when_IsOpen_is_false()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var g = new StaticGate( false );

        g.StaticLogger?.Fatal( ThrowingMessage() );

        g.IsOpen = true;
        FluentActions.Invoking( () => g.O( monitor )?.Fatal( ThrowingMessage() ) )
                     .Should().Throw<CKException>().WithMessage( "Called!" );

    }

    static string ThrowingMessage() => throw new CKException( "Called!" );

    [Test]
    public void StaticGatesConfigurator_tests()
    {
        StaticGate.TotalCount.Should().Be( 0 );
        StaticGateConfigurator.GetConfiguration().Should().BeEmpty();

        var gates = Enumerable.Range( 0, 5 ).Select( i => new StaticGate( $"n°{i}", false ) ).ToArray();
        var c = StaticGateConfigurator.GetConfiguration();
        c.Split( ';' ).All( x => x.EndsWith( ":!" ) ).Should().BeTrue();

        gates[0].IsOpen = true;
        gates[2].IsOpen = true;
        c = StaticGateConfigurator.GetConfiguration();
        c.Should().Be( "n°0;n°1:!;n°2;n°3:!;n°4:!" );

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            StaticGateConfigurator.ApplyConfiguration( TestHelper.Monitor, " FutureGateMustBeClosed  :   ! ;  n°0 : !  ;AnotherFutureMustBeOpened" );
            logs.Should().HaveCount( 1 );
            logs[0].Should().Match( "Applying StaticGate configuration: '*" );
        }
        gates[0].IsOpen.Should().BeFalse();
        var f = new StaticGate( "FutureGateMustBeClosed", open: true );
        f.IsOpen.Should().BeFalse( "Even if f wanted to be opened, current configuration closed it." );

        var a = new StaticGate( "AnotherFutureMustBeOpened", false );
        a.IsOpen.Should().BeTrue( "Even if a is initially closed, current configuration opened it." );

        StaticGateConfigurator.ApplyConfiguration( TestHelper.Monitor, "" );
    }

}
