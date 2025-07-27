using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Spakov.Terminal;
using Spakov.Furminal.Settings;
using Spakov.Furminal.ViewModels;
using System;
using System.IO;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using WinUIEx;

namespace Spakov.Furminal.Views
{
    /// <summary>
    /// The Terminal view.
    /// </summary>
    /// <remarks>
    /// <para><c>Terminal.Settings.xaml.cs</c> contains code related to
    /// settings management.</para>
    /// </remarks>
    public sealed partial class Terminal : Window
    {
        private readonly ILogger? _logger;

        /// <summary>
        /// A mystery offset of three pixels.
        /// </summary>
        /// <remarks>I have no idea why we're off by 3 pixels, but it seems to
        /// be consistent, at least.</remarks>
        private const int MysteryOffset = 3;

        private readonly ResourceLoader _resources;

        private readonly DependencyProperties _dependencyProperties;
        private readonly DispatcherQueueTimer _visualBellTimer;
        private TerminalViewModel? _viewModel;

        private readonly string? _startCommand;
        private readonly int? _startRows;
        private readonly int? _startColumns;

        private Dpi _dpi;
        private int? _clientAreaOffset;
        private bool _resizeLock;

        /// <summary>
        /// The <see cref="TerminalViewModel"/>.
        /// </summary>
        private TerminalViewModel? ViewModel
        {
            get => _viewModel;
            set => _viewModel = value;
        }

