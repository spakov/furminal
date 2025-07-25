using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// The settings window.
    /// </summary>
    /// <remarks>
    /// <para>This is a native window presenting a XAML island.</para>
    /// <para>Why not just use a <see cref="Window"/>? Well, if we do, we're
    /// relying on the assumption that the consumer of <see
    /// cref="TerminalControl"/> is running in a <see cref="Window"/>. For
    /// TermBar, that's not the case—it's running in a <see
    /// cref="DesktopWindowXamlSource"/>. If we use a <see cref="Window"/>,
    /// closing the settings window closes the entire hosting application,
    /// which is obviously not desired behavior.</para>
    /// </remarks>
    public sealed partial class SettingsWindow : UserControl
    {
        private const int PreferredWidth = 400;
        private const int PreferredHeight = 640;
        private const int PreferredMargin = 8;

        private const string LightThemeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string LightThemeName = "AppsUseLightTheme";

        private readonly TerminalControl _terminalControl;
        private readonly SettingsViewModel _viewModel;

        private readonly HWND _nativeHWnd;
        private readonly HWND _islandHWnd;
        private readonly DesktopWindowXamlSource _xamlSource;

        private static WNDPROC? s_settingsWindowProc = null;
        private int _lastDarkMode = -1;

        /// <summary>
        /// The <see cref="SettingsViewModel"/>.
        /// </summary>
        internal SettingsViewModel ViewModel => _viewModel;

        /// <summary>
        /// Initializes a <see cref="SettingsWindow"/>.
        /// </summary>
        /// <param name="terminalControl">A <see
        /// cref="TerminalControl"/>.</param>
        public SettingsWindow(TerminalControl terminalControl)
        {
            _terminalControl = terminalControl;

            _viewModel = new(terminalControl);
            DataContext = ViewModel;
            InitializeComponent();

            string className = "SettingsWindow";
            HMODULE hInstance = PInvoke.GetModuleHandle((PCWSTR)null);
            WNDCLASSEXW wndClass;

            if (s_settingsWindowProc is null)
            {
                s_settingsWindowProc = new(SettingsWindowProc);

                unsafe
                {
                    fixed (char* classNamePtr = "SettingsWindow")
                    {
                        wndClass = new()
                        {
                            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
                            lpfnWndProc = s_settingsWindowProc,
                            hInstance = hInstance,
                            lpszClassName = new(classNamePtr)
                        };
                    }
                }

                if (PInvoke.RegisterClassEx(wndClass) == 0)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            unsafe
            {
                _nativeHWnd = PInvoke.CreateWindowEx(
                    WINDOW_EX_STYLE.WS_EX_COMPOSITED
                    | WINDOW_EX_STYLE.WS_EX_LAYERED
                    | WINDOW_EX_STYLE.WS_EX_TOOLWINDOW
                    | WINDOW_EX_STYLE.WS_EX_TOPMOST,
                    className,
                    terminalControl.ResourceLoader.GetString("SettingsWindowTitle"),
                    WINDOW_STYLE.WS_OVERLAPPED
                    | WINDOW_STYLE.WS_CAPTION
                    | WINDOW_STYLE.WS_POPUPWINDOW,
                    0,
                    0,
                    PreferredWidth,
                    PreferredHeight,
                    HWND.Null,
                    null,
                    null,
                    null
                );
            }

            if (_nativeHWnd == HWND.Null)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Our Win32 window's cursor, at this point, is (probably)
            // IDC_WAIT. It "bleeds through" when we do things like display
            // context menus. Explanation from Raymond Chen:
            // https://devblogs.microsoft.com/oldnewthing/20250424-00/?p=111114
            PInvoke.SetCursor(PInvoke.LoadCursor((HMODULE)(nint)0, PInvoke.IDC_ARROW));

            _xamlSource = new();
            _xamlSource.Initialize(Win32Interop.GetWindowIdFromWindow(_nativeHWnd));
            _xamlSource.Content = this;

            _islandHWnd = new(Win32Interop.GetWindowFromWindowId(_xamlSource.SiteBridge.WindowId));

            if (terminalControl.ShowSettingsSaveAsDefaultsButton)
            {
                SaveAsDefaultsContainer.Visibility = Visibility.Visible;
            }

            int backdropType = 2;

            unsafe
            {
                PInvoke.DwmSetWindowAttribute(
                    _nativeHWnd,
                    DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
                    &backdropType,
                    sizeof(int)
                );
            }

            ApplySystemTheme();
        }

        /// <summary>
        /// Sets up and displays the settings window.
        /// </summary>
        public void Display(TerminalControl terminalControl)
        {
            // Keep the terminal window cursor blinking so the user can preview
            // changes
            terminalControl.HasFocus = true;

            if (!PInvoke.IsWindowVisible(_islandHWnd))
            {
                HWND terminalWindowHWnd = new(terminalControl.HWnd);
                RECT terminalWindowRect = new();
                HMONITOR terminalWindowMonitor;
                MONITORINFO terminalWindowMonitorInfo = new();
                terminalWindowMonitorInfo.cbSize = (uint)Marshal.SizeOf(terminalWindowMonitorInfo);

                if (!PInvoke.GetWindowRect(
                    terminalWindowHWnd,
                    out terminalWindowRect
                ))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                terminalWindowMonitor = PInvoke.MonitorFromWindow(
                    terminalWindowHWnd,
                    MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST
                );

                if (!PInvoke.GetMonitorInfo(
                    terminalWindowMonitor,
                    ref terminalWindowMonitorInfo
                ))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                int settingsWindowX = terminalWindowRect.X;
                int settingsWindowY = terminalWindowRect.Y;

                SET_WINDOW_POS_FLAGS settingsWindowFlags = SET_WINDOW_POS_FLAGS.SWP_NOZORDER;

                // Determine settings window position, in order of preference
                if ( // To the right
                    terminalWindowRect.X + terminalWindowRect.Width + PreferredWidth + (PreferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Width - terminalWindowMonitorInfo.rcMonitor.X
                    && terminalWindowRect.Y + PreferredHeight + (PreferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
                )
                {
                    settingsWindowX += terminalWindowRect.Width + PreferredMargin;
                }
                else if ( // To the left
                    terminalWindowRect.X - PreferredWidth - (PreferredMargin * 2) > terminalWindowMonitorInfo.rcMonitor.X
                    && terminalWindowRect.Y + PreferredHeight + (PreferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
                )
                {
                    settingsWindowX -= PreferredWidth + PreferredMargin;
                }
                else if ( // To the bottom
                    terminalWindowRect.Y + terminalWindowRect.Height + PreferredHeight + (PreferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
                )
                {
                    settingsWindowX += (terminalWindowRect.Width / 2) - (PreferredWidth / 2);
                    settingsWindowY += terminalWindowRect.Height + PreferredMargin;
                }
                else if ( // To the top
                    terminalWindowRect.Y - PreferredHeight - (PreferredMargin * 2) > terminalWindowMonitorInfo.rcMonitor.Y
                )
                {
                    settingsWindowX += (terminalWindowRect.Width / 2) - (PreferredWidth / 2);
                    settingsWindowY -= PreferredHeight + PreferredMargin;
                }
                else
                { // No good way to position the window
                    settingsWindowFlags |= SET_WINDOW_POS_FLAGS.SWP_NOMOVE;
                }

                if (!PInvoke.SetWindowPos(
                    _nativeHWnd,
                    HWND.Null,
                    settingsWindowX,
                    settingsWindowY,
                    PreferredWidth,
                    PreferredHeight,
                    settingsWindowFlags | SET_WINDOW_POS_FLAGS.SWP_SHOWWINDOW
                ))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                RECT nativeWindowClientArea = new();

                if (!PInvoke.GetClientRect(
                    _nativeHWnd,
                    out nativeWindowClientArea
                ))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            else
            {
                if (PInvoke.SetActiveWindow(_nativeHWnd) == HWND.Null)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }

            // Occasionally the terminal's canvas clears at this point. It has
            // something to do with modifying the XAML tree, but I'm not quite
            // sure what causes the canvas clear. In any case, work around it.
            terminalControl.TerminalEngine.MarkOffscreenBufferDirty();
        }

        /// <summary>
        /// Handles <see cref="WNDPROC"/> callbacks for the native window.
        /// </summary>
        /// <param name="hWnd">The window handle.</param>
        /// <param name="uMsg">The message.</param>
        /// <param name="wParam">Additional message information.</param>
        /// <param name="lParam">Additional message information.</param>
        /// <returns>The result of a call to <see
        /// cref="PInvoke.DefWindowProc"/>.</returns>
        private LRESULT SettingsWindowProc(HWND hWnd, uint uMsg, WPARAM wParam, LPARAM lParam)
        {
            switch (uMsg)
            {
                case PInvoke.WM_SETTINGCHANGE:
                    ApplySystemTheme();

                    break;

                case PInvoke.WM_CLOSE:
                    foreach (KeyValuePair<DependencyProperty, long> callbackToken in _viewModel.CallbackTokens)
                    {
                        _terminalControl.UnregisterPropertyChangedCallback(callbackToken.Key, callbackToken.Value);
                    }

                    _terminalControl.SettingsWindow = null;

                    break;
            }

            return PInvoke.DefWindowProc(hWnd, uMsg, wParam, lParam);
        }

        /// <summary>
        /// Applies the current system theme (light or dark) to the native window's
        /// backdrop.
        /// </summary>
        /// <remarks>Does nothing if the system theme did not change.</remarks>
        private void ApplySystemTheme()
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(LightThemeKey);

            object? value = key?.GetValue(LightThemeName);

            if (value is int appsUseLightTheme)
            {
                int darkMode = appsUseLightTheme == 0 ? 1 : 0;

                if (_lastDarkMode != darkMode)
                {
                    unsafe
                    {
                        PInvoke.DwmSetWindowAttribute(
                            _nativeHWnd,
                            DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                            &darkMode,
                            sizeof(int)
                        );
                    }

                    _lastDarkMode = darkMode;
                }
            }
        }

        /// <summary>
        /// Saves the current settings as the default settings.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void SaveAsDefaultsButton_Click(object sender, RoutedEventArgs e) => _terminalControl.InvokeSaveSettingsAsDefault();

        /// <summary>
        /// Closes the window.
        /// </summary>
        /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc cref="RoutedEventHandler"
        /// path="/param[@name='e']"/></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e) => PInvoke.PostMessage(_nativeHWnd, PInvoke.WM_CLOSE, 0, 0);
    }
}
