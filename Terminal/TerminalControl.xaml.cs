#if DEBUG
using Microsoft.Extensions.Logging;
#endif
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Terminal.Helpers;
using Terminal.Settings;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;

namespace Terminal {
  /// <summary>
  /// A Win2D terminal control.
  /// </summary>
  /// <remarks>
  /// <para><c>TerminalControl.Behaviors.cs</c> contains methods to support
  /// terminal behaviors.</para>
  /// <para><c>TerminalControl.DependencyProperties.cs</c> contains <see
  /// cref="DependencyProperty"/>s that can be interacted with by the <see
  /// cref="TerminalControl"/> consumer.</para>
  /// <para><c>TerminalControl.Events.cs</c> contains WinUI 3 event-handler
  /// methods.</para>
  /// <para><c>TerminalControl.Properties.cs</c> contains both properties that
  /// can be interacted with by the <see cref="TerminalControl"/> consumer and
  /// properties that are used by <see cref="TerminalControl"/>.</para>
  /// </remarks>
  public sealed partial class TerminalControl : UserControl {
#if DEBUG
    internal readonly ILogger logger;
    internal static readonly LogLevel logLevel = LogLevel.None;
#endif

    private readonly ResourceLoader resourceLoader;

    private readonly DispatcherQueue _dispatcherQueue;

    private string? _windowTitle;
    private bool _visualBell;

    private bool _hasFocus;
    private DispatcherQueueTimer? _cursorTimer;

    private InputKeyboardSource? inputKeyboardSource;
    private MouseButtons _lastMouseButton;

    private MenuFlyout? _contextMenu;

    private MenuFlyoutItem? _copyMenuItem;
    private MenuFlyoutItem? _pasteMenuItem;

    private MenuFlyoutItem? _smallerTextMenuItem;
    private MenuFlyoutItem? _largerTextMenuItem;

    private ToggleMenuFlyoutItem? _backgroundIsInvisibleMenuItem;
    private ToggleMenuFlyoutItem? _useVisualBellMenuItem;

    private ToggleMenuFlyoutItem? _copyOnMouseUpMenuItem;
    private ToggleMenuFlyoutItem? _pasteOnRightClickMenuItem;
    private ToggleMenuFlyoutItem? _pasteOnMiddleClickMenuItem;

    private MenuFlyoutSubItem? _cursorMenuItem;
    private ToggleMenuFlyoutItem? _blockCursorMenuItem;
    private ToggleMenuFlyoutItem? _underlineCursorMenuItem;
    private ToggleMenuFlyoutItem? _barCursorMenuItem;
    private ToggleMenuFlyoutItem? _cursorBlinkMenuItem;

    private MenuFlyoutItem? _settingsMenuItem;

    private SettingsWindow? _settingsWindow;

    private readonly TerminalEngine terminalEngine;

    /// <summary>
    /// Initializes a <see cref="TerminalControl"/>.
    /// </summary>
    public TerminalControl() {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(logLevel);
        }
      );

      logger = factory.CreateLogger<TerminalControl>();
#endif

      _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

      InitializeComponent();
      resourceLoader = ResourceLoader.GetForViewIndependentUse();
      MenuHelper.InitializeContextMenu(this);

      terminalEngine = new(this, Canvas);
    }

    /// <summary>
    /// Writes a message to the terminal.
    /// </summary>
    /// <remarks>Keep in mind that the source of the message may be confusing
    /// to the user.</remarks>
    /// <param name="message"></param>
    public void Write(string message) => terminalEngine.VideoTerminal.Write(message);

    /// <summary>
    /// Writes an error to the terminal.
    /// </summary>
    /// <remarks>This is meant to be used in exceptional cases that prevent
    /// the terminal from working at all.</remarks>
    /// <param name="message"></param>
    public void WriteError(string message) => terminalEngine.VideoTerminal.WriteError(message);

    /// <summary>
    /// Tells XAML how large the <see cref="TerminalControl"/> should be.
    /// </summary>
    /// <remarks>This takes potentially a few iterations to sort itself
    /// out.</remarks>
    /// <param name="availableSize"><inheritdoc
    /// cref="FrameworkElement.MeasureOverride"
    /// path="/param[@name='availableSize']"/></param>
    /// <returns>The size required to display the terminal.</returns>
    protected override Size MeasureOverride(Size availableSize) {
      // We need *something* so Draw will be invoked (and the cell size will
      // be calculated)
      return new(
        Math.Max(NominalSizeInPixels.Width, 1.0),
        Math.Max(NominalSizeInPixels.Height, 1.0)
      );
    }
  }
}