        /// <summary>
        /// A "lock", to prevent resizing from cascading out of control.
        /// </summary>
        internal bool ResizeLock
        {
            get => _resizeLock;

            set
            {
                _resizeLock = value;
                _logger?.LogDebug("{resizeLock} ResizeLock", _resizeLock ? "Set" : "Cleared");
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
        /// <param name="startCommand">The command to run in the terminal.</param>
        /// <param name="startRows">The number of terminal rows.</param>
        /// <param name="startColumns">The number of terminal columns.</param>
        public Terminal(string? startCommand, int? startRows, int? startColumns)
        {
            _logger = LoggerHelper.CreateLogger<Terminal>();

            _startCommand = startCommand;
            _startRows = startRows;
            _startColumns = startColumns;

            _logger?.LogInformation("<command>: {startCommand}", startCommand);
            _logger?.LogInformation("--rows: {startRows}", startRows);
            _logger?.LogInformation("--columns: {startColumns}", startColumns);

            _settingsJsonWatcher = new()
            {
                Path = ApplicationData.Current.LocalFolder.Path,
                Filter = SettingsHelper.SettingsPath,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            _settingsJsonWatcher.Changed += SettingsJsonWatcher_Changed;

            _settingsSaveTimer = new()
            {
                Interval = SettleDelay,
                AutoReset = false
            };

            _dependencyProperties = new(this);
            LoadSettings(initialLoad: true);

            _visualBellTimer = DispatcherQueue.CreateTimer();
            UpdateVisualBellTimerInterval();
            _visualBellTimer.Tick += VisualBellTimer_Tick;

            ViewModel = new(this, _dependencyProperties.StartDirectory, _dependencyProperties.Command);
            ViewModel.PseudoconsoleDied += ViewModel_PseudoconsoleDied;
            InitializeComponent();
            _resources = ResourceLoader.GetForViewIndependentUse();

            AppWindow.Changed += AppWindow_Changed;

            // Our Win32 window's cursor, at this point, is (probably)
            // IDC_WAIT. It "bleeds through" when we do things like display
            // context menus. Explanation from Raymond Chen:
            // https://devblogs.microsoft.com/oldnewthing/20250424-00/?p=111114
            PInvoke.SetCursor(PInvoke.LoadCursor((HMODULE)(nint)0, PInvoke.IDC_ARROW));

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
        private void ViewModel_PseudoconsoleDied(Exception e)
        {
            StringBuilder message = new();

            message.Append(e.Message);

            if (e.InnerException is not null)
            {
                message.Append("\r\n");
                message.Append(e.InnerException.Message);
            }

            TerminalControl.WriteError(message.ToString());
        }

        /// <summary>
        /// Updates the visual bell timer interval from <see
        /// cref="DependencyProperties.VisualBellDisplayTime"/>.
        /// </summary>
        internal void UpdateVisualBellTimerInterval() => _visualBellTimer.Interval = TimeSpan.FromSeconds(_dependencyProperties.VisualBellDisplayTime);

        /// <summary>
        /// Invoked when the terminal's window title changes.
        /// </summary>
        private void TerminalControl_WindowTitleChanged()
        {
            TitleTextBlock.Text = TerminalControl.WindowTitle;
            Title = TerminalControl.WindowTitle;
        }

        /// <summary>
        /// Invoked when the terminal's visual bell is ringing.
        /// </summary>
        private void TerminalControl_VisualBellRinging()
        {
            VisualBellFontIcon.Visibility = Visibility.Visible;
            _visualBellTimer.Start();
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
        private void SetFontFamily(DependencyObject sender, DependencyProperty dp)
        {
            if (TerminalControl.FontFamily is not null)
            {
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
        private void TerminalControl_TerminalResize()
        {
            // Ignore resizes caused by Window_SizeChanged()
            if (ResizeLock)
            {
                _logger?.LogDebug("TerminalControl_TerminalResize(): no-op due to ResizeLock");

                return;
            }

            _logger?.LogDebug("TerminalControl_TerminalResize():");
            _logger?.LogDebug("  NominalSizeInPixels: {width} x {height}", TerminalControl.NominalSizeInPixels.Width, TerminalControl.NominalSizeInPixels.Height);

            AppWindow.ResizeClient(
                new(
                    (int)Math.Ceiling(TerminalControl.NominalSizeInPixels.Width * (_dpi.X / (float)PInvoke.USER_DEFAULT_SCREEN_DPI)),
                    (int)Math.Ceiling(TerminalControl.NominalSizeInPixels.Height * (_dpi.Y / (float)PInvoke.USER_DEFAULT_SCREEN_DPI)) + MysteryOffset
                )
            );
        }

        /// <summary>
        /// Invoked when the window changes, which includes scenarios during
        /// which we'd want to obtain new DPI values.
        /// </summary>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void AppWindow_Changed(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowChangedEventArgs args)
        {
            uint dpi = PInvoke.GetDpiForWindow(new(WinRT.Interop.WindowNative.GetWindowHandle(this)));

            _dpi.X = dpi;
            _dpi.Y = dpi;
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
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            TitleTextBlock.Foreground = args.WindowActivationState == WindowActivationState.Deactivated
                ? (SolidColorBrush)App.Current.Resources["WindowCaptionForegroundDisabled"]
                : (SolidColorBrush)App.Current.Resources["WindowCaptionForeground"];
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
        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            _logger?.LogDebug("Window_SizeChanged(): invoked with ({width}, {height})", args.Size.Width, args.Size.Height);

            // Ignore resizes caused by App.OnLaunched()
            if (ResizeLock)
            {
                _logger?.LogDebug("Window_SizeChanged(): no-op due to ResizeLock");

                return;
            }

            // Make sure the terminal is ready
            Size terminalSize = TerminalControl.NominalSizeInPixels.ToSize();
            if (terminalSize.Width == 0.0 && terminalSize.Height == 0.0)
            {
                _logger?.LogDebug("Window_SizeChanged(): no-op due to terminal not ready");

                return;
            }

            // Determine the client area offset, which should remain constant.
            // This is the actual height of the window chrome, which can (and
            // evidently does) differ from the height set in XAML.
            _clientAreaOffset ??= (int)(AppWindow.ClientSize.Height - TerminalControl.NominalSizeInPixels.Height);

            // Account for the title bar, which is part of the client area
            double requestedWidth = AppWindow.ClientSize.Width;
            double requestedHeight = AppWindow.ClientSize.Height - (int)_clientAreaOffset!;

            ResizeLock = true;
            TerminalControl.NominalSizeInPixels = new(requestedWidth, requestedHeight);
            ResizeLock = false;

            _logger?.LogDebug("Window_SizeChanged():");
            _logger?.LogDebug("  Requested: {width} x {height}", requestedWidth, requestedHeight);
            _logger?.LogDebug("  NominalSizeInPixels: {width}, {height}", TerminalControl.NominalSizeInPixels.Width, TerminalControl.NominalSizeInPixels.Height);

            ApplicationData.Current.LocalSettings.Values["WindowWidth"] = requestedWidth;
            ApplicationData.Current.LocalSettings.Values["WindowHeight"] = requestedHeight + MysteryOffset;

            args.Handled = true;
        }

        /// <summary>
        /// Invoked when the <see cref="_visualBellTimer"/> goes off.
        /// </summary>
        /// <remarks>Hides the visual bell.</remarks>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void VisualBellTimer_Tick(DispatcherQueueTimer sender, object args)
        {
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