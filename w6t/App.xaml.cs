using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Spakov.W6t {
  /// <summary>
  /// Provides application-specific behavior to supplement the default
  /// Application class.
  /// </summary>
  public partial class App : Application {
    private Window? _window;

    /// <summary>
    /// Initializes the singleton application object.  This is the first line
    /// of authored code executed, and as such is the logical equivalent of
    /// main() or WinMain().
    /// </summary>
    public App() {
      InitializeComponent();
      UnhandledException += (sender, e) => {
        PInvoke.MessageBox(
          HWND.Null,
          $"Unhandled exception: {e.Exception}",
          "Unhandled Exception",
          MESSAGEBOX_STYLE.MB_OK
        );
      };
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and
    /// process.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args) {
      ResourceLoader resources = ResourceLoader.GetForViewIndependentUse();

      StringWriter commandLineOutput = new();
      string[] rawCommandLineArgs = Environment.GetCommandLineArgs();
      List<string> commandLineArgs = [];

      for (int i = 1; i < rawCommandLineArgs.Length; i++) {
        commandLineArgs.Add(rawCommandLineArgs[i]);
      }

      string[]? startCommand = null;
      int? startRows = null;
      int? startColumns = null;

      Argument<string[]> commandArgument = new("command") {
        Description = resources.GetString("CommandArgumentDescription"),
        Arity = ArgumentArity.ZeroOrMore
      };

      Option<int?> rowsOption = new("--rows", ["-r", "/r"]) {
        Description = resources.GetString("RowsOptionDescription"),
        Required = false,
        Arity = ArgumentArity.ExactlyOne
      };

      Option<int?> columnsOption = new("--columns", ["--cols", "-c", "/c"]) {
        Description = resources.GetString("ColumnsOptionDescription"),
        Required = false,
        Arity = ArgumentArity.ExactlyOne
      };

      RootCommand rootCommand = new() {
        Description = resources.GetString("Description")
      };

      CommandLineConfiguration commandLineConfiguration = new(rootCommand) {
        Output = commandLineOutput
      };

      rootCommand.Options.Insert(0, rowsOption);
      rootCommand.Options.Insert(1, columnsOption);
      rootCommand.Arguments.Add(commandArgument);

      rootCommand.SetAction(parseResult => {
        startCommand = parseResult.GetValue(commandArgument);
        startRows = parseResult.GetValue(rowsOption);
        startColumns = parseResult.GetValue(columnsOption);

        if (startRows < 1) startRows = null;
        if (startColumns < 1) startColumns = null;

        return 0;
      });

      ParseResult parseResult = rootCommand.Parse(commandLineArgs, commandLineConfiguration);
      parseResult.Invoke();

      if (commandLineOutput.ToString().Length > 0 || parseResult.Errors.Count > 0) {
        StringBuilder commandLineMessage = new();

        foreach (ParseError parseError in parseResult.Errors) {
          commandLineMessage.AppendLine(string.Format(resources.GetString("CommandLineError"), parseError.Message));
        }

        if (parseResult.Errors.Count > 0) {
          commandLineMessage.AppendLine(null);
        }

        commandLineMessage.Append(commandLineOutput.ToString());

        PInvoke.MessageBox(
          HWND.Null,
          commandLineMessage.ToString(),
          "w6t",
          Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK
        );

        Exit();
        return;
      }

      _window = new Views.Terminal(startCommand, startRows, startColumns);

      int? width = (int?) (double?) ApplicationData.Current.LocalSettings.Values["WindowWidth"] ?? 600;
      int? height = (int?) (double?) ApplicationData.Current.LocalSettings.Values["WindowHeight"] ?? 400;

      ((Views.Terminal) _window).ResizeLock = true;
      _window.AppWindow.ResizeClient(new((int) width, (int) height));
      _window.Activate();
      ((Views.Terminal) _window).ResizeLock = false;
    }
  }
}
