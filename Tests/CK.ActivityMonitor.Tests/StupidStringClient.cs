using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CK.Core.Tests.Monitoring
{
    public class StupidStringClient : IActivityMonitorClient
    {
        public class Entry
        {
            public readonly ActivityMonitorLogData Data;
            public readonly IActivityLogGroup? GroupForConclusions;
            public IReadOnlyList<ActivityLogGroupConclusion>? Conclusions;

            public Entry( ref ActivityMonitorLogData d )
            {
                Data = d;
            }
                        
            public Entry( IActivityLogGroup d )
                : this( ref d.Data)
            {
                GroupForConclusions = d;
            }

            public override string ToString() => $"{Data.MaskedLevel} - {Data.Text}";
        }
        public readonly List<Entry> Entries;
        public StringWriter Writer { get; private set; }
        public bool WriteTags { get; private set; }
        public bool WriteConclusionTraits { get; private set; }

        int _curLevel;

        public StupidStringClient( bool writeTags = false, bool writeConclusionTraits = false )
        {
            _curLevel = -1;
            Entries = new List<Entry>();
            Writer = new StringWriter();
            WriteTags = writeTags;
            WriteConclusionTraits = writeConclusionTraits;
        }


        #region IActivityMonitorClient members

        void IActivityMonitorClient.OnTopicChanged( string newTopic, string? fileName, int lineNumber )
        {
        }

        void IActivityMonitorClient.OnAutoTagsChanged( CKTrait newTrait )
        {
        }

        void IActivityMonitorClient.OnUnfilteredLog( ref ActivityMonitorLogData data )
        {
            var level = data.Level & LogLevel.Mask;

            if( data.Text == ActivityMonitor.ParkLevel )
            {
                if( _curLevel != -1 )
                {
                    OnLeaveLevel( (LogLevel)_curLevel );
                }
                _curLevel = -1;
            }
            else
            {
                if( _curLevel == (int)level )
                {
                    OnContinueOnSameLevel( ref data );
                }
                else
                {
                    if( _curLevel != -1 )
                    {
                        OnLeaveLevel( (LogLevel)_curLevel );
                    }
                    OnEnterLevel( ref data );
                    _curLevel = (int)level;
                }
            }
        }

        void IActivityMonitorClient.OnOpenGroup( IActivityLogGroup group )
        {
            if( _curLevel != -1 )
            {
                OnLeaveLevel( (LogLevel)_curLevel );
                _curLevel = -1;
            }

            OnGroupOpen( group );
        }

        void IActivityMonitorClient.OnGroupClosing( IActivityLogGroup group, ref List<ActivityLogGroupConclusion>? conclusions )
        {
        }

        void IActivityMonitorClient.OnGroupClosed( IActivityLogGroup group, IReadOnlyList<ActivityLogGroupConclusion> conclusions )
        {
            if( _curLevel != -1 )
            {
                OnLeaveLevel( (LogLevel)_curLevel );
                _curLevel = -1;
            }

            OnGroupClose( group, conclusions );
        }

        #endregion IActivityMonitorClient members

        void OnEnterLevel( ref ActivityMonitorLogData data )
        {
            Entries.Add( new Entry( ref data ) );
            Writer.WriteLine();
            Writer.Write( data.MaskedLevel.ToString() + ": " + data.Text );
            if( WriteTags ) Writer.Write( "-[{0}]", data.Tags.ToString() );
            if( data.Exception != null ) Writer.Write( "Exception: " + data.Exception.Message );
        }

        void OnContinueOnSameLevel( ref ActivityMonitorLogData data )
        {
            Entries.Add( new Entry( ref data ) );
            Writer.Write( data.Text );
            if( WriteTags ) Writer.Write( "-[{0}]", data.Tags.ToString() );
            if( data.Exception != null ) Writer.Write( "Exception: " + data.Exception.Message );
        }

        void OnLeaveLevel( LogLevel level )
        {
            Writer.Flush();
        }

        void OnGroupOpen( IActivityLogGroup g )
        {
            Entries.Add( new Entry( g ) );
            Writer.WriteLine();
            Writer.Write( new String( '+', g.Data.Depth + 1 ) );
            Writer.Write( "{1} ({0})", g.Data.MaskedLevel, g.Data.Text );
            if( g.Data.Exception != null ) Writer.Write( "Exception: " + g.Data.Exception.Message );
            if( WriteTags ) Writer.Write( "-[{0}]", g.Data.Tags.ToString() );
        }

        void OnGroupClose( IActivityLogGroup g, IReadOnlyList<ActivityLogGroupConclusion>? conclusions )
        {
            Entries.Last( e => e.GroupForConclusions == g ).Conclusions = conclusions;
            Writer.WriteLine();
            Writer.Write( new String( '-', g.Data.Depth + 1 ) );
            if( conclusions != null )
            {
                if( WriteConclusionTraits )
                {
                    Writer.Write( String.Join( ", ", conclusions.Select( c => c.Text + "-/[/" + c.Tag.ToString() + "/]/" ) ) );
                }
                else
                {
                    Writer.Write( String.Join( ", ", conclusions.Select( c => c.Text ) ) );
                }
            }
        }

        public override string ToString()
        {
            return Writer.ToString();
        }
    }

}
