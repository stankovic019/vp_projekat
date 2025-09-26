using Common.Enums;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class VoltageSpikeEventArgs : EventArgs
    {
        public EisSample PreviousSample { get; set; }
        public EisSample CurrentSample { get; set; }
        public double DeltaV { get; set; }
        public double AbsoluteDeltaV { get; set; }
        public VoltageSpikeDirection Direction { get; set; }
        public double Threshold { get; set; }
        public EisMeta SessionInfo { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
