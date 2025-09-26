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

            string datasetPath = @"C:\Users\Dimitrije\Documents\GitHub\vp_projekat\VP_Baterija\Common\Files";
            //string datasetPath = @"D:\Downloads\Github\vp_projekat\VP_Baterija\Common\Files";

            var processor = new EisFileProcessor(datasetPath);
            Console.WriteLine("Processing EIS files...");
            var eisFiles = processor.ProcessAllEisFiles();

            // Display processing results
            Console.WriteLine($"Successfully processed {eisFiles.Count} files locally");

            if (processor.WarningLog.Any())
            {
                Console.WriteLine($"Warnings: {processor.WarningLog.Count}");
            }

            // NOW SEND DATA TO WCF SERVICE
            Console.WriteLine("\n=== SENDING DATA TO SERVER ===");

            var client = factory.CreateChannel();

            try
            {
                // Send each file to the server
                foreach (var eisFile in eisFiles)
                {
                    Console.WriteLine($"\nSending {eisFile.BatteryId}/{eisFile.TestId}/{eisFile.SoCPercentage}%...");

                    // Create metadata
                    var meta = new EisMeta
                    {
                        BatteryId = eisFile.BatteryId,
                        TestId = eisFile.TestId,
                        SoC = eisFile.SoCPercentage,
                        FileName = $"{eisFile.SoCPercentage}.csv",
                        TotalRows = eisFile.Samples.Count
                    };

                    // Start session
                    client.StartSession(meta);
                    Console.WriteLine($"  Session started: {eisFile.Samples.Count} samples to send");

                    // Send samples one by one
                    for (int i = 0; i < eisFile.Samples.Count; i++)
                    {
                        var sample = eisFile.Samples[i];
                        sample.RowIndex = i; // Ensure correct row index

                        client.PushSample(sample);
                        Console.WriteLine($"  Sent sample {i + 1}/{eisFile.Samples.Count}");
                    }

                    // End session
                    client.EndSession();
                    Console.WriteLine($"  ✓ Session completed for {eisFile.BatteryId}/{eisFile.TestId}/{eisFile.SoCPercentage}%");
                }

                Console.WriteLine("\n✓ All data sent to server successfully!");
                Console.WriteLine("Check the server console and Data directory for created files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending data to server: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                try
                {
                    ((ICommunicationObject)client).Close();
                    factory.Close();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
            //    try
            //    {
            //        // Test with valid session
            //        var meta = new EisMeta
            //        {
            //            BatteryId = "B01",
            //            TestId = "Test_1",
            //            SoC = 50,
            //            FileName = "test.csv",
            //            TotalRows = 3
            //        };

            //        Console.WriteLine("=== Testing Events ===");
            //        client.StartSession(meta);

            //        // Send samples
            //        for (int i = 0; i < 3; i++)
            //        {
            //            var sample = new EisSample
            //            {
            //                FrequencyHz = 1000 * (i + 1),
            //                R_ohm = 0.5,
            //                X_ohm = 0.3,
            //                V = 3.7,
            //                T_degC = 25,
            //                Range_ohm = 1.0,
            //                RowIndex = i
            //            };

            //            client.PushSample(sample);
            //            System.Threading.Thread.Sleep(500); // Delay to see events
            //        }

            //        client.EndSession();
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine($"Error: {ex.Message}");
            //    }

            //    factory.Close();
            //    Console.ReadLine();
            //}
        }
    }
}
