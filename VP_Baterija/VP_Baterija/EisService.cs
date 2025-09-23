using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common.Services;
using Common.Models;

namespace VP_Baterija
{
    public class EisService : IEisService
    {
        public string StartSession(EisMeta meta)
        {
            Console.WriteLine($"StartSession called for Battery: {meta.BatteryId}, Test: {meta.TestId}");
            return "ACK IN_PROGRESS";
        }

        public string PushSample(EisSample sample)
        {
            Console.WriteLine($"Sample received: Row {sample.RowIndex}, V={sample.V}");
            return "ACK";
        }

        public string EndSession()
        {
            Console.WriteLine("EndSession called");
            return "COMPLETED";
        }
    }
}
