using Microsoft.Win32.SafeHandles;
using Spakov.ConPTY.Win32.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

namespace Spakov.ConPTY
{
    /// <summary>
    /// A psuedoconsole based on <see
    /// href="https://github.com/microsoft/terminal/tree/main/samples/ConPTY/GUIConsole/GUIConsole.ConPTY"
    /// />.
    /// </summary>
    /// <param name="command">The command to execute to which to
    /// attach.</param>
    /// <param name="rows">The ConPTY number of rows.</param>
    /// <param name="columns">The ConPTY number of columns.</param>
    /// <param name="restartOnDone">Whether to restart the process when it
    /// exits normally.</param>
    public class Pseudoconsole(string command, uint rows = 80, uint columns = 24, bool restartOnDone = false) : IDisposable
    {
        private Pipe? _stdin;
        private Pipe? _stdout;
        private PseudoConsole? _pseudoConsole;
        private Process? _process;

        private bool _disposedValue;

        /// <summary>
        /// Callback for receiving notification that the pseudoconsole is
        /// ready.
        /// </summary>
        public delegate void OnReady();

        /// <summary>
        /// Invoked when the console is ready.
        /// </summary>
        public event OnReady? Ready;

        /// <summary>
        /// Callback for receiving notification that the pseudoconsole is done.
        /// </summary>
        public delegate void OnDone(uint exitCode);

        /// <summary>
        /// Invoked when the psuedoconsole is done.
        /// </summary>
        public event OnDone? Done;

        /// <summary>
        /// A stream of VT-100-enabled output from the console.
        /// </summary>
        public FileStream? ConsoleOutStream { get; private set; }

        /// <summary>
        /// A stream to which VT-100 enabled input can be written to the
        /// console.
        /// </summary>
        public FileStream? ConsoleInStream { get; private set; }

        /// <summary>
        /// The command the pseudoconsole was started with.
        /// </summary>
        public string Command => command;

        /// <summary>
        /// The number of console rows.
        /// </summary>
        public uint Rows
        {
            get => rows;

            set
            {
                if (rows != value)
                {
                    rows = value;
                    _pseudoConsole?.Resize(rows, columns);
                }
            }
        }

        /// <summary>
        /// The number of console columns.
        /// </summary>
        public uint Columns
        {
            get => columns;

            set
            {
                if (columns != value)
                {
                    columns = value;
                    _pseudoConsole?.Resize(rows, columns);
                }
            }
        }

        /// <summary>
        /// Whether to restart the process when it exits normally.
        /// </summary>
        public bool RestartOnDone
        {
            get => restartOnDone;

            set
            {
                if (restartOnDone != value)
                {
                    restartOnDone = value;
                }
            }
        }

        /// <summary>
        /// Starts the pseudoconsole, runs the command, and attaches input and
        /// output streams.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception"></exception>
        public async Task Start()
        {
            _stdin = new();
            _stdout = new();
            _pseudoConsole = PseudoConsole.Create(_stdin.Read, _stdout.Write, rows, columns);

            ConsoleOutStream = new FileStream(_stdout.Read, FileAccess.Read);
            ConsoleInStream = new FileStream(_stdin.Write, FileAccess.Write);

            Ready?.Invoke();

            uint exitCode;

            do
            {
                _process = ProcessHelper.Start(command, PseudoConsole.PseudoConsoleThreadAttribute, _pseudoConsole.Handle);
                await Task.Run(() => WaitForExit(_process).WaitOne(Timeout.Infinite));
                PInvoke.GetExitCodeProcess(new SafeProcessHandle(_process.ProcessInformation.hProcess, false), out exitCode);
                Done?.Invoke(exitCode);
            } while (RestartOnDone && exitCode == 0);
        }

        /// <summary>
        /// Creates an <see cref="AutoResetEvent"/> that can be used to wait
        /// for the specified process to exit.
        /// </summary>
        /// <remarks>The returned <see cref="AutoResetEvent"/> is associated
        /// with the process handle and must be disposed properly to release
        /// system resources. Ensure that the <see cref="Process"/> instance
        /// remains valid while using the event.</remarks>
        /// <param name="process">The <see cref="Process"/> instance
        /// representing the process to monitor.</param>
        /// <returns>An <see cref="AutoResetEvent"/> configured to signal when
        /// the process exits.</returns>
        private static AutoResetEvent WaitForExit(Process process)
        {
            return new(false)
            {
                SafeWaitHandle = new SafeWaitHandle(process.ProcessInformation.hProcess, false)
            };
        }

        /// <summary>
        /// Disposes all <see cref="IDisposable"/>s in <paramref
        /// name="disposables"/>.
        /// </summary>
        /// <param name="disposables">The <see cref="IDisposable"/>s to
        /// dispose.</param>
        private static void DisposeResources(IDisposable[] disposables)
        {
            foreach (IDisposable disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Disposes of resources.
        /// </summary>
        /// <param name="disposing">Whether to dispose of managed
        /// resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    List<IDisposable> disposables = [];

                    if (_process is not null)
                    {
                        disposables.Add(_process);
                    }

                    if (_pseudoConsole is not null)
                    {
                        disposables.Add(_pseudoConsole);
                    }

                    if (_stdout is not null)
                    {
                        disposables.Add(_stdout);
                    }

                    if (_stdin is not null)
                    {
                        disposables.Add(_stdin);
                    }

                    DisposeResources([.. disposables]);
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
