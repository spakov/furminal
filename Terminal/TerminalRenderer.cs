using AnsiProcessor.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terminal.Helpers;
using Windows.UI;
using Windows.UI.Text;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace Terminal {
  /// <summary>
  /// A terminal renderer, responsible for drawing operations.
  /// </summary>
  /// <remarks>Does not interact with the UI thread.</remarks>
  internal class TerminalRenderer {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
    public static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEXW lpmi);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore IDE0079 // Remove unnecessary suppression

#if DEBUG
    internal readonly ILogger logger;
#endif

    /// <summary>
    /// The Windows DPI scale factor constant.
    /// </summary>
    private const float dpiConstant = 96.0f;

    /// <summary>
    /// A full-block character, used to calculate the size of a cell.
    /// </summary>
    private const string em = "█";

    /// <summary>
    /// The midpoint of text to decorate, as a fraction of cell height.
    /// </summary>
    private const float decorationMidpoint = 0.5f;

    /// <summary>
    /// The "almost bottom" of text to decorate, as a fraction of cell height.
    /// </summary>
    private const float decorationAlmostBottom = 0.85f;

    /// <summary>
    /// The bottom of text to decorate, as a fraction of cell height.
    /// </summary>
    private const float decorationBottom = 1.0f;

    /// <summary>
    /// The line weight of text decorations, as a fraction of font size.
    /// </summary>
    private const float decorationWeight = 0.05f;

    /// <summary>
    /// The line weight of undercurl text decorations, as a fraction of font
    /// size.
    /// </summary>
    private const float decorationUndercurlWeight = 0.05f;

    private readonly TerminalEngine terminalEngine;

    private readonly CanvasTextFormat[] textFormats = new CanvasTextFormat[0x07];

    private readonly Dictionary<CellFingerprint, CanvasTextLayout> canvasTextLayoutCache;
    private readonly Dictionary<CellFingerprint, RectF> overfillCache;

    private SizeF cellSize;
    private bool cellSizeDirty;

    private CanvasRenderTarget? offscreenBuffer;
    private bool offscreenBufferDirty;
    private Caret lastFrameBounds;
    private Cell[,] lastFrameCells;
    private readonly Queue<DrawableCell> drawableCells;
    private readonly Queue<DrawableCell> drawableCellForegrounds;
    private readonly HashSet<DrawableCell> seenDrawableCells;

    private bool _cursorDisplayed;
    private bool _cursorVisible;

    private double _refreshRate;

    /// <summary>
    /// The terminal text formats.
    /// </summary>
    /// <remarks>
    /// <para>This is an array of seven <see cref="CanvasTextFormat"/>s:</para>
    /// <list type="bullet">
    /// <item><c>0x00</c>: plain</item>
    /// <item><c>0x01</c>: bold</item>
    /// <item><c>0x02</c>: faint</item>
    /// <item><c>0x03</c>: unused</item>
    /// <item><c>0x04</c>: italic</item>
    /// <item><c>0x05</c>: bold and italic</item>
    /// <item><c>0x06</c>: faint and italic</item>
    /// </list>
    /// </remarks>
    internal CanvasTextFormat[] TextFormats => textFormats;

    /// <summary>
    /// The <see cref="CanvasTextLayout"/> cache.
    /// </summary>
    internal Dictionary<CellFingerprint, CanvasTextLayout> CanvasTextLayoutCache => canvasTextLayoutCache;

    /// <summary>
    /// The overfill cache.
    /// </summary>
    internal Dictionary<CellFingerprint, RectF> OverfillCache => overfillCache;

    /// <summary>
    /// The terminal cell size.
    /// </summary>
    internal SizeF CellSize {
      get => cellSize;
      set => cellSize = value;
    }

    /// <summary>
    /// Whether the terminal cell size needs to be recalculated.
    /// </summary>
    internal bool CellSizeDirty {
      get => cellSizeDirty;
      set => cellSizeDirty = value;
    }

    /// <summary>
    /// The offscreen buffer.
    /// </summary>
    internal CanvasRenderTarget? OffscreenBuffer => offscreenBuffer;

    /// <summary>
    /// Whether the offscreen buffer needs to be redrawn.
    /// </summary>
    internal bool OffscreenBufferDirty {
      get => offscreenBufferDirty;
      set => offscreenBufferDirty = value;
    }

    /// <summary>
    /// Whether the cursor is to be displayed on the next Draw.
    /// </summary>
    /// <remarks>This is used by the drawing routines to toggle the cursor's
    /// visibility to facilitate blinking.</remarks>
    internal bool CursorDisplayed {
      get => _cursorDisplayed;
      set => _cursorDisplayed = value;
    }

    /// <summary>
    /// Whether the cursor is visible.
    /// </summary>
    /// <remarks>This differs from <see cref="CursorDisplayed"/> in that it is
    /// controlled via CSI DECSET escape sequence <see
    /// cref="AnsiProcessor.Ansi.EscapeSequences.CSI.DECSET_DECTCEM"/> and
    /// overrides <see cref="CursorDisplayed"/>.</remarks>
    internal bool CursorVisible {
      get => _cursorVisible;
      set => _cursorVisible = value;
    }

    /// <summary>
    /// The window's monitor's refresh rate.
    /// </summary>
    private double RefreshRate {
      get => _refreshRate;

      set {
        if (_refreshRate != value) {
          _refreshRate = value;

#if DEBUG
          logger.LogInformation("Refresh rate is {refreshRate:F} Hz", _refreshRate);
#endif
        }
      }
    }

    /// <summary>
    /// Initializes a <see cref="TerminalRenderer"/>.
    /// </summary>
    internal TerminalRenderer(TerminalEngine terminalEngine) {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(TerminalControl.logLevel);
        }
      );

      logger = factory.CreateLogger<TerminalRenderer>();
