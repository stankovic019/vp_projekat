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
    public class EisMeta
    {
        [DataMember] public string BatteryId { get; set; }  // B01..B11
        [DataMember] public string TestId { get; set; }     // Test_1 or Test_2
        [DataMember] public int SoC { get; set; }           // SoC% from filename
        [DataMember] public string FileName { get; set; }
        [DataMember] public int TotalRows { get; set; }
    }
}
