using Common.Enums;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class OutOfBandWarningEventArgs : EventArgs
    {
        public EisSample Sample { get; set; }
        public double CurrentZ { get; set; }
        public double RunningMean { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public OutOfBandDirection Direction { get; set; }
        public double DeviationPercent { get; set; }
        public EisMeta SessionInfo { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