#endif

      this.terminalEngine = terminalEngine;

      InitializeTextFormats();

      canvasTextLayoutCache = [];
      overfillCache = [];

      lastFrameBounds = new(-1, -1);
      lastFrameCells = new Cell[0, 0];
      drawableCells = [];
      drawableCellForegrounds = [];
      seenDrawableCells = [];

      // This will be updated when the TerminalControl is added to the XAML
      // tree
      _refreshRate = 60.0;

      Task.Run(() => {
        Stopwatch stopwatch = Stopwatch.StartNew();
        long lastFrameTicks = stopwatch.ElapsedTicks;
        long ticksPerFrame = (int) (Stopwatch.Frequency / RefreshRate);

        while (true) {
          long nowTicks = stopwatch.ElapsedTicks;

          if (nowTicks - lastFrameTicks >= ticksPerFrame) {
            lastFrameTicks = nowTicks;

            terminalEngine.DispatcherQueue.TryEnqueue(() => {
              lock (terminalEngine.ScreenBufferLock) {
                DrawFrame();
              }
            });
          } else {
            Thread.Sleep(1);
          }
        }
      });
    }

    /// <summary>
    /// Calculates the cell size and resizes resources appropriately.
    /// </summary>
    /// <remarks>Intended to be invoked by <see cref="TerminalControl"/> when
    /// it attempts to draw but sees that <see cref="CellSizeDirty"/> is <see
    /// langword="true"/>.</remarks>
    internal void CleanCellSize() {
      MeasureCell();
      ResizeOffscreenBuffer();
    }

    /// <summary>
    /// Resizes the offscreen buffer to match <see
    /// cref="TerminalEngine.NominalSizeInPixels"/>.
    /// </summary>
    /// <remarks>If <paramref name="force"/> is <see langword="true"/>,
    /// instantiates the offscreen buffer.</remarks>
    /// <param name="force">Whether to force instantiation of the offscreen
    /// buffer.</param>
    internal void ResizeOffscreenBuffer(bool force = false) {
      if (offscreenBuffer is not null || force) {
        offscreenBufferDirty = true;
        offscreenBuffer = new(terminalEngine.Canvas, terminalEngine.NominalSizeInPixels.ToSize());

        using (CanvasDrawingSession drawingSession = offscreenBuffer.CreateDrawingSession()) {
          drawingSession.Clear(Colors.Transparent);
        }
      }
    }

    /// <summary>
    /// Initializes <see cref="CanvasTextFormat"/>s with the font-related
    /// properties configured in the <see cref="TerminalControl"/>.
    /// </summary>
    internal void InitializeTextFormats() {
      byte boldVariant = 0x01;
      byte faintVariant = 0x02;
      byte italicVariant = 0x04;

      for (byte i = 0x00; i < boldVariant + faintVariant + italicVariant; i++) {
        if (i == boldVariant + faintVariant) continue;

        textFormats[i] = new() {
          FontFamily = terminalEngine.FontFamily,
          FontSize = (float) terminalEngine.FontSize,
          HorizontalAlignment = CanvasHorizontalAlignment.Left,
          VerticalAlignment = CanvasVerticalAlignment.Top
        };

        if ((i & boldVariant) != 0) {
          textFormats[i]!.FontWeight = new(700);
        } else if ((i & faintVariant) != 0) {
          textFormats[i]!.FontWeight = new(300);
        }

        if ((i & italicVariant) != 0) {
          textFormats[i]!.FontStyle = FontStyle.Italic;
        }
      }

      CellSizeDirty = true;
    }

    /// <summary>
    /// Invalidates the layout caches.
    /// </summary>
    /// <remarks>Intended to be invoked when the font changes.</remarks>
    internal void InvalidateLayoutCaches() {
      canvasTextLayoutCache.Clear();
      overfillCache.Clear();
    }

    /// <summary>
    /// Updates <see cref="RefreshRate"/> based on the monitor on which our
    /// window is displayed.
    /// </summary>
    internal void UpdateRefreshRate() {
      HMONITOR hMonitor = PInvoke.MonitorFromWindow(new(terminalEngine.HWnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

      MONITORINFOEXW monitorInfoEx = new();
      monitorInfoEx.monitorInfo.cbSize = (uint) Marshal.SizeOf(monitorInfoEx);

      if (GetMonitorInfoW(hMonitor, ref monitorInfoEx)) {
        DEVMODEW devMode = new();

        if (PInvoke.EnumDisplaySettings(monitorInfoEx.szDevice.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode)) {
          RefreshRate = devMode.dmDisplayFrequency;

          return;
        }
      }

      RefreshRate = 60.0;
    }

    /// <summary>
    /// Measures the size of one terminal cell based on <see cref="em"/>.
    /// </summary>
    /// <remarks>The "plain" <see cref="textFormats"/> is used for layout
    /// purposes. Italicized text, e.g., will still flow nicely out of bounds,
    /// if necessary.</remarks>
    private void MeasureCell() {
      CanvasTextLayout canvasTextLayout = new(
        terminalEngine.Canvas.Device,
        em,
        textFormats[0],
        0.0f,
        0.0f
      );

      CellSize = new(
        MathF.Round((float) canvasTextLayout.DrawBounds.Width),
        MathF.Round((float) canvasTextLayout.DrawBounds.Height)
      );

#if DEBUG
      logger.LogInformation("Measured cell size: ({width}, {height})", CellSize.Width, CellSize.Height);
#endif

      CellSizeDirty = false;
    }

    /// <summary>
    /// Draws a terminal frame.
    /// </summary>
    /// <remarks>
    /// <para>Intended to be executed on the UI thread.</para>
    /// <para>Does not compose the cursor—that is handled by <see
    /// cref="TerminalControl.Canvas_Draw"/>.</para>
    /// </remarks>
    private void DrawFrame() {
      if (offscreenBuffer is null) return;

      int rows = terminalEngine.Rows;
      int columns = terminalEngine.Columns;
      TextAntialiasingStyles textAntialiasing = terminalEngine.TextAntialiasing;
      System.Drawing.Color defaultBackgroundColor = terminalEngine.Palette.DefaultBackgroundColor;
      bool backgroundIsInvisible = terminalEngine.BackgroundIsInvisible;

      Cell[,] thisFrameCells = new Cell[rows, columns];
      drawableCells.Clear();

      using (CanvasDrawingSession drawingSession = offscreenBuffer.CreateDrawingSession()) {
        for (int row = 0; row < rows; row++) {
          for (int column = 0; column < columns; column++) {
            Caret caret = new(row, column);

            Vector2 point = new(column * CellSize.Width, row * CellSize.Height);

            Cell cell = terminalEngine.VideoTerminal[row][column];

            if (
              offscreenBufferDirty
              || rows != lastFrameBounds.Row
              || columns != lastFrameBounds.Column
            ) {
              drawableCells.Enqueue(
                new(
                  this,
                  drawingSession,
                  caret,
                  point,
                  terminalEngine.VideoTerminal[row][column]
                )
              );
            } else {
              // Check cells that differ from the previous frame
              if (caret.Row < lastFrameBounds.Row && caret.Column < lastFrameBounds.Column) {
                if (cell != lastFrameCells[row, column]) {
                  DrawableCell drawableCell;
                  DrawableCell? upstairsNeighbor = null;
                  DrawableCell? leftNeighbor = null;
                  DrawableCell? rightNeighbor = null;
                  DrawableCell? downstairsNeighbor = null;

                  drawableCell = new(
                    this,
                    drawingSession,
                    caret,
                    point,
                    terminalEngine.VideoTerminal[row][column]
                  );

                  if (row > 0) {
                    upstairsNeighbor = new(
                      this,
                      drawingSession,
                      new(row - 1, column),
                      new(column * CellSize.Width, (row - 1) * CellSize.Height),
                      terminalEngine.VideoTerminal[row - 1][column]
                    );
                  }

                  if (column > 0) {
                    leftNeighbor = new(
                      this,
                      drawingSession,
                      new(row, column - 1),
                      new((column - 1) * CellSize.Width, row * CellSize.Height),
                      terminalEngine.VideoTerminal[row][column - 1]
                    );
                  }

                  if (column < columns - 1) {
                    rightNeighbor = new(
                      this,
                      drawingSession,
                      new(row, column + 1),
                      new((column + 1) * CellSize.Width, row * CellSize.Height),
                      terminalEngine.VideoTerminal[row][column + 1]
                    );
                  }

                  if (row < rows - 1) {
                    downstairsNeighbor = new(
                      this,
                      drawingSession,
                      new(row + 1, column),
                      new(column * CellSize.Width, (row + 1) * CellSize.Height),
                      terminalEngine.VideoTerminal[row + 1][column]
                    );
                  }

                  // Check for overfill from the cell that was here on the last
                  // frame
                  if (overfillCache.TryGetValue(new CellFingerprint(lastFrameCells[row, column]), out RectF overfill)) {
                    // Redraw this cell and its neighbors, but only if needed
                    if (overfill.Top > 0.0f && upstairsNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) upstairsNeighbor);
                    }

                    if (overfill.Left > 0.0f && leftNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) leftNeighbor);
                    }

                    if (overfill.Right > 0.0f && rightNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) rightNeighbor);
                    }

                    if (overfill.Bottom > 0.0f && downstairsNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) downstairsNeighbor);
                    }

                    drawableCells.Enqueue(drawableCell);
                  } else {
                    // This is a cache miss, so assume the worst case
                    if (upstairsNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) upstairsNeighbor);
                    }

                    if (leftNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) leftNeighbor);
                    }

                    if (rightNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) rightNeighbor);
                    }

                    if (downstairsNeighbor != null) {
                      drawableCells.Enqueue((DrawableCell) downstairsNeighbor);
                    }

                    drawableCells.Enqueue(drawableCell);
                  }
                }
              }
            }

            thisFrameCells[caret.Row, caret.Column] = cell;
          }
        }

        if (offscreenBufferDirty) {
          offscreenBufferDirty = false;
        }

        lastFrameCells = thisFrameCells;
        seenDrawableCells.Clear();

        // Draw cell backgrounds
        while (drawableCells.TryDequeue(out DrawableCell drawableCell)) {
          if (seenDrawableCells.Contains(drawableCell)) continue;

          DrawBackground(
            drawingSession,
            defaultBackgroundColor,
            backgroundIsInvisible,
            drawableCell
          );

          seenDrawableCells.Add(drawableCell);

          if (drawableCell.Cell.Rune != null && !Rune.IsWhiteSpace((Rune) drawableCell.Cell.Rune)) {
            drawableCellForegrounds.Enqueue(drawableCell);
          }
        }

        seenDrawableCells.Clear();

        // Draw cell foregrounds and decorations
        while (drawableCellForegrounds.TryDequeue(out DrawableCell drawableCell)) {
          if (seenDrawableCells.Contains(drawableCell)) continue;

          DrawForeground(
            drawingSession,
            textAntialiasing,
            defaultBackgroundColor,
            backgroundIsInvisible,
            drawableCell
          );

          DrawDecoration(
            drawingSession,
            drawableCell
          );

          seenDrawableCells.Add(drawableCell);
        }
      }

      lastFrameBounds = new(rows, columns);

      terminalEngine.InvalidateCanvas();
    }

    /// <summary>
    /// Draws <paramref name="drawableCell"/>'s background to <paramref
    /// name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession">The draw loop's <see
    /// cref="CanvasDrawingSession"/>.</param>
    /// <param name="defaultBackgroundColor">The default background color with
    /// which to draw.</param>
    /// <param name="backgroundIsInvisible">Whether the background, if
    /// <paramref name="defaultBackgroundColor"/>, should be drawn as
    /// transparent.</param>
    /// <param name="drawableCell">The cell to draw.</param>
    private void DrawBackground(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, DrawableCell drawableCell) {
      Color calculatedColor = drawableCell.Cell.GraphicRendition.Inverse ^ drawableCell.Cell.Selected
        ? drawableCell.Cell.GraphicRendition.CalculatedForegroundColor()
        : drawableCell.Cell.GraphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible);

      drawingSession.Blend = CanvasBlend.Copy;

      drawingSession.FillRectangle(
        MathF.Round(drawableCell.Point.X * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        MathF.Round(drawableCell.Point.Y * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        MathF.Round(CellSize.Width * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        MathF.Round(CellSize.Height * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        calculatedColor
      );

      drawingSession.Blend = CanvasBlend.SourceOver;
    }

    /// <summary>
    /// Draws <paramref name="drawableCell"/>'s foreground to <paramref
    /// name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession">The draw loop's <see
    /// cref="CanvasDrawingSession"/>.</param>
    /// <param name="textAntialiasing">The text antialiasing style with which
    /// to draw <paramref name="drawableCell"/>.</param>
    /// <param name="defaultBackgroundColor">The default background color with
    /// which to draw.</param>
    /// <param name="backgroundIsInvisible">Whether the background, if
    /// <paramref name="defaultBackgroundColor"/>, should be drawn as
    /// transparent.</param>
    /// <param name="drawableCell">The cell to draw.</param>
    /// <exception cref="ArgumentException"></exception>
    private static void DrawForeground(CanvasDrawingSession drawingSession, TextAntialiasingStyles textAntialiasing, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, DrawableCell drawableCell) {
      if (drawableCell.Cell.Rune is null) return;

      // Always present box-drawing characters as aliased
      drawingSession.TextAntialiasing = ((Rune) drawableCell.Cell.Rune!).Value is >= 0x2500 and <= 0x257f
        ? CanvasTextAntialiasing.Aliased
        : textAntialiasing switch {
            (TextAntialiasingStyles) (-1) => CanvasTextAntialiasing.Aliased,
            TextAntialiasingStyles.None => CanvasTextAntialiasing.Aliased,
            TextAntialiasingStyles.Grayscale => CanvasTextAntialiasing.Grayscale,
            TextAntialiasingStyles.ClearType => CanvasTextAntialiasing.ClearType,
            _ => throw new ArgumentException($"Invalid TextAntialiasingStyles {textAntialiasing}.", nameof(textAntialiasing))
          };

      drawingSession.DrawTextLayout(
        drawableCell.CanvasTextLayout,
        MathF.Round(drawableCell.Point.X * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        MathF.Round(drawableCell.Point.Y * (drawingSession.Dpi / dpiConstant)) / (drawingSession.Dpi / dpiConstant),
        drawableCell.Cell.GraphicRendition.Inverse ^ drawableCell.Cell.Selected
          ? drawableCell.Cell.GraphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible, honorBackgroundIsInvisible: false)
          : drawableCell.Cell.GraphicRendition.CalculatedForegroundColor()
      );
    }

    /// <summary>
    /// Draws <paramref name="drawableCell"/>'s decorations to <paramref
    /// name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession">The draw loop's <see
    /// cref="CanvasDrawingSession"/>.</param>
    /// <param name="drawableCell">The cell to draw.</param>
    private void DrawDecoration(CanvasDrawingSession drawingSession, DrawableCell drawableCell) {
      // Single underline (or double underline)
      if (
        (
          drawableCell.Cell.GraphicRendition.Underline
          && (
            drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Single
            || drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Double
          )
        ) || drawableCell.Cell.GraphicRendition.DoubleUnderline
      ) {
        DrawTextLine(
          drawingSession,
          drawableCell,
          (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationBottom),
          useUnderlineColor: true
        );
      }

      // Crossed-out
      if (drawableCell.Cell.GraphicRendition.CrossedOut) {
        DrawTextLine(
          drawingSession,
          drawableCell,
          (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationMidpoint),
          useUnderlineColor: false
        );
      }

      // Double underline
      if (
        drawableCell.Cell.GraphicRendition.DoubleUnderline
        || (
          drawableCell.Cell.GraphicRendition.Underline
          && drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Double
        )
      ) {
        DrawTextLine(
          drawingSession,
          drawableCell,
          (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom),
          useUnderlineColor: true
        );
      }

      // Undercurl
      if (
        drawableCell.Cell.GraphicRendition.Underline
        && drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Undercurl
      ) {
        DrawUndercurl(
          drawingSession,
          drawableCell
        );
      }
    }

    /// <summary>
    /// Draws a text line to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="drawableCell"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='cell']"/></param>
    /// <param name="underlineY">The Y location at which to draw the
    /// underline.</param>
    /// <param name="useUnderlineColor">Whether to use the underline color
    /// (<see langword="true"/>) or the foreground color (<see
    /// langword="false"/>).</param>
    private void DrawTextLine(CanvasDrawingSession drawingSession, DrawableCell drawableCell, float underlineY, bool useUnderlineColor) {
      drawingSession.DrawLine(
        drawableCell.Caret.Column * CellSize.Width,
        underlineY,
        (drawableCell.Caret.Column * CellSize.Width) + CellSize.Width,
        underlineY,
        useUnderlineColor
          ? drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor()
          : drawableCell.Cell.GraphicRendition.CalculatedForegroundColor(),
        (float) terminalEngine.FontSize * decorationWeight
      );
    }

    /// <summary>
    /// Draws a text undercurl to <paramref name="drawingSession"/>.
    /// </summary>
    /// <remarks>Undercurl is drawn like a small w under each
    /// character.</remarks>
    /// <param name="drawingSession"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="drawableCell"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='cell']"/></param>
    private void DrawUndercurl(CanvasDrawingSession drawingSession, DrawableCell drawableCell) {
      // The top-left point of our w
      Vector2 pointA = new(
        drawableCell.Caret.Column * CellSize.Width,
        (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      // The bottom of the first half of our w
      Vector2 pointB = new(
        (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.25f),
        (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationBottom)
      );

      // The top midpoint of our w
      Vector2 pointC = new(
        (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.5f),
        (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      // The bottom of the second half of our w
      Vector2 pointD = new(
        (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.75f),
        (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationBottom)
      );

      // The top-right point of our w
      Vector2 pointE = new(
        (drawableCell.Caret.Column * CellSize.Width) + CellSize.Width,
        (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      drawingSession.DrawLine(
        pointA,
        pointB,
        drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointB,
        pointC,
        drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointC,
        pointD,
        drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointD,
        pointE,
        drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );
    }
  }
}
