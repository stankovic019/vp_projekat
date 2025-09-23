using Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class EisCsvReader : DisposableBase
    {
        private FileStream fileStream;
        private StreamReader streamReader;
        private readonly string filePath;

        public EisCsvReader(string filePath)
        {
            this.filePath = filePath;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                streamReader = new StreamReader(fileStream, Encoding.UTF8);
                Console.WriteLine($"Opened file for reading: {filePath}");
            }
            catch
            {
                // Clean up if initialization fails
                streamReader?.Dispose();
                fileStream?.Dispose();
                throw;
            }
        }

        public string ReadLine()
        {
            ThrowIfDisposed();
            return streamReader?.ReadLine();
        }

        public string ReadAllText()
        {
            ThrowIfDisposed();
            return streamReader?.ReadToEnd();
        }

        public bool EndOfStream
        {
            get
            {
                ThrowIfDisposed();
                return streamReader?.EndOfStream ?? true;
            }
        }

        protected override void DisposeManagedResources()
        {
            streamReader?.Dispose();
            fileStream?.Dispose();
            Console.WriteLine($"Resources disposed for: {filePath}");
        }
    }
}
