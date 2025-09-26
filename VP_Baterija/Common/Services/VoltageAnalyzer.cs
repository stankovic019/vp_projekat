using Common.Enums;
using Common.Events;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Services
{
    public class VoltageAnalyzer
    {
        private readonly double _voltageThreshold;
        private readonly List<VoltageReading> _voltageHistory;

        // Events for voltage spike detection
        public event EventHandler<VoltageSpikeEventArgs> VoltageSpike;

        public VoltageAnalyzer()
        {
            // Read threshold from configuration
            _voltageThreshold = ReadVoltageThresholdFromConfig();
            _voltageHistory = new List<VoltageReading>();

            Console.WriteLine($"VoltageAnalyzer initialized with threshold: {_voltageThreshold}V");
        }

        public VoltageAnalyzer(double voltageThreshold)
        {
            _voltageThreshold = voltageThreshold;
            _voltageHistory = new List<VoltageReading>();

            Console.WriteLine($"VoltageAnalyzer initialized with custom threshold: {_voltageThreshold}V");
        }

        /// <summary>
        /// Analyzes voltage changes for consecutive samples within the same SoC level
        /// </summary>
        /// <param name="samples">EIS samples from the same CSV file (same SoC)</param>
        /// <param name="sessionInfo">Session metadata for context</param>
        public void AnalyzeVoltageChanges(List<EisSample> samples, EisMeta sessionInfo)
        {
            if (samples == null || samples.Count < 2)
            {
                Console.WriteLine("Insufficient samples for voltage analysis");
                return;
            }

            Console.WriteLine($"\n=== Voltage Analysis for {sessionInfo.BatteryId}/{sessionInfo.TestId}/{sessionInfo.SoC}% ===");
            Console.WriteLine($"Analyzing {samples.Count} samples with threshold: {_voltageThreshold}V");

            // Clear previous history for new session
            _voltageHistory.Clear();

            // Sort samples by RowIndex to ensure correct sequence
            var sortedSamples = samples.OrderBy(s => s.RowIndex).ToList();

            for (int i = 0; i < sortedSamples.Count; i++)
            {
                var currentSample = sortedSamples[i];
                var currentReading = new VoltageReading
                {
                    RowIndex = currentSample.RowIndex,
                    Voltage = currentSample.V,
                    Frequency = currentSample.FrequencyHz,
                    Timestamp = DateTime.Now,
                    SessionInfo = sessionInfo
                };

                _voltageHistory.Add(currentReading);

                // Calculate ΔV for consecutive samples (n and n-1)
                if (i > 0)
                {
                    var previousSample = sortedSamples[i - 1];
                    var deltaV = currentSample.V - previousSample.V;
                    var absoluteDeltaV = Math.Abs(deltaV);

                    Console.WriteLine($"Sample {currentSample.RowIndex}: V={currentSample.V:F4}V, " +
                                    $"ΔV={deltaV:F6}V (|ΔV|={absoluteDeltaV:F6}V)");

                    // Check if voltage change exceeds threshold
                    if (absoluteDeltaV > _voltageThreshold)
                    {
                        var spikeDirection = DetermineSpikeDirection(deltaV);
                        var eventArgs = new VoltageSpikeEventArgs
                        {
                            PreviousSample = previousSample,
                            CurrentSample = currentSample,
                            DeltaV = deltaV,
                            AbsoluteDeltaV = absoluteDeltaV,
                            Direction = spikeDirection,
                            Threshold = _voltageThreshold,
                            SessionInfo = sessionInfo,
                            DetectedAt = DateTime.Now
                        };

                        // Raise voltage spike event
                        OnVoltageSpike(eventArgs);
                    }
                }
                else
                {
                    Console.WriteLine($"Sample {currentSample.RowIndex}: V={currentSample.V:F4}V (baseline)");
                }
            }

            // Summary statistics
            LogVoltageSummary(sortedSamples, sessionInfo);
        }

        /// <summary>
        /// Analyzes single sample against the most recent previous sample
        /// Used for real-time analysis during streaming
        /// </summary>
        /// <param name="sample">Current EIS sample</param>
        /// <param name="sessionInfo">Session metadata</param>
        public void AnalyzeSingleSample(EisSample sample, EisMeta sessionInfo)
        {
            var currentReading = new VoltageReading
            {
                RowIndex = sample.RowIndex,
                Voltage = sample.V,
                Frequency = sample.FrequencyHz,
                Timestamp = DateTime.Now,
                SessionInfo = sessionInfo
            };

            // Check against previous sample in the same session
            var previousReading = _voltageHistory.LastOrDefault(r =>
                r.SessionInfo.BatteryId == sessionInfo.BatteryId &&
                r.SessionInfo.TestId == sessionInfo.TestId &&
                r.SessionInfo.SoC == sessionInfo.SoC);

            _voltageHistory.Add(currentReading);

            if (previousReading != null)
            {
                var deltaV = sample.V - previousReading.Voltage;
                var absoluteDeltaV = Math.Abs(deltaV);

                Console.WriteLine($"Real-time voltage analysis: Sample {sample.RowIndex}, " +
                                $"V={sample.V:F4}V, ΔV={deltaV:F6}V");

                if (absoluteDeltaV > _voltageThreshold)
                {
                    var spikeDirection = DetermineSpikeDirection(deltaV);
                    var eventArgs = new VoltageSpikeEventArgs
                    {
                        PreviousSample = new EisSample { V = previousReading.Voltage, RowIndex = previousReading.RowIndex },
                        CurrentSample = sample,
                        DeltaV = deltaV,
                        AbsoluteDeltaV = absoluteDeltaV,
                        Direction = spikeDirection,
                        Threshold = _voltageThreshold,
                        SessionInfo = sessionInfo,
                        DetectedAt = DateTime.Now
                    };

                    OnVoltageSpike(eventArgs);
                }
            }
        }

        private VoltageSpikeDirection DetermineSpikeDirection(double deltaV)
        {
            if (deltaV > 0)
                return VoltageSpikeDirection.AboveExpected;
            else if (deltaV < 0)
                return VoltageSpikeDirection.BelowExpected;
            else
                return VoltageSpikeDirection.NoChange;
        }

        private void OnVoltageSpike(VoltageSpikeEventArgs e)
        {
            Console.WriteLine($"\n🚨 VOLTAGE SPIKE DETECTED! 🚨");
            Console.WriteLine($"Session: {e.SessionInfo.BatteryId}/{e.SessionInfo.TestId}/{e.SessionInfo.SoC}%");
            Console.WriteLine($"Between samples {e.PreviousSample.RowIndex} and {e.CurrentSample.RowIndex}");
            Console.WriteLine($"Voltage change: {e.PreviousSample.V:F4}V → {e.CurrentSample.V:F4}V");
            Console.WriteLine($"ΔV = {e.DeltaV:F6}V (|ΔV| = {e.AbsoluteDeltaV:F6}V)");
            Console.WriteLine($"Threshold: {e.Threshold:F6}V");
            Console.WriteLine($"Direction: {e.Direction}");
            Console.WriteLine($"Detected at: {e.DetectedAt:HH:mm:ss.fff}");
            Console.WriteLine();

            // Raise the event for subscribers
            VoltageSpike?.Invoke(this, e);
        }

        private void LogVoltageSummary(List<EisSample> samples, EisMeta sessionInfo)
        {
            if (samples.Count < 2) return;

            var voltages = samples.Select(s => s.V).ToList();
            var deltaVs = new List<double>();

            for (int i = 1; i < samples.Count; i++)
            {
                deltaVs.Add(Math.Abs(samples[i].V - samples[i - 1].V));
            }

            Console.WriteLine($"\n=== Voltage Analysis Summary ===");
            Console.WriteLine($"Min voltage: {voltages.Min():F4}V");
            Console.WriteLine($"Max voltage: {voltages.Max():F4}V");
            Console.WriteLine($"Voltage range: {(voltages.Max() - voltages.Min()):F4}V");
            Console.WriteLine($"Average |ΔV|: {deltaVs.Average():F6}V");
            Console.WriteLine($"Max |ΔV|: {deltaVs.Max():F6}V");
            Console.WriteLine($"Spikes detected: {deltaVs.Count(dv => dv > _voltageThreshold)}");
            Console.WriteLine($"Spike threshold: {_voltageThreshold:F6}V");
            Console.WriteLine();
        }

        private double ReadVoltageThresholdFromConfig()
        {
            try
            {
                // Try to read from app.config
                var configValue = ConfigurationManager.AppSettings["V_threshold"];
                if (configValue != null && double.TryParse(configValue, out double threshold))
                {
                    return threshold;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not read V_threshold from config: {ex.Message}");
            }

            // Default threshold if config is not available
            return 0.001; // 1mV default threshold
        }

        public void ClearHistory()
        {
            _voltageHistory.Clear();
            Console.WriteLine("Voltage analysis history cleared");
        }

        public List<VoltageReading> GetVoltageHistory()
        {
            return _voltageHistory.ToList();
        }
    }
}
