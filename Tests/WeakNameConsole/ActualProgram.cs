using CK.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WeakNameConsole
{
    class ActualProgram
    {
        [MethodImpl( MethodImplOptions.NoInlining )]
        public static void Run()
        {
            var m = new ActivityMonitor();
            m.Output.RegisterClient( new ActivityMonitorConsoleClient() );
            m.Info( "From Monitor." );
        }

    }
}
