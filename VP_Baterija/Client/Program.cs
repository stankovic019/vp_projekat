using Common.Events;
using Common.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

            //var processor = new EisFileProcessor(datasetPath);
            //Console.WriteLine("Processing EIS files...");
            //var eisFiles = processor.ProcessAllEisFiles();

            //// Display processing results
            //Console.WriteLine($"Successfully processed {eisFiles.Count} files locally");

            //if (processor.WarningLog.Any())
            //{
            //    Console.WriteLine($"Warnings: {processor.WarningLog.Count}");
            //}

            //// NOW SEND DATA TO WCF SERVICE
            //Console.WriteLine("\n=== SENDING DATA TO SERVER ===");

            //var client = factory.CreateChannel();

            //try
            //{
            //    // Send each file to the server
            //    foreach (var eisFile in eisFiles)
            //    {
            //        Console.WriteLine($"\nSending {eisFile.BatteryId}/{eisFile.TestId}/{eisFile.SoCPercentage}%...");

            //        // Create metadata
            //        var meta = new EisMeta
            //        {
            //            BatteryId = eisFile.BatteryId,
            //            TestId = eisFile.TestId,
            //            SoC = eisFile.SoCPercentage,
            //            FileName = $"{eisFile.SoCPercentage}.csv",
            //            TotalRows = eisFile.Samples.Count
            //        };

            //        // Start session
            //        client.StartSession(meta);
            //        Console.WriteLine($"  Session started: {eisFile.Samples.Count} samples to send");

            //        // Send samples one by one
            //        for (int i = 0; i < eisFile.Samples.Count; i++)
            //        {
            //            var sample = eisFile.Samples[i];
            //            sample.RowIndex = i; // Ensure correct row index

            //            client.PushSample(sample);
            //            Console.WriteLine($"  Sent sample {i + 1}/{eisFile.Samples.Count}");
            //        }

            //        // End session
            //        client.EndSession();
            //        Console.WriteLine($"  ✓ Session completed for {eisFile.BatteryId}/{eisFile.TestId}/{eisFile.SoCPercentage}%");
            //    }

            //    Console.WriteLine("\n✓ All data sent to server successfully!");
            //    Console.WriteLine("Check the server console and Data directory for created files.");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Error sending data to server: {ex.Message}");
            //    if (ex.InnerException != null)
            //    {
            //        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            //    }
            //}
            //finally
            //{
            //    try
            //    {
            //        ((ICommunicationObject)client).Close();
            //        factory.Close();
            //    }
            //    catch
            //    {
            //        // Ignore cleanup errors
            //    }
            //}

            //RunVoltageAnalysisExample();
            //GenerateTestSamplesWithSpikes();

            //Console.WriteLine("\nPress any key to exit.");
            //Console.ReadKey();
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
            RunImpedanceAnalysisExample();
            GenerateTestSamplesWithImpedanceVariations();

        }


        public static void RunImpedanceAnalysisExample()
        {
            Console.WriteLine("=== Task 10: Impedance Analysis Example ===\n");

            // Create impedance analyzer
            var impedanceAnalyzer = new ImpedanceAnalyzer(0.005); // 5mΩ threshold

            // Subscribe to events
            impedanceAnalyzer.ImpedanceJump += OnImpedanceJumpDetected;
            impedanceAnalyzer.OutOfBandWarning += OnOutOfBandWarningDetected;

            // Create test data with impedance variations
            var testSamples = GenerateTestSamplesWithImpedanceVariations();
            var sessionInfo = new EisMeta
            {
                BatteryId = "B01",
                TestId = "Test_1",
                SoC = 75,
                FileName = "test_impedance.csv",
                TotalRows = testSamples.Count
            };

            // Analyze complete session
            Console.WriteLine("=== Batch Impedance Analysis ===");
            impedanceAnalyzer.AnalyzeImpedanceChanges(testSamples, sessionInfo);

            // Simulate real-time analysis
            Console.WriteLine("\n=== Real-time Impedance Analysis Simulation ===");
            impedanceAnalyzer.ClearHistory();

            foreach (var sample in testSamples)
            {
                impedanceAnalyzer.AnalyzeSingleSample(sample, sessionInfo);
                System.Threading.Thread.Sleep(150); // Simulate time between samples
            }

            Console.WriteLine($"\n=== Final Running Mean: {impedanceAnalyzer.GetCurrentRunningMean():F6}Ω ===");
            Console.WriteLine("=== Impedance Analysis Complete ===");
        }

        private static void OnImpedanceJumpDetected(object sender, ImpedanceJumpEventArgs e)
        {
            Console.WriteLine($"📧 Event subscriber notified of impedance jump:");
            Console.WriteLine($"   |ΔZ| = {e.AbsoluteDeltaZ:F8}Ω > {e.Threshold:F8}Ω");
            Console.WriteLine($"   Direction: {e.Direction}");
        }

        private static void OnOutOfBandWarningDetected(object sender, OutOfBandWarningEventArgs e)
        {
            Console.WriteLine($"📧 Event subscriber notified of out-of-band condition:");
            Console.WriteLine($"   Z = {e.CurrentZ:F6}Ω is {e.Direction}");
            Console.WriteLine($"   Deviation: {e.DeviationPercent:F2}% beyond ±25% range");
        }

        private static List<EisSample> GenerateTestSamplesWithImpedanceVariations()
        {
            var samples = new List<EisSample>();
            var random = new Random(42);

            for (int i = 0; i < 12; i++)
            {
                var baseR = 0.1;
                var baseX = -0.05;

                // Add normal variations
                var R = baseR + (random.NextDouble() - 0.5) * 0.002; // ±1mΩ variation
                var X = baseX + (random.NextDouble() - 0.5) * 0.001; // ±0.5mΩ variation

                // Add impedance jumps at specific samples
                if (i == 4) { R += 0.008; X -= 0.004; } // Jump
                if (i == 7) { R -= 0.006; X += 0.003; } // Jump
                if (i == 10) { R += 0.012; X -= 0.006; } // Large jump + out-of-band

                // Add out-of-band values
                if (i == 8) { R *= 1.4; X *= 1.3; } // +40% and +30% - should trigger out-of-band
                if (i == 9) { R *= 0.6; X *= 0.7; } // -40% and -30% - should trigger out-of-band

                samples.Add(new EisSample
                {
                    RowIndex = i,
                    FrequencyHz = 10000.0 / (i + 1),
                    R_ohm = R,
                    X_ohm = X,
                    V = 3.7 + (random.NextDouble() - 0.5) * 0.001,
                    T_degC = 25.0 + (random.NextDouble() - 0.5) * 2.0,
                    Range_ohm = 1000.0
                });
            }

            return samples;
        }

        //

        public static void RunVoltageAnalysisExample()
        {
            Console.WriteLine("=== Task 9: Voltage Change Detection Example ===\n");

            // Create voltage analyzer
            var voltageAnalyzer = new VoltageAnalyzer(0.002); // 2mV threshold

            // Subscribe to voltage spike events
            voltageAnalyzer.VoltageSpike += OnVoltageSpikeDetected;

            // Create test data with voltage spikes
            var testSamples = GenerateTestSamplesWithSpikes();
            var sessionInfo = new EisMeta
            {
                BatteryId = "B01",
                TestId = "Test_1",
                SoC = 50,
                FileName = "test_data.csv",
                TotalRows = testSamples.Count
            };

            // Analyze complete session
            Console.WriteLine("=== Batch Analysis ===");
            voltageAnalyzer.AnalyzeVoltageChanges(testSamples, sessionInfo);

            // Simulate real-time analysis
            Console.WriteLine("\n=== Real-time Analysis Simulation ===");
            voltageAnalyzer.ClearHistory();

            foreach (var sample in testSamples)
            {
                voltageAnalyzer.AnalyzeSingleSample(sample, sessionInfo);
                System.Threading.Thread.Sleep(100); // Simulate time between samples
            }

            Console.WriteLine("\n=== Analysis Complete ===");
        }

        private static void OnVoltageSpikeDetected(object sender, VoltageSpikeEventArgs e)
        {
            Console.WriteLine($"📧 Event subscriber notified of voltage spike:");
            Console.WriteLine($"   |ΔV| = {e.AbsoluteDeltaV:F6}V > {e.Threshold:F6}V");
            Console.WriteLine($"   Direction: {e.Direction}");

            // Here you could log to file, send notifications, etc.
        }

        private static List<EisSample> GenerateTestSamplesWithSpikes()
        {
            var samples = new List<EisSample>();
            var baseVoltage = 3.7;
            var random = new Random(42); // Fixed seed for reproducible results

            for (int i = 0; i < 10; i++)
            {
                var voltage = baseVoltage;

                // Add normal variation
                voltage += (random.NextDouble() - 0.5) * 0.001; // ±0.5mV normal variation

                // Add spikes at specific samples
                if (i == 3) voltage += 0.003; // 3mV spike up
                if (i == 6) voltage -= 0.0025; // 2.5mV spike down
                if (i == 8) voltage += 0.004; // 4mV spike up

                samples.Add(new EisSample
                {
                    RowIndex = i,
                    FrequencyHz = 1000.0 / (i + 1),
                    R_ohm = 0.1 + i * 0.01,
                    X_ohm = -0.05 - i * 0.005,
                    V = voltage,
                    T_degC = 25.0,
                    Range_ohm = 1000.0
                });
            }

            return samples;
        }
    }
}
