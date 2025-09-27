using System;
using System.Configuration;

namespace Common.Services
{
    public class AnalyticsConfiguration
    {
        public double V_threshold { get; set; } = 0.1;
        public double Z_threshold { get; set; } = 5.0;
        public double DeviationPercent { get; set; } = 25.0;

        public static AnalyticsConfiguration LoadFromConfig()
        {
            var config = new AnalyticsConfiguration();

            try
            {
                config.V_threshold = double.Parse(ConfigurationManager.AppSettings["V_threshold"] ?? "0.1");
                config.Z_threshold = double.Parse(ConfigurationManager.AppSettings["Z_threshold"] ?? "5.0");
                config.DeviationPercent = double.Parse(ConfigurationManager.AppSettings["DeviationPercent"] ?? "25.0");

                Console.WriteLine($"Configuration loaded: V_threshold={config.V_threshold}, Z_threshold={config.Z_threshold}, DeviationPercent={config.DeviationPercent}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error loading configuration, using defaults. {ex.Message}");
            }

            return config;
        }
    }
}
