using AnsiProcessor.Input;
using AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using Terminal.Helpers;
using Terminal.Settings;

namespace Terminal {
  public sealed partial class TerminalControl : UserControl {
    /// <summary>
    /// Callback for receiving notification that the terminal dimensions
    /// changed.
    /// </summary>
    public delegate void OnTerminalResize();

    /// <summary>
    /// Invoked when the terminal dimensions change.
    /// </summary>
    /// <remarks>The terminal dimensions (in pixels) are accessible via <see
    /// cref="NominalSizeInPixels"/>.</remarks>
    public event OnTerminalResize? TerminalResize;

    /// <summary>
    /// Callback for receiving notification that the window title changed.
    /// </summary>
    public delegate void OnWindowTitleChanged();

    /// <summary>
    /// Invoked when the terminal window title changes.
    /// </summary>
    /// <remarks>The terminal window title is accessible via <see
    /// cref="WindowTitle"/>.</remarks>
    public event OnWindowTitleChanged? WindowTitleChanged;

    /// <summary>
    /// Callback for receiving notification that the visual bell is ringing.
    /// </summary>
    public delegate void OnVisualBellRinging();

    /// <summary>
    /// Invoked when the visual bell is ringing.
    /// </summary>
    /// <remarks>The visual bell state should be reset by setting <see
    /// cref="VisualBell"/> to <see langword="false"/>.</remarks>
    public event OnVisualBellRinging? VisualBellRinging;

    /// <summary>
    /// Callback for customizing the <see cref="SettingsViewModel"/> before the
    /// window is displayed.
    /// </summary>
    /// <param name="settingsViewModel">The <see cref="Settings.SettingsWindow"/>'s
    /// <see cref="SettingsViewModel"/>.</param>
    public delegate void OnCustomizeSettingsWindowSettings(SettingsViewModel settingsViewModel);

    /// <summary>
    /// Invoked before the settings window is displayed so its <see
    /// cref="SettingsViewModel"/> can be customized.
    /// </summary>
    /// <remarks>See <see cref="SettingsViewModel.SettingsViewModel"/> for
    /// details.</remarks>
    public event OnCustomizeSettingsWindowSettings? CustomizeSettingsWindowSettings;

    /// <summary>
    /// Callback for receiving notification that the settings window's save as
    /// defaults button was clicked.
    /// </summary>
    public delegate void OnSaveSettingsAsDefaults();

    /// <summary>
    /// Invoked when the user clicks the settings window's save as defaults
    /// button.
    /// </summary>
    /// <remarks>The event handler (i.e., an assembly that's hosting the <see
    /// cref="TerminalControl"/>) should use <see cref="DependencyProperty"/>s
    /// to save the current settings and apply them at startup.</remarks>
    public event OnSaveSettingsAsDefaults? SaveSettingsAsDefaults;

    internal void InvokeSaveSettingsAsDefault() => SaveSettingsAsDefaults?.Invoke();

    /// <summary>
    /// Callback for reading text.
    /// </summary>
    /// <param name="text">The text to read.</param>
    private delegate void TextCallback(string text);

    /// <summary>
    /// Callback for reading escape sequences.
    /// </summary>
    /// <param name="escapeSequence">The escape sequence, not including the
    /// leading <see cref="C0.ESC"/>.</param>
    /// <param name="sgrEscapeSequence">An <see cref="SGREscapeSequence"/>, if
    /// the escape sequence is an <see
    /// cref="Ansi.EscapeSequences.SGR"/> escape sequence.</param>
    private delegate void EscapeSequenceCallback(string escapeSequence, SGREscapeSequence? sgrEscapeSequence = null);

    /// <summary>
    /// Callback for reading control characters.
    /// </summary>
    /// <param name="controlCharacter">The control character.</param>
    private delegate void ControlCharacterCallback(char controlCharacter);

    /// <summary>
    /// Invoked when the <see cref="TerminalControl"/> has been added to a XAML
    /// tree.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void UserControl_Loaded(object sender, RoutedEventArgs e) {
      terminalEngine.UpdateRefreshRate();

      AppWindow appWindow = AppWindow.GetFromWindowId(XamlRoot.ContentIsland.Environment.AppWindowId);

      appWindow.Changed += (_, _) => {
        if (XamlRoot is not null && XamlRoot.ContentIsland is not null) {
          terminalEngine.UpdateRefreshRate();
        }
      };
    }

