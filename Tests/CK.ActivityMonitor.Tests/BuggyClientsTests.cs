
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Linq;
using CK.Core.Impl;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

namespace CK.Core.Tests.Monitoring
{
    [TestFixture]
    public class BuggyClientsTests
    {
        public enum BugCall
        {
            MinimalFilter,
            IsDead,
            SetMonitorWhenRemoving,
            OnUnfilteredLog,
            OnOpenGroup,
            OnGroupClosing,
            OnGroupClosed,
            OnAutoTagsChanged,
            OnTopicChanged
        }

        class BuggyClient : ActivityMonitorClient, IActivityMonitorBoundClient
        {
            IActivityMonitorImpl? _source;
            int _countBeforeBug;
            BugCall _call;
            LogFilter _minimalFilter;

            public BuggyClient( BugCall c, int countBeforeBug )
            {
                _call = c;
                _countBeforeBug = countBeforeBug;
            }

            void ThrowOn( BugCall c )
            {
                --_countBeforeBug;
                if( c == _call && _countBeforeBug <= 0 ) throw new Exception( $"Bug {c}." );
            }

            public override LogFilter MinimalFilter
            {
                get
                {
                    ThrowOn( BugCall.MinimalFilter );
                    return _minimalFilter;
                }
            }

            public void SetMinimalFilter( LogFilter value )
            {
                if( _minimalFilter != value )
                {
                    var old = _minimalFilter;
                    _minimalFilter = value;
                    if( _source != null ) _source.SignalChange();
                }
            }

            public bool IsDead
            {
                get
                {
                    ThrowOn( BugCall.IsDead );
                    return false;
                }
            }

            public void SetMonitor( IActivityMonitorImpl? source, bool forceBuggyRemove )
            {
                _source = source;
                if( source == null ) ThrowOn( BugCall.SetMonitorWhenRemoving );
            }

            protected override void OnOpenGroup( IActivityLogGroup group )
            {
                ThrowOn( BugCall.OnOpenGroup );
            }

            protected override void OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
            {
                ThrowOn( BugCall.OnGroupClosing );
            }

            protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
            {
                ThrowOn( BugCall.OnUnfilteredLog );
            }

            protected override void OnAutoTagsChanged( CKTrait newTags )
            {
                ThrowOn( BugCall.OnAutoTagsChanged );
            }

            protected override void OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
            {
                ThrowOn( BugCall.OnGroupClosed );
            }

            protected override void OnTopicChanged( string newTopic, string? fileName, int lineNumber )
            {
                ThrowOn( BugCall.OnTopicChanged );
            }
        }

        [TestCase( BugCall.IsDead )]
        [TestCase( BugCall.MinimalFilter, 2 )]
        [TestCase( BugCall.OnUnfilteredLog )]
        [TestCase( BugCall.OnOpenGroup )]
        [TestCase( BugCall.OnGroupClosing )]
        [TestCase( BugCall.OnGroupClosed )]
        [TestCase( BugCall.OnAutoTagsChanged )]
        [TestCase( BugCall.OnTopicChanged )]
        public void Bug_appears_in_remaining_clients( BugCall c, int countBeforeBug = 0 )
        {
            var m = new ActivityMonitor();
            var output = new StupidStringClient();
            m.Output.RegisterClient( output );
            var buggy = new BuggyClient( c, countBeforeBug );
            m.Output.RegisterClient( buggy );

            m.AutoTags = ActivityMonitor.Tags.Register( "Test" );
            using( m.OpenInfo( "A group" ) )
            {
                m.Warn( "a warning" );
            }
            m.SetTopic( "A new topic." );

            // This triggers a recomputation of the actual filters.
            // Clients are enumerated and their IsDead and MinimalFilter properties are called.
            buggy.SetMinimalFilter( LogFilter.Release );
            buggy.SetMinimalFilter( LogFilter.Verbose );

            m.Info( "Still working." );

            output.Writer.ToString().Should()
                .Contain( $"Bug {c}." )
                .And.Contain( "Still working" );

        }
    }
}
