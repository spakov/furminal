using AnsiProcessor.Output;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    private readonly Dictionary<(int, bool, bool, bool), CanvasTextLayout> canvasTextLayoutCache = [];
    private readonly Dictionary<(int, bool, bool, bool), RectF> overfillCache = [];

    private SizeF _cellSize;
    private bool _cellSizeDirty;

    private CanvasRenderTarget? offscreenBuffer;
    private CanvasRenderTarget? lastFrameOffscreenBuffer;
    private List<Cell[]>? lastFrameScreenBuffer;
    private bool lastFrameBackgroundIsInvisible;

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
    /// The terminal cell size.
    /// </summary>
    internal SizeF CellSize {
      get => _cellSize;
      set => _cellSize = value;
    }

    /// <summary>
    /// Whether the terminal cell size needs to be recalculated.
    /// </summary>
    internal bool CellSizeDirty {
      get => _cellSizeDirty;
      set => _cellSizeDirty = value;
    }

    /// <summary>
    /// The offscreen buffer.
    /// </summary>
    internal CanvasRenderTarget? OffscreenBuffer => offscreenBuffer;

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
        _refreshRate = value;

#if DEBUG
        logger.LogInformation("Refresh rate is {refreshRate:F} Hz", _refreshRate);
#endif
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

            lock (terminalEngine.ScreenBufferLock) {
              DrawFrame();
            }
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
        offscreenBuffer = new(terminalEngine.Canvas, terminalEngine.NominalSizeInPixels.ToSize());
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
        canvasTextLayout.DrawBounds.Width,
        canvasTextLayout.DrawBounds.Height
      );

#if DEBUG
      logger.LogInformation("Measured cell size: ({width}, {height})", CellSize.Width, CellSize.Height);
