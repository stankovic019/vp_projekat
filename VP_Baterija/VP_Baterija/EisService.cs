using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using Common.Services;
using Common.Models;

namespace VP_Baterija
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession)]
    public class EisService : IEisService, IDisposable
    {
        private EisMeta _currentSessionMeta;
        private List<EisSample> _sessionSamples;
        private int _expectedRowIndex = 0;
        private bool _sessionActive = false;
        private bool _disposed = false;

        public EisService()
        {
            _sessionSamples = new List<EisSample>();
        }

        [OperationBehavior]
        public void StartSession(EisMeta meta)
        {
            ValidateMetaData(meta);

            if (_sessionActive)
            {
                throw new FaultException("Session already active. End current session first.");
            }

            _currentSessionMeta = meta;
            _sessionSamples.Clear();
            _expectedRowIndex = 0;
            _sessionActive = true;

            Console.WriteLine($"ACK - Session started for Battery: {meta.BatteryId}, Test: {meta.TestId}, SoC: {meta.SoC}% - Status: IN_PROGRESS");
        }

        [OperationBehavior]
        public void PushSample(EisSample sample)
        {
            if (!_sessionActive)
            {
                throw new FaultException("No active session. Start session first.");
            }

            ValidateSample(sample);

            ValidateRowIndexSequence(sample.RowIndex);

            _sessionSamples.Add(sample);
            _expectedRowIndex++;

            Console.WriteLine($"ACK - Sample received: Row {sample.RowIndex}, Frequency: {sample.FrequencyHz} Hz - Status: IN_PROGRESS");
        }

        [OperationBehavior]
        public void EndSession()
        {
            if (!_sessionActive)
            {
                throw new FaultException("No active session to end.");
            }

            if (_sessionSamples.Count != _currentSessionMeta.TotalRows)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Received sample count doesn't match expected total rows",
                        Field = "TotalRows",
                        ActualValue = _sessionSamples.Count,
                        AllowedRange = $"Expected: {_currentSessionMeta.TotalRows}"
                    });
            }

            Console.WriteLine($"ACK - Session completed. Total samples: {_sessionSamples.Count} - Status: COMPLETED");

            _sessionActive = false;
            _currentSessionMeta = null;
            _sessionSamples.Clear();
            _expectedRowIndex = 0;
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

            if (meta.TotalRows <= 0 || meta.TotalRows != 28)
            {
                throw new FaultException<ValidationFault>(
                    new ValidationFault
                    {
                        Message = "Invalid TotalRows count",
                        Field = "TotalRows",
                        ActualValue = meta.TotalRows,
                        AllowedRange = "Expected: 28 rows"
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
