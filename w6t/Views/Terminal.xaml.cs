#if DEBUG
using Microsoft.Extensions.Logging;
#endif
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spakov.Terminal;
using Spakov.W6t.Settings;
using Spakov.W6t.Settings.Json;
using Spakov.W6t.ViewModels;
using System;
using System.IO;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace Spakov.W6t.Views {
  /// <summary>
  /// The w6t terminal.
  /// </summary>
  /// <remarks>
  /// <para><c>Terminal.Settings.xaml.cs</c> contains code related to settings
  /// management.</para>
  /// </remarks>
  public sealed partial class Terminal : Window {
#if DEBUG
    internal readonly ILogger logger;
    internal static readonly LogLevel logLevel = LogLevel.None;
#endif

    // I have no idea why we're off by 3 pixels, but it seems to be
    // consistent, at least
    private const int mysteryOffset = 3;

    private readonly ResourceLoader resources;

    private readonly DependencyProperties dependencyProperties;

    private readonly DispatcherQueueTimer visualBellTimer;

    private TerminalViewModel? viewModel;

    private readonly string[]? startCommand;
    private readonly int? startRows;
    private readonly int? startColumns;

    private int? clientAreaOffset;
    private bool _resizeLock;

    /// <summary>
    /// The <see cref="TerminalViewModel"/>.
    /// </summary>
    private TerminalViewModel? ViewModel {
      get => viewModel;
      set => viewModel = value;
    }

    /// <summary>
    /// The start command provided on the command line.
    /// </summary>
    internal string[]? StartCommand => startCommand;

    /// <summary>
    /// The start number of rows provided on the command line.
    /// </summary>
    internal int? StartRows => startRows;

    /// <summary>
    /// The start number of columns provided on the command line.
    /// </summary>
    internal int? StartColumns => startColumns;

    /// <summary>
    /// A "lock", to prevent resizing from cascading out of control.
    /// </summary>
    internal bool ResizeLock {
      get => _resizeLock;

      set {
        _resizeLock = value;

#if DEBUG
        logger.LogDebug("{resizeLock} ResizeLock", _resizeLock ? "Set" : "Cleared");
#endif
      }
    }

    /// <summary>
    /// The Mica window backdrop.
    /// </summary>
    internal static MicaBackdrop MicaWindowBackdrop { get; } = new();

    /// <summary>
    /// The Acrylic window backdrop.
    /// </summary>
    internal static DesktopAcrylicBackdrop AcrylicWindowBackdrop { get; } = new();

    /// <summary>
    /// The blurrerd window backdrop.
    /// </summary>
    internal static BlurredBackdrop BlurredWindowBackdrop { get; } = new();

    /// <summary>
    /// The transparent window backdrop.
    /// </summary>
    internal static TransparentTintBackdrop TransparentWindowBackdrop { get; } = new();

    /// <summary>
    /// Initializes a <see cref="Terminal"/>.
    /// </summary>
    /// <param name="startRows">The number of terminal rows.</param>
    /// <param name="startColumns">The number of terminal columns.</param>
    public Terminal(string[]? startCommand, int? startRows, int? startColumns) {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(logLevel);
        }
      );

      logger = factory.CreateLogger<Terminal>();
#endif

      this.startCommand = startCommand;
      this.startRows = startRows;
      this.startColumns = startColumns;

#if DEBUG
      if (startCommand is not null) {
        logger.LogInformation("<command>:");

        foreach (string startCommandPart in startCommand) {
          logger.LogInformation("  {startCommandPart}", startCommandPart);
        }
      }

      logger.LogInformation("--rows: {startRows}", startRows);
      logger.LogInformation("--columns: {startColumns}", startColumns);
#endif

      jsonSerializerOptions = new(SettingsContext.Default.Options);
      jsonSerializerOptions.Converters.Add(new ColorJsonConverter());

      settingsJsonPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, settingsJsonFilename);

      settingsJsonWatcher = new() {
        Path = ApplicationData.Current.LocalFolder.Path,
        Filter = settingsJsonFilename,
        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
        EnableRaisingEvents = true
      };
      settingsJsonWatcher.Changed += SettingsJsonWatcher_Changed;

      settingsSaveTimer = new() {
        Interval = settleDelay,
        AutoReset = false
      };

      dependencyProperties = new(this);
      LoadSettings(initialLoad: true);

      visualBellTimer = DispatcherQueue.CreateTimer();
      UpdateVisualBellTimerInterval();
      visualBellTimer.Tick += VisualBellTimer_Tick;

      ViewModel = new(this, dependencyProperties.StartDirectory, dependencyProperties.Command);
      ViewModel.PseudoconsoleDied += ViewModel_PseudoconsoleDied;
      InitializeComponent();
      resources = ResourceLoader.GetForViewIndependentUse();

      // Our Win32 window's cursor, at this point, is (probably) IDC_WAIT. It
      // "bleeds through" when we do things like display context menus.
      // Explanation from Raymond Chen:
      // https://devblogs.microsoft.com/oldnewthing/20250424-00/?p=111114
      PInvoke.SetCursor(PInvoke.LoadCursor((HMODULE) (nint) 0, PInvoke.IDC_ARROW));

      ExtendsContentIntoTitleBar = true;

      TerminalControl.Focus(FocusState.Programmatic);
      TerminalControl.WindowTitleChanged += TerminalControl_WindowTitleChanged;
      TerminalControl.VisualBellRinging += TerminalControl_VisualBellRinging;
      TerminalControl.CustomizeSettingsWindowSettings += TerminalControl_CustomizeSettingsWindowSettings;
      TerminalControl.SaveSettingsAsDefaults += TerminalControl_SaveSettingsAsDefaults;

      TitleTextBlock.FontFamily = new(TerminalControl.FontFamily);
      TitleTextBlock.FontSize = TerminalControl.FontSize;
      TerminalControl.RegisterPropertyChangedCallback(TerminalControl.FontFamilyProperty, SetFontFamily);
      TerminalControl.RegisterPropertyChangedCallback(TerminalControl.FontSizeProperty, SetFontSize);
    }

    /// <summary>
    /// Writes <paramref name="message"/> to the terminal.
    /// </summary>
    /// <param name="message">The message to write.</param>
    internal void Write(string message) => TerminalControl.Write(message);

    /// <summary>
    /// Handles the case in which the <see cref="ViewModel"/>'s <see
    /// cref="ConPTY.Pseudoconsole"/> dies.
    /// </summary>
    private void ViewModel_PseudoconsoleDied(Exception e) {
      StringBuilder message = new();

      message.Append(e.Message);

      if (e.InnerException is not null) {
        message.Append("\r\n");
        message.Append(e.InnerException.Message);
      }

      TerminalControl.WriteError(message.ToString());
    }

    /// <summary>
    /// Updates the visual bell timer interval from <see
    /// cref="DependencyProperties.VisualBellDisplayTime"/>.
    /// </summary>
    internal void UpdateVisualBellTimerInterval() => visualBellTimer.Interval = TimeSpan.FromSeconds(dependencyProperties.VisualBellDisplayTime);

    /// <summary>
    /// Invoked when the terminal's window title changes.
    /// </summary>
    private void TerminalControl_WindowTitleChanged() {
      TitleTextBlock.Text = TerminalControl.WindowTitle;
      Title = TerminalControl.WindowTitle;
    }

    /// <summary>
    /// Invoked when the terminal's visual bell is ringing.
    /// </summary>
    private void TerminalControl_VisualBellRinging() {
      VisualBellFontIcon.Visibility = Visibility.Visible;
      visualBellTimer.Start();
    }

    /// <summary>
    /// Invoked when the terminal font family changed.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="dp"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void SetFontFamily(DependencyObject sender, DependencyProperty dp) {
      if (TerminalControl.FontFamily is not null) {
        TitleTextBlock.FontFamily = new(TerminalControl.FontFamily);
      }
    }

    /// <summary>
    /// Invoked when the terminal font size changed.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="dp"><inheritdoc
    /// cref="DependencyPropertyChangedEventHandler"
    /// path="/param[@name='e']"/></param>
    /// <exception cref="NotImplementedException"></exception>
    private void SetFontSize(DependencyObject sender, DependencyProperty dp) => TitleTextBlock.FontSize = TerminalControl.FontSize;

    /// <summary>
    /// Invoked when the terminal is resized.
    /// </summary>
    private void TerminalControl_TerminalResize() {
      // Ignore resizes caused by Window_SizeChanged()
      if (ResizeLock) {
#if DEBUG
        logger.LogDebug("TerminalControl_TerminalResize(): no-op due to ResizeLock");
#endif

        return;
      }

#if DEBUG
      logger.LogDebug("TerminalControl_TerminalResize():");
      logger.LogDebug("  NominalSizeInPixels: {width} x {height}", TerminalControl.NominalSizeInPixels.Width, TerminalControl.NominalSizeInPixels.Height);
#endif

      AppWindow.ResizeClient(
        new(
          (int) Math.Ceiling(TerminalControl.NominalSizeInPixels.Width),
          (int) Math.Ceiling(TerminalControl.NominalSizeInPixels.Height) + mysteryOffset
        )
      );
    }

    /// <summary>
    /// Invoked when the window is activated.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Window_Activated(object sender, WindowActivatedEventArgs args) {
      TitleTextBlock.Foreground = args.WindowActivationState == WindowActivationState.Deactivated
        ? (SolidColorBrush) App.Current.Resources["WindowCaptionForegroundDisabled"]
        : (SolidColorBrush) App.Current.Resources["WindowCaptionForeground"];
    }

    /// <summary>
    /// Invoked when the window is resized.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args) {
