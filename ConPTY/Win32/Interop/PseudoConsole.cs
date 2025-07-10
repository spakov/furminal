using Microsoft.Win32.SafeHandles;
using System;
using System.ComponentModel;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Console;

namespace Spakov.ConPTY.Win32.Interop {
  internal class PseudoConsole : IDisposable {
    internal static nuint PseudoConsoleThreadAttribute => PInvoke.PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE;

    internal ClosePseudoConsoleSafeHandle Handle { get; }

    private PseudoConsole(ClosePseudoConsoleSafeHandle handle) {
      Handle = handle;
    }

    /// <summary>
    /// Creates a pseudoconsole of dimensions <paramref name="rows"/> by
    /// <paramref name="columns"/> and attaches <paramref name="inputRead"/> and
    /// <paramref name="outputWrite"/>.
    /// </summary>
    /// <param name="inputRead">The input stream.</param>
    /// <param name="outputWrite">The output stream.</param>
    /// <param name="rows">The pseudoconsole width.</param>
    /// <param name="columns">The pseudoconsole height.</param>
    /// <returns>A <see cref="PseudoConsole"/>.</returns>
    /// <exception cref="Win32Exception"></exception>
    internal static PseudoConsole Create(SafeFileHandle inputRead, SafeFileHandle outputWrite, uint rows, uint columns) {
      HRESULT hr = PInvoke.CreatePseudoConsole(
        new COORD() { X = (short) columns, Y = (short) rows },
        inputRead,
        outputWrite,
        0,
        out ClosePseudoConsoleSafeHandle phPC
      );

      return hr == 0
        ? new(phPC)
        : throw new Win32Exception(hr);
    }

    /// <summary>
    /// Resizes the pseudoconsole to <paramref name="rows"/> by <paramref
    /// name="columns"/>.
    /// </summary>
    /// <param name="rows">The pseudoconsole width.</param>
    /// <param name="columns">The pseudoconsole height.</param>
    /// <returns><see langword="true"/> if the console was resized or <see
    /// langword="false"/> otherwise.</returns>
    internal bool Resize(uint rows, uint columns) {
      HRESULT hr = PInvoke.ResizePseudoConsole(
        Handle,
        new COORD() { X = (short) columns, Y = (short) rows }
      );

      return hr == 0;
    }

    public void Dispose() => PInvoke.ClosePseudoConsole((HPCON) Handle.DangerousGetHandle());
  }
}
