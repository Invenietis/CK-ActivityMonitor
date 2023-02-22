using System.Collections.Generic;

namespace CK.Core.Tests.Monitoring
{
    /// <summary>
    /// Test artifact that plays with <see cref="ActivityMonitorLogData.AcquireExternalData()"/>.
    /// </summary>
    public sealed class LogRetainerClient : ActivityMonitorClient
    {
        readonly List<ActivityMonitorExternalLogData> _retained;
        readonly int _maxCount;

        public LogRetainerClient( int maxCount )
        {
            _retained = new List<ActivityMonitorExternalLogData>();
            _maxCount = maxCount;
        }

        public IReadOnlyList<ActivityMonitorExternalLogData> Retained => _retained;

        public void Clear() => _retained.Clear();

        protected override void OnUnfilteredLog( ref ActivityMonitorLogData data )
        {
            if( _retained.Count < _maxCount )
            {
                _retained.Add( data.AcquireExternalData() );
            }
        }
    }
}