#if DEBUG
      logger.LogDebug("Window_SizeChanged(): invoked with ({width}, {height})", args.Size.Width, args.Size.Height);
#endif

      // Ignore resizes caused by App.OnLaunched()
      if (ResizeLock) {
#if DEBUG
        logger.LogDebug("Window_SizeChanged(): no-op due to ResizeLock");
#endif

        return;
      }

      // Make sure the terminal is ready
      Size terminalSize = TerminalControl.NominalSizeInPixels.ToSize();
      if (terminalSize.Width == 0.0 && terminalSize.Height == 0.0) {
#if DEBUG
        logger.LogDebug("Window_SizeChanged(): no-op due to terminal not ready");
#endif

        return;
      }

      // Determine the client area offset, which should remain constant. This
      // is the actual height of the window chrome, which can (and evidently
      // does) differ from the height set in XAML.
      clientAreaOffset ??= (int) (AppWindow.ClientSize.Height - TerminalControl.NominalSizeInPixels.Height);

      // Account for the title bar, which is part of the client area
      double requestedWidth = AppWindow.ClientSize.Width;
      double requestedHeight = AppWindow.ClientSize.Height - (int) clientAreaOffset!;

      ResizeLock = true;
      TerminalControl.NominalSizeInPixels = new(requestedWidth, requestedHeight);
      ResizeLock = false;

#if DEBUG
      logger.LogDebug("Window_SizeChanged():");
      logger.LogDebug("  Requested: {width} x {height}", requestedWidth, requestedHeight);
      logger.LogDebug("  NominalSizeInPixels: {width}, {height}", TerminalControl.NominalSizeInPixels.Width, TerminalControl.NominalSizeInPixels.Height);
#endif

      ApplicationData.Current.LocalSettings.Values["WindowWidth"] = requestedWidth;
      ApplicationData.Current.LocalSettings.Values["WindowHeight"] = requestedHeight + mysteryOffset;

      args.Handled = true;
    }

    /// <summary>
    /// Invoked when the <see cref="visualBellTimer"/> goes off.
    /// </summary>
    /// <remarks>Hides the visual bell.</remarks>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void VisualBellTimer_Tick(DispatcherQueueTimer sender, object args) {
      VisualBellFontIcon.Visibility = Visibility.Collapsed;
      TerminalControl.VisualBell = false;
    }

    /// <summary>
    /// Invoked when the <see cref="TerminalControl"/> has been added to the
    /// XAML tree.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void TerminalControl_Loaded(object sender, RoutedEventArgs e) => LoadSettings(initialLoad: true, terminalIsInitialized: true);
  }
}
