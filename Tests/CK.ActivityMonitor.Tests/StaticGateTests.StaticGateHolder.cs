using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CK.Core.Tests.Monitoring
{
    interface IGateHolder
    {
        List<StaticGate> Gates { get; }
    }

    public class StaticGateHolder : IGateHolder
    {
        readonly StaticGate _gate = new StaticGate( "StaticGateHolder", false );

        public StaticGateHolder()
        {
            Throw.CheckState( _gate != null );
            Gates = StaticGate.GetStaticGates().ToList();
        }

        public List<StaticGate> Gates { get; }
    }

    public class StaticGateHolderDynamic : IGateHolder
    {
        readonly StaticGate _gate = new StaticGate( "StaticGateHolderDynamic", false );

        public StaticGateHolderDynamic()
        {
            Throw.CheckState( _gate != null );
            Gates = StaticGate.GetStaticGates().ToList();
        }

        public List<StaticGate> Gates { get; }
    }

    public partial class StaticGateTests
    {
        [TestCase( true )]
        public void gates_are_immediately_visible( bool activator )
        {
            var h = activator ? CreateActivator() : CreateDirect();
            h.Gates.Should().HaveCount( 1 );
            h.Gates[0].DisplayName.Should().Be( activator ? "StaticGateHolderDynamic" : "StaticGateHolder" );
        }

        [MethodImpl( MethodImplOptions.NoInlining )]
        static IGateHolder CreateDirect()
        {
            return new StaticGateHolder();
        }

        [MethodImpl( MethodImplOptions.NoInlining )]
        static IGateHolder CreateActivator()
        {
            return (IGateHolder)Activator.CreateInstance( typeof( StaticGateHolderDynamic ) )!;
        }
    }
}
