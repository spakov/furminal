#if DEBUG
using Microsoft.Extensions.Logging;
#endif
using Microsoft.UI.Input;
using Spakov.AnsiProcessor.Ansi.EscapeSequences;
using Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.Helpers;
using Spakov.AnsiProcessor.Output;
using Spakov.AnsiProcessor.Output.EscapeSequences;
using Spakov.WideCharacter;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Spakov.Terminal {
  /// <summary>
  /// The screen buffer and its state, which is analogous to a VT100.
  /// </summary>
  /// <remarks>
  /// <para>Does not interact with the UI thread.</para>
  /// <para><c>VideoTerminal.ProcessEscapeSequence.cs</c> contains the
  /// escape-sequence-processing parts of <see
  /// cref="VideoTerminal"/>.</para>
  /// </remarks>
  internal partial class VideoTerminal {
#if DEBUG
    private readonly ILogger logger;
#endif

    private readonly TerminalEngine terminalEngine;

    private readonly List<Cell[]> screenBuffer;
    private Caret _caret;

    private bool _selectionMode;
    private bool _lazySelectionMode;
    private Caret lastSelection;

    private List<Cell[]>? scrollbackBuffer;
    private List<Cell[]>? scrollforwardBuffer;

    private GraphicRendition graphicRendition;
    private Palette palette;
    private System.Drawing.Color backgroundColorErase;

    private readonly List<Cell[]> alternateScreenBuffer;
    private bool _useAlternateScreenBuffer;

    // For HTS, TBC, and HT
    private readonly List<int> tabStops;

    // For CSI DECSET DECAWM
    private bool autoWrapMode;
    private bool _wrapPending;

    // For CSI DECSTBM, CSI DECSET DECOM
    private int scrollRegionTop;
    private int scrollRegionBottom;
    private bool originMode;

    // For Fp DECSC/DECRC
    private CursorState? savedCursorState;

    // For CSI SAVE_CURSOR and RESTORE_CURSOR
    private Caret? savedCursorPosition;

    // For CSI XTWINOPS 22 and 23
    private readonly string?[] windowTitleStack;
    private int windowTitleStackLength;

    // For DECSET 2031
    private bool reportPaletteUpdate;

    /// <summary>
    /// The ANSI color palette being used.
    /// </summary>
    internal Palette Palette {
      get => palette;

      set {
        if (palette != value) {
          palette = value;
          graphicRendition.InitializeFromPalette(palette);

          if (reportPaletteUpdate) {
            DSRThemeQueryResponse();
          }
        }
      }
    }

    /// <summary>
    /// The caret.
    /// </summary>
    internal Caret Caret => _caret;

    /// <summary>
    /// Whether the user is selecting terminal text.
    /// </summary>
    internal bool SelectionMode {
      get => _selectionMode;

      set {
        if (_selectionMode != value) {
          // Ending selection mode and in lazy selection mode
          if (!value && _lazySelectionMode) {
            _lazySelectionMode = false;
          }

          // Ending selection mode and already in selection mode
          if (!value && _selectionMode) {
            if (terminalEngine.CopyOnMouseUp) {
              EndSelectionMode(copy: true);
            } else {
              _lazySelectionMode = true;
            }
          }

          // Starting selection mode and in lazy selection mode
          if (value && _lazySelectionMode) {
            EndSelectionMode(copy: false);
            _lazySelectionMode = false;
          }

          // Starting selection mode but already in selection mode
          if (value && _selectionMode) {
            EndSelectionMode(copy: false);
          }

          _selectionMode = value;
        }
      }
    }

    /// <summary>
    /// Whether the user has selected text.
    /// </summary>
    internal bool TextIsSelected {
      get {
        for (int row = 0; row < screenBuffer.Count; row++) {
          for (int col = 0; col < screenBuffer[row].Length; col++) {
            if (screenBuffer[row][col].Selected) {
              return true;
            }
          }
        }

        return false;
      }
    }

    /// <summary>
    /// Whether the terminal is in scrollback mode.
    /// </summary>
    /// <remarks>Set to <see langword="false"/> to force the terminal out of
    /// scrollback mode.</remarks>
    internal bool ScrollbackMode {
      get => scrollforwardBuffer is not null && scrollforwardBuffer.Count > 0;

      set {
        if (scrollforwardBuffer is not null) {
          if (!value) {
            if (ScrollbackMode) {
              ShiftToScrollback((uint) scrollforwardBuffer.Count);
            }
          }
        }
      }
    }

    /// <summary>
    /// The caret row.
    /// </summary>
    private int Row {
      get => Caret.Row;
      set => _caret.Row = value;
    }

    /// <summary>
    /// The caret column.
    /// </summary>
    private int Column {
      get => Caret.Column;
      set => _caret.Column = value;
    }

    /// <summary>
    /// Whether to use the alternate screen buffer.
    /// </summary>
    private bool UseAlternateScreenBuffer {
      get => _useAlternateScreenBuffer;

      set {
        if (_useAlternateScreenBuffer != value) {
          _useAlternateScreenBuffer = value;

          SwapBuffers(screenBuffer, alternateScreenBuffer);
        }
      }
    }

    /// <summary>
    /// Whether a wrap is pending, for CSI DECSET DECAWM
    /// </summary>
    private bool WrapPending {
      get => _wrapPending;

      set {
        if (autoWrapMode) {
          _wrapPending = value;

#if DEBUG
          logger.LogDebug("{set} WrapPending", _wrapPending ? "Set" : "Cleared");
#endif
        }
      }
    }

    /// <summary>
    /// Initializes a <see cref="VideoTerminal"/>.
    /// </summary>
    /// <param name="terminalEngine">A <see cref="TerminalEngine"/>.</param>
    internal VideoTerminal(TerminalEngine terminalEngine) {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(TerminalControl.logLevel);
        }
      );

      logger = factory.CreateLogger<VideoTerminal>();
#endif

      this.terminalEngine = terminalEngine;

      screenBuffer = [];
      alternateScreenBuffer = [];
      tabStops = [];

      graphicRendition = new();
      palette = terminalEngine.Palette!;
      graphicRendition.InitializeFromPalette(palette);

      if (terminalEngine.UseBackgroundColorErase) {
        backgroundColorErase = graphicRendition.BackgroundColor;
      }

      terminalEngine.CursorVisible = true;
      terminalEngine.AutoRepeatKeys = true;
      autoWrapMode = true;

      windowTitleStack = new string?[10];
      windowTitleStackLength = 0;

      // Initialize screen buffers
      Resize();

      InitializeTabStops();

      Task.Run(() => {
        while (true) {
          terminalEngine.VTQueueReady.WaitOne();

          while (terminalEngine.VTQueue.TryDequeue(out object? vtInput)) {
            if (vtInput is null) {
              continue;
            } else if (vtInput is string vtString) {
              lock (terminalEngine.ScreenBufferLock) {
                WriteText(vtString);
              }
            } else if (vtInput is char vtCharacter) {
              lock (terminalEngine.ScreenBufferLock) {
                WriteGraphemeCluster(vtCharacter.ToString());
              }
            } else if (vtInput is EscapeSequence vtEscapeSequence) {
              lock (terminalEngine.ScreenBufferLock) {
                ProcessEscapeSequence(vtEscapeSequence);
              }
            }
          }
        }
      });
    }

    /// <summary>
    /// Initializes the screen buffers as needed to account for a resize.
    /// </summary>
    /// <remarks>Intended to be invoked after the terminal is
    /// resized.</remarks>
    internal void Resize() {
#if DEBUG
      logger.LogDebug("Resizing to {rows} x {columns}", terminalEngine.Rows, terminalEngine.Columns);
#endif

      bool useScrollback = scrollbackBuffer is not null && scrollbackBuffer.Count > 0;

      Resize(screenBuffer, useScrollback: useScrollback);
      Resize(alternateScreenBuffer, useScrollback: false);

#if DEBUG
      logger.LogDebug("Caret is at {row}, {column}", Row, Column);
#endif

      if (Row > terminalEngine.Rows - 1) {
        Row = terminalEngine.Rows - 1;

#if DEBUG
        logger.LogDebug("Moved caret up to {row}", Row);
#endif
      }

      if (Column > terminalEngine.Columns - 1) {
        Column = terminalEngine.Columns - 1;

#if DEBUG
        logger.LogDebug("Moved caret left to {column}", Column);
#endif
      }

      tabStops.Sort();
      for (int i = tabStops.Count - 1; i >= 0; i--) {
        if (tabStops[i] > Column) {
          tabStops.RemoveAt(i);
        }
      }

      WrapPending = Column == terminalEngine.Columns - 1;

      scrollRegionTop = 0;
      scrollRegionBottom = terminalEngine.Rows - 1;
    }

    /// <summary>
    /// Initializes the scrollback buffer to account for a resize.
    /// </summary>
    /// <remarks>Intended to be invoked after the scrollback is
    /// changed.</remarks>
    internal void ResizeScrollback() {
      scrollbackBuffer ??= [];
      scrollforwardBuffer ??= [];

      if (scrollbackBuffer.Count > terminalEngine.Scrollback) {
        scrollbackBuffer.RemoveRange(
          terminalEngine.Scrollback - 1,
          scrollbackBuffer.Count - terminalEngine.Scrollback
        );
      }
    }

    /// <summary>
    /// Writes <paramref name="message"/> to the terminal.
    /// </summary>
    /// <param name="message">The message to write.</param>
    internal void Write(string message) {
      lock (terminalEngine.ScreenBufferLock) {
        graphicRendition.ForegroundColor = System.Drawing.Color.White;
        graphicRendition.BackgroundColor = System.Drawing.Color.Navy;

        NextRow();
        WriteText(message);
        NextRow();
      }
    }

    /// <summary>
    /// Writes <paramref name="message"/> to the terminal in very pronounced
    /// colors.
    /// </summary>
    /// <remarks>This is meant to be used in exceptional cases that prevent
    /// the terminal from working at all.</remarks>
    /// <param name="message">The message to write.</param>
    internal void WriteError(string message) {
      lock (terminalEngine.ScreenBufferLock) {
        graphicRendition.ForegroundColor = System.Drawing.Color.White;
        graphicRendition.BackgroundColor = System.Drawing.Color.Red;

        NextRow();
        WriteText(message);
        NextRow();
      }
    }

    /// <summary>
    /// Writes <paramref name="text"/> to the screen buffer.
    /// </summary>
    /// <param name="text">The text to write.</param>
    private void WriteText(string text) {
#if DEBUG
      logger.LogInformation("Processing text \"{text}\"", text);
#endif

      TextElementEnumerator graphemeClusterEnumerator = StringInfo.GetTextElementEnumerator(text);

      while (graphemeClusterEnumerator.MoveNext()) {
        WriteGraphemeCluster(graphemeClusterEnumerator.GetTextElement());
      }
    }

    /// <summary>
    /// Writes <paramref name="graphemeCluster"/> to the screen buffer.
    /// </summary>
    /// <param name="graphemeCluster">The grapheme cluster to write.</param>
    private void WriteGraphemeCluster(string? graphemeCluster) {
      // Snap out of scrollback mode
      ScrollbackMode = false;

#if DEBUG
      logger.LogDebug("Handling grapheme cluster {graphemeCluster}", PrintableHelper.MakePrintable(graphemeCluster));
#endif

      if (graphemeCluster is not null) {
        if (graphemeCluster[0] < 0x20) {
          if (autoWrapMode) WrapPending = false;
        }

        switch (graphemeCluster[0]) {
          // Null character
          case AnsiProcessor.Ansi.C0.NUL:
            return;

          // Backspace
          case AnsiProcessor.Ansi.C0.BS:
            CaretLeft();

            return;

          // Horizontal tabulation
          case AnsiProcessor.Ansi.C0.HT:
            NextTabStop();

            return;

          // Line feed
          case AnsiProcessor.Ansi.C0.LF:

          // Vertical tabulation
          case AnsiProcessor.Ansi.C0.VT:

          // Form feed
          case AnsiProcessor.Ansi.C0.FF:
            if (Row == terminalEngine.Rows - 1) {
              ShiftToScrollback(1, force: true);
            } else {
              CaretDown();
            }

            return;

          // Carriage return
          case AnsiProcessor.Ansi.C0.CR:
            Column = 0;

            return;
        }
      }

      // Something printable (hopefully)
      if (autoWrapMode && WrapPending) {
#if DEBUG
        logger.LogDebug("Auto-wrap initiated");
#endif

        NextRow();
        WrapPending = false;
      }

      int graphemeClusterWidth = graphemeCluster.WideCharacterWidth();

      // Wrap if we don't have space to write a wide character
      if (graphemeClusterWidth > 1 && autoWrapMode && Column == terminalEngine.Columns - 2) {
#if DEBUG
        logger.LogDebug("Auto-wrap initiated (wide character)");
#endif

        NextRow();
        WrapPending = false;
      }

      screenBuffer[Row][Column] = new() {
        GraphemeCluster = graphemeCluster,
        GraphicRendition = graphicRendition,
      };

      if (graphemeClusterWidth > 1) {
        if (autoWrapMode && Column == terminalEngine.Columns - 1) {
          WrapPending = true;
        } else {
          CaretRight();
        }

        if (autoWrapMode && WrapPending) {
#if DEBUG
          logger.LogDebug("Auto-wrap initiated (wide character)");
#endif

          NextRow();
          WrapPending = false;
        }

        screenBuffer[Row][Column] = new() {
          GraphemeCluster = null,
          GraphicRendition = graphicRendition,
        };
      }

#if DEBUG
      logger.LogDebug("screenBuffer[{Row}][{Column}] = '{graphemeCluster}'", Row, Column, graphemeCluster);
#endif

      if (autoWrapMode && Column == terminalEngine.Columns - 1) {
        WrapPending = true;
      } else {
        CaretRight();
      }
    }

    /// <summary>
    /// Handles pointer presses.
    /// </summary>
    /// <remarks>
    /// Intended to be invoked by <see
    /// cref="TerminalControl.Canvas_PointerPressed"/> to handle the event.
    /// </remarks>
    /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
    /// cref="TerminalControl.Canvas_PointerPressed"/>.</param>
    internal void PointerPressed(PointerPoint pointerPoint) {
      (int row, int column) = PointToCellIndices(pointerPoint.Position);
      if (row < 0 || column < 0) return;

      // Handle mouse tracking
      if (
        terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
      ) {
        if (
          !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
          && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
        ) {
          // For mouse tracking
          byte cb = 0x20;

          if (pointerPoint.Properties.IsLeftButtonPressed) {
            cb += 0x00;
          } else if (pointerPoint.Properties.IsMiddleButtonPressed) {
            cb += 0x01;
          } else if (pointerPoint.Properties.IsRightButtonPressed) {
            cb += 0x02;
          }

          if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)) {
            if (terminalEngine.ShiftPressed) cb += 0x04;
            if (terminalEngine.AltPressed) cb += 0x08;
            if (terminalEngine.ControlPressed) cb += 0x10;
          }

          if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20) {
            cb = byte.MaxValue;
          }

          if (cb < byte.MaxValue) {
            terminalEngine.AnsiWriter?.SendEscapeSequence(
              [
                (byte) Fe.CSI,
                (byte) CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                cb,
                (byte) (column + 1 + 0x20),
                (byte) (row + 1 + 0x20)
              ],
              brokenMode: true
            );
          }
        } else {
          // For mouse tracking
          uint cb = 0x00;

          if (pointerPoint.Properties.IsLeftButtonPressed) {
            cb += 0x00;
          } else if (pointerPoint.Properties.IsMiddleButtonPressed) {
            cb += 0x01;
          } else if (pointerPoint.Properties.IsRightButtonPressed) {
            cb += 0x02;
          }

          if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)) {
            if (terminalEngine.ShiftPressed) cb += 0x04;
            if (terminalEngine.AltPressed) cb += 0x08;
            if (terminalEngine.ControlPressed) cb += 0x10;
          }

          StringBuilder mouseReport = new();

          if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)) {
            mouseReport.Append(Fe.CSI);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
            mouseReport.Append(cb);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(column + 1);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(row + 1);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
          } else if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)) {
            mouseReport.Append(Fe.CSI);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
            mouseReport.Append(cb);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(pointerPoint.Position.X);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(pointerPoint.Position.Y);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
          }

          terminalEngine.AnsiWriter?.SendEscapeSequence(
            Encoding.ASCII.GetBytes(mouseReport.ToString()),
            brokenMode: true
          );
        }
      }

      // Handle selection changes
      if (pointerPoint.Properties.IsLeftButtonPressed) {
        lastSelection.Column = column;
        lastSelection.Row = row;

        SelectionMode = true;
      }
    }

    /// <summary>
    /// Handles pointer movement.
    /// </summary>
    /// <remarks>
    /// Intended to be invoked by <see
    /// cref="TerminalControl.Canvas_PointerMoved"/> to handle the event.
    /// </remarks>
    /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
    /// cref="TerminalControl.Canvas_PointerMoved"/>.</param>
    internal void PointerMoved(PointerPoint pointerPoint) {
      (int row, int column) = PointToCellIndices(pointerPoint.Position);
      if (row < 0 || column < 0) return;

      // Handle mouse tracking
      if (
        terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
      ) {
        bool trackMouse = true;

        if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)) {
          // Cell motion occurs only when a button is pressed
          if (
            !pointerPoint.Properties.IsLeftButtonPressed
            && !pointerPoint.Properties.IsMiddleButtonPressed
            && !pointerPoint.Properties.IsRightButtonPressed
          ) {
            trackMouse = false;
          }
        }

        if (trackMouse) {
          if (
            !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
            && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
          ) {
            // For mouse tracking
            byte cb = 0x20;

            if (pointerPoint.Properties.IsLeftButtonPressed) {
              cb += 0x00;
            } else if (pointerPoint.Properties.IsMiddleButtonPressed) {
              cb += 0x01;
            } else if (pointerPoint.Properties.IsRightButtonPressed) {
              cb += 0x02;
            } else {
              // No button pressed
              cb += 0x03;
            }

            // This is a mouse move
            cb += 0x20;

            if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20) {
              cb = byte.MaxValue;
            }

            if (cb < byte.MaxValue) {
              terminalEngine.AnsiWriter?.SendEscapeSequence(
                [
                  (byte) Fe.CSI,
                  (byte) CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                  cb,
                  (byte) (column + 1 + 0x20),
                  (byte) (row + 1 + 0x20)
                ],
                brokenMode: true
              );
            }
          } else {
            // For mouse tracking
            uint cb = 0x00;

            if (pointerPoint.Properties.IsLeftButtonPressed) {
              cb += 0x00;
            } else if (pointerPoint.Properties.IsMiddleButtonPressed) {
              cb += 0x01;
            } else if (pointerPoint.Properties.IsRightButtonPressed) {
              cb += 0x02;
            } else {
              // No button pressed
              cb += 0x03;
            }

            // This is a mouse move
            cb += 0x20;

            StringBuilder mouseReport = new();

            if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)) {
              mouseReport.Append(AnsiProcessor.Ansi.C0.ESC);
              mouseReport.Append(Fe.CSI);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
              mouseReport.Append(cb);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
              mouseReport.Append(column + 1);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
              mouseReport.Append(row + 1);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
            } else if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)) {
              mouseReport.Append(AnsiProcessor.Ansi.C0.ESC);
              mouseReport.Append(Fe.CSI);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
              mouseReport.Append(cb);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
              mouseReport.Append(pointerPoint.Position.X);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
              mouseReport.Append(pointerPoint.Position.Y);
              mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
            }

            terminalEngine.AnsiWriter?.SendEscapeSequence(
              Encoding.ASCII.GetBytes(mouseReport.ToString()),
              brokenMode: true
            );
          }
        }
      }

      // Handle selection changes
      if (!SelectionMode) return;

      if (lastSelection.Row == -1 && lastSelection.Column == -1) {
        lastSelection.Row = row;
        lastSelection.Column = column;
      }

      // Use Bresenham's line algorithm to interpolate missing points in the
      // mouse movement and account for all selection addition and subtraction
      // operations
      foreach ((int deltaRow, int deltaColumn) in FourConnectedBresenhamInterpolation(lastSelection, (row, column))) {
        HorizontalSelectionChange(deltaRow, deltaColumn);
        VerticalSelectionChange(deltaRow, deltaColumn);

        screenBuffer[deltaRow][deltaColumn].Selected = true;

        lastSelection.Row = deltaRow;
        lastSelection.Column = deltaColumn;
      }
    }

    /// <summary>
    /// Handles pointer releases.
    /// </summary>
    /// <remarks>
    /// <para>Intended to be invoked by <see
    /// cref="TerminalControl.Canvas_PointerReleased"/> to handle the
    /// event.</para>
    /// <para><c>SelectionMode = false</c> is handled by the caller.</para>
    /// </remarks>
    /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
    /// cref="TerminalControl.Canvas_PointerReleased"/>.</param>
    internal void PointerReleased(PointerPoint pointerPoint) {
      (int row, int column) = PointToCellIndices(pointerPoint.Position);
      if (row < 0 || column < 0) return;

      // Handle mouse tracking
      if (
        terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
        || terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
      ) {
        if (
          !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
          && !terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
        ) {
          // For mouse tracking
          byte cb = 0x20;

          // This is a mouse button release
          cb += 0x03;

          if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20) {
            cb = byte.MaxValue;
          }

          if (cb < byte.MaxValue) {
            terminalEngine.AnsiWriter?.SendEscapeSequence(
              [
                (byte) Fe.CSI,
                (byte) CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                cb,
                (byte) (column + 1 + 0x20),
                (byte) (row + 1 + 0x20)
              ],
              brokenMode: true
            );
          }
        } else {
          // For mouse tracking
          uint cb = 0x00;

          if (terminalEngine.LastMouseButton == MouseButtons.Left) {
            cb += 0x00;
          } else if (terminalEngine.LastMouseButton == MouseButtons.Middle) {
            cb += 0x01;
          } else if (terminalEngine.LastMouseButton == MouseButtons.Right) {
            cb += 0x02;
          }

          StringBuilder mouseReport = new();

          if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)) {
            mouseReport.Append(AnsiProcessor.Ansi.C0.ESC);
            mouseReport.Append(Fe.CSI);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
            mouseReport.Append(cb);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(column + 1);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(row + 1);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_RELEASE_TERMINATOR);
          } else if (terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)) {
            mouseReport.Append(AnsiProcessor.Ansi.C0.ESC);
            mouseReport.Append(Fe.CSI);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
            mouseReport.Append(cb);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(pointerPoint.Position.X);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
            mouseReport.Append(pointerPoint.Position.Y);
            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_RELEASE_TERMINATOR);
          }

          terminalEngine.AnsiWriter?.SendEscapeSequence(
            Encoding.ASCII.GetBytes(mouseReport.ToString()),
            brokenMode: true
          );
        }
      }
    }

    /// <summary>
    /// Updates the horizontal selection.
    /// </summary>
    /// <param name="row">The row to which to adjust the selection.</param>
    /// <param name="column">The column to which to adjust the
    /// selection.</param>
    private void HorizontalSelectionChange(int row, int column) {
      if (lastSelection.Column != column) {
        int delta = lastSelection.Column > -1
          ? column - lastSelection.Column
          : 0;

        int firstSelectedRowInLastSelectionColumn = int.MaxValue;
        int lastSelectedRowInLastSelectionColumn = int.MinValue;

        if (lastSelection.Column > -1) {
          for (int i = 0; i < screenBuffer.Count; i++) {
            if (screenBuffer[i].Length > lastSelection.Column) {
              if (screenBuffer[i][lastSelection.Column].Selected) {
                if (i < firstSelectedRowInLastSelectionColumn) {
                  firstSelectedRowInLastSelectionColumn = i;
                }

                if (i > lastSelectedRowInLastSelectionColumn) {
                  lastSelectedRowInLastSelectionColumn = i;
                }
              }
            }
          }
        }

        if (!terminalEngine.AltPressed) {
          // Line selection mode
          if (delta > 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaColumn = lastSelection.Column; deltaColumn < column; deltaColumn++) {
                screenBuffer[row][deltaColumn].Selected = false;
              }
            } else {
              // The user is adding to the selection
              for (int deltaColumn = lastSelection.Column + 1; deltaColumn <= column; deltaColumn++) {
                screenBuffer[row][deltaColumn].Selected = true;
              }
            }
          } else if (delta < 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaColumn = lastSelection.Column; deltaColumn > column; deltaColumn--) {
                screenBuffer[row][deltaColumn].Selected = false;
              }
            } else {
              // The user is adding to the selection
              for (int deltaColumn = lastSelection.Column - 1; deltaColumn >= column; deltaColumn--) {
                screenBuffer[row][deltaColumn].Selected = true;
              }
            }
          }
        } else {
          // Block selection mode
          if (delta > 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaColumn = lastSelection.Column; deltaColumn < column; deltaColumn++) {
                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = false;
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaColumn = lastSelection.Column + 1; deltaColumn <= column; deltaColumn++) {
                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = true;
                }
              }
            }
          } else if (delta < 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaColumn = lastSelection.Column; deltaColumn > column; deltaColumn--) {
                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = false;
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaColumn = lastSelection.Column - 1; deltaColumn >= column; deltaColumn--) {
                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = true;
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Updates the vertical selection.
    /// </summary>
    /// <param name="row">The row to which to adjust the selection.</param>
    /// <param name="column">The column to which to adjust the
    /// selection.</param>
    private void VerticalSelectionChange(int row, int column) {
      if (lastSelection.Row != row) {
        int delta = lastSelection.Row > -1
          ? row - lastSelection.Row
          : 0;

        int firstSelectedColumnInLastSelectionRow = int.MaxValue;
        int lastSelectedColumnInLastSelectionRow = int.MinValue;

        if (lastSelection.Row > -1) {
          for (int i = 0; i < screenBuffer[lastSelection.Row].Length; i++) {
            if (screenBuffer[lastSelection.Row][i].Selected) {
              if (i < firstSelectedColumnInLastSelectionRow) {
                firstSelectedColumnInLastSelectionRow = i;
              }

              if (i > lastSelectedColumnInLastSelectionRow) {
                lastSelectedColumnInLastSelectionRow = i;
              }
            }
          }
        }

        if (!terminalEngine.AltPressed) {
          // Line selection mode
          if (delta > 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaRow = lastSelection.Row; deltaRow <= row; deltaRow++) {
                if (deltaRow == row) {
                  // The first selected row
                  for (int deltaColumn = 0; deltaColumn <= firstSelectedColumnInLastSelectionRow; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = false;
                  }
                } else {
                  // Any other row
                  for (int deltaColumn = 0; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = false;
                  }
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaRow = lastSelection.Row; deltaRow <= row; deltaRow++) {
                if (deltaRow == lastSelection.Row) {
                  // The first selected row
                  for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                } else if (deltaRow == row) {
                  // The last selected row
                  for (int deltaColumn = 0; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                } else {
                  // Any other row
                  for (int deltaColumn = 0; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                }
              }
            }
          } else if (delta < 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaRow = lastSelection.Row; deltaRow >= row; deltaRow--) {
                if (deltaRow == row) {
                  // The first selected row
                  for (int deltaColumn = lastSelectedColumnInLastSelectionRow; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = false;
                  }
                } else {
                  // Any other row
                  for (int deltaColumn = 0; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = false;
                  }
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaRow = lastSelection.Row; deltaRow >= row; deltaRow--) {
                if (deltaRow == lastSelection.Row) {
                  // The first selected row
                  for (int deltaColumn = 0; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                } else if (deltaRow == row) {
                  // The last selected row
                  for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                } else {
                  // Any other row
                  for (int deltaColumn = 0; deltaColumn < screenBuffer[deltaRow].Length; deltaColumn++) {
                    screenBuffer[deltaRow][deltaColumn].Selected = true;
                  }
                }
              }
            }
          }
        } else {
          // Block selection mode
          if (delta > 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaRow = lastSelection.Row; deltaRow < row; deltaRow++) {
                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = false;
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaRow = lastSelection.Row + 1; deltaRow <= row; deltaRow++) {
                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = true;
                }
              }
            }
          } else if (delta < 0) {
            if (screenBuffer[row][column].Selected) {
              // The user is removing from the selection
              for (int deltaRow = lastSelection.Row; deltaRow > row; deltaRow--) {
                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = false;
                }
              }
            } else {
              // The user is adding to the selection
              for (int deltaRow = lastSelection.Row - 1; deltaRow >= row; deltaRow--) {
                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++) {
                  screenBuffer[deltaRow][deltaColumn].Selected = true;
                }
              }
            }
          }
        }
      }
    }

    /// <summary>
    /// Clears the selection, copying if <paramref name="copy"/> is <see
    /// langword="true"/>.
    /// </summary>
    /// <remarks>Internal scope: only invoke this if <see
    /// cref="TerminalControl.CopyOnMouseUp"/> is <see langword="false"/>, and
    /// then immediately set <see cref="SelectionMode"/> to <see
    /// langword="false"/>. In this case, ensure to invoke with <paramref
    /// name="copy"/> set to <see langword="true"/> to copy the
    /// selection.</remarks>
    /// <param name="copy">Whether to copy the selection or simply clear
    /// it.</param>
    internal void EndSelectionMode(bool copy) {
      StringBuilder selection = new();

      for (int row = 0; row < screenBuffer.Count; row++) {
        bool hadSelection = false;

        for (int col = 0; col < screenBuffer[row].Length; col++) {
          if (screenBuffer[row][col].Selected) {
            hadSelection = true;
            screenBuffer[row][col].Selected = false;

            if (copy) {
              selection.Append(screenBuffer[row][col].GraphemeCluster);
            }
          }
        }

        if (copy && hadSelection) {
          selection.Append(terminalEngine.CopyNewline);
        }
      }

      if (copy && selection.Length > 0) {
        selection.Remove(selection.Length - terminalEngine.CopyNewline.Length, terminalEngine.CopyNewline.Length);

        DataPackage dataPackage = new() {
          RequestedOperation = DataPackageOperation.Copy
        };

        dataPackage.SetText(selection.ToString());
        Clipboard.SetContent(dataPackage);
      }

      lastSelection.Row = -1;
      lastSelection.Column = -1;
    }

    /// <summary>
    /// Shifts <paramref name="rows"/> rows from the top of the screen buffer.
    /// </summary>
    /// <remarks>
    /// <para>If the scrollback and scrollforward buffers are initialized,
    /// shifts into the scrollback buffer, shifting out of the scrollforward
    /// buffer as needed.</para>
    /// <para>If <see cref="UseAlternateScreenBuffer"/> is <see
    /// langword="true"/>, behaves the same as if the scrollback and
    /// scrollforward buffers are not initialized.</para>
    /// </remarks>
    /// <param name="rows">The number of rows to shift.</param>
    /// <param name="force">Whether to force the shift to scrollback, even if
    /// it will result in empty lines.</param>