#endif

      CellSizeDirty = false;
    }

    /// <summary>
    /// Draws a terminal frame.
    /// </summary>
    /// <remarks>Does not compose the cursor—that is handled by <see
    /// cref="TerminalControl.Canvas_Draw"/>.</remarks>
    private void DrawFrame() {
      if (offscreenBuffer is null) return;

      int rows = terminalEngine.Rows;
      int columns = terminalEngine.Columns;
      System.Drawing.Color defaultBackgroundColor = terminalEngine.Palette.DefaultBackgroundColor;
      bool backgroundIsInvisible = terminalEngine.BackgroundIsInvisible;

      using (CanvasDrawingSession drawingSession = offscreenBuffer.CreateDrawingSession()) {
        RectF overfill;

        for (int row = 0; row < rows; row++) {
          overfill = new();

          for (int column = 0; column < columns; column++) {
            // These rules were *atrocious* to work out, but seem to handle
            // cell-tracked change draw updates nicely
            if (
              // 1. Always redraw if our offscreen buffer was recreated
              lastFrameOffscreenBuffer != offscreenBuffer

              // 2. Always redraw on the first frame
              || lastFrameScreenBuffer is null

              // 3. Always redraw if BackgroundIsInvisible changed
              || lastFrameBackgroundIsInvisible != backgroundIsInvisible

              // 4. Always redraw if our screen buffer grew
              || row > lastFrameScreenBuffer.Count - 1
              || column > lastFrameScreenBuffer[row].Length - 1

              // 5. Always redraw if this cell changed
              || terminalEngine.VideoTerminal[row][column] != lastFrameScreenBuffer[row][column]

              // 6. Always redraw if the cell above this one had overfill on
              //    the last frame
              /*|| (
                row > 0
                && lastFrameScreenBuffer[row - 1][column].ContainsOverfillFromBelow
              )

              // 7. Always redraw if the cell before this one had overfill on
              //    the last frame
              || (
                column > 0
                && lastFrameScreenBuffer[row][column - 1].ContainsOverfillFromAfter
              )

              // 8. Always redraw if the cell after this one had overfill on
              //    the last frame
              || (
                column < columns - 1
                && lastFrameScreenBuffer[row][column + 1].ContainsOverfillFromBefore
              )

              // 9. Always redraw if the cell below this one had overfill on
              //    the last frame
              || (
                row < rows - 1
                && lastFrameScreenBuffer[row + 1][column].ContainsOverfillFromAbove
              )

              // 10. Always redraw if this cell is not null and contained
              //     overfill on the last frame
              || (
                terminalEngine.VideoTerminal[row][column].Rune is not null
                && (
                  lastFrameScreenBuffer[row][column].ContainsOverfillFromBelow
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromAfter
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromBefore
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromAbove
                )
              )

              // 11. Always redraw if this cell is null, contained overfill on
              //     the last draw, and does not contain overfill now
              || (
                (
                  terminalEngine.VideoTerminal[row][column].Rune is null
                  || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row][column].Rune!)
                )
                && (
                  lastFrameScreenBuffer[row][column].ContainsOverfillFromBelow
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromAfter
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromBefore
                  || lastFrameScreenBuffer[row][column].ContainsOverfillFromAbove
                )
                && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromBelow
                && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromAfter
                && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromBefore
                && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromAbove
              )*/
            ) {
              if (terminalEngine.VideoTerminal[row][column].Rune is not null) {
                float underfill = overfill.Right;

                overfill = DrawCell(
                  drawingSession,
                  defaultBackgroundColor,
                  backgroundIsInvisible,
                  terminalEngine.VideoTerminal[row][column],
                  row,
                  column,
                  overfill.Right
                );

                // Special case 1: if we just drew a cell and had overfill to
                // the top and the cell above it is null, we must draw the
                // null cell above this one, then redraw this cell
                // ISSUE: will overwrite existing overfill
                /*if (
                  overfill.Top > 0.0f
                  && row > 0
                  && (
                    terminalEngine.VideoTerminal[row - 1][column].Rune is null
                    || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row - 1][column].Rune!)
                  )
                ) {
                  DrawNullCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    row - 1,
                    column,
                    terminalEngine.VideoTerminal[row - 1][column].GraphicRendition
                  );

                  overfill = DrawCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    terminalEngine.VideoTerminal[row][column],
                    row,
                    column,
                    underfill
                  );
                }

                // Special case 2: if we just drew a cell and had overfill to
                // the left and the cell before it is null, we must draw the
                // null cell before this one, then redraw this cell
                // ISSUE: will overwrite existing overfill
                if (
                  overfill.Left > 0.0f
                  && column > 0
                  && (
                    terminalEngine.VideoTerminal[row][column - 1].Rune is null
                    || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row][column - 1].Rune!)
                  )
                ) {
                  DrawNullCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    row,
                    column - 1,
                    terminalEngine.VideoTerminal[row][column - 1].GraphicRendition
                  );

                  overfill = DrawCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    terminalEngine.VideoTerminal[row][column],
                    row,
                    column,
                    underfill
                  );
                }

                // Special case 3: if we just drew a cell and had overfill to
                // the right and the cell after it is null, we must draw the
                // null cell after this one, then redraw this cell
                if (
                  overfill.Right > 0.0f
                  && column < columns - 1
                  && (
                    terminalEngine.VideoTerminal[row][column + 1].Rune is null
                    || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row][column + 1].Rune!)
                  )
                ) {
                  DrawNullCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    row,
                    column + 1,
                    terminalEngine.VideoTerminal[row][column + 1].GraphicRendition
                  );

                  overfill = DrawCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    terminalEngine.VideoTerminal[row][column],
                    row,
                    column,
                    underfill
                  );
                }

                // Special case 4: if we just drew a cell and had overfill to
                // the bottom and the cell below it is null, we must draw the
                // null cell below this one, then redraw this cell
                if (
                  overfill.Bottom > 0.0f
                  && row < rows - 1
                  && (
                    terminalEngine.VideoTerminal[row + 1][column].Rune is null
                    || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row + 1][column].Rune!)
                  )
                ) {
                  DrawNullCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    row + 1,
                    column,
                    terminalEngine.VideoTerminal[row + 1][column].GraphicRendition
                  );

                  overfill = DrawCell(
                    drawingSession,
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    terminalEngine.VideoTerminal[row][column],
                    row,
                    column,
                    underfill
                  );
                }*/
              } else {
                DrawNullCell(
                  drawingSession,
                  defaultBackgroundColor,
                  backgroundIsInvisible,
                  row,
                  column,
                  terminalEngine.VideoTerminal[row][column].GraphicRendition,
                  overfill.Right
                );

                overfill = new();
              }

              if (row > 0) {
                terminalEngine.VideoTerminal[row - 1][column].ContainsOverfillFromBelow = overfill.Top > 0.0f;
              }

              if (column > 0) {
                terminalEngine.VideoTerminal[row][column - 1].ContainsOverfillFromAfter = overfill.Left > 0.0f;
              }

              if (column < columns - 1) {
                terminalEngine.VideoTerminal[row][column + 1].ContainsOverfillFromBefore = overfill.Right > 0.0f;
              }

              if (row < rows - 1) {
                terminalEngine.VideoTerminal[row + 1][column].ContainsOverfillFromAbove = overfill.Bottom > 0.0f;
              }
            }
          }
        }

        // Clean up any left-over artifacts
        /*for (int row = 0; row < rows; row++) {
          for (int column = 0; column < columns; column++) {
            if (
              // Special case 5: if this cell is null and does not contain
              // overfill now, redraw it
              (
                terminalEngine.VideoTerminal[row][column].Rune is null
                || Rune.IsWhiteSpace((Rune) terminalEngine.VideoTerminal[row][column].Rune!)
              )
              && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromBelow
              && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromAfter
              && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromBefore
              && !terminalEngine.VideoTerminal[row][column].ContainsOverfillFromAbove
            ) {
              DrawNullCell(
                drawingSession,
                defaultBackgroundColor,
                backgroundIsInvisible,
                row,
                column,
                terminalEngine.VideoTerminal[row][column].GraphicRendition
              );
            }
          }
        }*/
      }

      lastFrameOffscreenBuffer = offscreenBuffer;
      lastFrameScreenBuffer = [];

      for (int row = 0; row < rows; row++) {
        lastFrameScreenBuffer.Add(new Cell[columns]);

        for (int column = 0; column < columns; column++) {
          lastFrameScreenBuffer[row][column] = terminalEngine.VideoTerminal[row][column];
        }
      }

      lastFrameBackgroundIsInvisible = backgroundIsInvisible;

      terminalEngine.InvalidateCanvas();
    }

    /// <summary>
    /// Draws <paramref name="cell"/> to <paramref name="drawingSession"/>.
    /// </summary>
    /// <remarks>
    /// <para>It is assumed that <paramref name="cell"/>'s <see
    /// cref="Cell.Rune"/> is not <see langword="null"/>.</para>
    /// </remarks>
    /// <param name="drawingSession">A <see
    /// cref="CanvasDrawingSession"/>.</param>
    /// <param name="defaultBackgroundColor">The default background color with
    /// which to draw.</param>
    /// <param name="backgroundIsInvisible">Whether the background, if
    /// <paramref name="defaultBackgroundColor"/>, should be drawn as
    /// transparent.</param>
    /// <param name="cell">The <see cref="Cell"/> to draw.</param>
    /// <param name="row">The row at which <paramref name="cell"/> should be
    /// drawn.</param>
    /// <param name="col">The column at which <paramref name="cell"/> should be
    /// drawn.</param>
    /// <param name="underfill">The overfill of the previous character, which
    /// will not be drawn over.</param>
    /// <returns>A <see cref="RectF"/> containing any overfill beyond <see
    /// cref="CellSize"/> that was used in each direction, or <c>0.0f</c> if
    /// none.</returns>
    private RectF DrawCell(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, Cell cell, int row, int col, float underfill = 0.0f) {
      Vector2 point = new(
        col * CellSize.Width,
        row * CellSize.Height
      );

      int runeIndex = cell.Rune is null ? -1 : ((Rune) cell.Rune).Value;
      bool bold = cell.GraphicRendition.Bold;
      bool faint = cell.GraphicRendition.Faint;
      bool italic = cell.GraphicRendition.Italic;

      if (!canvasTextLayoutCache.TryGetValue((runeIndex, bold, faint, italic), out CanvasTextLayout? canvasTextLayout)) {
        canvasTextLayout = new(
          drawingSession,
          cell.Rune.ToString(),
          cell.GraphicRendition.TextFormat(this),
          0.0f,
          0.0f
        );

        canvasTextLayoutCache.Add((runeIndex, bold, faint, italic), canvasTextLayout);
      }

      if (!overfillCache.TryGetValue((runeIndex, bold, faint, italic), out RectF overfill)) {
        float overfillTop = Math.Abs(
          Math.Min(
            (float) canvasTextLayout.DrawBounds.Y,
            0.0f
          )
        );

        float overfillLeft = Math.Abs(
          Math.Min(
            (float) canvasTextLayout.DrawBounds.X,
            0.0f
          )
        );

        float overfillRight = Math.Max(
          Math.Max(
            (float) (canvasTextLayout.DrawBounds.X + canvasTextLayout.DrawBounds.Width),
            (float) canvasTextLayout.LayoutBounds.Width
          ) - CellSize.Width,
          0.0f
        );

        float overfillBottom = Math.Max(
          Math.Max(
            (float) (canvasTextLayout.DrawBounds.Y + canvasTextLayout.DrawBounds.Height),
            (float) canvasTextLayout.LayoutBounds.Height
          ) - CellSize.Height,
          0.0f
        );

        overfill = new(overfillTop, overfillLeft, overfillRight, overfillBottom);

        overfillCache.Add((runeIndex, bold, faint, italic), overfill);
      }

#if DEBUG
      logger.LogTrace("Rune '{rune}' yields width = {width}", cell.Rune.ToString(), canvasTextLayout.DrawBounds.Width);

      if (overfill.Top > 0.0f || overfill.Left > 0.0f || overfill.Right > 0.0f || overfill.Bottom > 0.0f) {
        logger.LogTrace("Rune '{rune}' resulted in overfill:", cell.Rune.ToString());
        logger.LogTrace("  Top: {top}", overfill.Top);
        logger.LogTrace("  Left: {left}", overfill.Left);
        logger.LogTrace("  Right: {right}", overfill.Right);
        logger.LogTrace("  Bottom: {bottom}", overfill.Bottom);
      }
#endif

      DrawBackground(
        drawingSession,
        defaultBackgroundColor,
        backgroundIsInvisible,
        cell,
        point,
        underfill,
        overfill.Right
      );

      DrawForeground(
        drawingSession,
        defaultBackgroundColor,
        backgroundIsInvisible,
        cell,
        canvasTextLayout,
        point
      );

      DrawDecoration(
        drawingSession,
        cell,
        row,
        col
      );

      return overfill;
    }

    /// <summary>
    /// Draws a null cell to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession">A <see
    /// cref="CanvasDrawingSession"/>.</param>
    /// <param name="defaultBackgroundColor">The default background color with
    /// which to draw.</param>
    /// <param name="backgroundIsInvisible">Whether the background, if
    /// <paramref name="defaultBackgroundColor"/>, should be drawn as
    /// transparent.</param>
    /// <param name="row">The row at which <paramref name="cell"/> should be
    /// drawn.</param>
    /// <param name="col">The column at which <paramref name="cell"/> should be
    /// drawn.</param>
    /// <param name="graphicRendition">The <see cref="GraphicRendition"/>
    /// to use.</param>
    /// <param name="underfill">The overfill of the previous character, which
    /// will not be drawn over.</param>
    private void DrawNullCell(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, int row, int col, GraphicRendition graphicRendition, float underfill = 0.0f) {
      Vector2 point = new(
        col * CellSize.Width,
        row * CellSize.Height
      );

      DrawNullBackground(
        drawingSession,
        graphicRendition,
        defaultBackgroundColor,
        backgroundIsInvisible,
        point,
        underfill
      );
    }

    /// <summary>
    /// Draws the cell background to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawCell"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="defaultBackgroundColor"><inheritdoc cref="DrawCell"
    /// path="/param[@name='defaultBackgroundColor']"/></param>
    /// <param name="backgroundIsInvisible"><inheritdoc cref="DrawCell"
    /// path="/param[@name='backgroundIsInvisible']"/></param>
    /// <param name="cell"><inheritdoc cref="DrawCell"
    /// path="/param[@name='cell']"/></param>
    /// <param name="point">The point at which the background should be
    /// drawn.</param>
    /// <param name="underfill">The overfill of the previous character, which
    /// will not be drawn over.</param>
    /// <param name="overfill">The overfill of the current character.</param>
    private void DrawBackground(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, Cell cell, Vector2 point, float underfill, float overfill) {
      Color calculatedColor = cell.GraphicRendition.Inverse ^ cell.Selected
        ? cell.GraphicRendition.CalculatedForegroundColor()
        : cell.GraphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible);

      drawingSession.Antialiasing = CanvasAntialiasing.Aliased;
      drawingSession.Blend = CanvasBlend.Copy;

      drawingSession.FillRectangle(
        point.X + underfill,
        point.Y,
        CellSize.Width - underfill + overfill,
        CellSize.Height,
        calculatedColor
      );

      drawingSession.Blend = CanvasBlend.SourceOver;
      drawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
    }

    /// <summary>
    /// Draws a null cell background to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawNullCell"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="graphicRendition"><inheritdoc cref="DrawNullCell"
    /// path="/param[@name='graphicRendition']"/></param>
    /// <param name="defaultBackgroundColor"><inheritdoc cref="DrawNullCell"
    /// path="/param[@name='defaultBackgroundColor']"/></param>
    /// <param name="backgroundIsInvisible"><inheritdoc cref="DrawNullCell"
    /// path="/param[@name='backgroundIsInvisible']"/></param>
    /// <param name="point">The point at which the background should be
    /// drawn.</param>
    /// <param name="underfill">The overfill of the previous character, which
    /// will not be drawn over.</param>
    private void DrawNullBackground(CanvasDrawingSession drawingSession, GraphicRendition graphicRendition, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, Vector2 point, float underfill) {
      Color calculatedColor = graphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible);

      drawingSession.Antialiasing = CanvasAntialiasing.Aliased;
      drawingSession.Blend = CanvasBlend.Copy;

      drawingSession.FillRectangle(
        point.X,
        point.Y,
        CellSize.Width - underfill,
        CellSize.Height,
        calculatedColor
      );

      drawingSession.Blend = CanvasBlend.SourceOver;
      drawingSession.Antialiasing = CanvasAntialiasing.Antialiased;
    }

    /// <summary>
    /// Draws the cell foreground to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawCell"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="defaultBackgroundColor"><inheritdoc cref="DrawCell"
    /// path="/param[@name='defaultBackgroundColor']"/></param>
    /// <param name="backgroundIsInvisible"><inheritdoc cref="DrawCell"
    /// path="/param[@name='backgroundIsInvisible']"/></param>
    /// <param name="cell"><inheritdoc cref="DrawCell"
    /// path="/param[@name='cell']"/></param>
    /// <param name="canvasTextLayout">The <see cref="CanvasTextLayout"/>
    /// representing the cell's contents.</param>
    /// <param name="point">The point at which the foreground should be
    /// drawn.</param>
    private static void DrawForeground(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, Cell cell, CanvasTextLayout canvasTextLayout, Vector2 point) {
      if (cell.Rune is null) return;

      // Always present box-drawing and block-element characters as aliased
      drawingSession.TextAntialiasing = ((Rune) cell.Rune!).Value is (>= 0x2500 and <= 0x257f) or (>= 0x2580 and <= 0x259f)
        ? CanvasTextAntialiasing.Aliased
        : CanvasTextAntialiasing.Grayscale;

      drawingSession.DrawTextLayout(
        canvasTextLayout,
        point,
        cell.GraphicRendition.Inverse ^ cell.Selected
          ? cell.GraphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible, honorBackgroundIsInvisible: false)
          : cell.GraphicRendition.CalculatedForegroundColor()
      );
    }

    /// <summary>
    /// Draws the cell decorations to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawCell"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="cell"><inheritdoc cref="DrawCell"
    /// path="/param[@name='cell']"/></param>
    /// <param name="row">The row at which the decoration should be
    /// drawn.</param>
    /// <param name="col">The column at which the decoration should be
    /// drawn.</param>
    private void DrawDecoration(CanvasDrawingSession drawingSession, Cell cell, int row, int col) {
      // Single underline (or double underline)
      if (
        (
          cell.GraphicRendition.Underline
          && (
            cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Single
            || cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Double
          )
        ) || cell.GraphicRendition.DoubleUnderline
      ) {
        DrawTextLine(
          drawingSession,
          cell,
          col,
          (row * CellSize.Height) + (CellSize.Height * decorationBottom),
          useUnderlineColor: true
        );
      }

      // Crossed-out
      if (cell.GraphicRendition.CrossedOut) {
        DrawTextLine(
          drawingSession,
          cell,
          col,
          (row * CellSize.Height) + (CellSize.Height * decorationMidpoint),
          useUnderlineColor: false
        );
      }

      // Double underline
      if (
        cell.GraphicRendition.DoubleUnderline
        || (
          cell.GraphicRendition.Underline
          && cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Double
        )
      ) {
        DrawTextLine(
          drawingSession,
          cell,
          col,
          (row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom),
          useUnderlineColor: true
        );
      }

      // Undercurl
      if (
        cell.GraphicRendition.Underline
        && cell.GraphicRendition.UnderlineStyle == UnderlineStyles.Undercurl
      ) {
        DrawUndercurl(
          drawingSession,
          cell,
          row,
          col
        );
      }
    }

    /// <summary>
    /// Draws a text line to <paramref name="drawingSession"/>.
    /// </summary>
    /// <param name="drawingSession"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='drawingSession']"/></param>
    /// <param name="cell"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='cell']"/></param>
    /// <param name="col"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='col']"/></param>
    /// <param name="underlineY">The Y location at which to draw the
    /// underline.</param>
    /// <param name="useUnderlineColor">Whether to use the underline color
    /// (<see langword="true"/>) or the foreground color (<see
    /// langword="false"/>).</param>
    private void DrawTextLine(CanvasDrawingSession drawingSession, Cell cell, int col, float underlineY, bool useUnderlineColor) {
      drawingSession.DrawLine(
        col * CellSize.Width,
        underlineY,
        (col * CellSize.Width) + CellSize.Width,
        underlineY,
        useUnderlineColor
          ? cell.GraphicRendition.UnderlineColor.ToWindowsUIColor()
          : cell.GraphicRendition.CalculatedForegroundColor(),
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
    /// <param name="cell"><inheritdoc cref="DrawDecoration"
    /// path="/param[@name='cell']"/></param>
    /// <param name="row">The row at which the undercurl should be
    /// drawn.</param>
    /// <param name="col">The column at which the undercurl should be
    /// drawn.</param>
    private void DrawUndercurl(CanvasDrawingSession drawingSession, Cell cell, int row, int col) {
      // The top-left point of our w
      Vector2 pointA = new(
        col * CellSize.Width,
        (row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      // The bottom of the first half of our w
      Vector2 pointB = new(
        (col * CellSize.Width) + (CellSize.Width * 0.25f),
        (row * CellSize.Height) + (CellSize.Height * decorationBottom)
      );

      // The top midpoint of our w
      Vector2 pointC = new(
        (col * CellSize.Width) + (CellSize.Width * 0.5f),
        (row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      // The bottom of the second half of our w
      Vector2 pointD = new(
        (col * CellSize.Width) + (CellSize.Width * 0.75f),
        (row * CellSize.Height) + (CellSize.Height * decorationBottom)
      );

      // The top-right point of our w
      Vector2 pointE = new(
        (col * CellSize.Width) + CellSize.Width,
        (row * CellSize.Height) + (CellSize.Height * decorationAlmostBottom)
      );

      drawingSession.DrawLine(
        pointA,
        pointB,
        cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointB,
        pointC,
        cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointC,
        pointD,
        cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );

      drawingSession.DrawLine(
        pointD,
        pointE,
        cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
        (float) terminalEngine.FontSize * decorationUndercurlWeight
      );
    }
  }
}
