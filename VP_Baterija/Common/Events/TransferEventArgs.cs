using System;

namespace Common.Events
{
    public class TransferEventArgs : EventArgs
    {
        public string BatteryId { get; set; }
        public string TestId { get; set; }
        public int SoC { get; set; }
        public DateTime Timestamp { get; set; }
        public int TotalSamples { get; set; }
    }
}