    #region "Canvas Event Handlers"

    /// <summary>
    /// Invoked when the terminal is loaded into the XAML content tree.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_Loaded(object sender, RoutedEventArgs e) {
      inputKeyboardSource = InputKeyboardSource.GetForIsland(XamlRoot.ContentIsland);
      inputKeyboardSource.KeyDown += InputKeyboardSource_KeyDown;

      // Required to handle Alt (and maybe F10?)
      inputKeyboardSource.SystemKeyDown += InputKeyboardSource_KeyDown;
      inputKeyboardSource.SystemKeyUp += InputKeyboardSource_SystemKeyUp;
    }

    /// <summary>
    /// Invoked when the terminal is unloaded from the XAML content tree.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_Unloaded(object sender, RoutedEventArgs e) {
      if (inputKeyboardSource is not null) {
        inputKeyboardSource.KeyDown -= InputKeyboardSource_KeyDown;
        inputKeyboardSource.SystemKeyDown -= InputKeyboardSource_KeyDown;
        inputKeyboardSource.SystemKeyUp -= InputKeyboardSource_SystemKeyUp;
      }
    }

    /// <summary>
    /// Invoked when the terminal canvas is creating resources.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args) => terminalEngine.InstantiateOffscreenBuffer();

    /// <summary>
    /// Invoked when the terminal should be drawn.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args) {
      if (terminalEngine.CellSizeDirty) {
        terminalEngine.CleanCellSize();
        TerminalResize?.Invoke();
      }

      CanvasDrawingSession drawingSession = args.DrawingSession;

      drawingSession.DrawImage(terminalEngine.OffscreenBuffer);
      CursorHelper.DrawCursor(this, drawingSession);
    }

    /// <summary>
    /// Invoked when a context menu is about to be displayed.
    /// </summary>
    /// <remarks>Required to override any ascendants' context menus.</remarks>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Mark members as static
    private void Canvas_ContextRequested(UIElement sender, ContextRequestedEventArgs args) => args.Handled = true;
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression

    /// <summary>
    /// Invoked when the user right-clicks the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_RightTapped(object sender, RightTappedRoutedEventArgs e) {
      // Do not display menu or paste when mouse tracking mode is enabled
      if (
        terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
      ) {
        e.Handled = true;

        return;
      }

      if (CopyMenuItem is not null) {
        CopyMenuItem.IsEnabled = terminalEngine.TextIsSelected;
      }

      if (PasteOnRightClick) {
        if (ControlPressed) {
          ContextMenu?.ShowAt(Canvas, e.GetPosition(Canvas));
        } else {
          PasteFromClipboard();
        }
      } else {
        ContextMenu?.ShowAt(Canvas, e.GetPosition(Canvas));
      }

      e.Handled = true;
    }

    /// <summary>
    /// Invoked when the user presses a key on the terminal.
    /// </summary>
    /// <remarks>Required to intercept the Tab key.</remarks>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_PreviewKeyDown(object sender, KeyRoutedEventArgs e) {
      if (e.OriginalKey == Windows.System.VirtualKey.Tab) {
        if (inputKeyboardSource is not null && HasFocus && terminalEngine.AnsiWriter is not null) {
          Keystroke keystroke = new() {
            Key = (Keys) Windows.System.VirtualKey.Tab,
            IsRepeat = e.KeyStatus.WasKeyDown,
            AutoRepeatKeys = terminalEngine.AutoRepeatKeys
          };

          // If this is a Ctrl-Tab, convert it to a "regular" Tab
          if (
            inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
            || inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
          ) {
            bool forward = true;

            if (
              inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
              || inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
            ) {
              forward = false;
            }

            FocusManager.TryMoveFocus(
              forward ? FocusNavigationDirection.Next : FocusNavigationDirection.Previous,
              new() { SearchRoot = XamlRoot.Content }
            );

            // This is a Tab, so intercept it
          } else {
            if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)) {
              keystroke.ModifierKeys |= ModifierKeys.LeftShift;
            }

            if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)) {
              keystroke.ModifierKeys |= ModifierKeys.RightShift;
            }

            terminalEngine.SendKeystroke(keystroke);
          }

          e.Handled = true;
        }
      }
    }

    /// <summary>
    /// Invoked when the user presses the mouse button on the terminal.
    /// </summary>
    /// <remarks>See <see
    /// href="https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.input.pointerroutedeventargs?view=winrt-26100#:~:text=only%20when%20that%20same%20mouse%20button%20is%20released"
    /// /> for insight into the mouse button tracking approach.</remarks>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e) {
      PointerPoint pointerPoint = e.GetCurrentPoint(Canvas);

      Canvas.Focus(FocusState.Pointer);
      terminalEngine.PointerPressed(pointerPoint);

      if (pointerPoint.Properties.IsLeftButtonPressed) {
        LastMouseButton = MouseButtons.Left;
      } else if (pointerPoint.Properties.IsMiddleButtonPressed) {
        LastMouseButton = MouseButtons.Middle;
      } else if (pointerPoint.Properties.IsRightButtonPressed) {
        LastMouseButton = MouseButtons.Right;
      }
    }

    /// <summary>
    /// Invoked when the user scrolls the mouse wheel in the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e) => MouseWheelHelper.HandleMouseWheel(this, e.GetCurrentPoint(Canvas));

    /// <summary>
    /// Invoked when the user moves the mouse over the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e) => terminalEngine.PointerMoved(e.GetCurrentPoint(Canvas));

    /// <summary>
    /// Invoked when the user releases the mouse button on the terminal.
    /// </summary>
    /// <remarks><inheritdoc cref="Canvas_PointerPressed"
    /// path="/remarks"/></remarks>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e) {
      PointerPoint pointerPoint = e.GetCurrentPoint(Canvas);

      if (LastMouseButton == MouseButtons.Left) {
        terminalEngine.SelectionMode = false;
      } else if (LastMouseButton == MouseButtons.Middle) {
        if (PasteOnMiddleClick) {
          // Do not paste when mouse tracking mode is enabled
          if (
            !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
            && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
            && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
            && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
          ) {
            PasteFromClipboard();
          }
        }
      }

      terminalEngine.PointerReleased(pointerPoint);

      e.Handled = true;
    }

    /// <summary>
    /// Invoked when the terminal got focus.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_GotFocus(object sender, RoutedEventArgs e) => HasFocus = true;

    /// <summary>
    /// Invoked when the terminal is losing focus.
    /// </summary>
    /// <remarks>This is required to work around a nuisance described in <see
    /// href="https://github.com/microsoft/microsoft-ui-xaml/issues/6330"/>.
    /// The RootScrollViewer aggressively tries to steal focus from the
    /// terminal, so we must cancel the event.</remarks>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CA1822 // Mark members as static
    private void Canvas_LosingFocus(UIElement sender, LosingFocusEventArgs args) {
      if (args.NewFocusedElement is null) return;

      if (VisualTreeHelper.GetParent(args.NewFocusedElement) is null) {
        args.Cancel = true;
      }
    }
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression

    /// <summary>
    /// Invoked when the terminal lost focus.
    /// </summary>
    /// <remarks>Pretends we maintain focus when the settings window is open
    /// so the user can live-preview their changes</remarks>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_LostFocus(object sender, RoutedEventArgs e) {
      if (SettingsWindow is null) {
        HasFocus = false;
      }
    }

    /// <summary>
    /// Invoked when the terminal changes size.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => ReportWindowSize();

    #endregion

    #region "Other Event Handlers"

    /// <summary>
    /// Invoked when a key is pressed in the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void InputKeyboardSource_KeyDown(InputKeyboardSource sender, KeyEventArgs args) {
      ShiftPressed = inputKeyboardSource is not null
        && (
          inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
          || inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
        );

      ControlPressed = inputKeyboardSource is not null
        && (
          inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
          || inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
        );

      AltPressed = inputKeyboardSource is not null
        && (
          inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down)
          || inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down)
        );

      if (inputKeyboardSource is not null && HasFocus && terminalEngine.AnsiWriter is not null) {
        int virtualKey = (int) args.VirtualKey;

        if (Enum.IsDefined(typeof(Keys), virtualKey)) {
          Keystroke keystroke = new() {
            Key = (Keys) virtualKey,
            IsRepeat = args.KeyStatus.WasKeyDown,
            AutoRepeatKeys = terminalEngine.AutoRepeatKeys,
            ApplicationCursorKeys = terminalEngine.ApplicationCursorKeys
          };

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.LeftShift;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.RightShift;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.LeftAlt;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.RightAlt;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.LeftControl;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.RightControl;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftWindows).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.LeftMeta;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightWindows).HasFlag(VirtualKeyStates.Down)) {
            keystroke.ModifierKeys |= ModifierKeys.RightMeta;
          }

          if (inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.CapitalLock).HasFlag(VirtualKeyStates.Locked)) {
            keystroke.CapsLock = true;
          }

          CursorHelper.ShowCursorImmediately(this);

          if (!KeystrokeHelper.HandleKeystroke(this, keystroke)) {
            terminalEngine.SendKeystroke(keystroke);
          }

          args.Handled = true;
        }
      }
    }

    /// <summary>
    /// Invoked when a system key (i.e., Alt [or maybe F10?]) is released in
    /// the terminal.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void InputKeyboardSource_SystemKeyUp(InputKeyboardSource sender, KeyEventArgs args) {
      if (
        inputKeyboardSource is not null
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
      ) {
        ShiftPressed = false;
      }

      if (
        inputKeyboardSource is not null
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
      ) {
        ControlPressed = false;
      }

      if (
        inputKeyboardSource is not null
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down)
        && !inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down)
      ) {
        AltPressed = false;
      }
    }

    /// <summary>
    /// Invoked when the cursor should change its blink state.
    /// </summary>
    /// <param name="sender"><see cref="cursorTimer"/></param>
    /// <param name="args">Unused.</param>
    internal void CursorTimer_Tick(DispatcherQueueTimer sender, object args) => terminalEngine.CursorDisplayed = !terminalEngine.CursorDisplayed;

    #endregion

    #region "Menu Item Event Handlers"

    /// <summary>
    /// Invoked when the user clicks the copy menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void CopyMenuItem_Click(object sender, RoutedEventArgs e) => terminalEngine.CopySelection();

    /// <summary>
    /// Invoked when the user clicks the paste menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void PasteMenuItem_Click(object sender, RoutedEventArgs e) => PasteFromClipboard();

    /// <summary>
    /// Invoked when the user clicks the smaller text menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void SmallerTextMenuItem_Click(object sender, RoutedEventArgs e) {
      if (FontSize >= 2.0f) {
        FontSize -= 2.0f;
      }
    }

    /// <summary>
    /// Invoked when the user clicks the larger text menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void LargerTextMenuItem_Click(object sender, RoutedEventArgs e) => FontSize += 2.0f;

    /// <summary>
    /// Invoked when the user clicks the background is invisible menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void BackgroundIsInvisibleMenuItem_Click(object sender, RoutedEventArgs e) => BackgroundIsInvisible = !BackgroundIsInvisible;

    /// <summary>
    /// Invoked when the user clicks the use visual bell menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void UseVisualBellMenuItem_Click(object sender, RoutedEventArgs e) => UseVisualBell = !UseVisualBell;

    /// <summary>
    /// Invoked when the user clicks the block cursor menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void BlockCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyles.Block;

    /// <summary>
    /// Invoked when the user clicks the underline cursor menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void UnderlineCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyles.Underline;

    /// <summary>
    /// Invoked when the user clicks the bar cursor menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void BarCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyles.Bar;

    /// <summary>
    /// Invoked when the user clicks the cursor blink menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void CursorBlinkMenuItem_Click(object sender, RoutedEventArgs e) => CursorBlink = !CursorBlink;

    /// <summary>
    /// Invoked when the user clicks the copy on mouse up menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void CopyOnMouseUpMenuItem_Click(object sender, RoutedEventArgs e) => CopyOnMouseUp = !CopyOnMouseUp;

    /// <summary>
    /// Invoked when the user clicks the paste on right click menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void PasteOnRightClickMenuItem_Click(object sender, RoutedEventArgs e) => PasteOnRightClick = !PasteOnRightClick;

    /// <summary>
    /// Invoked when the user clicks the paste on middle click menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void PasteOnMiddleClickMenuItem_Click(object sender, RoutedEventArgs e) => PasteOnMiddleClick = !PasteOnMiddleClick;

    /// <summary>
    /// Invoked when the user clicks the settings menu item.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    internal void SettingsMenuItem_Click(object sender, RoutedEventArgs e) {
      if (SettingsWindow is null) {
        SettingsWindow ??= new(this);
        CustomizeSettingsWindowSettings?.Invoke(SettingsWindow.ViewModel);
      }

      SettingsWindow.Display(this);
    }

    #endregion

  }
}
