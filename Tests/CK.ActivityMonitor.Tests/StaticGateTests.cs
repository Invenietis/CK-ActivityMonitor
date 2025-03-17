using Shouldly;
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
        g.ToString().ShouldMatch( @"StaticGateTests\.cs \[Closed] @.*/Tests/CK\.ActivityMonitor\.Tests/StaticGateTests\.cs;.* - Key: 0" );

        var gN = new StaticGate( "Hop", true );
        gN.ToString().ShouldMatch( @"Hop \[Opened] @.*/Tests/CK\.ActivityMonitor\.Tests/StaticGateTests\.cs;.* - Key: 1" );
    }

    [Test]
    public void finding_gate_by_index()
    {
        StaticGate.Find( 0 ).ShouldBeNull();
        StaticGate.Find( 1 ).ShouldBeNull();

        var g0 = new StaticGate( true );
        StaticGate.Find( 0 ).ShouldBeSameAs( g0 );
        StaticGate.Find( 1 ).ShouldBeNull();

        var g1 = new StaticGate( false );
        StaticGate.Find( 0 ).ShouldBeSameAs( g0 );
        StaticGate.Find( 1 ).ShouldBeSameAs( g1 );
        StaticGate.Find( 2 ).ShouldBeNull();

        var g2 = new StaticGate( false );
        StaticGate.Find( 0 ).ShouldBeSameAs( g0 );
        StaticGate.Find( 1 ).ShouldBeSameAs( g1 );
        StaticGate.Find( 2 ).ShouldBeSameAs( g2 );
        StaticGate.Find( 3 ).ShouldBeNull();
    }

    [Test]
    public void log_method_is_not_called_at_all_when_IsOpen_is_false()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var g = new StaticGate( false );

        g.O( monitor )?.UnfilteredLog( ThrowingLogLevel(), null, null, null );
        g.O( monitor )?.Error( ThrowingMessage() );

        g.IsOpen = true;
        Util.Invokable( () => g.O( monitor )?.UnfilteredLog( ThrowingLogLevel(), null, null, null ) )
                     .ShouldThrow<CKException>().Message.ShouldBe( "Called!" );
        Util.Invokable( () => g.O( monitor )?.Error( ThrowingMessage() ) )
                     .ShouldThrow<CKException>().Message.ShouldBe( "Called!" );

        static LogLevel ThrowingLogLevel() => throw new CKException( "Called!" );
    }

    [Test]
    public void enemurating_gates()
    {
        StaticGate.GetStaticGates().ShouldBeEmpty();
        var g0 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0 } ).ShouldBeTrue();
        var g1 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1 } ).ShouldBeTrue();
        var g2 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1, g2 } ).ShouldBeTrue();
        var g3 = new StaticGate( false );
        StaticGate.GetStaticGates().SequenceEqual( new[] { g0, g1, g2, g3 } ).ShouldBeTrue();
    }

    [Test]
    public void OpenedCount_and_TotalCount_are_available()
    {
        StaticGate.TotalCount.ShouldBe( 0 );
        StaticGate.OpenedCount.ShouldBe( 0 );
        var g0 = new StaticGate( false );
        StaticGate.TotalCount.ShouldBe( 1 );
        StaticGate.OpenedCount.ShouldBe( 0 );
        var g1 = new StaticGate( true );
        StaticGate.TotalCount.ShouldBe( 2 );
        StaticGate.OpenedCount.ShouldBe( 1 );
        var g2 = new StaticGate( false );
        var g3 = new StaticGate( false );
        StaticGate.TotalCount.ShouldBe( 4 );
        StaticGate.OpenedCount.ShouldBe( 1 );
        g1.IsOpen = false;
        StaticGate.OpenedCount.ShouldBe( 0 );
        g2.IsOpen = true;
        StaticGate.OpenedCount.ShouldBe( 1 );
        g1.IsOpen = g2.IsOpen = g3.IsOpen = true;
        StaticGate.OpenedCount.ShouldBe( 3 );
        g0.IsOpen = true;
        StaticGate.OpenedCount.ShouldBe( 4 );
        g0.IsOpen = g1.IsOpen = g2.IsOpen = g3.IsOpen = g3.IsOpen = false;
        StaticGate.OpenedCount.ShouldBe( 0 );
    }

    [Test]
    public void Open_requires_a_valid_index_and_CoreApplicationIdentity_InstanceId()
    {
        var g = new StaticGate( false );
        StaticGate.Open( 0, "not the instanceId", true ).ShouldBeFalse();
        g.IsOpen.ShouldBeFalse();

        StaticGate.Open( 3712, CoreApplicationIdentity.InstanceId, true ).ShouldBeFalse();
        g.IsOpen.ShouldBeFalse();

        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).ShouldBeTrue();
        g.IsOpen.ShouldBeTrue();
        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, true ).ShouldBeTrue();
        g.IsOpen.ShouldBeTrue();

        StaticGate.Open( 0, CoreApplicationIdentity.InstanceId, false ).ShouldBeTrue();
        g.IsOpen.ShouldBeFalse();
    }

    [Test]
    public void StaticLogger_methods_are_not_called_at_all_when_IsOpen_is_false()
    {
        var monitor = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        var g = new StaticGate( false );

        g.StaticLogger?.Fatal( ThrowingMessage() );

        g.IsOpen = true;
        Util.Invokable( () => g.O( monitor )?.Fatal( ThrowingMessage() ) )
                     .ShouldThrow<CKException>().Message.ShouldBe( "Called!" );

    }

    static string ThrowingMessage() => throw new CKException( "Called!" );

    [Test]
    public void StaticGatesConfigurator_tests()
    {
        StaticGate.TotalCount.ShouldBe( 0 );
        StaticGateConfigurator.GetConfiguration().ShouldBeEmpty();

        var gates = Enumerable.Range( 0, 5 ).Select( i => new StaticGate( $"n°{i}", false ) ).ToArray();
        var c = StaticGateConfigurator.GetConfiguration();
        c.Split( ';' ).All( x => x.EndsWith( ":!" ) ).ShouldBeTrue();

        gates[0].IsOpen = true;
        gates[2].IsOpen = true;
        c = StaticGateConfigurator.GetConfiguration();
        c.ShouldBe( "n°0;n°1:!;n°2;n°3:!;n°4:!" );

        using( TestHelper.Monitor.CollectTexts( out var logs ) )
        {
            StaticGateConfigurator.ApplyConfiguration( TestHelper.Monitor, " FutureGateMustBeClosed  :   ! ;  n°0 : !  ;AnotherFutureMustBeOpened" );
            logs.Count.ShouldBe( 1 );
            logs[0].ShouldMatch( "Applying StaticGate configuration: '.*" );
        }
        gates[0].IsOpen.ShouldBeFalse();
        var f = new StaticGate( "FutureGateMustBeClosed", open: true );
        f.IsOpen.ShouldBeFalse( "Even if f wanted to be opened, current configuration closed it." );

        var a = new StaticGate( "AnotherFutureMustBeOpened", false );
        a.IsOpen.ShouldBeTrue( "Even if a is initially closed, current configuration opened it." );

        StaticGateConfigurator.ApplyConfiguration( TestHelper.Monitor, "" );
    }

}
