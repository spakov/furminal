using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace Spakov.ConPTY.Win32.Interop
{
    /// <summary>
    /// A psuedoconsole pipe.
    /// </summary>
    internal sealed class Pipe : IDisposable
    {
        private readonly SafeFileHandle _read;
        private readonly SafeFileHandle _write;

        private bool _disposedValue;

        /// <summary>
        /// The read side of the pipe.
        /// </summary>
        public SafeFileHandle Read => _read;

        /// <summary>
        /// The write side of the pipe.
        /// </summary>
        public SafeFileHandle Write => _write;

        /// <summary>
        /// Initializes a <see cref="Pipe"/>.
        /// </summary>
        public Pipe()
        {
            if (!PInvoke.CreatePipe(out _read, out _write, null, 0))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _read?.Dispose();
                    _write?.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool
            // disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
