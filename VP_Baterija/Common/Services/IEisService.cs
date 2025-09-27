using Common.Models;
using System.ServiceModel;

namespace Common.Services
{
    [ServiceContract]
    public interface IEisService
    {
        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string StartSession(EisMeta meta);

        [OperationContract]
        [FaultContract(typeof(DataFormatFault))]
        [FaultContract(typeof(ValidationFault))]
        string PushSample(EisSample sample);

        [OperationContract]
        string EndSession();

    }
}
