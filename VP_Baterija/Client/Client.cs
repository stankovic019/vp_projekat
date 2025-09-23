using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace Client
{
    public class Client
    {
        static void Main(string[] args)
        {
            ChannelFactory<IBatteryAnalysisService> factory = new ChannelFactory<IBatteryAnalysisService>("BatteryService");


        }
    }
}
