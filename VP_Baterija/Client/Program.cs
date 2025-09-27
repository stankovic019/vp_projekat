using Common.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading;


namespace Client
{
    public class Program
    {
        static void Main(string[] args)
        {
            string startTime = DateTime.Now.ToString(); ;
            Console.Title = "Battery Analysis Client";
            Console.WriteLine("=== Battery Data Processing Client ===");

            string datasetPath = @"C:\Users\Dimitrije\Documents\GitHub\vp_projekat\VP_Baterija\Common\Files";

            try
            {
                Console.WriteLine("Loading batteries...");
                Thread.Sleep(200); //delay for writing to be more natural
                EisFileProcessor processor = new EisFileProcessor(datasetPath);
                List<EisFileData> eisFiles = processor.ProcessAllEisFiles();

                Console.WriteLine($"Successfully processed {eisFiles.Count} files");
                foreach (var logEntry in processor.SuccessLog)
                {
                    Console.WriteLine($"SUCCESS: {logEntry}");
                }

                if (processor.WarningLog.Count > 0)
                {
                    Console.WriteLine("\nWarnings:");
                    foreach (var warning in processor.WarningLog)
                    {
                        Console.WriteLine($"WARNING: {warning}");
                    }
                }

                if (eisFiles.Count == 0)
                {
                    Console.WriteLine("No files to process!");
                    return;
                }

                Console.WriteLine("\nConnecting to server...");
                Thread.Sleep(200);
                ChannelFactory<IEisService> factory = new ChannelFactory<IEisService>("EisService");
                var client = factory.CreateChannel();

                Console.WriteLine($"\nSending {eisFiles.Count} files to server for analysis...");

                foreach (var eisFile in eisFiles)
                {
                    Console.WriteLine($"\nProcessing: {eisFile.BatteryId}/{eisFile.TestId}/{eisFile.SoCPercentage}%");

                    try
                    {
                        var meta = new EisMeta
                        {
                            BatteryId = eisFile.BatteryId,
                            TestId = eisFile.TestId,
                            SoC = eisFile.SoCPercentage,
                            FileName = eisFile.FileName,
                            TotalRows = eisFile.TotalRows
                        };

                        string startResponse = client.StartSession(meta);
                        if (startResponse.Contains("NACK"))
                        {
                            Console.WriteLine($"  Session start failed: {startResponse}");
                            continue; 
                        }
                        Console.WriteLine($"  Session started: {startResponse}");

                        int successCount = 0;
                        for (int i = 0; i < eisFile.Samples.Count; i++)
                        {
                            EisSample sample = eisFile.Samples[i];
                            sample.RowIndex = i;

                            string sampleResponse = client.PushSample(sample);
                            if (sampleResponse.Contains("ACK"))
                            {
                                successCount++;
                            }

                            //showing progress
                            int percent = (i * 100) / eisFile.Samples.Count;
                            if (percent == 25)
                            {
                                Console.WriteLine("Processed 25% of files");
                                Thread.Sleep(200);
                            }
                            else if (percent == 50)
                            {
                                Console.WriteLine("Processed 50% of files");
                                Thread.Sleep(200);
                            }
                            else if (percent == 75)
                            {
                                Console.WriteLine("Processed 75% of files");
                                Thread.Sleep(200);
                            }
                        }

                        string endResponse = client.EndSession();
                        Console.WriteLine($"  Session ended: {endResponse} - {successCount}/{eisFile.Samples.Count} samples accepted");
                        Thread.Sleep(400); //initial 1000 ms splitted into 4 pieces
                        // Thread.Sleep(1000); // delay so that we could see the processing more naturally
                    }
                    catch (FaultException<ValidationFault> ex)
                    {
                        Console.WriteLine($"  Validation error: {ex.Detail.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Error: {ex.Message}");
                    }
                }

                factory.Close();
                Console.WriteLine("\nAll files processed!");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine();
            Console.WriteLine($"Start time: {startTime.Split(' ')[1]}");
            Console.WriteLine($"End time: {DateTime.Now.ToString().Split(' ')[1]}");
            Console.WriteLine();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadKey();
        }
    }
}
