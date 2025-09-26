using Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Events
{
    public class WarningEventArgs : EventArgs
    {
        public string WarningType { get; set; }
        public string Message { get; set; }
        public EisSample Sample { get; set; }
        public double ActualValue { get; set; }
        public double ThresholdValue { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
