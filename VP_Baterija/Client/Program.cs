using Common.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            ChannelFactory<IEisService> factory = new ChannelFactory<IEisService>("EisService");
            try
            {

                var client = factory.CreateChannel();

                // Test the service
                var meta = new EisMeta
                {
                    BatteryId = "B01",
                    TestId = "Test_1",
                    SoC = 50,
                    FileName = "test.csv",
                    TotalRows = 28
                };

                string result = client.StartSession(meta);
                Console.WriteLine($"StartSession result: {result}");

                var sample = new EisSample
                {
                    FrequencyHz = 1000,
                    R_ohm = 0.5,
                    X_ohm = 0.3,
                    V = 3.7,
                    T_degC = 25,
                    Range_ohm = 1.0,
                    RowIndex = 1
                };

                result = client.PushSample(sample);
                Console.WriteLine($"PushSample result: {result}");

                result = client.EndSession();
                Console.WriteLine($"EndSession result: {result}");

                factory.Close();
                Console.WriteLine("Client test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.ReadLine();
        }


    }
    
}
