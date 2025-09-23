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
            //ChannelFactory<IEisService> factory = new ChannelFactory<IEisService>("EisService");

            ChannelFactory<IEisService> channelFactory = new ChannelFactory<IEisService>("EisService");
            var client = channelFactory.CreateChannel();

            Console.WriteLine("=== Task 3 - WCF Service Validation Tests ===\n");

            try
            {
                // Test 1: Valid Session Start
                Console.WriteLine("Test 1: Valid session start");
                var validMeta = new EisMeta
                {
                    BatteryId = "B01",
                    TestId = "Test_1",
                    SoC = 50,
                    FileName = "B01_50_SOC.csv",
                    TotalRows = 28
                };

                client.StartSession(validMeta);
                Console.WriteLine("✅ Valid session started successfully\n");

                // Test 2: Valid Sample Push
                Console.WriteLine("Test 2: Valid sample push");
                var validSample = new EisSample
                {
                    FrequencyHz = 1000.0,
                    R_ohm = 0.125,
                    X_ohm = -0.089,
                    V = 3.7,
                    T_degC = 25.5,
                    Range_ohm = 1000.0,
                    RowIndex = 0
                };

                client.PushSample(validSample);
                Console.WriteLine("✅ Valid sample pushed successfully\n");

                // End current session for next tests
                client.EndSession();
                Console.WriteLine("Session ended for next tests\n");

            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"❌ DataFormatFault: {ex.Detail.Message}");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"❌ ValidationFault: {ex.Detail.Message}");
            }
            catch (FaultException ex)
            {
                Console.WriteLine($"❌ General Fault: {ex.Message}");
            }

            // Test 3: Invalid Meta Data Tests
            Console.WriteLine("\n=== Invalid Meta Data Tests ===");

            TestInvalidMeta(client, "Invalid BatteryId", new EisMeta
            {
                BatteryId = "X99", // Invalid format
                TestId = "Test_1",
                SoC = 50,
                FileName = "test.csv",
                TotalRows = 28
            });

            TestInvalidMeta(client, "Invalid TestId", new EisMeta
            {
                BatteryId = "B01",
                TestId = "Test_3", // Invalid TestId
                SoC = 50,
                FileName = "test.csv",
                TotalRows = 28
            });

            TestInvalidMeta(client, "Invalid SoC Percentage", new EisMeta
            {
                BatteryId = "B01",
                TestId = "Test_1",
                SoC = 33, // Not multiple of 5
                FileName = "test.csv",
                TotalRows = 28
            });

            TestInvalidMeta(client, "Invalid TotalRows", new EisMeta
            {
                BatteryId = "B01",
                TestId = "Test_1",
                SoC = 50,
                FileName = "test.csv",
                TotalRows = 30 // Should be 28
            });

            // Test 4: Invalid Sample Data Tests
            Console.WriteLine("\n=== Invalid Sample Data Tests ===");

            // Start valid session for sample tests
            var testMeta = new EisMeta
            {
                BatteryId = "B02",
                TestId = "Test_2",
                SoC = 75,
                FileName = "B02_75_SOC.csv",
                TotalRows = 28
            };

            try
            {
                client.StartSession(testMeta);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Setup failed: {ex.Message}");
                return;
            }

            TestInvalidSample(client, "Negative Frequency", new EisSample
            {
                FrequencyHz = -100.0, // Invalid: must be positive
                R_ohm = 0.125,
                X_ohm = -0.089,
                V = 3.7,
                T_degC = 25.0,
                Range_ohm = 1000.0,
                RowIndex = 0
            });

            TestInvalidSample(client, "NaN Impedance", new EisSample
            {
                FrequencyHz = 1000.0,
                R_ohm = double.NaN, // Invalid: NaN value
                X_ohm = -0.089,
                V = 3.7,
                T_degC = 25.0,
                Range_ohm = 1000.0,
                RowIndex = 0
            });

            TestInvalidSample(client, "Negative Voltage", new EisSample
            {
                FrequencyHz = 1000.0,
                R_ohm = 0.125,
                X_ohm = -0.089,
                V = -1.5, // Invalid: negative voltage
                T_degC = 25.0,
                Range_ohm = 1000.0,
                RowIndex = 0
            });

            TestInvalidSample(client, "Zero Range", new EisSample
            {
                FrequencyHz = 1000.0,
                R_ohm = 0.125,
                X_ohm = -0.089,
                V = 3.7,
                T_degC = 25.0,
                Range_ohm = 0.0, // Invalid: must be positive
                RowIndex = 0
            });

            // Test 5: Row Index Sequence Test
            Console.WriteLine("\n=== Row Index Sequence Test ===");

            try
            {
                // End previous session and start new one
                try { client.EndSession(); } catch { }
                client.StartSession(testMeta);

                // Send valid sample with RowIndex 0
                client.PushSample(new EisSample
                {
                    FrequencyHz = 1000.0,
                    R_ohm = 0.125,
                    X_ohm = -0.089,
                    V = 3.7,
                    T_degC = 25.0,
                    Range_ohm = 1000.0,
                    RowIndex = 0
                });

                // Try to send sample with RowIndex 2 (skipping 1)
                client.PushSample(new EisSample
                {
                    FrequencyHz = 500.0,
                    R_ohm = 0.130,
                    X_ohm = -0.095,
                    V = 3.7,
                    T_degC = 25.0,
                    Range_ohm = 1000.0,
                    RowIndex = 2 // Should be 1, this will fail
                });

            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"✅ Row index validation working: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Unexpected error: {ex.Message}");
            }

            // Test 6: Session State Tests
            Console.WriteLine("\n=== Session State Tests ===");

            try
            {
                // Try to push sample without active session
                try { client.EndSession(); } catch { }

                client.PushSample(new EisSample
                {
                    FrequencyHz = 1000.0,
                    R_ohm = 0.125,
                    X_ohm = -0.089,
                    V = 3.7,
                    T_degC = 25.0,
                    Range_ohm = 1000.0,
                    RowIndex = 0
                });
            }
            catch (FaultException ex)
            {
                Console.WriteLine($"✅ Session state validation working: {ex.Message}");
            }

            Console.WriteLine("\n=== All Tests Completed ===");

            try
            {
                channelFactory.Close();
            }
            catch
            {
                channelFactory.Abort();
            }
        }

        static void TestInvalidMeta(IEisService client, string testName, EisMeta meta)
        {
            try
            {
                Console.WriteLine($"Testing: {testName}");
                client.StartSession(meta);
                Console.WriteLine($"❌ {testName} should have failed but didn't");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"✅ {testName} correctly failed: {ex.Detail.Message}");
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"✅ {testName} correctly failed: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {testName} failed with unexpected error: {ex.Message}");
            }
        }

        static void TestInvalidSample(IEisService client, string testName, EisSample sample)
        {
            try
            {
                Console.WriteLine($"Testing: {testName}");
                client.PushSample(sample);
                Console.WriteLine($"❌ {testName} should have failed but didn't");
            }
            catch (FaultException<ValidationFault> ex)
            {
                Console.WriteLine($"✅ {testName} correctly failed: {ex.Detail.Message}");
            }
            catch (FaultException<DataFormatFault> ex)
            {
                Console.WriteLine($"✅ {testName} correctly failed: {ex.Detail.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ {testName} failed with unexpected error: {ex.Message}");
            }
        }
    }
}
