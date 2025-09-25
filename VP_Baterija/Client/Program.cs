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

            string datasetPath = @"D:\Downloads\Github\vp_projekat\VP_Baterija\Common\Files";

            var processor = new EisFileProcessor(datasetPath);

            Console.WriteLine("Processing EIS files...");
            var eisFiles = processor.ProcessAllEisFiles();

            // Ispis SUCCESS loga
            Console.WriteLine("\n=== SUCCESS LOG ===");
            foreach (var logEntry in processor.SuccessLog)
            {
                Console.WriteLine(logEntry);
            }

            // Ispis WARNING loga, ako postoji
            if (processor.WarningLog.Any())
            {
                Console.WriteLine("\n=== WARNING LOG ===");
                foreach (var logEntry in processor.WarningLog)
                {
                    Console.WriteLine(logEntry);
                }
            }

            // Rezultati
            Console.WriteLine($"\n=== RESULTS ===");
            Console.WriteLine($"Successfully processed {eisFiles.Count} files");

            // Prikaz prvih 5 fajlova (ili svih ako ih je manje)
            foreach (var file in eisFiles.Take(5))
            {
                Console.WriteLine($"{file.BatteryId}/{file.TestId}/{file.SoCPercentage}% - {file.Samples.Count} samples");
            }

            Console.WriteLine("\nProcessing complete. Press any key to exit.");
            Console.ReadKey();
        }
    }
}