#if DEBUG
    internal void ShiftToScrollback(uint rows = 1, bool force = false, [System.Runtime.CompilerServices.CallerMemberName] string? callerMemberName = null) {
#else
    internal void ShiftToScrollback(uint rows = 1, bool force = false) {
#endif
      bool useScrollback = scrollbackBuffer is not null && scrollforwardBuffer is not null && terminalEngine.Scrollback > 0 && !UseAlternateScreenBuffer;

      SelectionMode = false;

      if (useScrollback) {
        if (!force) {
          if (rows > scrollforwardBuffer!.Count) {
            rows = (uint) scrollforwardBuffer.Count;
          }
        }
      }

#if DEBUG
      logger.LogInformation("{callerMemberName} => ShiftToScrollback({rows}, force: {force})", callerMemberName, rows, force);
#endif

      if (!useScrollback && !force) return;

      for (int row = 0; row < rows; row++) {
        if (useScrollback) {
          if (scrollbackBuffer!.Count == terminalEngine.Scrollback) {
            scrollbackBuffer.RemoveAt(0);
          }
        }

        if (useScrollback) {
          scrollbackBuffer!.Add(screenBuffer[0]);
        }

        screenBuffer.RemoveAt(0);

        if (useScrollback && scrollforwardBuffer!.Count > 0) {
          screenBuffer.Add(scrollforwardBuffer[0]);

          scrollforwardBuffer.RemoveAt(0);
        } else {
          screenBuffer.Add(new Cell[terminalEngine.Columns]);

          for (int col = 0; col < terminalEngine.Columns; col++) {
            screenBuffer[terminalEngine.Rows - 1][col] = new() {
              GraphicRendition = graphicRendition
            };

            if (terminalEngine.UseBackgroundColorErase) {
              screenBuffer[terminalEngine.Rows - 1][col].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }
        }
      }
    }

    /// <summary>
    /// Shifts <paramref name="rows"/> rows from the bottom of the screen
    /// buffer.
    /// </summary>
    /// <remarks>
    /// <para>If the scrollback and scrollforward buffers are initialized,
    /// shifts from the bottom of the scrollback buffer into the screen buffer,
    /// shifting into the scrollforward buffer as needed.</para>
    /// <para>If <see cref="UseAlternateScreenBuffer"/> is <see
    /// langword="true"/>, behaves the same as if the scrollback and
    /// scrollforward buffers are not initialized.</para>
    /// </remarks>
    /// <param name="rows">The number of rows to shift.</param>
    /// <param name="force">Whether to force the shift to scrollforward, even
    /// if it will result in empty lines.</param>
#if DEBUG
    internal void ShiftFromScrollback(uint rows = 1, bool force = false, [System.Runtime.CompilerServices.CallerMemberName] string? callerMemberName = null) {
#else
    internal void ShiftFromScrollback(uint rows = 1, bool force = false) {
#endif
      bool useScrollback = scrollbackBuffer is not null && scrollforwardBuffer is not null && terminalEngine.Scrollback > 0 && !UseAlternateScreenBuffer;

      SelectionMode = false;

      if (useScrollback) {
        if (!force) {
          if (rows > scrollbackBuffer!.Count) {
            rows = (uint) scrollbackBuffer.Count;
          }
        }
      }

#if DEBUG
      logger.LogInformation("{callerMemberName} => ShiftFromScrollback({rows}, force: {force})", callerMemberName, rows, force);
#endif

      if (!useScrollback && !force) return;

      for (int row = 0; row < rows; row++) {
        if (useScrollback) {
          scrollforwardBuffer!.Insert(0, screenBuffer[terminalEngine.Rows - 1]);
        }

        screenBuffer.RemoveAt(terminalEngine.Rows - 1);

        if (useScrollback && scrollbackBuffer!.Count > 0) {
          screenBuffer.Insert(0, scrollbackBuffer[^1]);

          scrollbackBuffer.RemoveAt(scrollbackBuffer.Count - 1);
        } else {
          screenBuffer.Insert(0, new Cell[terminalEngine.Columns]);

          for (int col = 0; col < terminalEngine.Columns; col++) {
            screenBuffer[0][col] = new() {
              GraphicRendition = graphicRendition
            };

            if (terminalEngine.UseBackgroundColorErase) {
              screenBuffer[0][col].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }
        }
      }
    }

    /// <summary>
    /// Returns the indices of the <see cref="Cell"/> within <see
    /// cref="screenBuffer"/> corresponding to <paramref name="point"/>.
    /// </summary>
    /// <param name="point">A <see cref="Point"/>.</param>
    /// <returns>A <see cref="ValueTuple{T1, T2}"/> containing the row and the
    /// column indices within <see cref="screenBuffer"/> of the matching <see
    /// cref="Cell"/>, or <c>-1</c> if there is no <see cref="Cell"/> at
    /// <paramref name="point"/>.</returns>
    internal (int row, int column) PointToCellIndices(Point point) {
      int row = (int) (point.Y / (terminalEngine.Rows * terminalEngine.CellSize.Height) * terminalEngine.Rows);
      int column = (int) (point.X / (terminalEngine.Columns * terminalEngine.CellSize.Width) * terminalEngine.Columns);

      if (row < 0 || column < 0) return (-1, -1);

      return row > screenBuffer.Count - 1
        ? (-1, -1)
        : column > screenBuffer[row].Length - 1
          ? (row, -1)
          : (row, column);
    }

    /// <summary>
    /// Row indexer.
    /// </summary>
    /// <param name="row">The requested row.</param>
    /// <returns>The row at index <paramref name="row"/>.</returns>
    internal Cell[] this[int row] => screenBuffer[row];

    /// <summary>
    /// Initializes <paramref name="buffer"/> as needed to account for a
    /// resize.
    /// </summary>
    /// <remarks>Note that this method does not invoke <see
    /// cref="ShiftToScrollback"/>; if <paramref name="useScrollback"/> is
    /// <see langword="true"/>, the scrollback buffer must be
    /// initialized.</remarks>
    /// <param name="buffer">A screen buffer.</param>
    /// <param name="useScrollback">Whether to use scrollback.</param>
    private void Resize(List<Cell[]> buffer, bool useScrollback) {
      // Resize columns in each row
      for (int row = 0; row < buffer.Count; row++) {
        Cell[] newRow = new Cell[terminalEngine.Columns];

        for (int col = 0; col < Math.Min(buffer[row].Length, terminalEngine.Columns); col++) {
          newRow[col] = buffer[row][col];
        }

        for (int col = buffer[row].Length - 1; col < terminalEngine.Columns; col++) {
          newRow[col] = new() {
            GraphicRendition = graphicRendition
          };

          if (terminalEngine.UseBackgroundColorErase) {
            newRow[col].GraphicRendition.BackgroundColor = backgroundColorErase;
          }
        }

        buffer[row] = newRow;
      }

      // Adjust number of rows
      if (terminalEngine.Rows > buffer.Count) { // Adding rows
        for (int row = buffer.Count; row < terminalEngine.Rows; row++) {
          buffer.Add(new Cell[terminalEngine.Columns]);

          for (int col = 0; col < terminalEngine.Columns; col++) {
            buffer[row][col] = new();

            if (terminalEngine.UseBackgroundColorErase) {
              buffer[row][col].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }
        }
      } else if (terminalEngine.Rows < buffer.Count) { // Removing rows
        SelectionMode = false;

        for (int row = buffer.Count - 1; row >= terminalEngine.Rows; row--) {
          if (useScrollback) {
            if (scrollbackBuffer!.Count == terminalEngine.Scrollback) {
              scrollbackBuffer.RemoveAt(0);
            }

            scrollbackBuffer!.Add(buffer[0]);
          }

          buffer.RemoveAt(0);
        }
      }
    }

    /// <summary>
    /// Swaps each <see cref="Cell"/> in <paramref name="bufferA"/> and
    /// <paramref name="bufferB"/>.
    /// </summary>
    /// <remarks>Assumes that <paramref name="bufferA"/> and <paramref
    /// name="bufferB"/> are both <see cref="TerminalControl.Rows"/> by <see
    /// cref="TerminalControl.Columns"/>.</remarks>
    /// <param name="bufferA">A <see cref="List{T}"/> of an array of <see
    /// cref="Cell"/>s.</param>
    /// <param name="bufferB">A <see cref="List{T}"/> of an array of <see
    /// cref="Cell"/>s.</param>
    private void SwapBuffers(List<Cell[]> bufferA, List<Cell[]> bufferB) {
      Cell swap;

      for (int row = 0; row < terminalEngine.Rows; row++) {
        for (int col = 0; col < terminalEngine.Columns; col++) {
          swap = bufferA[row][col];
          bufferA[row][col] = bufferB[row][col];
          bufferB[row][col] = swap;
        }
      }
    }

    /// <summary>
    /// Clears the screen.
    /// </summary>
    /// <param name="screenClearType">The type of screen clear.</param>
    private void ClearScreen(ScreenClearTypes screenClearType) {
      switch (screenClearType) {
        case ScreenClearTypes.Before:
          for (int i = 0; i <= Row; i++) {
            if (i < Row) {
              for (int j = 0; j < terminalEngine.Columns; j++) {
                screenBuffer[i][j] = new() {
                  GraphicRendition = graphicRendition
                };

                if (terminalEngine.UseBackgroundColorErase) {
                  screenBuffer[i][j].GraphicRendition.BackgroundColor = backgroundColorErase;
                }
              }
            } else {
              for (int j = 0; j < Column; j++) {
                screenBuffer[i][j] = new() {
                  GraphicRendition = graphicRendition
                };

                if (terminalEngine.UseBackgroundColorErase) {
                  screenBuffer[i][j].GraphicRendition.BackgroundColor = backgroundColorErase;
                }
              }
            }
          }

          break;

        case ScreenClearTypes.After:
          for (int i = Row; i < terminalEngine.Rows; i++) {
            if (i > Row) {
              for (int j = 0; j < terminalEngine.Columns; j++) {
                screenBuffer[i][j] = new() {
                  GraphicRendition = graphicRendition
                };

                if (terminalEngine.UseBackgroundColorErase) {
                  screenBuffer[i][j].GraphicRendition.BackgroundColor = backgroundColorErase;
                }
              }
            } else {
              for (int j = Column; j < terminalEngine.Columns; j++) {
                screenBuffer[i][j] = new() {
                  GraphicRendition = graphicRendition
                };

                if (terminalEngine.UseBackgroundColorErase) {
                  screenBuffer[i][j].GraphicRendition.BackgroundColor = backgroundColorErase;
                }
              }
            }
          }

          break;

        case ScreenClearTypes.Entire:
        case ScreenClearTypes.EntireWithScrollback:
          for (int i = Row; i < terminalEngine.Rows; i++) {
            for (int j = 0; j < terminalEngine.Columns; j++) {
              screenBuffer[i][j] = new() {
                GraphicRendition = graphicRendition
              };

              if (terminalEngine.UseBackgroundColorErase) {
                screenBuffer[i][j].GraphicRendition.BackgroundColor = backgroundColorErase;
              }
            }
          }

          if (screenClearType == ScreenClearTypes.EntireWithScrollback) {
            ScrollbackMode = false;
            scrollbackBuffer?.Clear();
          }

          break;
      }
    }

    /// <summary>
    /// Clears the line.
    /// </summary>
    /// <param name="lineClearType">The type of line clear.</param>
    private void ClearLine(LineClearTypes lineClearType) {
      switch (lineClearType) {
        case LineClearTypes.Before:
          for (int j = 0; j < Column; j++) {
            screenBuffer[Row][j] = new() {
              GraphicRendition = graphicRendition
            };

            if (terminalEngine.UseBackgroundColorErase) {
              screenBuffer[Row][j].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }

          break;

        case LineClearTypes.After:
          for (int j = Column; j < terminalEngine.Columns; j++) {
            screenBuffer[Row][j] = new() {
              GraphicRendition = graphicRendition
            };

            if (terminalEngine.UseBackgroundColorErase) {
              screenBuffer[Row][j].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }

          break;

        case LineClearTypes.Entire:
          for (int j = 0; j < terminalEngine.Columns; j++) {
            screenBuffer[Row][j] = new() {
              GraphicRendition = graphicRendition
            };

            if (terminalEngine.UseBackgroundColorErase) {
              screenBuffer[Row][j].GraphicRendition.BackgroundColor = backgroundColorErase;
            }
          }

          break;
      }
    }

    /// <summary>
    /// Initializes tab stops based on the current terminal size.
    /// </summary>
    private void InitializeTabStops() {
      tabStops.Clear();

      for (int i = 0; i < terminalEngine.Columns; i += terminalEngine.TabWidth) {
        tabStops.Add(i);
      }
    }

    /// <summary>
    /// Moves the caret to the next tab stop.
    /// </summary>
    private void NextTabStop() {
      tabStops.Sort();

      foreach (int tabStop in tabStops) {
        if (tabStop <= Column) continue;
        Column = tabStop;
        return;
      }
    }

    /// <summary>
    /// Moves the caret to the previous tab stop.
    /// </summary>
    private void PreviousTabStop() {
      tabStops.Sort((a, b) => b.CompareTo(a));

      foreach (int tabStop in tabStops) {
        if (tabStop >= Column) continue;
        Column = tabStop;
        return;
      }
    }

    /// <summary>
    /// Moves the caret to the beginning of the next row.
    /// </summary>
    private void NextRow() {
      Column = 0;

      if (++Row == terminalEngine.Rows) {
        Row--;
        ShiftToScrollback(1, force: true);
      }
    }

    /// <summary>
    /// Moves the caret to the beginning of the previous row.
    /// </summary>
    private void PreviousRow() {
      if (--Row < 0) Row = 0;
      Column = 0;
    }

    /// <summary>
    /// Moves the caret to the left.
    /// </summary>
    private void CaretLeft() {
      if (--Column < 0) Column++;
    }

    /// <summary>
    /// Moves the caret to the right.
    /// </summary>
    private void CaretRight() {
      if (++Column == screenBuffer[Row].Length) Column--;
    }

    /// <summary>
    /// Moves the caret up.
    /// </summary>
    private void CaretUp() {
      if (--Row < 0) Row++;
    }

    /// <summary>
    /// Moves the caret down.
    /// </summary>
    private void CaretDown() {
      if (++Row == terminalEngine.Rows) Row--;
    }

    /// <summary>
    /// Generates all (row, column) points along a 4-connected Bresenham line
    /// from <paramref name="lastSelection"/> to <paramref
    /// name="newSelection"/>.
    /// </summary>
    /// <remarks>
    /// <para>Intended for cell-based selection where coordinates are
    /// discrete.</para>
    /// <para>Source: <see href="https://stackoverflow.com/a/14506390"/></para>
    /// </remarks>
    /// <param name="lastSelection">The last selection <see
    /// cref="Terminal.Caret"/>.</param>
    /// <param name="newSelection">A <see cref="ValueTuple{T1, T2}"/> of the
    /// target row and column.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see
    /// cref="ValueTuple{T1, T2}"/> of row and column.</returns>
    private static IEnumerable<(int row, int column)> FourConnectedBresenhamInterpolation(Caret lastSelection, ValueTuple<int, int> newSelection) {
      int column0 = lastSelection.Column;
      int column1 = newSelection.Item2;
      int row0 = lastSelection.Row;
      int row1 = newSelection.Item1;

      int deltaColumn = Math.Abs(column1 - column0);
      int deltaRow = Math.Abs(row1 - row0);
      int signColumn = column0 < column1 ? 1 : -1;
      int signRow = row0 < row1 ? 1 : -1;
      int err = 0;

      for (int i = 0; i <= deltaColumn + deltaRow; i++) {
        yield return (row0, column0);

        int err1 = err + deltaRow;
        int err2 = err - deltaColumn;

        if (Math.Abs(err1) < Math.Abs(err2)) {
          column0 += signColumn;
          err = err1;
        } else {
          row0 += signRow;
          err = err2;
        }
      }
    }

    /// <summary>
    /// Responds to a <see cref="CSI_DSR.DSR_THEME_QUERY"/>.
    /// </summary>
    private void DSRThemeQueryResponse() {
      float backgroundColorGamma = 0.0f;

      backgroundColorGamma += (float) Palette.DefaultBackgroundColor.R / byte.MaxValue;
      backgroundColorGamma += (float) Palette.DefaultBackgroundColor.G / byte.MaxValue;
      backgroundColorGamma += (float) Palette.DefaultBackgroundColor.B / byte.MaxValue;

      backgroundColorGamma /= 3;

      StringBuilder themeResponse = new();

      themeResponse.Append(Fe.CSI);
      themeResponse.Append('?');
      themeResponse.Append(CSI_DSR.DSR_THEME_RESPONSE);
      themeResponse.Append(CSI_DSR.DSR_THEME_SEPARATOR);
      themeResponse.Append(backgroundColorGamma > 0.5 ? CSI_DSR.DSR_THEME_LIGHT : CSI_DSR.DSR_THEME_DARK);
      themeResponse.Append(CSI.DSR);

      terminalEngine.AnsiWriter?.SendEscapeSequence(
        Encoding.ASCII.GetBytes(themeResponse.ToString())
      );
    }

    /// <summary>
    /// The cursor state, as in DECSC/DECRC.
    /// </summary>
    private readonly struct CursorState {
      /// <summary>
      /// A snapshot of <see cref="VideoTerminal.Caret"/>.
      /// </summary>
      public readonly Caret Caret;

      /// <summary>
      /// A snapshot of <see cref="TerminalControl.CursorVisible"/>.
      /// </summary>
      public readonly bool CursorVisible;

      /// <summary>
      /// A snapshot of <see cref="autoWrapMode"/>.
      /// </summary>
      public readonly bool AutoWrapMode;

      /// <summary>
      /// A snapshot of <see cref="VideoTerminal.WrapPending"/>.
      /// </summary>
      public readonly bool WrapPending;

      /// <summary>
      /// A snapshot of <see cref="originMode"/>.
      /// </summary>
      public readonly bool OriginMode;

      /// <summary>
      /// A snapshot of <see cref="graphicRendition"/>.
      /// </summary>
      public readonly GraphicRendition GraphicRendition;

      /// <summary>
      /// Initializes a <see cref="CursorState"/> based on <paramref
      /// name="screenBuffer"/>.
      /// </summary>
      /// <param name="screenBuffer">A <see cref="VideoTerminal"/>.</param>
      public CursorState(VideoTerminal screenBuffer) {
        Caret = screenBuffer.Caret;
        CursorVisible = screenBuffer.terminalEngine.CursorVisible;
        AutoWrapMode = screenBuffer.autoWrapMode;
        WrapPending = screenBuffer.WrapPending;
        OriginMode = screenBuffer.originMode;
        GraphicRendition = screenBuffer.graphicRendition;

#if DEBUG
        screenBuffer.logger.LogDebug("Saving cursor state:");
        screenBuffer.logger.LogDebug("  Row = {row}", Caret.Row);
        screenBuffer.logger.LogDebug("  Column = {column}", Caret.Column);
        screenBuffer.logger.LogDebug("  CursorVisible = {cursorVisible}", CursorVisible);
        screenBuffer.logger.LogDebug("  AutoWrapMode = {autoWrapMode}", AutoWrapMode);
        screenBuffer.logger.LogDebug("  WrapPending = {wrapPending}", WrapPending);
        screenBuffer.logger.LogDebug("  OriginMode = {originMode}", OriginMode);
        screenBuffer.logger.LogDebug("  GraphicRendition = {graphicRendition}", GraphicRendition);
#endif
      }

      /// <summary>
      /// Restores a <see cref="CursorState"/> to <paramref
      /// name="screenBuffer"/>.
      /// </summary>
      /// <param name="screenBuffer"></param>
      public readonly void Restore(VideoTerminal screenBuffer) {
        screenBuffer.Row = Caret.Row;
        screenBuffer.Column = Caret.Column;
        screenBuffer.terminalEngine.CursorVisible = CursorVisible;
        screenBuffer.autoWrapMode = AutoWrapMode;
        screenBuffer.WrapPending = WrapPending;
        screenBuffer.originMode = OriginMode;
        screenBuffer.graphicRendition = GraphicRendition;

#if DEBUG
        screenBuffer.logger.LogDebug("Restoring cursor state:");
        screenBuffer.logger.LogDebug("  Row = {row}", screenBuffer.Row);
        screenBuffer.logger.LogDebug("  Column = {column}", screenBuffer.Column);
        screenBuffer.logger.LogDebug("  CursorVisible = {cursorVisible}", screenBuffer.terminalEngine.CursorVisible);
        screenBuffer.logger.LogDebug("  AutoWrapMode = {autoWrapMode}", screenBuffer.autoWrapMode);
        screenBuffer.logger.LogDebug("  WrapPending = {wrapPending}", screenBuffer.WrapPending);
        screenBuffer.logger.LogDebug("  OriginMode = {originMode}", screenBuffer.originMode);
        screenBuffer.logger.LogDebug("  GraphicRendition = {graphicRendition}", screenBuffer.graphicRendition);
#endif
      }
    }

    /// <summary>
    /// Represents the last selection.
    /// </summary>
    private struct LastSelection {
      /// <summary>
      /// The row index of <see cref="screenBuffer"/>.
      /// </summary>
      public int Row;

      /// <summary>
      /// The column index of <see cref="screenBuffer"/>.
      /// </summary>
      public int Column;

      /// <summary>
      /// Initializes a <see cref="LastSelection"/>.
      /// </summary>
      public LastSelection() {
        Row = -1;
        Column = -1;
      }
    }

    /// <summary>
    /// Types of screen clears.
    /// </summary>
    private enum ScreenClearTypes {
      /// <summary>
      /// Clear after the caret.
      /// </summary>
      After = 0,

      /// <summary>
      /// Clear before the caret.
      /// </summary>
      Before = 1,

      /// <summary>
      /// Clear the entire screen.
      /// </summary>
      Entire = 2,

      /// <summary>
      /// Clear the entire screen and clear the scrollback buffer.
      /// </summary>
      EntireWithScrollback = 3
    }

    /// <summary>
    /// Types of line clears.
    /// </summary>
    private enum LineClearTypes {
      /// <summary>
      /// Clear after the caret.
      /// </summary>
      After = 0,

      /// <summary>
      /// Clear before the caret.
      /// </summary>
      Before = 1,

      /// <summary>
      /// Clear the entire line.
      /// </summary>
      Entire = 2
    }
  }
}
