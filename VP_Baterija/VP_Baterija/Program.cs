using System;
using System.ServiceModel;

namespace VP_Baterija
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Battery Analysis Server";
            Console.WriteLine("=== Battery Li-ion Analysis Server ===");

            ServiceHost svc = null;

            try
            {
                svc = new ServiceHost(typeof(EisService));
                svc.Open();

                Console.WriteLine("Service started successfully!");
                Console.WriteLine("Endpoint: net.tcp://localhost:4000/EisService");
                Console.WriteLine("Ready to process battery data with real-time analytics");
                Console.WriteLine();
                Console.WriteLine("Press Enter to stop the server...");

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                try
                {
                    svc?.Close();
                    Console.WriteLine("Server stopped.");
                }
                catch
                {
                    svc?.Abort();
                }
            }

            Console.ReadLine();
        }
    }
}
