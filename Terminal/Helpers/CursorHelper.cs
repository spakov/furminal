using Microsoft.Graphics.Canvas;
using System;

namespace Terminal.Helpers {
  /// <summary>
  /// Methods for manipulating the cursor.
  /// </summary>
  internal static class CursorHelper {
    /// <summary>
    /// Draws the terminal cursor.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    /// <param name="drawingSession">A <see
    /// cref="CanvasDrawingSession"/>.</param>
    internal static void DrawCursor(TerminalControl terminalControl, CanvasDrawingSession drawingSession) {
      if (
        !terminalControl.TerminalEngine.ScrollbackMode
        && terminalControl.TerminalEngine.CursorVisible
        && terminalControl.TerminalEngine.CursorDisplayed
        && terminalControl.HasFocus
      ) {
        Caret caret = terminalControl.TerminalEngine.Caret;

        if (terminalControl.CursorStyle == CursorStyles.Block) {
          drawingSession.FillRectangle(
            caret.Column * terminalControl.TerminalEngine.CellSize.Width,
            caret.Row * terminalControl.TerminalEngine.CellSize.Height,
            Math.Max(terminalControl.TerminalEngine.CellSize.Width, terminalControl.TerminalEngine.CellSize.Width),
            Math.Max(terminalControl.TerminalEngine.CellSize.Height, terminalControl.TerminalEngine.CellSize.Height),
            terminalControl.CursorColor
          );
        } else if (terminalControl.CursorStyle == CursorStyles.Underline) {
          float underlineY = (caret.Row * terminalControl.TerminalEngine.CellSize.Height) + terminalControl.TerminalEngine.CellSize.Height - 2;

          drawingSession.DrawLine(
            caret.Column * terminalControl.TerminalEngine.CellSize.Width,
            underlineY,
            (caret.Column * terminalControl.TerminalEngine.CellSize.Width) + terminalControl.TerminalEngine.CellSize.Width,
            underlineY,
            terminalControl.CursorColor,
            (float) (terminalControl.FontSize * terminalControl.CursorThickness)
          );
        } else if (terminalControl.CursorStyle == CursorStyles.Bar) {
          float barX = (caret.Column * terminalControl.TerminalEngine.CellSize.Width) + 2;

          drawingSession.DrawLine(
            barX,
            caret.Row * terminalControl.TerminalEngine.CellSize.Height,
            barX,
            (caret.Row * terminalControl.TerminalEngine.CellSize.Height) + terminalControl.TerminalEngine.CellSize.Height,
            terminalControl.CursorColor,
            (float) (terminalControl.FontSize * terminalControl.CursorThickness)
          );
        }
      }
    }

    /// <summary>
    /// Sets up the cursor blink timer.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    /// <remarks>This is intended to be invoked on change of focus.</remarks>
    internal static void SetUpCursorTimer(TerminalControl terminalControl) {
      if (terminalControl.CursorBlink) {
        if (terminalControl.HasFocus) {
          InitializeCursorTimer(terminalControl);
        } else {
          DestroyCursorTimer(terminalControl);
        }
      } else {
        DestroyCursorTimer(terminalControl);
      }

      terminalControl.TerminalEngine.CursorDisplayed = terminalControl.HasFocus;
    }

    /// <summary>
    /// If cursor blinking is enabled and the terminal has focus, shows the
    /// cursor immediately.
    /// </summary>
    /// <remarks>Useful on keypress or when previewing changes to the
    /// cursor. Does nothing if the cursor is not blinking or <see
    /// cref="TerminalControl.HasFocus"/> is <see langword="false"/>.</remarks>
    /// <param name="terminalControl"></param>
    internal static void ShowCursorImmediately(TerminalControl terminalControl) {
      if (terminalControl.CursorBlink) {
        if (terminalControl.CursorTimer is not null && terminalControl.HasFocus) {
          terminalControl.CursorTimer.Stop();
          terminalControl.TerminalEngine.CursorDisplayed = true;
          terminalControl.CursorTimer.Start();
        }
      }
    }

    /// <summary>
    /// Initializes the cursor timer.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    internal static void InitializeCursorTimer(TerminalControl terminalControl) {
      if (terminalControl.CursorTimer is null) {
        terminalControl.CursorTimer = terminalControl.DispatcherQueue.CreateTimer();
        terminalControl.CursorTimer.Interval = TimeSpan.FromMilliseconds(terminalControl.CursorBlinkRate);
        terminalControl.CursorTimer.Tick += terminalControl.CursorTimer_Tick;
        terminalControl.CursorTimer.Start();
      }
    }

    /// <summary>
    /// Destroys the cursor timer.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    internal static void DestroyCursorTimer(TerminalControl terminalControl) {
      if (terminalControl.CursorTimer is not null) {
        terminalControl.CursorTimer.Tick -= terminalControl.CursorTimer_Tick;
        terminalControl.CursorTimer.Stop();
        terminalControl.CursorTimer = null;
      }
    }
  }
}
