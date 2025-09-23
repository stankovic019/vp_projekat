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
    public class ValidationFault
    {
        [DataMember] public string Message { get; set; }
        [DataMember] public string ValidationError { get; set; }
        [DataMember] public string Field { get; set; }
        [DataMember] public object ActualValue { get; set; }
        [DataMember] public string AllowedRange { get; set; }
    }
}
