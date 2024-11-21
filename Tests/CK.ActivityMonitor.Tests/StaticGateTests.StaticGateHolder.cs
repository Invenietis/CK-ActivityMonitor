using FluentAssertions;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CK.Core.Tests.Monitoring;

interface IGateHolder
{
    List<StaticGate> Gates { get; }
}

public partial class StaticGateTests
{
    // This doesn't work!
    public class ThisOneDoesntWork : IGateHolder
    {
        readonly static StaticGate _gate1 = new StaticGate( "StaticGateHolder1", false );

        public ThisOneDoesntWork()
        {
            Gates = StaticGate.GetStaticGates().ToList();
        }

        public List<StaticGate> Gates { get; }
    }

    [Test]
    public void static_fields_are_initialized_only_when_they_are_accessed()
    {
        var h = new ThisOneDoesntWork();
        h.Gates.Should().HaveCount( 0, "No gates registered." );
    }

    // This works.
    public class WithInitializer : IGateHolder
    {
        readonly static StaticGate _gate1;

        static WithInitializer()
        {
            _gate1 = new StaticGate( "G", false );
        }

        public WithInitializer()
        {
            Gates = StaticGate.GetStaticGates().ToList();
        }

        public List<StaticGate> Gates { get; }
    }

    // This also works.
    public class WithEmptyInitializer : IGateHolder
    {
        readonly static StaticGate _gate1 = new StaticGate( "G", false );

        static WithEmptyInitializer()
        {
        }

        public WithEmptyInitializer()
        {
            Gates = StaticGate.GetStaticGates().ToList();
        }

        public List<StaticGate> Gates { get; }
    }

    [TestCase( "EmptyInitializer" )]
    [TestCase( "Initializer" )]
    public void gate_fields_SHOULD_use_type_initializer_or_declare_an_empty_initializer( string mode )
    {
        IGateHolder h = mode == "Initializer" ? new WithInitializer() : new WithEmptyInitializer();
        h.Gates.Should().HaveCount( 1 );
        h.Gates[0].DisplayName.Should().Be( "G" );
    }

}
