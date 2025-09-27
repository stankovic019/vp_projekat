using System;

namespace Common.Services
{
    public abstract class DisposableBase : IDisposable
    {
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    DisposeManagedResources();
                }
                DisposeUnmanagedResources();
                disposed = true;
            }
        }

        protected abstract void DisposeManagedResources();
        protected virtual void DisposeUnmanagedResources() { }

        protected void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        ~DisposableBase()
        {
            Dispose(false);
        }
    }
}
