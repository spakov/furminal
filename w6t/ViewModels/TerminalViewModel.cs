using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.ConPTY;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace Spakov.W6t.ViewModels
{
    /// <summary>
    /// The Terminal viewmodel.
    /// </summary>
    internal partial class TerminalViewModel : ObservableObject
    {
        private readonly Views.Terminal _terminal;

        private readonly DispatcherQueue _dispatcherQueue;

        private readonly Pseudoconsole _pseudoconsole;

        private Palette _palette;

        private FileStream? _consoleOutput;
        private FileStream? _consoleInput;
        private int _rows;
        private int _columns;

        /// <summary>
        /// Callback for handling the case in which the pseudoconsole dies.
        /// </summary>
        public delegate void OnPseudoconsoleDied(Exception e);

        /// <summary>
        /// Invoked if the pseudoconsole dies.
        /// </summary>
        public event OnPseudoconsoleDied? PseudoconsoleDied;

        /// <summary>
        /// The <see cref="Palette"/> used for ANSI colors.
        /// </summary>
        public Palette AnsiColors
        {
            get => _palette;
            set => SetProperty(ref _palette, value);
        }

        /// <summary>
        /// The console's output <see cref="FileStream"/>.
        /// </summary>
        public FileStream? ConsoleOutput
        {
            get => _consoleOutput;
            set => SetProperty(ref _consoleOutput, value);
        }

        /// <summary>
        /// The console's input <see cref="FileStream"/>.
        /// </summary>
        public FileStream? ConsoleInput
        {
            get => _consoleInput;
            set => SetProperty(ref _consoleInput, value);
        }

        /// <summary>
        /// The number of console rows.
        /// </summary>
        /// <remarks>It's important to make sure we invoke <see
        /// cref="ObservableObject.SetProperty"/> before we tell the pseudoconsole
        /// about the change to ensure scrollback is handled gracefully!</remarks>
        public int Rows
        {
            get => _rows;

            set
            {
                int oldRows = _rows;

                SetProperty(ref _rows, value);

                if (oldRows != value)
                {
                    _pseudoconsole.Rows = (uint)value;
                }
            }
        }

        /// <summary>
        /// The number of console columns.
        /// </summary>
        public int Columns
        {
            get => _columns;

            set
            {
                int oldColumns = _columns;

                SetProperty(ref _columns, value);

                if (oldColumns != value)
                {
                    _pseudoconsole.Columns = (uint)value;
                }
            }
        }

        /// <summary>
        /// Initializes a <see cref="TerminalViewModel"/>.
        /// </summary>
        /// <param name="terminal">A <see cref="Views.Terminal"/>.</param>
        /// <param name="startDirectory">The directory in which to start the
        /// shell.</param>
        /// <param name="command">The command to execute in the
        /// pseudoconsole.</param>
        internal TerminalViewModel(Views.Terminal terminal, string? startDirectory, string command)
        {
            _terminal = terminal;

            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

            _palette = new();

            _rows = 24;
            _columns = 80;

            if (startDirectory is not null)
            {
                string expandedStartDirectory = Environment.ExpandEnvironmentVariables(startDirectory);

                if (Directory.Exists(expandedStartDirectory) && Directory.GetCurrentDirectory() == Environment.SystemDirectory)
                {
                    Directory.SetCurrentDirectory(expandedStartDirectory);
                }
            }

            _pseudoconsole = new(command, (uint)_rows, (uint)_columns);
            _pseudoconsole.Ready += Pseudoconsole_Ready;
            _pseudoconsole.Done += Pseudoconsole_Done;

            StartPseudoconsole();
        }

        /// <summary>
        /// Starts the pseudoconsole, checking for error conditions.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private async void StartPseudoconsole()
        {
            try
            {
                await Task.Run(_pseudoconsole.Start);
            }
            catch (Win32Exception e)
            {
                PseudoconsoleDied?.Invoke(new ArgumentException($"Failed to start pseudoconsole with command \"{_pseudoconsole.Command}\".", e));
            }
        }

        /// <summary>
        /// Invoked when the pseudoconsole is ready.
        /// </summary>
        private void Pseudoconsole_Ready()
        {
            ConsoleOutput = _pseudoconsole.ConsoleOutStream;
            ConsoleInput = _pseudoconsole.ConsoleInStream;
        }

        /// <summary>
        /// Invoked when the pseudoconsole is being disposed.
        /// </summary>
        /// <param name="exitCode">The exit code of the command that
        /// executed.</param>
        private void Pseudoconsole_Done(uint exitCode)
        {
            if (exitCode == 0)
            {
                _dispatcherQueue.TryEnqueue(Application.Current.Exit);
            }
            else
            {
                _terminal.Write($"{_pseudoconsole.Command} exited with {exitCode}");
            }
        }
    }
}
