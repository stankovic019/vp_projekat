using Common.Enums;
using Common.Events;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace Common.Services
{
    public class ImpedanceAnalyzer
    {
        private readonly double _impedanceThreshold;
        private readonly List<ImpedanceReading> _impedanceHistory;
        private double _runningSum = 0.0;
        private int _sampleCount = 0;

        public event EventHandler<ImpedanceJumpEventArgs> ImpedanceJump;
        public event EventHandler<OutOfBandWarningEventArgs> OutOfBandWarning;

        public ImpedanceAnalyzer()
        {
            _impedanceThreshold = ReadImpedanceThresholdFromConfig();
            _impedanceHistory = new List<ImpedanceReading>();

            Console.WriteLine($"ImpedanceAnalyzer initialized with threshold: {_impedanceThreshold}Ω");
        }

        public ImpedanceAnalyzer(double impedanceThreshold)
        {
            _impedanceThreshold = impedanceThreshold;
            _impedanceHistory = new List<ImpedanceReading>();

            Console.WriteLine($"ImpedanceAnalyzer initialized with custom threshold: {_impedanceThreshold}Ω");
        }

        public void AnalyzeImpedanceChanges(List<EisSample> samples, EisMeta sessionInfo)
        {
            if (samples == null || samples.Count < 2)
            {
                Console.WriteLine("Insufficient samples for impedance analysis");
                return;
            }

            Console.WriteLine($"\n=== Impedance Analysis for {sessionInfo.BatteryId}/{sessionInfo.TestId}/{sessionInfo.SoC}% ===");
            Console.WriteLine($"Analyzing {samples.Count} samples with dZ threshold: {_impedanceThreshold}Ω");

            ResetRunningAverage();
            _impedanceHistory.Clear();


            var sortedSamples = samples.OrderBy(s => s.RowIndex).ToList();

            for (int i = 0; i < sortedSamples.Count; i++)
            {
                var currentSample = sortedSamples[i];

                var currentZ = CalculateImpedance(currentSample.R_ohm, currentSample.X_ohm);

                var currentReading = new ImpedanceReading
                {
                    RowIndex = currentSample.RowIndex,
                    RealPart = currentSample.R_ohm,
                    ImaginaryPart = currentSample.X_ohm,
                    Impedance = currentZ,
                    Frequency = currentSample.FrequencyHz,
                    Timestamp = DateTime.Now,
                    SessionInfo = sessionInfo
                };

                _impedanceHistory.Add(currentReading);

                UpdateRunningAverage(currentZ);
                var runningMean = GetRunningMean();

                Console.WriteLine($"Sample {currentSample.RowIndex}: R={currentSample.R_ohm:F6}Ω, " +
                                $"X={currentSample.X_ohm:F6}Ω, Z={currentZ:F6}Ω, " +
                                $"Z̄={runningMean:F6}Ω");

                if (i > 0)
                {
                    var previousSample = sortedSamples[i - 1];
                    var previousZ = CalculateImpedance(previousSample.R_ohm, previousSample.X_ohm);
                    var deltaZ = currentZ - previousZ;
                    var absoluteDeltaZ = Math.Abs(deltaZ);

                    Console.WriteLine($"  dZ={deltaZ:F8}Ω (|dZ|={absoluteDeltaZ:F8}Ω)");

                    if (absoluteDeltaZ > _impedanceThreshold)
                    {
                        var jumpDirection = DetermineJumpDirection(deltaZ);
                        var jumpEventArgs = new ImpedanceJumpEventArgs
                        {
                            PreviousSample = previousSample,
                            CurrentSample = currentSample,
                            PreviousZ = previousZ,
                            CurrentZ = currentZ,
                            DeltaZ = deltaZ,
                            AbsoluteDeltaZ = absoluteDeltaZ,
                            Direction = jumpDirection,
                            Threshold = _impedanceThreshold,
                            SessionInfo = sessionInfo,
                            DetectedAt = DateTime.Now
                        };

                        OnImpedanceJump(jumpEventArgs);
                    }
                }

                CheckOutOfBandWarning(currentSample, currentZ, runningMean, sessionInfo);
            }


            LogImpedanceSummary(sortedSamples, sessionInfo);
        }

        public void AnalyzeSingleSample(EisSample sample, EisMeta sessionInfo)
        {
            var currentZ = CalculateImpedance(sample.R_ohm, sample.X_ohm);

            var currentReading = new ImpedanceReading
            {
                RowIndex = sample.RowIndex,
                RealPart = sample.R_ohm,
                ImaginaryPart = sample.X_ohm,
                Impedance = currentZ,
                Frequency = sample.FrequencyHz,
                Timestamp = DateTime.Now,
                SessionInfo = sessionInfo
            };

            var previousReading = _impedanceHistory.LastOrDefault(r =>
                r.SessionInfo.BatteryId == sessionInfo.BatteryId &&
                r.SessionInfo.TestId == sessionInfo.TestId &&
                r.SessionInfo.SoC == sessionInfo.SoC);

            _impedanceHistory.Add(currentReading);

            UpdateRunningAverage(currentZ);
            var runningMean = GetRunningMean();

            Console.WriteLine($"Real-time impedance analysis: Sample {sample.RowIndex}, " +
                            $"Z={currentZ:F6}Ω, Z̄={runningMean:F6}Ω");

            if (previousReading != null)
            {
                var deltaZ = currentZ - previousReading.Impedance;
                var absoluteDeltaZ = Math.Abs(deltaZ);

                if (absoluteDeltaZ > _impedanceThreshold)
                {
                    var jumpDirection = DetermineJumpDirection(deltaZ);
                    var jumpEventArgs = new ImpedanceJumpEventArgs
                    {
                        PreviousSample = new EisSample { R_ohm = previousReading.RealPart, X_ohm = previousReading.ImaginaryPart, RowIndex = previousReading.RowIndex },
                        CurrentSample = sample,
                        PreviousZ = previousReading.Impedance,
                        CurrentZ = currentZ,
                        DeltaZ = deltaZ,
                        AbsoluteDeltaZ = absoluteDeltaZ,
                        Direction = jumpDirection,
                        Threshold = _impedanceThreshold,
                        SessionInfo = sessionInfo,
                        DetectedAt = DateTime.Now
                    };

                    OnImpedanceJump(jumpEventArgs);
                }
            }

            CheckOutOfBandWarning(sample, currentZ, runningMean, sessionInfo);
        }

        private double CalculateImpedance(double realPart, double imaginaryPart)
        {
            return Math.Sqrt(realPart * realPart + imaginaryPart * imaginaryPart);
        }

        private void UpdateRunningAverage(double impedance)
        {
            _runningSum += impedance;
            _sampleCount++;
        }

        private double GetRunningMean()
        {
            return _sampleCount > 0 ? _runningSum / _sampleCount : 0.0;
        }

        private void ResetRunningAverage()
        {
            _runningSum = 0.0;
            _sampleCount = 0;
        }

        private void CheckOutOfBandWarning(EisSample sample, double currentZ, double runningMean, EisMeta sessionInfo)
        {
            if (_sampleCount < 2) return;

            var lowerBound = runningMean * 0.75;
            var upperBound = runningMean * 1.25;

            Console.WriteLine($"  Out-of-band check: Z={currentZ:F6}Ω vs bounds [{lowerBound:F6}Ω, {upperBound:F6}Ω]");

            if (currentZ < lowerBound || currentZ > upperBound)
            {
                var warningDirection = currentZ < lowerBound ?
                    OutOfBandDirection.BelowExpected : OutOfBandDirection.AboveExpected;

                var deviation = currentZ < lowerBound ?
                    (lowerBound - currentZ) / runningMean * 100 :
                    (currentZ - upperBound) / runningMean * 100;

                var warningEventArgs = new OutOfBandWarningEventArgs
                {
                    Sample = sample,
                    CurrentZ = currentZ,
                    RunningMean = runningMean,
                    LowerBound = lowerBound,
                    UpperBound = upperBound,
                    Direction = warningDirection,
                    DeviationPercent = deviation,
                    SessionInfo = sessionInfo,
                    DetectedAt = DateTime.Now
                };

                OnOutOfBandWarning(warningEventArgs);
            }
        }

        private ImpedanceJumpDirection DetermineJumpDirection(double deltaZ)
        {
            if (deltaZ > 0)
                return ImpedanceJumpDirection.Increase;
            else if (deltaZ < 0)
                return ImpedanceJumpDirection.Decrease;
            else
                return ImpedanceJumpDirection.NoChange;
        }

        private void OnImpedanceJump(ImpedanceJumpEventArgs e)
        {
            Console.WriteLine($"\n⚡ IMPEDANCE JUMP DETECTED! ⚡");
            Console.WriteLine($"Session: {e.SessionInfo.BatteryId}/{e.SessionInfo.TestId}/{e.SessionInfo.SoC}%");
            Console.WriteLine($"Between samples {e.PreviousSample.RowIndex} and {e.CurrentSample.RowIndex}");
            Console.WriteLine($"Impedance change: {e.PreviousZ:F6}Ω → {e.CurrentZ:F6}Ω");
            Console.WriteLine($"dZ = {e.DeltaZ:F8}Ω (|dZ| = {e.AbsoluteDeltaZ:F8}Ω)");
            Console.WriteLine($"Threshold: {e.Threshold:F8}Ω");
            Console.WriteLine($"Direction: {e.Direction}");
            Console.WriteLine($"Detected at: {e.DetectedAt:HH:mm:ss.fff}");
            Console.WriteLine();
            ImpedanceJump?.Invoke(this, e);
        }

        private void OnOutOfBandWarning(OutOfBandWarningEventArgs e)
        {
            Console.WriteLine($"\n⚠️  OUT-OF-BAND WARNING! ⚠️");
            Console.WriteLine($"Session: {e.SessionInfo.BatteryId}/{e.SessionInfo.TestId}/{e.SessionInfo.SoC}%");
            Console.WriteLine($"Sample {e.Sample.RowIndex}: Z = {e.CurrentZ:F6}Ω");
            Console.WriteLine($"Running mean: {e.RunningMean:F6}Ω");
            Console.WriteLine($"Expected range: [{e.LowerBound:F6}Ω, {e.UpperBound:F6}Ω] (±25%)");
            Console.WriteLine($"Direction: {e.Direction}");
            Console.WriteLine($"Deviation: {e.DeviationPercent:F2}% beyond acceptable range");
            Console.WriteLine($"Detected at: {e.DetectedAt:HH:mm:ss.fff}");
            Console.WriteLine();
            OutOfBandWarning?.Invoke(this, e);
        }

        private void LogImpedanceSummary(List<EisSample> samples, EisMeta sessionInfo)
        {
            if (samples.Count < 2) return;

            var impedances = samples.Select(s => CalculateImpedance(s.R_ohm, s.X_ohm)).ToList();
            var deltaZs = new List<double>();

            for (int i = 1; i < impedances.Count; i++)
            {
                deltaZs.Add(Math.Abs(impedances[i] - impedances[i - 1]));
            }

            var runningMean = GetRunningMean();
            var outOfBandCount = impedances.Count(z => z < runningMean * 0.75 || z > runningMean * 1.25);

            Console.WriteLine($"\n=== Impedance Analysis Summary ===");
            Console.WriteLine($"Min impedance: {impedances.Min():F6}Ω");
            Console.WriteLine($"Max impedance: {impedances.Max():F6}Ω");
            Console.WriteLine($"Running mean: {runningMean:F6}Ω");
            Console.WriteLine($"Impedance range: {(impedances.Max() - impedances.Min()):F6}Ω");
            Console.WriteLine($"Average |dZ|: {deltaZs.Average():F8}Ω");
            Console.WriteLine($"Max |dZ|: {deltaZs.Max():F8}Ω");
            Console.WriteLine($"Impedance jumps: {deltaZs.Count(dz => dz > _impedanceThreshold)}");
            Console.WriteLine($"Out-of-band samples: {outOfBandCount} (±25% from mean)");
            Console.WriteLine($"Jump threshold: {_impedanceThreshold:F8}Ω");
            Console.WriteLine();
        }

        private double ReadImpedanceThresholdFromConfig()
        {
            try
            {
                var configValue = ConfigurationManager.AppSettings["Z_threshold"];
                if (configValue != null && double.TryParse(configValue, out double threshold))
                {
                    return threshold;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not read Z_threshold from config: {ex.Message}");
            }

            return 0.01;
        }

        public void ClearHistory()
        {
            _impedanceHistory.Clear();
            ResetRunningAverage();
            Console.WriteLine("Impedance analysis history cleared");
        }

        public List<ImpedanceReading> GetImpedanceHistory()
        {
            return _impedanceHistory.ToList();
        }

        public double GetCurrentRunningMean()
        {
            return GetRunningMean();
        }
    }

}
