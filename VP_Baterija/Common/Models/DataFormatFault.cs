using System.Runtime.Serialization;

namespace Common.Models
{
    [DataContract]
    public class DataFormatFault
    {
        [DataMember] public string Message { get; set; }
        [DataMember] public string Field { get; set; }
        [DataMember] public string ExpectedFormat { get; set; }
    }
}
