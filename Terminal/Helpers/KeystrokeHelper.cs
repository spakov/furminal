using Spakov.AnsiProcessor.Helpers;
using Spakov.AnsiProcessor.Input;

namespace Spakov.Terminal.Helpers {
  /// <summary>
  /// Methods for responding to keystrokes.
  /// </summary>
  internal static class KeystrokeHelper {
    /// <summary>
    /// Handles a <see cref="Keystroke"/>.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    /// <param name="keystroke">A <see cref="Keystroke"/>.</param>
    /// <returns><see langword="true"/> if the event was handled or <see
    /// langword="false"/> otherwise.</returns>
    internal static bool HandleKeystroke(TerminalControl terminalControl, Keystroke keystroke) {
      // Shift + Page Up => scroll back a screen
      if (keystroke.Is(Keys.PageUp, ExtendedModifierKeys.Shift)) {
        lock (terminalControl.TerminalEngine.ScreenBufferLock) {
          terminalControl.TerminalEngine.VideoTerminal.ShiftFromScrollback((uint) terminalControl.LinesPerScrollback);
        }

        return true;

      // Shift + Page Down => scroll forward a screen
      } else if (keystroke.Is(Keys.PageDown, ExtendedModifierKeys.Shift)) {
        lock (terminalControl.TerminalEngine.ScreenBufferLock) {
          terminalControl.TerminalEngine.VideoTerminal.ShiftToScrollback((uint) terminalControl.LinesPerScrollback);
        }

        return true;

      // Shift + Up => scroll back a line
      } else if (keystroke.Is(Keys.Up, ExtendedModifierKeys.Shift)) {
        lock (terminalControl.TerminalEngine.ScreenBufferLock) {
          terminalControl.TerminalEngine.VideoTerminal.ShiftFromScrollback((uint) terminalControl.LinesPerSmallScrollback);
        }

        return true;

      // Shift + Down => scroll forward a line
      } else if (keystroke.Is(Keys.Down, ExtendedModifierKeys.Shift)) {
        lock (terminalControl.TerminalEngine.ScreenBufferLock) {
          terminalControl.TerminalEngine.VideoTerminal.ShiftToScrollback((uint) terminalControl.LinesPerSmallScrollback);
        }

        return true;

      // Ctrl + Shift + C => copy selection
      } else if (keystroke.Is(Keys.C, ExtendedModifierKeys.Control | ExtendedModifierKeys.Shift)) {
        if (!terminalControl.CopyOnMouseUp) {
          terminalControl.TerminalEngine.VideoTerminal.EndSelectionMode(copy: true);
          terminalControl.TerminalEngine.VideoTerminal.SelectionMode = false;

          return true;
        }

      // Ctrl + Shift + V => paste
      } else if (keystroke.Is(Keys.V, ExtendedModifierKeys.Control | ExtendedModifierKeys.Shift)) {
        if (terminalControl.TerminalEngine.VideoTerminal.TextIsSelected) {
          terminalControl.PasteFromClipboard();

          return true;
        }

      // Ctrl + Space => NUL
      } else if (keystroke.Is(Keys.Space, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.NUL.ToString());
        return true;

      // Ctrl + H => BS
      } else if (keystroke.Is(Keys.H, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.BS.ToString());
        return true;

      // Ctrl + [ => ESC
      } else if (keystroke.Is(Keys.LeftSquareBracket, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.ESC.ToString());
        return true;

      // Ctrl + \ => FS
      } else if (keystroke.Is(Keys.ReverseSolidus, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.FS.ToString());
        return true;

      // Ctrl + ] => GS
      } else if (keystroke.Is(Keys.RightSquareBracket, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.GS.ToString());
        return true;

      // Ctrl + ` => RS
      } else if (keystroke.Is(Keys.GraveAccent, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.RS.ToString());
        return true;

      // Ctrl + - => US
      } else if (keystroke.Is(Keys.HyphenMinus, ExtendedModifierKeys.Control)) {
        terminalControl.TerminalEngine.SendText(AnsiProcessor.Ansi.C0.US.ToString());
        return true;
      }

      return false;
    }
  }
}
