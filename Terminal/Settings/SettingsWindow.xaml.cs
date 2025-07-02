using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using WinUIEx;

namespace Terminal.Settings {
  /// <summary>
  /// The settings window.
  /// </summary>
  public sealed partial class SettingsWindow : WindowEx {
    private const int preferredWidth = 400;
    private const int preferredHeight = 640;
    private const int preferredMargin = 8;

    private readonly TerminalControl terminalControl;
    private readonly SettingsViewModel viewModel;

    /// <summary>
    /// The <see cref="SettingsViewModel"/>.
    /// </summary>
    internal SettingsViewModel ViewModel => viewModel;

    /// <summary>
    /// Initializes a <see cref="SettingsWindow"/>.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    public SettingsWindow(TerminalControl terminalControl) {
      this.terminalControl = terminalControl;

      viewModel = new(terminalControl);

      if (PInvoke.SetWindowLong(
        new(WinRT.Interop.WindowNative.GetWindowHandle(this)),
        WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE,
        (int) (WINDOW_EX_STYLE.WS_EX_TOOLWINDOW | WINDOW_EX_STYLE.WS_EX_TOPMOST)
      ) == 0) {
        throw new Win32Exception(Marshal.GetLastWin32Error());
      }

      InitializeComponent();
      Title = terminalControl.ResourceLoader.GetString("SettingsWindowTitle");

      if (terminalControl.ShowSettingsSaveAsDefaultsButton) {
        SaveAsDefaultsContainer.Visibility = Visibility.Visible;
      }
    }

    /// <summary>
    /// Sets up and displays the settings window.
    /// </summary>
    public void Display(TerminalControl terminalControl) {
      // Keep the terminal window cursor blinking so the user can preview
      // changes
      terminalControl.HasFocus = true;

      HWND settingsWindowHWnd = new(Microsoft.UI.Win32Interop.GetWindowFromWindowId(AppWindow.Id));

      if (!PInvoke.IsWindowVisible(settingsWindowHWnd)) {
        HWND terminalWindowHWnd = new(terminalControl.HWnd);
        RECT terminalWindowRect = new();
        HMONITOR terminalWindowMonitor;
        MONITORINFO terminalWindowMonitorInfo = new();
        terminalWindowMonitorInfo.cbSize = (uint) Marshal.SizeOf(terminalWindowMonitorInfo);

        if (!PInvoke.GetWindowRect(
          terminalWindowHWnd,
          out terminalWindowRect
        )) {
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        terminalWindowMonitor = PInvoke.MonitorFromWindow(
          terminalWindowHWnd,
          MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST
        );

        if (!PInvoke.GetMonitorInfo(
          terminalWindowMonitor,
          ref terminalWindowMonitorInfo
        )) {
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        int settingsWindowX = terminalWindowRect.X;
        int settingsWindowY = terminalWindowRect.Y;

        // Activate() will activate the window
        SET_WINDOW_POS_FLAGS settingsWindowFlags = SET_WINDOW_POS_FLAGS.SWP_NOACTIVATE;

        // Determine settings window position, in order of preference
        if ( // To the right
          terminalWindowRect.X + terminalWindowRect.Width + preferredWidth + (preferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Width - terminalWindowMonitorInfo.rcMonitor.X
          && terminalWindowRect.Y + preferredHeight + (preferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
        ) {
          settingsWindowX += terminalWindowRect.Width + preferredMargin;
        } else if ( // To the left
          terminalWindowRect.X - preferredWidth - (preferredMargin * 2) > terminalWindowMonitorInfo.rcMonitor.X
          && terminalWindowRect.Y + preferredHeight + (preferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
        ) {
          settingsWindowX -= preferredWidth + preferredMargin;
        } else if ( // To the bottom
          terminalWindowRect.Y + terminalWindowRect.Height + preferredHeight + (preferredMargin * 2) < terminalWindowMonitorInfo.rcMonitor.Height - terminalWindowMonitorInfo.rcMonitor.Y
        ) {
          settingsWindowX += (terminalWindowRect.Width / 2) - (preferredWidth / 2);
          settingsWindowY += terminalWindowRect.Height + preferredMargin;
        } else if ( // To the top
          terminalWindowRect.Y - preferredHeight - (preferredMargin * 2) > terminalWindowMonitorInfo.rcMonitor.Y
        ) {
          settingsWindowX += (terminalWindowRect.Width / 2) - (preferredWidth / 2);
          settingsWindowY -= preferredHeight + preferredMargin;
        } else { // No good way to position the window, so let Activate() do
                 // it
          settingsWindowFlags |= SET_WINDOW_POS_FLAGS.SWP_NOMOVE;
        }

        if (!PInvoke.SetWindowPos(
          settingsWindowHWnd,
          HWND.HWND_TOPMOST,
          settingsWindowX,
          settingsWindowY,
          preferredWidth,
          preferredHeight,
          settingsWindowFlags
        )) {
          throw new Win32Exception(Marshal.GetLastWin32Error());
        }
      }

      Activate();

      // Occasionally the terminal's canvas clears at this point. Not sure why.
      terminalControl.TerminalEngine.MarkOffscreenBufferDirty();
    }

    /// <summary>
    /// Invoked when the window is closed.
    /// </summary>
    /// <param name="sender"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='sender']"/></param>
    /// <param name="args"><inheritdoc
    /// cref="TypedEventHandler{TSender, TResult}"
    /// path="/param[@name='args']"/></param>
    private void Window_Closed(object sender, WindowEventArgs args) {
      foreach (KeyValuePair<DependencyProperty, long> callbackToken in viewModel.CallbackTokens) {
        terminalControl.UnregisterPropertyChangedCallback(callbackToken.Key, callbackToken.Value);
      }

      terminalControl.SettingsWindow = null;
    }

    /// <summary>
    /// Invoked when the user clicks the save as defaults button.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void SaveAsDefaultsButton_Click(object sender, RoutedEventArgs e) => terminalControl.InvokeSaveSettingsAsDefault();

    /// <summary>
    /// Invoked when the user clicks the close button.
    /// </summary>
    /// <param name="sender"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='sender']"/></param>
    /// <param name="e"><inheritdoc cref="RoutedEventHandler"
    /// path="/param[@name='e']"/></param>
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
  }
}
