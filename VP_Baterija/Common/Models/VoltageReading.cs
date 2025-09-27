using System;

namespace Common.Models
{
    public class VoltageReading
    {
        public int RowIndex { get; set; }
        public double Voltage { get; set; }
        public double Frequency { get; set; }
        public DateTime Timestamp { get; set; }
        public EisMeta SessionInfo { get; set; }
    }
}
