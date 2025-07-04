using AnsiProcessor.AnsiColors;
using CommunityToolkit.Mvvm.ComponentModel;
using ConPTY;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;

namespace w6t.ViewModels {
  internal partial class TerminalViewModel : ObservableObject {
    private readonly Views.Terminal terminal;

    private readonly DispatcherQueue dispatcherQueue;

    private readonly Pseudoconsole pseudoconsole;

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
    public Palette AnsiColors {
      get => _palette;
      set => SetProperty(ref _palette, value);
    }

    /// <summary>
    /// The console's output <see cref="FileStream"/>.
    /// </summary>
    public FileStream? ConsoleOutput {
      get => _consoleOutput;
      set => SetProperty(ref _consoleOutput, value);
    }

    /// <summary>
    /// The console's input <see cref="FileStream"/>.
    /// </summary>
    public FileStream? ConsoleInput {
      get => _consoleInput;
      set => SetProperty(ref _consoleInput, value);
    }

    /// <summary>
    /// The number of console rows.
    /// </summary>
    /// <remarks>It's important to make sure we invoke <see
    /// cref="ObservableObject.SetProperty"/> before we tell the pseudoconsole
    /// about the change to ensure scrollback is handled gracefully!</remarks>
    public int Rows {
      get => _rows;

      set {
        int oldRows = _rows;

        SetProperty(ref _rows, value);

        if (oldRows != value) {
          pseudoconsole.Rows = (uint) value;
        }
      }
    }

    /// <summary>
    /// The number of console columns.
    /// </summary>
    public int Columns {
      get => _columns;

      set {
        int oldColumns = _columns;

        SetProperty(ref _columns, value);

        if (oldColumns != value) {
          pseudoconsole.Columns = (uint) value;
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
    internal TerminalViewModel(Views.Terminal terminal, string? startDirectory, string command) {
      this.terminal = terminal;

      dispatcherQueue = DispatcherQueue.GetForCurrentThread();

      _palette = new();

      _rows = 24;
      _columns = 80;

      if (startDirectory is not null) {
        string expandedStartDirectory = Environment.ExpandEnvironmentVariables(startDirectory);

        if (Directory.Exists(expandedStartDirectory) && Directory.GetCurrentDirectory() == Environment.SystemDirectory) {
          Directory.SetCurrentDirectory(expandedStartDirectory);
        }
      }

      pseudoconsole = new(command, (uint) _rows, (uint) _columns);
      pseudoconsole.Ready += Pseudoconsole_Ready;
      pseudoconsole.Done += Pseudoconsole_Done;

      StartPseudoconsole();
    }

    /// <summary>
    /// Starts the pseudoconsole, checking for error conditions.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    private async void StartPseudoconsole() {
      try {
        await Task.Run(pseudoconsole.Start);
      } catch (Win32Exception e) {
         PseudoconsoleDied?.Invoke(new ArgumentException($"Failed to start pseudoconsole with command \"{pseudoconsole.Command}\".", e));
      }
    }

    /// <summary>
    /// Invoked when the pseudoconsole is ready.
    /// </summary>
    private void Pseudoconsole_Ready() {
      ConsoleOutput = pseudoconsole.ConsoleOutStream;
      ConsoleInput = pseudoconsole.ConsoleInStream;
    }

    /// <summary>
    /// Invoked when the pseudoconsole is being disposed.
    /// </summary>
    /// <param name="exitCode">The exit code of the command that
    /// executed.</param>
    private void Pseudoconsole_Done(uint exitCode) {
      if (exitCode == 0) {
        dispatcherQueue.TryEnqueue(Application.Current.Exit);
      } else {
        terminal.Write($"{pseudoconsole.Command} exited with {exitCode}");
      }
    }
  }
}
