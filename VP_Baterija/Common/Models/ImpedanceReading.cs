using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class ImpedanceReading
    {
        public int RowIndex { get; set; }
        public double RealPart { get; set; }
        public double ImaginaryPart { get; set; }
        public double Impedance { get; set; }
        public double Frequency { get; set; }
        public DateTime Timestamp { get; set; }
        public EisMeta SessionInfo { get; set; }
    }
}
