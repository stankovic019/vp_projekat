using Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Models
{
    public class NetworkStreamWrapper : DisposableBase
    {
        private Stream networkStream;
        private BinaryWriter writer;
        private BinaryReader reader;

        public NetworkStreamWrapper(Stream stream)
        {
            networkStream = stream ?? throw new ArgumentNullException(nameof(stream));
            writer = new BinaryWriter(networkStream);
            reader = new BinaryReader(networkStream);
        }

        public void WriteData(byte[] data)
        {
            ThrowIfDisposed();
            writer.Write(data.Length);
            writer.Write(data);
            writer.Flush();
        }

        public byte[] ReadData()
        {
            ThrowIfDisposed();
            int length = reader.ReadInt32();
            return reader.ReadBytes(length);
        }

        protected override void DisposeManagedResources()
        {
            writer?.Dispose();
            reader?.Dispose();
            networkStream?.Dispose();
            Console.WriteLine("Network stream resources disposed");
        }
    }
}
