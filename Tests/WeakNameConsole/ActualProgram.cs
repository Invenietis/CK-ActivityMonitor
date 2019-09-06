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
            using( m.OpenInfo( "From inside WeakNameConsole." ) )
            {
                DumpVersion( m, typeof( CK.Text.StringMatcher ) );
                DumpVersion( m, typeof( CK.Core.SimpleServiceContainer ) );
            }
        }

        static void DumpVersion( IActivityMonitor m, Type t )
        {
            object[] attr = t.Assembly.GetCustomAttributes( typeof( System.Reflection.AssemblyInformationalVersionAttribute ), false );
            if( attr == null ) m.Info( "null AssemblyInformationalVersionAttribute for " + t.FullName );
            else if( attr.Length == 0 ) m.Info( "No AssemblyInformationalVersionAttribute for " + t.FullName );
            else m.Info( t.FullName + " ==> " + ((System.Reflection.AssemblyInformationalVersionAttribute)attr[0]).InformationalVersion );
        }

    }
}
