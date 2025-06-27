#if DEBUG
using Microsoft.Extensions.Logging;
#endif
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Text;
using Terminal;
using w6t.Settings;
using w6t.Settings.Json;
using w6t.ViewModels;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using WinUIEx;

namespace w6t.Views {
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

    private readonly ResourceLoader resources;

    private readonly DependencyProperties dependencyProperties;

    private readonly DispatcherQueueTimer visualBellTimer;

    private TerminalViewModel? viewModel;

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
    public Terminal() {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(logLevel);
        }
      );

      logger = factory.CreateLogger<Terminal>();
#endif

      jsonSerializerOptions = new() {
        WriteIndented = true
      };
      jsonSerializerOptions.Converters.Add(new ColorJsonConverter());

      settingsJsonPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, settingsJsonFilename);

      settingsJsonWatcher = new() {
        Path = Windows.Storage.ApplicationData.Current.LocalFolder.Path,
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
      LoadSettings();

      visualBellTimer = DispatcherQueue.CreateTimer();
      UpdateVisualBellTimerInterval();
      visualBellTimer.Tick += VisualBellTimer_Tick;

      ViewModel = new(dependencyProperties.StartDirectory, dependencyProperties.Command);
      ViewModel.PseudoconsoleDied += ViewModel_PseudoconsoleDied;
      InitializeComponent();
      resources = ResourceLoader.GetForViewIndependentUse();

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
          (int) Math.Ceiling(TerminalControl.NominalSizeInPixels.Height)
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
      // is the actual height of the window chrome, which can (and does) differ
      // from the height set in XAML
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
    private void TerminalControl_Loaded(object sender, RoutedEventArgs e) => LoadSettings(terminalIsInitialized: true);
  }
}
