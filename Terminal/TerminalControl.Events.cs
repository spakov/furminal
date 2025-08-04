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
using Spakov.AnsiProcessor.Input;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR;
using Spakov.Terminal.Helpers;
using Spakov.Terminal.Settings;
using System;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Spakov.Terminal
{
    public sealed partial class TerminalControl : UserControl
    {
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
        /// Callback for customizing the <see cref="SettingsViewModel"/> before
        /// the window is displayed.
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
        /// Callback for receiving notification that the settings window's save
        /// as defaults button was clicked.
        /// </summary>
        public delegate void OnSaveSettingsAsDefaults();

        /// <summary>
        /// Invoked when the user clicks the settings window's save as defaults
        /// button.
        /// </summary>
        /// <remarks>The event handler (i.e., an assembly that's hosting the
        /// <see cref="TerminalControl"/>) should use <see
        /// cref="DependencyProperty"/>s to save the current settings and apply
        /// them at startup.</remarks>
        public event OnSaveSettingsAsDefaults? SaveSettingsAsDefaults;

        /// <summary>
        /// Invokes <see cref="SaveSettingsAsDefaults"/>.
        /// </summary>
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
        /// leading <see cref="AnsiProcessor.Ansi.C0.ESC"/>.</param>
        /// <param name="sgrEscapeSequence">An <see cref="SGREscapeSequence"/>,
        /// if the escape sequence is an <see
        /// cref="AnsiProcessor.Ansi.EscapeSequences.SGR"/> escape
        /// sequence.</param>
        private delegate void EscapeSequenceCallback(string escapeSequence, SGREscapeSequence? sgrEscapeSequence = null);

        /// <summary>
        /// Callback for reading control characters.
        /// </summary>
        /// <param name="controlCharacter">The control character.</param>
        private delegate void ControlCharacterCallback(char controlCharacter);

        /// <summary>
        /// Registers the terminal refresh rate updater.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            _terminalEngine.UpdateRefreshRate();

            AppWindow appWindow = AppWindow.GetFromWindowId(XamlRoot.ContentIsland.Environment.AppWindowId);

            appWindow.Changed += (_, _) =>
            {
                if (XamlRoot is not null && XamlRoot.ContentIsland is not null)
                {
                    _terminalEngine.UpdateRefreshRate();
                }
            };
        }

        #region "Canvas Event Handlers"

        /// <summary>
        /// Registers key event handlers.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_Loaded(object sender, RoutedEventArgs e)
        {
            _inputKeyboardSource = InputKeyboardSource.GetForIsland(XamlRoot.ContentIsland);
            _inputKeyboardSource.KeyDown += InputKeyboardSource_KeyDown;

            // Required to handle Alt (and maybe F10?)
            _inputKeyboardSource.SystemKeyDown += InputKeyboardSource_KeyDown;
            _inputKeyboardSource.SystemKeyUp += InputKeyboardSource_SystemKeyUp;
        }

        /// <summary>
        /// Unregisters key event handlers.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_inputKeyboardSource is not null)
            {
                _inputKeyboardSource.KeyDown -= InputKeyboardSource_KeyDown;
                _inputKeyboardSource.SystemKeyDown -= InputKeyboardSource_KeyDown;
                _inputKeyboardSource.SystemKeyUp -= InputKeyboardSource_SystemKeyUp;
            }
        }

        /// <summary>
        /// Instantiates the offscreen buffer.
        /// </summary>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args) => _terminalEngine.InstantiateOffscreenBuffer();

        /// <summary>
        /// Draws from the offscreen buffer to the canvas and draws the cursor.
        /// </summary>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void Canvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (_terminalEngine.CellSizeDirty)
            {
                _terminalEngine.CleanCellSize();
                TerminalResize?.Invoke();
            }

            CanvasDrawingSession drawingSession = args.DrawingSession;

            drawingSession.DrawImage(_terminalEngine.OffscreenBuffer);
            CursorHelper.DrawCursor(this, drawingSession);
        }

        /// <summary>
        /// Prevents ascendants' context menus from overriding ours.
        /// </summary>
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
        /// Handles right clicks.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            // Do not display menu or paste when mouse tracking mode is enabled
            if (
                _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
            )
            {
                e.Handled = true;

                return;
            }

            if (CopyMenuItem is not null)
            {
                CopyMenuItem.IsEnabled = _terminalEngine.TextIsSelected;
            }

            if (PasteOnRightClick)
            {
                if (ControlPressed)
                {
                    ContextMenu?.ShowAt(Canvas, e.GetPosition(Canvas));
                }
                else
                {
                    PasteFromClipboard();
                }
            }
            else
            {
                ContextMenu?.ShowAt(Canvas, e.GetPosition(Canvas));
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles key presses.
        /// </summary>
        /// <remarks>Required to intercept the Tab key.</remarks>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.OriginalKey == Windows.System.VirtualKey.Tab)
            {
                if (_inputKeyboardSource is not null && HasFocus && _terminalEngine.AnsiWriter is not null)
                {
                    Keystroke keystroke = new()
                    {
                        Key = (Key)Windows.System.VirtualKey.Tab,
                        IsRepeat = e.KeyStatus.WasKeyDown,
                        AutoRepeatKeys = _terminalEngine.AutoRepeatKeys
                    };

                    // If this is a Ctrl-Tab, convert it to a "regular" Tab
                    if (
                        _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
                        || _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
                    )
                    {
                        bool forward = true;

                        if (
                            _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
                            || _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
                        )
                        {
                            forward = false;
                        }

                        FocusManager.TryMoveFocus(
                            forward ? FocusNavigationDirection.Next : FocusNavigationDirection.Previous,
                            new() { SearchRoot = XamlRoot.Content }
                        );
                    } // This is a Tab, so intercept it
                    else
                    {
                        if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down))
                        {
                            keystroke.ModifierKeys |= ModifierKeys.LeftShift;
                        }

                        if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down))
                        {
                            keystroke.ModifierKeys |= ModifierKeys.RightShift;
                        }

                        _terminalEngine.SendKeystroke(keystroke);
                    }

                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles mouse button presses.
        /// </summary>
        /// <remarks>See <see
        /// href="https://learn.microsoft.com/en-us/uwp/api/windows.ui.xaml.input.pointerroutedeventargs?view=winrt-26100#:~:text=only%20when%20that%20same%20mouse%20button%20is%20released"
        /// /> for insight into the mouse button tracking approach.</remarks>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pointerPoint = e.GetCurrentPoint(Canvas);

            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                DateTime now = DateTime.Now;

                if (
                    (now - _lastLeftClickTime).TotalMilliseconds <= PInvoke.GetDoubleClickTime()
                    && LastMouseButton == MouseButton.Left
                )
                {
                    _leftClickCount++;
                }
                else
                {
                    _leftClickCount = 1;
                }

                Point thisTapPoint = pointerPoint.Position;

                if (Math.Abs(thisTapPoint.X - _lastLeftClickPosition.X) > PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CXDOUBLECLK))
                {
                    _leftClickCount = 1;
                }

                if (Math.Abs(thisTapPoint.Y - _lastLeftClickPosition.Y) > PInvoke.GetSystemMetrics(SYSTEM_METRICS_INDEX.SM_CYDOUBLECLK))
                {
                    _leftClickCount = 1;
                }

                _lastLeftClickPosition = thisTapPoint;
                _lastLeftClickTime = now;
            }
            else
            {
                _leftClickCount = 0;
            }

            Canvas.Focus(FocusState.Pointer);
            _terminalEngine.PointerPressed(pointerPoint, _leftClickCount);

            if (pointerPoint.Properties.IsLeftButtonPressed)
            {
                LastMouseButton = MouseButton.Left;
            }
            else if (pointerPoint.Properties.IsMiddleButtonPressed)
            {
                LastMouseButton = MouseButton.Middle;
            }
            else if (pointerPoint.Properties.IsRightButtonPressed)
            {
                LastMouseButton = MouseButton.Right;
            }
        }

        /// <summary>
        /// Handles mouse movements.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e) => _terminalEngine.PointerMoved(e.GetCurrentPoint(Canvas));

        /// <summary>
        /// Handles mouse button releases.
        /// </summary>
        /// <remarks><inheritdoc cref="Canvas_PointerPressed"
        /// path="/remarks"/></remarks>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint pointerPoint = e.GetCurrentPoint(Canvas);

            if (LastMouseButton == MouseButton.Left)
            {
                _terminalEngine.SelectionMode = false;
            }
            else if (LastMouseButton == MouseButton.Middle)
            {
                if (PasteOnMiddleClick)
                {
                    // Do not paste when mouse tracking mode is enabled
                    if (
                        !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
                        && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
                        && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                        && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
                    )
                    {
                        PasteFromClipboard();
                    }
                }
            }

            _terminalEngine.PointerReleased(pointerPoint);

            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse wheel scrolls.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_PointerWheelChanged(object sender, PointerRoutedEventArgs e) => MouseWheelHelper.HandleMouseWheel(this, e.GetCurrentPoint(Canvas));

        /// <summary>
        /// Handles focuses.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_GotFocus(object sender, RoutedEventArgs e) => HasFocus = true;

        /// <summary>
        /// Handles the case in which we're about to lose focus.
        /// </summary>
        /// <remarks>This is required to work around a nuisance described in
        /// <see
        /// href="https://github.com/microsoft/microsoft-ui-xaml/issues/6330"
        /// />.
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
        private void Canvas_LosingFocus(UIElement sender, LosingFocusEventArgs args)
        {
            if (args.NewFocusedElement is null)
            {
                return;
            }

            if (VisualTreeHelper.GetParent(args.NewFocusedElement) is null)
            {
                args.Cancel = true;
            }
        }
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore IDE0079 // Remove unnecessary suppression

        /// <summary>
        /// Handles focus losses.
        /// </summary>
        /// <remarks>Pretends we maintain focus when the settings window is
        /// open so the user can live-preview their changes.</remarks>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SettingsWindow is null)
            {
                HasFocus = false;
            }
        }

        /// <summary>
        /// Handles resizes.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => ReportWindowSize();

        #endregion

        #region "Other Event Handlers"

        /// <summary>
        /// Handles key presses.
        /// </summary>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void InputKeyboardSource_KeyDown(InputKeyboardSource sender, KeyEventArgs args)
        {
            ShiftPressed = _inputKeyboardSource is not null
                && (
                    _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
                    || _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
                );

            ControlPressed = _inputKeyboardSource is not null
                && (
                    _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
                    || _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
                );

            AltPressed = _inputKeyboardSource is not null
                && (
                    _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down)
                    || _inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down)
                );

            if (_inputKeyboardSource is not null && HasFocus && _terminalEngine.AnsiWriter is not null)
            {
                int virtualKey = (int)args.VirtualKey;

                if (Enum.IsDefined(typeof(Key), virtualKey))
                {
                    Keystroke keystroke = new()
                    {
                        Key = (Key)virtualKey,
                        XTMODKEYS = _terminalEngine.XTMODKEYS,
                        IsRepeat = args.KeyStatus.WasKeyDown,
                        AutoRepeatKeys = _terminalEngine.AutoRepeatKeys,
                        ApplicationCursorKeys = _terminalEngine.ApplicationCursorKeys
                    };

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.LeftShift;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.RightShift;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.LeftAlt;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.RightAlt;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.LeftControl;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.RightControl;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftWindows).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.LeftMeta;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightWindows).HasFlag(VirtualKeyStates.Down))
                    {
                        keystroke.ModifierKeys |= ModifierKeys.RightMeta;
                    }

                    if (_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.CapitalLock).HasFlag(VirtualKeyStates.Locked))
                    {
                        keystroke.CapsLock = true;
                    }

                    CursorHelper.ShowCursorImmediately(this);

                    if (!KeystrokeHelper.HandleKeystroke(this, keystroke))
                    {
                        _terminalEngine.SendKeystroke(keystroke);
                    }

                    args.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles system key (i.e., Alt [or maybe F10?]) releases.
        /// </summary>
        /// <param name="sender"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='sender']"/></param>
        /// <param name="args"><inheritdoc
        /// cref="TypedEventHandler{TSender, TResult}"
        /// path="/param[@name='args']"/></param>
        private void InputKeyboardSource_SystemKeyUp(InputKeyboardSource sender, KeyEventArgs args)
        {
            if (
                _inputKeyboardSource is not null
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftShift).HasFlag(VirtualKeyStates.Down)
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightShift).HasFlag(VirtualKeyStates.Down)
            )
            {
                ShiftPressed = false;
            }

            if (
                _inputKeyboardSource is not null
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftControl).HasFlag(VirtualKeyStates.Down)
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightControl).HasFlag(VirtualKeyStates.Down)
            )
            {
                ControlPressed = false;
            }

            if (
                _inputKeyboardSource is not null
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.LeftMenu).HasFlag(VirtualKeyStates.Down)
                && !_inputKeyboardSource.GetKeyState(Windows.System.VirtualKey.RightMenu).HasFlag(VirtualKeyStates.Down)
            )
            {
                AltPressed = false;
            }
        }

        /// <summary>
        /// Handles cursor blink state changes.
        /// </summary>
        /// <param name="sender"><see cref="_cursorTimer"/></param>
        /// <param name="args">Unused.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void CursorTimer_Tick(DispatcherQueueTimer sender, object args) => _terminalEngine.CursorDisplayed = !_terminalEngine.CursorDisplayed;

        #endregion

        #region "Menu Item Event Handlers"

        /// <summary>
        /// Copies the selection.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void CopyMenuItem_Click(object sender, RoutedEventArgs e) => _terminalEngine.CopySelection();

        /// <summary>
        /// Pastes from the clipboard.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void PasteMenuItem_Click(object sender, RoutedEventArgs e) => PasteFromClipboard();

        /// <summary>
        /// Reduces the font size.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void SmallerTextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FontSize >= 2.0f)
            {
                FontSize -= 2.0f;
            }
        }

        /// <summary>
        /// Increases the font size.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void LargerTextMenuItem_Click(object sender, RoutedEventArgs e) => FontSize += 2.0f;

        /// <summary>
        /// Toggles the background visibility.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void BackgroundIsInvisibleMenuItem_Click(object sender, RoutedEventArgs e) => BackgroundIsInvisible = !BackgroundIsInvisible;

        /// <summary>
        /// Toggles the visual bell.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void UseVisualBellMenuItem_Click(object sender, RoutedEventArgs e) => UseVisualBell = !UseVisualBell;

        /// <summary>
        /// Enables the block cursor.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void BlockCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyle.Block;

        /// <summary>
        /// Enables the underline cursor.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void UnderlineCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyle.Underline;

        /// <summary>
        /// Enables the bar cursor.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void BarCursorMenuItem_Click(object sender, RoutedEventArgs e) => CursorStyle = CursorStyle.Bar;

        /// <summary>
        /// Toggles cursor blink.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void CursorBlinkMenuItem_Click(object sender, RoutedEventArgs e) => CursorBlink = !CursorBlink;

        /// <summary>
        /// Toggles copy on mouse up.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void CopyOnMouseUpMenuItem_Click(object sender, RoutedEventArgs e) => CopyOnMouseUp = !CopyOnMouseUp;

        /// <summary>
        /// Toggles paste on right click.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void PasteOnRightClickMenuItem_Click(object sender, RoutedEventArgs e) => PasteOnRightClick = !PasteOnRightClick;

        /// <summary>
        /// Toggles paste on middle click.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void PasteOnMiddleClickMenuItem_Click(object sender, RoutedEventArgs e) => PasteOnMiddleClick = !PasteOnMiddleClick;

        /// <summary>
        /// Opens the settings window.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Event handler")]
        internal void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsWindow is null)
            {
                SettingsWindow ??= new(this);
                CustomizeSettingsWindowSettings?.Invoke(SettingsWindow.ViewModel);
            }

            SettingsWindow.Display(this);
        }

        #endregion

    }
}
