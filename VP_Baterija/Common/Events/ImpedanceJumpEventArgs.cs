using Common.Enums;
using Common.Models;
using System;

namespace Common.Events
{
    public class ImpedanceJumpEventArgs : EventArgs
    {
        public EisSample PreviousSample { get; set; }
        public EisSample CurrentSample { get; set; }
        public double PreviousZ { get; set; }
        public double CurrentZ { get; set; }
        public double DeltaZ { get; set; }
        public double AbsoluteDeltaZ { get; set; }
        public ImpedanceJumpDirection Direction { get; set; }
        public double Threshold { get; set; }
        public EisMeta SessionInfo { get; set; }
        public DateTime DetectedAt { get; set; }
    }
}
