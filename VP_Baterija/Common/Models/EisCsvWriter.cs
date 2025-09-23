using Common.Services;
using System.IO;
using System.Text;
using System;

namespace Common.Models
{
    public class EisCsvWriter : DisposableBase
    {
        private FileStream fileStream;
        private StreamWriter streamWriter;
        private readonly string filePath;

        public EisCsvWriter(string filePath, bool append = false)
        {
            this.filePath = filePath;
            Initialize(append);
        }

        private void Initialize(bool append)
        {
            try
            {
                // Ensure directory exists
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                fileStream = new FileStream(filePath,
                    append ? FileMode.Append : FileMode.Create,
                    FileAccess.Write);
                streamWriter = new StreamWriter(fileStream, Encoding.UTF8);
                Console.WriteLine($"Opened file for writing: {filePath}");
            }
            catch
            {
                // Clean up if initialization fails
                streamWriter?.Dispose();
                fileStream?.Dispose();
                throw;
            }
        }

        public void WriteLine(string line)
        {
            ThrowIfDisposed();
            streamWriter?.WriteLine(line);
            streamWriter?.Flush(); // Ensure data is written
        }

        public void WriteEisSample(EisSample sample)
        {
            ThrowIfDisposed();
            var csvLine = $"{sample.FrequencyHz},{sample.R_ohm},{sample.X_ohm}," +
                         $"{sample.V},{sample.T_degC},{sample.Range_ohm},{sample.RowIndex}";
            WriteLine(csvLine);
        }

        protected override void DisposeManagedResources()
        {
            streamWriter?.Dispose();
            fileStream?.Dispose();
            Console.WriteLine($"Resources disposed for: {filePath}");
        }
    }
}
