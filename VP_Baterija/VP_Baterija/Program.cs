using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace VP_Baterija
{
    public class Program
    {
        static void Main(string[] args)
        {

            ServiceHost svc = new ServiceHost(typeof(BatteryService));

        }
    }
}
