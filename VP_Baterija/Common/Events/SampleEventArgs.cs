using Common.Models;
using System;

namespace Common.Events
{
    public class SampleEventArgs : EventArgs
    {
        public EisSample Sample { get; set; }
        public int SampleNumber { get; set; }
        public int TotalSamples { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
