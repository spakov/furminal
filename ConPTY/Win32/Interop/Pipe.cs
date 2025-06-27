using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace ConPTY.Win32.Interop {
  /// <summary>
  /// A psuedoconsole pipe.
  /// </summary>
  internal sealed class Pipe : IDisposable {
    private readonly SafeFileHandle read;
    private readonly SafeFileHandle write;

    private bool disposedValue;

    /// <summary>
    /// The read side of the pipe.
    /// </summary>
    public SafeFileHandle Read => read;

    /// <summary>
    /// The write side of the pipe.
    /// </summary>
    public SafeFileHandle Write => write;

    /// <summary>
    /// Initializes a <see cref="Pipe"/>.
    /// </summary>
    public Pipe() {
      if (!PInvoke.CreatePipe(out read, out write, null, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private void Dispose(bool disposing) {
      if (!disposedValue) {
        if (disposing) {
          read?.Dispose();
          write?.Dispose();
        }

        disposedValue = true;
      }
    }

    public void Dispose() {
      // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      Dispose(disposing: true);
      GC.SuppressFinalize(this);
    }
  }
}
