using ConPTY.Win32.Interop;
using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace ConPTY {
  /// <summary>
  /// A psuedoconsole based on <see
  /// href="https://github.com/microsoft/terminal/tree/main/samples/ConPTY/GUIConsole/GUIConsole.ConPTY"
  /// />.
  /// </summary>
  /// <param name="command">The command to execute to which to
  /// attach.</param>
  /// <param name="rows">The ConPTY number of rows.</param>
  /// <param name="columns">The ConPTY number of columns.</param>
  public class Pseudoconsole(string command, uint rows = 80, uint columns = 24) {
    private SafeFileHandle? consoleInputPipeWriteHandle;

    private PseudoConsole? pseudoConsole;

    /// <summary>
    /// Callback for receiving notification that the pseudoconsole is ready.
    /// </summary>
    public delegate void OnReady();

    /// <summary>
    /// Invoked when the console is ready.
    /// </summary>
    public event OnReady? Ready;

    /// <summary>
    /// Callback for receiving notification that the pseudoconsole is being
    /// disposed.
    /// </summary>
    public delegate void OnDisposing();

    /// <summary>
    /// Invoked when the psuedoconsole is being disposed.
    /// </summary>
    public event OnDisposing? Disposing;

    /// <summary>
    /// A stream of VT-100-enabled output from the console.
    /// </summary>
    public FileStream? ConsoleOutStream { get; private set; }

    /// <summary>
    /// A stream to which VT-100 enabled input can be written to the console.
    /// </summary>
    public FileStream? ConsoleInStream { get; private set; }

    /// <summary>
    /// The command the pseudoconsole was started with.
    /// </summary>
    public string Command => command;

    /// <summary>
    /// The number of console rows.
    /// </summary>
    public uint Rows {
      get => rows;

      set {
        if (rows != value) {
          rows = value;
          pseudoConsole?.Resize(rows, columns);
        }
      }
    }

    /// <summary>
    /// The number of console columns.
    /// </summary>
    public uint Columns {
      get => columns;

      set {
        if (columns != value) {
          columns = value;
          pseudoConsole?.Resize(rows, columns);
        }
      }
    }

    /// <summary>
    /// Starts the pseudoconsole, runs the command, and attaches input and
    /// output streams.
    /// </summary>
    /// <exception cref="System.ComponentModel.Win32Exception"></exception>
    public async Task Start() {
      using (Pipe stdin = new())
      using (Pipe stdout = new())
      using (pseudoConsole = PseudoConsole.Create(stdin.Read, stdout.Write, rows, columns))
      using (Process process = ProcessHelper.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, pseudoConsole.Handle)) {
        ConsoleOutStream = new FileStream(stdout.Read, FileAccess.Read);

        consoleInputPipeWriteHandle = stdin.Write;
        ConsoleInStream = new FileStream(consoleInputPipeWriteHandle, FileAccess.Write);

        Ready?.Invoke();
        OnClose(() => DisposeResources(process, pseudoConsole, stdout, stdin));
        await Task.Run(() => WaitForExit(process).WaitOne(Timeout.Infinite));
      }

      Disposing?.Invoke();
    }

    /// <summary>
    /// Creates an <see cref="AutoResetEvent"/> that can be used to wait for
    /// the specified process to exit.
    /// </summary>
    /// <remarks>The returned <see cref="AutoResetEvent"/> is associated with
    /// the process handle and must be disposed properly to release system
    /// resources. Ensure that the <see cref="Process"/> instance remains valid
    /// while using the event.</remarks>
    /// <param name="process">The <see cref="Process"/> instance representing
    /// the process to monitor.</param>
    /// <returns>An <see cref="AutoResetEvent"/> configured to signal when the
    /// process exits.</returns>
    private static AutoResetEvent WaitForExit(Process process) {
      return new(false) {
        SafeWaitHandle = new SafeWaitHandle(process.ProcessInformation.hProcess, false)
      };
    }

    /// <summary>
    /// Registers a handler to be invoked when a console close event occurs.
    /// </summary>
    /// <remarks>This method uses a platform invocation to set a console
    /// control handler. The provided  <paramref name="handler"/> will be
    /// called when a CTRL+C event is received. The method does not prevent
    /// the default behavior of the event.</remarks>
    /// <param name="handler">The action to execute when a console close event,
    /// such as a CTRL+C signal, is detected.</param>
    private static void OnClose(Action handler) {
      PInvoke.SetConsoleCtrlHandler(
        eventType => {
          if (eventType == PInvoke.CTRL_C_EVENT) handler();

          return false;
        },
        true
      );
    }

    /// <summary>
    /// Disposes all <see cref="IDisposable"/>s in <paramref
    /// name="disposables"/>.
    /// </summary>
    /// <param name="disposables">The <see cref="IDisposable"/>s to
    /// dispose.</param>
    private static void DisposeResources(params IDisposable[] disposables) {
      foreach (IDisposable disposable in disposables) disposable.Dispose();
    }
  }
}
