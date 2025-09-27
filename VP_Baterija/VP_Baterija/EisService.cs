using Common.Events;
using Common.Models;
using Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Text;

namespace VP_Baterija
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class EisService : IEisService, IDisposable, IBatteryEventPublisher
    {
        private const int MAX_ACCEPTED_SAMPLES = 28;

        private EisMeta _currentSessionMeta;
        private List<EisSample> _sessionSamples;
        private int _expectedRowIndex = 0;
        private bool _sessionActive = false;
        private bool _disposed = false;

        private StreamWriter _sessionWriter;
        private StreamWriter _rejectsWriter;
        private string _sessionDirectory;
        private readonly string _dataRootPath = "C:\\Users\\Dimitrije\\Documents\\GitHub\\vp_projekat\\VP_Baterija\\Common\\Data"; //dimitrije
        //private readonly string _dataRootPath = "D:\\Downloads\\Github\\vp_projekat\\VP_Baterija\\Common\\Data"; //vojin
        private readonly string _rejectsFileName = "rejects.csv";

        public event EventHandler<TransferEventArgs> OnTransferStarted;
        public event EventHandler<SampleEventArgs> OnSampleReceived;
        public event EventHandler<TransferEventArgs> OnTransferCompleted;
        public event EventHandler<WarningEventArgs> OnWarningRaised;
        public event EventHandler<VoltageSpikeEventArgs> OnVoltageSpikeRaised;
        public event EventHandler<ImpedanceJumpEventArgs> OnImpedanceJumpRaised;
        public event EventHandler<OutOfBandWarningEventArgs> OnOutOfBandWarningRaised;

        private int _rejectedCount = 0;

        private readonly AnalyticsConfiguration _config;
        private VoltageAnalyzer _voltageAnalyzer;     
        private ImpedanceAnalyzer _impedanceAnalyzer;  

        public EisService()
        {
            _sessionSamples = new List<EisSample>();
            _config = AnalyticsConfiguration.LoadFromConfig();
            _voltageAnalyzer = new VoltageAnalyzer(_config.V_threshold);
            _impedanceAnalyzer = new ImpedanceAnalyzer(_config.Z_threshold);
            
            SubscribeToAnalyticsEvents();
            SubscribeToEvents();
        }

        private void SubscribeToAnalyticsEvents()
        {
            _voltageAnalyzer.VoltageSpike += (sender, e) =>
            {
                OnVoltageSpikeRaised?.Invoke(this, e);
            };

            _impedanceAnalyzer.ImpedanceJump += (sender, e) =>
            {
                OnImpedanceJumpRaised?.Invoke(this, e);
            };

            _impedanceAnalyzer.OutOfBandWarning += (sender, e) =>
            {
                OnOutOfBandWarningRaised?.Invoke(this, e);
            };
        }

        private void SubscribeToEvents()
        {
            OnTransferStarted += (sender, e) =>
            {
                Console.WriteLine($"[EVENT] Transfer started: {e.BatteryId}/{e.TestId}/{e.SoC}% at {e.Timestamp:HH:mm:ss}");
            };

            OnSampleReceived += (sender, e) =>
            {
                Console.WriteLine($"[EVENT] Sample {e.SampleNumber}/{e.TotalSamples} received at {e.Timestamp:HH:mm:ss}");
            };

            OnTransferCompleted += (sender, e) =>
            {
                Console.WriteLine($"[EVENT] Transfer completed: {e.BatteryId}/{e.TestId}/{e.SoC}% - {e.TotalSamples} samples at {e.Timestamp:HH:mm:ss}");
            };

            OnVoltageSpikeRaised += (sender, e) =>
            {
                Console.WriteLine($"[VOLTAGE SPIKE] dV={e.AbsoluteDeltaV}, Threshold={e.Threshold}, Dir={e.Direction} at {e.DetectedAt:HH:mm:ss}");
            };

            OnImpedanceJumpRaised += (sender, e) =>
            {
                Console.WriteLine($"[IMPEDANCE JUMP] dZ={e.AbsoluteDeltaZ}, Threshold={e.Threshold}, Dir={e.Direction} at {e.DetectedAt:HH:mm:ss}");
            };

            OnOutOfBandWarningRaised += (sender, e) =>
            {
                Console.WriteLine($"[OUT OF BAND] Z={e.CurrentZ}, Mean={e.RunningMean}, Dir={e.Direction}, Deviation={e.DeviationPercent}% at {e.DetectedAt:HH:mm:ss}");
            };
        }


        [OperationBehavior]
        public string StartSession(EisMeta meta)
        {
            try
            {
                ValidateMetaData(meta);

                if (_sessionActive)
                {
                    Console.WriteLine("NACK - Session already active");
                    return "NACK";
                }

                _currentSessionMeta = meta;
                _sessionSamples.Clear();
                _expectedRowIndex = 0;
                _rejectedCount = 0;
                _sessionActive = true;

                _voltageAnalyzer.ClearHistory();
                _impedanceAnalyzer.ClearHistory();

                SetupSessionFiles(meta);
                OnTransferStarted?.Invoke(this, new TransferEventArgs
                {
                    BatteryId = meta.BatteryId,
                    TestId = meta.TestId,
                    SoC = meta.SoC,
                    TotalSamples = meta.TotalRows,
                    Timestamp = DateTime.Now
                });

                Console.WriteLine("\n=================================================================================================");
                Console.WriteLine($"ACK - Session started for Battery: {meta.BatteryId}, Test: {meta.TestId}, SoC: {meta.SoC}% - Status: IN_PROGRESS");
                return "ACK IN_PROGRESS";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NACK - StartSession failed: {ex.Message}");
                return "NACK";
            }
        }

        [OperationBehavior]
        public string PushSample(EisSample sample)
        {
            if (!_sessionActive)
            {
                Console.WriteLine("NACK - No active session");
                return "NACK";
            }

            try
            {
                ValidateSample(sample);
                ValidateRowIndexSequence(sample.RowIndex);

                _expectedRowIndex++;

                if (_sessionSamples.Count >= MAX_ACCEPTED_SAMPLES)
                {
                    WriteRejectedSample(sample, $"Exceeded allowed samples ({MAX_ACCEPTED_SAMPLES})");
                    _rejectedCount++;
                    Console.WriteLine($"NACK - Sample {sample.RowIndex} rejected: Exceeded allowed samples ({MAX_ACCEPTED_SAMPLES})");
                    return "NACK";
                }

                _sessionSamples.Add(sample);

                WriteValidSample(sample);

                _voltageAnalyzer.AnalyzeSingleSample(sample, _currentSessionMeta);
                _impedanceAnalyzer.AnalyzeSingleSample(sample, _currentSessionMeta);
                OnSampleReceived?.Invoke(this, new SampleEventArgs
                {
                    Sample = sample,
                    SampleNumber = _expectedRowIndex,
                    TotalSamples = _currentSessionMeta.TotalRows,
                    Timestamp = DateTime.Now
                });

                Console.WriteLine("\n-------------------------------------------------------------------------------------------------");
                Console.WriteLine($"ACK - Sample received: Row {sample.RowIndex}, Frequency: {sample.FrequencyHz} Hz - Status: IN_PROGRESS");


                return "ACK";
            }
            catch (FaultException<ValidationFault> ex)
            {
                WriteRejectedSample(sample, ex.Detail.Message);
                _rejectedCount++;
                OnWarningRaised?.Invoke(this, new WarningEventArgs
                {
                    WarningType = "ValidationError",
                    Message = ex.Detail.Message,
                    Sample = sample,
                    ActualValue = 0,
                    ThresholdValue = 0,
                    Timestamp = DateTime.Now
                });

                Console.WriteLine($"NACK - Validation error: {ex.Detail.Message}");
                return "NACK";
            }
            catch (Exception ex)
            {
                WriteRejectedSample(sample, $"Server error: {ex.Message}");
                _rejectedCount++;
                Console.WriteLine($"NACK - Error in PushSample: {ex.Message}");
                return "NACK";
            }
        }

        [OperationBehavior]
        public string EndSession()
        {
            if (!_sessionActive)
            {
                Console.WriteLine("NACK - No active session to end");
                return "NACK";
            }

            try
            {
                Console.WriteLine("\n*************************************************************************************************");
                Console.WriteLine($"ACK - Session ending. Received samples: {_expectedRowIndex} (RowIndex count). Accepted (success) samples: {_sessionSamples.Count}. Rejected samples: {_rejectedCount}");

                if (_sessionSamples.Count > 1)
                {
                    Console.WriteLine("\n=== PERFORMING FINAL SESSION ANALYTICS ===");
                    _voltageAnalyzer.AnalyzeVoltageChanges(_sessionSamples, _currentSessionMeta);
                    _impedanceAnalyzer.AnalyzeImpedanceChanges(_sessionSamples, _currentSessionMeta);
                }

                FinalizeSessionFiles();
                OnTransferCompleted?.Invoke(this, new TransferEventArgs
                {
                    BatteryId = _currentSessionMeta.BatteryId,
                    TestId = _currentSessionMeta.TestId,
                    SoC = _currentSessionMeta.SoC,
                    TotalSamples = _sessionSamples.Count,
                    Timestamp = DateTime.Now
                });

                _sessionActive = false;
                _currentSessionMeta = null;
                _sessionSamples.Clear();
                _expectedRowIndex = 0;
                _rejectedCount = 0;

                Console.WriteLine("\n=================================================================================================");

                return "COMPLETED";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"NACK - EndSession failed: {ex.Message}");
                return "NACK";
            }
        }

        private void SetupSessionFiles(EisMeta meta)
        {
            try
            {

                _sessionDirectory = Path.Combine(_dataRootPath, meta.BatteryId, meta.TestId, $"{meta.SoC}%");
                Directory.CreateDirectory(_sessionDirectory);

                string sessionFilePath = Path.Combine(_sessionDirectory, "session.csv");
                _sessionWriter = new StreamWriter(sessionFilePath, false, Encoding.UTF8);
                _sessionWriter.WriteLine("FrequencyHz,R_ohm,X_ohm,V,T_degC,Range_ohm,RowIndex");


                string rejectsFilePath = Path.Combine(_sessionDirectory, _rejectsFileName);
                _rejectsWriter = new StreamWriter(rejectsFilePath, false, Encoding.UTF8);
                _rejectsWriter.WriteLine("FrequencyHz,R_ohm,X_ohm,V,T_degC,Range_ohm,RowIndex,RejectReason");

                Console.WriteLine();
                Console.WriteLine($"Session files created in: {_sessionDirectory}");
                Console.WriteLine($"Session file: {sessionFilePath}");
                Console.WriteLine($"Rejects file: {rejectsFilePath}");
            }
            catch (Exception ex)
            {
                CleanupFileResources();
                throw new FaultException($"Failed to create session files: {ex.Message}");
            }
        }

        private void WriteValidSample(EisSample sample)
        {
            try
            {
                if (_sessionWriter != null && sample != null)
                {
                    string csvLine = $"{sample.FrequencyHz},{sample.R_ohm},{sample.X_ohm}," +
                                   $"{sample.V},{sample.T_degC},{sample.Range_ohm},{sample.RowIndex}";
                    _sessionWriter.WriteLine(csvLine);
                    _sessionWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to write sample to file: {ex.Message}");
            }
        }

        private void WriteRejectedSample(EisSample sample, string reason)
        {
            try
            {
                if (_rejectsWriter != null)
                {
                    if (sample != null)
                    {
                        string csvLine = $"{sample.FrequencyHz},{sample.R_ohm},{sample.X_ohm}," +
                                        $"{sample.V},{sample.T_degC},{sample.Range_ohm},{sample.RowIndex},\"{reason}\"";
                        _rejectsWriter.WriteLine(csvLine);
                    }
                    else
                    {

                        string csvLine = $",,,,,,\",\"{reason}\"";
                        _rejectsWriter.WriteLine(csvLine);
                    }

                    _rejectsWriter.Flush();

                    Console.WriteLine($"Sample {(sample != null ? sample.RowIndex.ToString() : "<null>")} rejected and logged: {reason}");
                }
                else
                {
                    Console.WriteLine($"Reject attempted but rejectsWriter is null. Reason: {reason}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to write rejected sample: {ex.Message}");
            }
        }

        private void FinalizeSessionFiles()
        {
            try
            {
                if (_sessionDirectory != null && _currentSessionMeta != null)
                {
                    string summaryPath = Path.Combine(_sessionDirectory, "session_summary.txt");
                    using (var summaryWriter = new StreamWriter(summaryPath))
                    {
                        summaryWriter.WriteLine($"Battery Analysis Session Summary");
                        summaryWriter.WriteLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                        summaryWriter.WriteLine();
                        summaryWriter.WriteLine($"Battery ID: {_currentSessionMeta.BatteryId}");
                        summaryWriter.WriteLine($"Test ID: {_currentSessionMeta.TestId}");
                        summaryWriter.WriteLine($"State of Charge: {_currentSessionMeta.SoC}%");
                        summaryWriter.WriteLine($"Original File: {_currentSessionMeta.FileName}");
                        summaryWriter.WriteLine();
                        summaryWriter.WriteLine($"Declared (client) Samples: {_currentSessionMeta.TotalRows}");
                        summaryWriter.WriteLine($"Accepted (success) Samples: {_sessionSamples.Count}");
                        summaryWriter.WriteLine($"Rejected Samples: {_rejectedCount}");
                        double successRate = _currentSessionMeta.TotalRows > 0 ? (_sessionSamples.Count * 100.0 / _currentSessionMeta.TotalRows) : 0.0;
                        summaryWriter.WriteLine($"Success Rate: {successRate:F1}%");
                    }

                    Console.WriteLine($"Session summary written to: {summaryPath}");
                }

                CleanupFileResources();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error finalizing session files: {ex.Message}");
                CleanupFileResources();
            }
        }

        private void CleanupFileResources()
        {
            try
            {
                _sessionWriter?.Close();
                _sessionWriter?.Dispose();
                _sessionWriter = null;

                _rejectsWriter?.Close();
                _rejectsWriter?.Dispose();
                _rejectsWriter = null;

                _sessionDirectory = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error cleaning up file resources: {ex.Message}");
            }
        }

        private void ValidateMetaData(EisMeta meta)
        {
            if (meta == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Meta data cannot be null",
                        Field = "EisMeta",
                        ExpectedFormat = "Valid EisMeta object"
                    });
            }

            if (string.IsNullOrWhiteSpace(meta.BatteryId))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "BatteryId is required",
                        Field = "BatteryId",
                        ActualValue = meta.BatteryId,
                        AllowedRange = "B01-B11"
                    });
            }

            if (!meta.BatteryId.StartsWith("B") || meta.BatteryId.Length != 3)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid BatteryId format",
                        Field = "BatteryId",
                        ActualValue = meta.BatteryId,
                        AllowedRange = "B01-B11 format"
                    });
            }

            if (string.IsNullOrWhiteSpace(meta.TestId) ||
                (meta.TestId != "Test_1" && meta.TestId != "Test_2"))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid TestId",
                        Field = "TestId",
                        ActualValue = meta.TestId,
                        AllowedRange = "Test_1 or Test_2"
                    });
            }

            if (meta.SoC < 5 || meta.SoC > 100 || meta.SoC % 5 != 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid SoC percentage",
                        Field = "SoCPercentage",
                        ActualValue = meta.SoC,
                        AllowedRange = "5, 10, 15, ..., 100 (multiples of 5)"
                    });
            }

            if (string.IsNullOrWhiteSpace(meta.FileName))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "FileName is required",
                        Field = "FileName",
                        ActualValue = meta.FileName,
                        AllowedRange = "Non-empty string"
                    });
            }

            if (meta.TotalRows <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid TotalRows count",
                        Field = "TotalRows",
                        ActualValue = meta.TotalRows,
                        AllowedRange = "Expected: > 0"
                    });
            }
        }

        private void ValidateSample(EisSample sample)
        {
            if (sample == null)
            {
                throw new FaultException<DataFormatFault>(
                    new DataFormatFault
                    {
                        Message = "Sample cannot be null",
                        Field = "EisSample",
                        ExpectedFormat = "Valid EisSample object"
                    });
            }

            if (sample.FrequencyHz <= 0 || !IsValidReal(sample.FrequencyHz))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid frequency value",
                        Field = "FrequencyHz",
                        ActualValue = sample.FrequencyHz,
                        AllowedRange = "Positive real number > 0"
                    });
            }

            if (!IsValidReal(sample.R_ohm))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid real impedance value",
                        Field = "R_ohm",
                        ActualValue = sample.R_ohm,
                        AllowedRange = "Valid real number"
                    });
            }

            if (!IsValidReal(sample.X_ohm))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid imaginary impedance value",
                        Field = "X_ohm",
                        ActualValue = sample.X_ohm,
                        AllowedRange = "Valid real number"
                    });
            }

            if (!IsValidReal(sample.V) || sample.V < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid voltage value",
                        Field = "V",
                        ActualValue = sample.V,
                        AllowedRange = "Non-negative real number"
                    });
            }

            if (!IsValidReal(sample.T_degC))
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid temperature value",
                        Field = "T_degC",
                        ActualValue = sample.T_degC,
                        AllowedRange = "Valid real number"
                    });
            }

            if (!IsValidReal(sample.Range_ohm) || sample.Range_ohm <= 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid range value",
                        Field = "Range_ohm",
                        ActualValue = sample.Range_ohm,
                        AllowedRange = "Positive real number > 0"
                    });
            }

            if (sample.RowIndex < 0)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid row index",
                        Field = "RowIndex",
                        ActualValue = sample.RowIndex,
                        AllowedRange = "Non-negative integer >= 0"
                    });
            }
        }

        private void ValidateRowIndexSequence(int rowIndex)
        {
            if (rowIndex != _expectedRowIndex)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Row index is not monotonically increasing",
                        Field = "RowIndex",
                        ActualValue = rowIndex,
                        AllowedRange = $"Expected: {_expectedRowIndex}"
                    });
            }
        }

        private bool IsValidReal(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _sessionSamples?.Clear();
                    _currentSessionMeta = null;
                    _sessionActive = false;
                }
                _disposed = true;
            }
        }

        ~EisService()
        {
            Dispose(false);
        }
    }
}
