using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Terminal.Helpers;
using Terminal.Settings;
using Windows.ApplicationModel.Resources;

namespace Terminal {
  public sealed partial class TerminalControl : UserControl {
    #region "Public Properties"

    /// <summary>
    /// The nominal size of the terminal, in pixels.
    /// </summary>
    /// <remarks>Clamps to the nearest integral number of rows and columns when
    /// setting.</remarks>
    public SizeF NominalSizeInPixels {
      get => terminalEngine.NominalSizeInPixels;

      set {
        if (terminalEngine.CellSize.Width == 0.0 || terminalEngine.CellSize.Height == 0.0) return;

        int newColumns = (int) (value.Width / terminalEngine.CellSize.Width);
        int newRows = (int) (value.Height / terminalEngine.CellSize.Height);

        if (newColumns > 0 && newRows > 0) {
          Columns = newColumns;
          Rows = newRows;

          double offsetX = (value.Width - terminalEngine.NominalSizeInPixels.Width) / 2;
          double offsetY = (value.Height - terminalEngine.NominalSizeInPixels.Height) / 2;

#if DEBUG
          logger.LogDebug("NominalSizeInPixels was set:");
          logger.LogDebug("  Requested(width, height): ({width:F}, {height:F})", value.Width, value.Height);
          logger.LogDebug("  Cell size(width, height): ({width:F}, {height:F})", terminalEngine.CellSize.Width, terminalEngine.CellSize.Height);
          logger.LogDebug("  New size(width, height): ({newWidth}, {newHeight})", NominalSizeInPixels.Width, NominalSizeInPixels.Height);
          logger.LogDebug("  New size(rows, columns): ({newRows}, {newColumns})", newRows, newColumns);
          logger.LogDebug("  Offset(x, y): ({offsetX:F}, {offsetY:F})", offsetX, offsetY);
#endif

          Canvas.Margin = new(offsetX, offsetY, offsetX, offsetY);
        }
      }
    }

    /// <summary>
    /// The terminal window title.
    /// </summary>
    public string? WindowTitle {
      get => _windowTitle;

      internal set {
        if (_windowTitle != value) {
          _windowTitle = value;

          WindowTitleChanged?.Invoke();
        }
      }
    }

    /// <summary>
    /// Whether the visual bell is ringing.
    /// </summary>
    public bool VisualBell {
      get => _visualBell;

      set {
        if (_visualBell != value) {
          _visualBell = value;

          if (_visualBell) {
            VisualBellRinging?.Invoke();
          }
        }
      }
    }

    /// <summary>
    /// The settings window.
    /// </summary>
    public SettingsWindow? SettingsWindow {
      get => _settingsWindow;
      set => _settingsWindow = value;
    }

    #endregion

    #region "Internal Properties"

    /// <summary>
    /// A <see cref="Windows.ApplicationModel.Resources.ResourceLoader"/> for
    /// i18n.
    /// </summary>
    internal ResourceLoader ResourceLoader => resourceLoader;

    /// <summary>
    /// The <see cref="Terminal.TerminalEngine"/>.
    /// </summary>
    internal TerminalEngine TerminalEngine => terminalEngine;

    /// <summary>
    /// The UI thread <see cref="Microsoft.UI.Dispatching.DispatcherQueue"/>.
    /// </summary>
    internal new DispatcherQueue DispatcherQueue => _dispatcherQueue;

    /// <summary>
    /// Our native HWND.
    /// </summary>
    internal nint HWnd => Microsoft.UI.Win32Interop.GetWindowFromWindowId(XamlRoot.ContentIsland.Environment.AppWindowId);

    /// <summary>
    /// Whether the terminal has focus.
    /// </summary>
    internal bool HasFocus {
      get => _hasFocus;

      set {
        if (_hasFocus != value) {
          _hasFocus = value;
          CursorHelper.SetUpCursorTimer(this);
          ReportFocus();
        }
      }
    }

    /// <summary>
    /// The cursor blink timer.
    /// </summary>
    internal DispatcherQueueTimer? CursorTimer {
      get => _cursorTimer;
      set => _cursorTimer = value;
    }

