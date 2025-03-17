using Shouldly;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable VSTHRD103 // Call async methods when in an async method

namespace CK.Core.Tests.Monitoring;

[TestFixture]
public partial class AsyncLockTests
{
    [Test]
    public async Task SemaphoreSlim_deadlocks_when_reentering_Async()
    {
        static async Task DeadlockAsync()
        {
            var s = new SemaphoreSlim( 1, 1 );
            await s.WaitAsync();
            await s.WaitAsync();
            throw new Exception( "Never here..." );
        }

        var dead = DeadlockAsync();
        var timeout = Task.Delay( 100 );
        await Task.WhenAny( dead, timeout );

        timeout.IsCompletedSuccessfully.ShouldBeTrue();
        dead.IsCompleted.ShouldBeFalse();
    }

    [TestCase( true, true, true )]
    [TestCase( true, true, false )]
    [TestCase( true, false, true )]
    [TestCase( true, false, false )]
    [TestCase( false, true, true )]
    [TestCase( false, true, false )]
    [TestCase( false, false, true )]
    [TestCase( false, false, false )]
    public async Task our_AsyncLock_handles_reentrancy_Async( bool firstAsync, bool secondAsync, bool thirdAsync )
    {
        var m = TestHelper.Monitor;

        var l = new AsyncLock( LockRecursionPolicy.SupportsRecursion );

        l.IsEnteredBy( m ).ShouldBeFalse();
        l.IsEntered.ShouldBeFalse();

        if( firstAsync ) await l.EnterAsync( m );
        else l.Enter( m );

        l.IsEnteredBy( m ).ShouldBeTrue();
        l.IsEntered.ShouldBeTrue();

        if( secondAsync ) await l.EnterAsync( m );
        else l.Enter( m );

        l.IsEnteredBy( m ).ShouldBeTrue();

        if( thirdAsync ) await l.EnterAsync( m );
        else l.Enter( m );

        using( await l.LockAsync( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
        }

        l.IsEnteredBy( m ).ShouldBeTrue();

        l.Leave( m );
        l.IsEnteredBy( m ).ShouldBeTrue();

        l.Leave( m );
        l.IsEnteredBy( m ).ShouldBeTrue();

        l.Leave( m );
        l.IsEnteredBy( m ).ShouldBeFalse();
        l.IsEntered.ShouldBeFalse();

        using( await l.LockAsync( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
        }

        using( l.Lock( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
        }

    }

    [TestCase( 3000, 3000 )]
    [TestCase( 35000, 25000 )]
    public async Task simple_stress_test_Async( int syncIncLoop, int asyncDecLoop )
    {
        AsyncLock guard = new AsyncLock( LockRecursionPolicy.NoRecursion, "G" );

        int nByJob = 0;
        int nByJobAsync = 0;

        Action<IActivityMonitor> job = m =>
        {
            Thread.Sleep( 10 );
            for( int i = 0; i < syncIncLoop; i++ )
            {
                guard.Enter( m );
                nByJob++;
                guard.Leave( m );
            }
        };

        Func<IActivityMonitor, Task> asyncJob = async m =>
         {
             Thread.Sleep( 10 );
             for( int i = 0; i < asyncDecLoop; i++ )
             {
                 await guard.EnterAsync( m );
                 nByJob--;
                 nByJobAsync++;
                 guard.Leave( m );
             }
         };

        IActivityMonitor m1 = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        IActivityMonitor m2 = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        IActivityMonitor m3 = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );
        IActivityMonitor m4 = new ActivityMonitor( ActivityMonitorOptions.SkipAutoConfiguration );

        await Task.WhenAll( Task.Run( () => job( m1 ) ),
                            Task.Run( () => job( m2 ) ),
                            Task.Run( () => asyncJob( m3 ) ),
                            Task.Run( () => asyncJob( m4 ) ) );

        nByJob.ShouldBe( syncIncLoop * 2 - asyncDecLoop * 2 );
        nByJobAsync.ShouldBe( asyncDecLoop * 2 );
    }


    [Test]
    public async Task our_AsyncLock_can_detect_reentrancy_and_throw_LockRecursionException_Async()
    {
        var m = TestHelper.Monitor;

        var l = new AsyncLock( LockRecursionPolicy.NoRecursion );

        l.IsEnteredBy( m ).ShouldBeFalse();

        using( await l.LockAsync( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
            Util.Invokable( () => l.Lock( m ) ).ShouldThrow<LockRecursionException>();
        }

        l.IsEnteredBy( m ).ShouldBeFalse();

        using( l.Lock( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
            await l.LockAsync( m ).AsTask().ShouldThrowAsync<LockRecursionException>();
        }

        l.IsEnteredBy( m ).ShouldBeFalse();

        using( l.Lock( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();
            Util.Invokable( () => l.Lock( m ) ).ShouldThrow<LockRecursionException>();
        }

        l.IsEnteredBy( m ).ShouldBeFalse();
    }

    [Test]
    public async Task TryEnter_works_as_expected_with_LockRecursionPolicy_SupportsRecursion_Async()
    {
        var m = TestHelper.Monitor;
        var l = new AsyncLock( LockRecursionPolicy.SupportsRecursion );

        using( await l.LockAsync( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();

            // SupportsRecursion
            l.TryEnter( m ).ShouldBeTrue();
            l.IsEnteredBy( m ).ShouldBeTrue();
            l.Leave( m );
            l.IsEnteredBy( m ).ShouldBeTrue();

            l.IsEnteredBy( m ).ShouldBeTrue();
        }
        l.IsEnteredBy( m ).ShouldBeFalse();

        var m2 = new ActivityMonitor();

        using( await l.LockAsync( m ) )
        {
            l.IsEnteredBy( m ).ShouldBeTrue();

            l.TryEnter( m2 ).ShouldBeFalse();
            l.IsEnteredBy( m2 ).ShouldBeFalse();

            l.IsEnteredBy( m ).ShouldBeTrue();
        }

    }
}
