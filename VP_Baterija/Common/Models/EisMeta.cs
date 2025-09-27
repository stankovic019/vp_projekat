using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class EisMeta
    {
        [DataMember] public string BatteryId { get; set; }
        [DataMember] public string TestId { get; set; }
        [DataMember] public int SoC { get; set; }
        [DataMember] public string FileName { get; set; }
        [DataMember] public int TotalRows { get; set; }
    }
}