    /// <summary>
    /// Whether the Shift key is pressed.
    /// </summary>
    internal bool ShiftPressed { get; private set; }

    /// <summary>
    /// Whether the Control key is pressed.
    /// </summary>
    internal bool ControlPressed { get; private set; }

    /// <summary>
    /// Whether the Alt key is pressed.
    /// </summary>
    internal bool AltPressed { get; private set; }

    /// <summary>
    /// The last mouse button that was pressed.
    /// </summary>
    internal MouseButtons LastMouseButton {
      get => _lastMouseButton;
      private set => _lastMouseButton = value;
    }

    /// <summary>
    /// The context menu.
    /// </summary>
    internal MenuFlyout? ContextMenu {
      get => _contextMenu;
      set => _contextMenu = value;
    }

    /// <summary>
    /// The copy menu item.
    /// </summary>
    internal MenuFlyoutItem? CopyMenuItem {
      get => _copyMenuItem;
      set => _copyMenuItem = value;
    }

    /// <summary>
    /// The paste menu item.
    /// </summary>
    internal MenuFlyoutItem? PasteMenuItem {
      get => _pasteMenuItem;
      set => _pasteMenuItem = value;
    }

    /// <summary>
    /// The smaller text menu item.
    /// </summary>
    internal MenuFlyoutItem? SmallerTextMenuItem {
      get => _smallerTextMenuItem;
      set => _smallerTextMenuItem = value;
    }

    /// <summary>
    /// The larger text menu item.
    /// </summary>
    internal MenuFlyoutItem? LargerTextMenuItem {
      get => _largerTextMenuItem;
      set => _largerTextMenuItem = value;
    }

    /// <summary>
    /// The background is invisible menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? BackgroundIsInvisibleMenuItem {
      get => _backgroundIsInvisibleMenuItem;
      set => _backgroundIsInvisibleMenuItem = value;
    }

    /// <summary>
    /// The use visual bell menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? UseVisualBellMenuItem {
      get => _useVisualBellMenuItem;
      set => _useVisualBellMenuItem = value;
    }

    /// <summary>
    /// The copy on mouse up menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? CopyOnMouseUpMenuItem {
      get => _copyOnMouseUpMenuItem;
      set => _copyOnMouseUpMenuItem = value;
    }

    /// <summary>
    /// The paste on right click menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? PasteOnRightClickMenuItem {
      get => _pasteOnRightClickMenuItem;
      set => _pasteOnRightClickMenuItem = value;
    }

    /// <summary>
    /// The paste on middle click menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? PasteOnMiddleClickMenuItem {
      get => _pasteOnMiddleClickMenuItem;
      set => _pasteOnMiddleClickMenuItem = value;
    }

    /// <summary>
    /// The cursor menu item.
    /// </summary>
    internal MenuFlyoutSubItem? CursorMenuItem {
      get => _cursorMenuItem;
      set => _cursorMenuItem = value;
    }

    /// <summary>
    /// The block cursor menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? BlockCursorMenuItem {
      get => _blockCursorMenuItem;
      set => _blockCursorMenuItem = value;
    }

    /// <summary>
    /// The underline cursor menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? UnderlineCursorMenuItem {
      get => _underlineCursorMenuItem;
      set => _underlineCursorMenuItem = value;
    }

    /// <summary>
    /// The bar cursor menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? BarCursorMenuItem {
      get => _barCursorMenuItem;
      set => _barCursorMenuItem = value;
    }

    /// <summary>
    /// The cursor blink menu item.
    /// </summary>
    internal ToggleMenuFlyoutItem? CursorBlinkMenuItem {
      get => _cursorBlinkMenuItem;
      set => _cursorBlinkMenuItem = value;
    }

    /// <summary>
    /// The settings menu item.
    /// </summary>
    internal MenuFlyoutItem? SettingsMenuItem {
      get => _settingsMenuItem;
      set => _settingsMenuItem = value;
    }

    #endregion
  }
}
