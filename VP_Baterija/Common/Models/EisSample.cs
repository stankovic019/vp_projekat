using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace Common.Models
{
    [DataContract]
    public class EisSample
    {
        [DataMember] public double FrequencyHz { get; set; }
        [DataMember] public double R_ohm { get; set; }
        [DataMember] public double X_ohm { get; set; }
        [DataMember] public double V { get; set; }
        [DataMember] public double T_degC { get; set; }
        [DataMember] public double Range_ohm { get; set; }
        [DataMember] public int RowIndex { get; set; }
    }

}
