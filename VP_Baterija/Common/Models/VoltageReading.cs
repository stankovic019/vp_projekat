using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
