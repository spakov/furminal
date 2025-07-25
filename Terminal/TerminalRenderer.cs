using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Spakov.AnsiProcessor.Output;
using Spakov.Terminal.Helpers;
using Spakov.WideCharacter;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Text;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;

namespace Spakov.Terminal
{
    /// <summary>
    /// A terminal renderer, responsible for drawing operations.
    /// </summary>
    /// <remarks>Does not interact with the UI thread.</remarks>
    internal class TerminalRenderer
    {
        /// <summary>
        /// The <see cref="GetMonitorInfoW"/> function retrieves information
        /// about a display monitor.
        /// </summary>
        /// <remarks>Implements managed support for a call with <see
        /// cref="MONITORINFOEXW"/>.</remarks>
        /// <param name="hMonitor">A handle to the display monitor of
        /// interest.</param>
        /// <param name="lpmi">
        /// <para>A pointer to a <c>MONITORINFO</c> or <see
        /// cref="MONITORINFOEXW"/> structure that receives information about
        /// the specified display monitor.</para>
        /// <para>You must set the <c>cbSize</c> member of the structure to
        /// <c>sizeof(MONITORINFO)</c> or <c>sizeof(MONITORINFOEXW)</c> before
        /// calling the <see cref="GetMonitorInfoW"/> function. Doing so lets
        /// the function determine the type of structure you are passing to
        /// it.</para>
        /// <para>The <see cref="MONITORINFOEXW"/> structure is a superset of
        /// the <c>MONITORINFO</c> structure. It has one additional member: a
        /// string that contains a name for the display monitor. Most
        /// applications have no use for a display monitor name, and so can
        /// save some bytes by using a <c>MONITORINFO</c> structure.</para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero.</para>
        /// </returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        public static extern bool GetMonitorInfoW(IntPtr hMonitor, ref MONITORINFOEXW lpmi);
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
#pragma warning restore IDE0079 // Remove unnecessary suppression

        internal readonly ILogger? logger;

        /// <summary>
        /// The Windows DPI scale factor constant.
        /// </summary>
        private const float DpiConstant = 96.0f;

        /// <summary>
        /// A full-block character, used to calculate the size of a cell.
        /// </summary>
        private const string Em = "█";

        /// <summary>
        /// The midpoint of text to decorate, as a fraction of cell height.
        /// </summary>
        private const float DecorationMidpoint = 0.5f;

        /// <summary>
        /// The "almost bottom" of text to decorate, as a fraction of cell height.
        /// </summary>
        private const float DecorationAlmostBottom = 0.85f;

        /// <summary>
        /// The bottom of text to decorate, as a fraction of cell height.
        /// </summary>
        private const float DecorationBottom = 1.0f;

        /// <summary>
        /// The line weight of text decorations, as a fraction of font size.
        /// </summary>
        private const float DecorationWeight = 0.05f;

        /// <summary>
        /// The line weight of undercurl text decorations, as a fraction of font
        /// size.
        /// </summary>
        private const float DecorationUndercurlWeight = 0.05f;

        private readonly TerminalEngine _terminalEngine;

        private readonly CanvasTextFormat[] _textFormats = new CanvasTextFormat[0x10];

        private readonly Dictionary<CellFingerprint, CanvasTextLayout?> _canvasTextLayoutCache;
        private readonly Dictionary<CellFingerprint, RectF> _overfillCache;

        private SizeF _cellSize;
        private bool _cellSizeDirty;

        private float _effectiveFrameRate;

        private CanvasRenderTarget? _offscreenBuffer;
        private bool _offscreenBufferDirty;
        private Caret _lastFrameBounds;
        private Cell[,] _lastFrameCells;
        private readonly Queue<DrawableCell> _drawableCells;
        private readonly Queue<DrawableCell> _drawableCellForegrounds;
        private readonly HashSet<DrawableCell> _seenDrawableCells;

        private bool _cursorDisplayed;
        private bool _cursorVisible;

        private double _refreshRate;

        /// <summary>
        /// The terminal text formats.
        /// </summary>
        /// <remarks>
        /// <para>This is an array of seven <see
        /// cref="CanvasTextFormat"/>s:</para>
        /// <list type="bullet">
        /// <item><c>0x00</c>: plain</item>
        /// <item><c>0x01</c>: bold</item>
        /// <item><c>0x02</c>: faint</item>
        /// <item><c>0x03</c>: unused</item>
        /// <item><c>0x04</c>: italic</item>
        /// <item><c>0x05</c>: bold and italic</item>
        /// <item><c>0x06</c>: faint and italic</item>
        /// <item><c>0x0f</c>: emoji</item>
        /// </list>
        /// </remarks>
        internal CanvasTextFormat[] TextFormats => _textFormats;

        /// <summary>
        /// The <see cref="CanvasTextLayout"/> cache.
        /// </summary>
        internal Dictionary<CellFingerprint, CanvasTextLayout?> CanvasTextLayoutCache => _canvasTextLayoutCache;

        /// <summary>
        /// The overfill cache.
        /// </summary>
        internal Dictionary<CellFingerprint, RectF> OverfillCache => _overfillCache;

        /// <inheritdoc cref="TerminalEngine.FullColorEmoji"/>
        internal bool FullColorEmoji => _terminalEngine.FullColorEmoji;

        /// <summary>
        /// The terminal cell size.
        /// </summary>
        internal SizeF CellSize
        {
            get => _cellSize;
            set => _cellSize = value;
        }

        /// <summary>
        /// Whether the terminal cell size needs to be recalculated.
        /// </summary>
        internal bool CellSizeDirty
        {
            get => _cellSizeDirty;
            set => _cellSizeDirty = value;
        }

        /// <summary>
        /// The offscreen buffer.
        /// </summary>
        internal CanvasRenderTarget? OffscreenBuffer => _offscreenBuffer;

        /// <summary>
        /// Whether the offscreen buffer needs to be redrawn.
        /// </summary>
        internal bool OffscreenBufferDirty
        {
            get => _offscreenBufferDirty;
            set => _offscreenBufferDirty = value;
        }

        /// <summary>
        /// Whether the cursor is to be displayed on the next Draw.
        /// </summary>
        /// <remarks>This is used by the drawing routines to toggle the
        /// cursor's visibility to facilitate blinking.</remarks>
        internal bool CursorDisplayed
        {
            get => _cursorDisplayed;
            set => _cursorDisplayed = value;
        }

        /// <summary>
        /// Whether the cursor is visible.
        /// </summary>
        /// <remarks>This differs from <see cref="CursorDisplayed"/> in that it
        /// is controlled via CSI DECSET escape sequence <see
        /// cref="AnsiProcessor.Ansi.EscapeSequences.Extensions.CSI_DECSET.DECSET_DECTCEM"
        /// /> and overrides <see cref="CursorDisplayed"/>.</remarks>
        internal bool CursorVisible
        {
            get => _cursorVisible;
            set => _cursorVisible = value;
        }

        /// <summary>
        /// The window's monitor's refresh rate.
        /// </summary>
        private double RefreshRate
        {
            get => _refreshRate;

            set
            {
                if (_refreshRate != value)
                {
                    _refreshRate = value;
                    logger?.LogInformation("Refresh rate is {refreshRate:F} Hz", _refreshRate);
                }
            }
        }

        /// <summary>
        /// Initializes a <see cref="TerminalRenderer"/>.
        /// </summary>
        internal TerminalRenderer(TerminalEngine terminalEngine)
        {
            logger = LoggerHelper.CreateLogger<TerminalRenderer>();

            _terminalEngine = terminalEngine;

            InitializeTextFormats();

            _canvasTextLayoutCache = [];
            _overfillCache = [];

            _lastFrameBounds = new(-1, -1);
            _lastFrameCells = new Cell[0, 0];
            _drawableCells = [];
            _drawableCellForegrounds = [];
            _seenDrawableCells = [];

            // This will be updated when the TerminalControl is added to the XAML
            // tree
            _refreshRate = 60.0;

            Task.Run(() =>
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                long lastFrameTicks = stopwatch.ElapsedTicks;
                long nominalTicksPerFrame = 0;
                float lastRefreshRate = 0.0f;
                long effectiveTicksPerFrame = 0;

                _effectiveFrameRate = 0.0f;

                while (true)
                {
                    if (lastRefreshRate != RefreshRate)
                    {
                        nominalTicksPerFrame = (int)(Stopwatch.Frequency / RefreshRate);
                        lastRefreshRate = (float)RefreshRate;
                    }

                    if (_effectiveFrameRate == 0.0f)
                    {
                        _effectiveFrameRate = lastRefreshRate;
                        effectiveTicksPerFrame = nominalTicksPerFrame;
                    }

                    long nowTicks = stopwatch.ElapsedTicks;

                    if (nowTicks - lastFrameTicks >= effectiveTicksPerFrame)
                    {
                        lastFrameTicks = nowTicks;

                        terminalEngine.DispatcherQueue.TryEnqueue(() =>
                        {
                            lock (terminalEngine.ScreenBufferLock)
                            {
                                DrawFrame();
                            }
                        });

                        if (terminalEngine.VTQueue.Count > 50)
                        {
                            // We're pushing too hard, so cut our frame rate
                            if (_effectiveFrameRate > 0.0)
                            {
                                if (_effectiveFrameRate > 1.0)
                                {
                                    _effectiveFrameRate--;
                                }
                                else
                                {
                                    _effectiveFrameRate /= 2;
                                }

                                effectiveTicksPerFrame = (int)(Stopwatch.Frequency / _effectiveFrameRate);
                            }
                        }
                        else
                        {
                            // Increase our frame rate, if feasible
                            if (nowTicks - lastFrameTicks < effectiveTicksPerFrame * 2 && _effectiveFrameRate < lastRefreshRate)
                            {
                                if (lastRefreshRate - _effectiveFrameRate < 1.0f)
                                {
                                    _effectiveFrameRate = lastRefreshRate;
                                }
                                else
                                {
                                    _effectiveFrameRate++;
                                }

                                effectiveTicksPerFrame = (int)(Stopwatch.Frequency / _effectiveFrameRate);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(1);
                    }
                }
            });
        }

        /// <summary>
        /// Calculates the cell size and resizes resources appropriately.
        /// </summary>
        /// <remarks>Intended to be invoked by <see cref="TerminalControl"/>
        /// when it attempts to draw but sees that <see cref="CellSizeDirty"/>
        /// is <see langword="true"/>.</remarks>
        internal void CleanCellSize()
        {
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
        internal void ResizeOffscreenBuffer(bool force = false)
        {
            if (_offscreenBuffer is not null || force)
            {
                _offscreenBufferDirty = true;
                _offscreenBuffer = new(_terminalEngine.Canvas, _terminalEngine.NominalSizeInPixels.ToSize());

                using CanvasDrawingSession drawingSession = _offscreenBuffer.CreateDrawingSession();

                drawingSession.Clear(Colors.Transparent);
            }
        }

        /// <summary>
        /// Initializes <see cref="CanvasTextFormat"/>s with the font-related
        /// properties configured in the <see cref="TerminalControl"/>.
        /// </summary>
        internal void InitializeTextFormats()
        {
            byte boldVariant = 0x01;
            byte faintVariant = 0x02;
            byte italicVariant = 0x04;

            for (byte i = 0x00; i < boldVariant + faintVariant + italicVariant; i++)
            {
                if (i == boldVariant + faintVariant)
                {
                    continue;
                }

                _textFormats[i] = new()
                {
                    FontFamily = _terminalEngine.FontFamily,
                    FontSize = (float)_terminalEngine.FontSize,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top
                };

                if ((i & boldVariant) != 0)
                {
                    _textFormats[i]!.FontWeight = new(700);
                }
                else if ((i & faintVariant) != 0)
                {
                    _textFormats[i]!.FontWeight = new(300);
                }

                if ((i & italicVariant) != 0)
                {
                    _textFormats[i]!.FontStyle = FontStyle.Italic;
                }
            }

            if (_terminalEngine.FullColorEmoji)
            {
                _textFormats[0x0f] = new()
                {
                    FontFamily = "Segoe UI Emoji",
                    FontSize = (float)_terminalEngine.FontSize,
                    Options = CanvasDrawTextOptions.EnableColorFont,
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top
                };
            }

            CellSizeDirty = true;
        }

        /// <summary>
        /// Invalidates the layout caches.
        /// </summary>
        /// <remarks>Intended to be invoked when the font changes.</remarks>
        internal void InvalidateLayoutCaches()
        {
            _canvasTextLayoutCache.Clear();
            _overfillCache.Clear();
        }

        /// <summary>
        /// Updates <see cref="RefreshRate"/> based on the monitor on which our
        /// window is displayed.
        /// </summary>
        internal void UpdateRefreshRate()
        {
            HMONITOR hMonitor = PInvoke.MonitorFromWindow(new(_terminalEngine.HWnd), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

            MONITORINFOEXW monitorInfoEx = new();
            monitorInfoEx.monitorInfo.cbSize = (uint)Marshal.SizeOf(monitorInfoEx);

            if (GetMonitorInfoW(hMonitor, ref monitorInfoEx))
            {
                DEVMODEW devMode = new();

                if (PInvoke.EnumDisplaySettings(monitorInfoEx.szDevice.ToString(), ENUM_DISPLAY_SETTINGS_MODE.ENUM_CURRENT_SETTINGS, ref devMode))
                {
                    RefreshRate = devMode.dmDisplayFrequency;

                    return;
                }
            }

            RefreshRate = 60.0;
        }

        /// <summary>
        /// Measures the size of one terminal cell based on <see cref="Em"/>.
        /// </summary>
        /// <remarks>The "plain" <see cref="_textFormats"/> is used for layout
        /// purposes. Italicized text, e.g., will still flow nicely out of
        /// bounds, if necessary.</remarks>
        private void MeasureCell()
        {
            CanvasTextLayout canvasTextLayout = new(
                _terminalEngine.Canvas.Device,
                Em,
                _textFormats[0],
                0.0f,
                0.0f
            );

            CellSize = new(
                MathF.Round((float)canvasTextLayout.DrawBounds.Width),
                MathF.Round((float)canvasTextLayout.DrawBounds.Height)
            );

            logger?.LogInformation("Measured cell size: ({width}, {height})", CellSize.Width, CellSize.Height);

            CellSizeDirty = false;
        }

        /// <summary>
        /// Draws a terminal frame.
        /// </summary>
        /// <remarks>
        /// <para>Assumptions:</para>
        /// <list type="bullet">
        /// <item>Intended to be executed on the UI thread.</item>
        /// <item>Each cell can only possibly overfill up to one cell away on
        /// each of its eight adjacent cells.</item>
        /// <item>Does not compose the cursor—that is handled by <see
        /// cref="TerminalControl.Canvas_Draw"/>.</item>
        /// </list>
        /// </remarks>
        private void DrawFrame()
        {
            if (_offscreenBuffer is null)
            {
                return;
            }

            int rows = _terminalEngine.Rows;
            int columns = _terminalEngine.Columns;
            TextAntialiasingStyle textAntialiasing = _terminalEngine.TextAntialiasing;
            System.Drawing.Color defaultBackgroundColor = _terminalEngine.Palette.DefaultBackgroundColor;
            bool backgroundIsInvisible = _terminalEngine.BackgroundIsInvisible;

            Cell[,] thisFrameCells = new Cell[rows, columns];
            bool frameChanged = false;
            _drawableCells.Clear();

            if (rows != _lastFrameBounds.Row || columns != _lastFrameBounds.Column || _offscreenBufferDirty)
            {
                frameChanged = true;
            }
            else
            {
                for (int row = 0; row < rows; row++)
                {
                    for (int column = 0; column < columns; column++)
                    {
                        if (row < _lastFrameBounds.Row && column < _lastFrameBounds.Column)
                        {
                            thisFrameCells[row, column] = column < _terminalEngine.VideoTerminal[row].Length
                                ? _terminalEngine.VideoTerminal[row][column]
                                : new();

                            if (_lastFrameCells[row, column] != thisFrameCells[row, column])
                            {
                                if (!frameChanged)
                                {
                                    frameChanged = true;
                                }
                            }
                        }
                        else
                        {
                            if (!frameChanged)
                            {
                                frameChanged = true;
                            }
                        }
                    }
                }
            }

            using (CanvasDrawingSession drawingSession = _offscreenBuffer.CreateDrawingSession())
            {
                if (frameChanged)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < columns; column++)
                        {
                            if (column >= _terminalEngine.VideoTerminal[row].Length)
                            {
                                continue;
                            }

                            Caret caret = new(row, column);
                            Vector2 point = new(column * CellSize.Width, row * CellSize.Height);
                            Cell cell = _terminalEngine.VideoTerminal[row][column];

                            if (
                                _offscreenBufferDirty
                                || rows >= _lastFrameBounds.Row
                                || columns >= _lastFrameBounds.Column
                            )
                            {
                                _drawableCells.Enqueue(
                                    new(
                                        this,
                                        drawingSession,
                                        caret,
                                        point,
                                        _terminalEngine.VideoTerminal[row][column]
                                    )
                                );

                                thisFrameCells[row, column] = _terminalEngine.VideoTerminal[row][column];
                            }
                            else
                            {
                                DrawableCell? upstairsLeftNeighbor = null;
                                DrawableCell? upstairsNeighbor = null;
                                DrawableCell? upstairsRightNeighbor = null;
                                DrawableCell? leftNeighbor = null;
                                DrawableCell? rightNeighbor = null;
                                DrawableCell? downstairsLeftNeighbor = null;
                                DrawableCell? downstairsNeighbor = null;
                                DrawableCell? downstairsRightNeighbor = null;
                                DrawableCell? thisCell = null;

                                // Check cells that differ from the previous
                                // frame
                                if (caret.Row < _lastFrameBounds.Row && caret.Column < _lastFrameBounds.Column)
                                {
                                    if (cell != _lastFrameCells[row, column])
                                    {
                                        // Check for overfill from the cell
                                        // that was here on the last frame
                                        if (_overfillCache.TryGetValue(new CellFingerprint(_lastFrameCells[row, column]), out RectF overfill))
                                        {
                                            // Redraw this cell and its
                                            // neighbors, but only if needed
                                            if (overfill.Top > 0.0f && row > 0)
                                            {
                                                upstairsNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column),
                                                    new(column * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsNeighbor);
                                            }

                                            if (overfill.Left > 0.0f && column > 0)
                                            {
                                                leftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row, column - 1),
                                                    new((column - 1) * CellSize.Width, row * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)leftNeighbor);
                                            }

                                            if (upstairsNeighbor != null && leftNeighbor != null)
                                            {
                                                upstairsLeftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column - 1),
                                                    new((column - 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsLeftNeighbor);
                                            }

                                            if (overfill.Right > 0.0f && column < columns - 1)
                                            {
                                                rightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row, column + 1),
                                                    new((column + 1) * CellSize.Width, row * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)rightNeighbor);
                                            }

                                            if (upstairsNeighbor != null && rightNeighbor != null)
                                            {
                                                upstairsRightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column + 1),
                                                    new((column + 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsRightNeighbor);
                                            }

                                            if (overfill.Bottom > 0.0f && row < rows - 1)
                                            {
                                                downstairsNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column),
                                                    new(column * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsNeighbor);
                                            }

                                            if (downstairsNeighbor != null && leftNeighbor != null)
                                            {
                                                downstairsLeftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column - 1),
                                                    new((column - 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsLeftNeighbor);
                                            }

                                            if (downstairsNeighbor != null && rightNeighbor != null)
                                            {
                                                downstairsRightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column + 1),
                                                    new((column + 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsRightNeighbor);
                                            }

                                            thisCell = new(
                                                this,
                                                drawingSession,
                                                caret,
                                                point,
                                                _terminalEngine.VideoTerminal[row][column]
                                            );

                                            _drawableCells.Enqueue((DrawableCell)thisCell);
                                        }
                                        else
                                        {
                                            // This is a cache miss, so assume
                                            // the worst case
                                            if (row > 0)
                                            {
                                                upstairsNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column),
                                                    new(column * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsNeighbor);
                                            }

                                            if (column > 0)
                                            {
                                                leftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row, column - 1),
                                                    new((column - 1) * CellSize.Width, row * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)leftNeighbor);
                                            }

                                            if (upstairsNeighbor != null && leftNeighbor != null)
                                            {
                                                upstairsLeftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column - 1),
                                                    new((column - 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsLeftNeighbor);
                                            }

                                            if (column < columns - 1)
                                            {
                                                rightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row, column + 1),
                                                    new((column + 1) * CellSize.Width, row * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)rightNeighbor);
                                            }

                                            if (upstairsNeighbor != null && rightNeighbor != null)
                                            {
                                                upstairsRightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row - 1, column + 1),
                                                    new((column + 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row - 1][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)upstairsRightNeighbor);
                                            }

                                            if (row < rows - 1)
                                            {
                                                downstairsNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column),
                                                    new(column * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsNeighbor);
                                            }

                                            if (downstairsNeighbor != null && leftNeighbor != null)
                                            {
                                                downstairsLeftNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column - 1),
                                                    new((column - 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column - 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsLeftNeighbor);
                                            }

                                            if (downstairsNeighbor != null && rightNeighbor != null)
                                            {
                                                downstairsRightNeighbor = new(
                                                    this,
                                                    drawingSession,
                                                    new(row + 1, column + 1),
                                                    new((column + 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                    _terminalEngine.VideoTerminal[row + 1][column + 1]
                                                );

                                                _drawableCells.Enqueue((DrawableCell)downstairsRightNeighbor);
                                            }

                                            thisCell = new(
                                                this,
                                                drawingSession,
                                                caret,
                                                point,
                                                _terminalEngine.VideoTerminal[row][column]
                                            );

                                            _drawableCells.Enqueue((DrawableCell)thisCell);
                                        }
                                    }
                                }

                                // Now for the tricky bit: for each null cell,
                                // we must determine whether another cell
                                // overfills into this cell, and if so, we must
                                // redraw it as well. This is *expensive*, so
                                // skip it if our effective frame rate drops
                                // below 10.
                                if (
                                    _effectiveFrameRate >= 10.0f
                                    && (
                                        _terminalEngine.VideoTerminal[row][column].GraphemeCluster == null
                                        || char.IsWhiteSpace(_terminalEngine.VideoTerminal[row][column].GraphemeCluster![0])
                                    )
                                )
                                {
                                    bool enqueued = false;

                                    if (row > 0)
                                    {
                                        if (_terminalEngine.VideoTerminal[row - 1][column].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row - 1][column].GraphemeCluster![0]))
                                        {
                                            upstairsNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row - 1, column),
                                                new(column * CellSize.Width, (row - 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row - 1][column]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)upstairsNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Bottom > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)upstairsNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)upstairsNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (column > 0)
                                    {
                                        if (_terminalEngine.VideoTerminal[row][column - 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row][column - 1].GraphemeCluster![0]))
                                        {
                                            leftNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row, column - 1),
                                                new((column - 1) * CellSize.Width, row * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row][column - 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)leftNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Right > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)leftNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)leftNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (row > 0 && column > 0)
                                    {
                                        if (_terminalEngine.VideoTerminal[row - 1][column - 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row - 1][column - 1].GraphemeCluster![0]))
                                        {
                                            upstairsLeftNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row - 1, column - 1),
                                                new((column - 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row - 1][column - 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)upstairsLeftNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Left > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)upstairsLeftNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)upstairsLeftNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (column < columns - 1)
                                    {
                                        if (_terminalEngine.VideoTerminal[row][column + 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row][column + 1].GraphemeCluster![0]))
                                        {
                                            rightNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row, column + 1),
                                                new((column + 1) * CellSize.Width, row * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row][column + 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)rightNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Left > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)rightNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)rightNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (row > 0 && column < columns - 1)
                                    {
                                        if (_terminalEngine.VideoTerminal[row - 1][column + 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row - 1][column + 1].GraphemeCluster![0]))
                                        {
                                            upstairsRightNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row - 1, column + 1),
                                                new((column + 1) * CellSize.Width, (row - 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row - 1][column + 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)upstairsRightNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Left > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)upstairsRightNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)upstairsRightNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (row < rows - 1)
                                    {
                                        if (_terminalEngine.VideoTerminal[row + 1][column].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row + 1][column].GraphemeCluster![0]))
                                        {
                                            downstairsNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row + 1, column),
                                                new(column * CellSize.Width, (row + 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row + 1][column]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)downstairsNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Top > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)downstairsNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)downstairsNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (row < rows - 1 && column > 0)
                                    {
                                        if (_terminalEngine.VideoTerminal[row + 1][column - 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row + 1][column - 1].GraphemeCluster![0]))
                                        {
                                            downstairsLeftNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row + 1, column - 1),
                                                new((column - 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row + 1][column - 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)downstairsLeftNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Left > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)downstairsLeftNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)downstairsLeftNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (row < rows - 1 && column < columns - 1)
                                    {
                                        if (_terminalEngine.VideoTerminal[row + 1][column + 1].GraphemeCluster != null && !char.IsWhiteSpace(_terminalEngine.VideoTerminal[row + 1][column + 1].GraphemeCluster![0]))
                                        {
                                            downstairsRightNeighbor ??= new(
                                                this,
                                                drawingSession,
                                                new(row + 1, column + 1),
                                                new((column + 1) * CellSize.Width, (row + 1) * CellSize.Height),
                                                _terminalEngine.VideoTerminal[row + 1][column + 1]
                                            );

                                            if (_overfillCache.TryGetValue(((DrawableCell)downstairsRightNeighbor).CellFingerprint, out RectF overfill))
                                            {
                                                if (overfill.Left > 0.0f)
                                                {
                                                    _drawableCells.Enqueue((DrawableCell)downstairsRightNeighbor);
                                                    enqueued = true;
                                                }
                                            }
                                            else
                                            {
                                                _drawableCells.Enqueue((DrawableCell)downstairsRightNeighbor);
                                                enqueued = true;
                                            }
                                        }
                                    }

                                    if (enqueued)
                                    {
                                        thisCell ??= new(
                                            this,
                                            drawingSession,
                                            caret,
                                            point,
                                            _terminalEngine.VideoTerminal[row][column]
                                        );

                                        _drawableCells.Enqueue((DrawableCell)thisCell);
                                    }
                                }
                            }
                        }
                    }
                }

                if (_offscreenBufferDirty)
                {
                    _offscreenBufferDirty = false;
                }

                _lastFrameCells = thisFrameCells;
                _seenDrawableCells.Clear();

                // Draw cell backgrounds
                while (_drawableCells.TryDequeue(out DrawableCell drawableCell))
                {
                    if (_seenDrawableCells.Contains(drawableCell))
                    {
                        continue;
                    }

                    DrawBackground(
                      drawingSession,
                      defaultBackgroundColor,
                      backgroundIsInvisible,
                      drawableCell
                    );

                    _seenDrawableCells.Add(drawableCell);

                    if (drawableCell.Cell.GraphemeCluster != null && !char.IsWhiteSpace(drawableCell.Cell.GraphemeCluster![0]))
                    {
                        _drawableCellForegrounds.Enqueue(drawableCell);
                    }
                }

                _seenDrawableCells.Clear();

                // Draw cell foregrounds and decorations
                while (_drawableCellForegrounds.TryDequeue(out DrawableCell drawableCell))
                {
                    if (_seenDrawableCells.Contains(drawableCell))
                    {
                        continue;
                    }

                    DrawForeground(
                        drawingSession,
                        textAntialiasing,
                        defaultBackgroundColor,
                        backgroundIsInvisible,
                        drawableCell,
                        logger
                    );

                    DrawDecoration(
                        drawingSession,
                        drawableCell
                    );

                    _seenDrawableCells.Add(drawableCell);
                }

#if DEBUG
                drawingSession.TextAntialiasing = CanvasTextAntialiasing.Aliased;
                drawingSession.Antialiasing = CanvasAntialiasing.Aliased;

                CanvasTextLayout effectiveFrameRateTextLayout = new(
                    drawingSession,
                    _effectiveFrameRate.ToString(),
                    new CanvasTextFormat() { FontSize = 8.0f },
                    50.0f,
                    8.0f
                );

                drawingSession.FillRectangle(
                    new Windows.Foundation.Rect(
                        0.0,
                        0.0,
                        effectiveFrameRateTextLayout.DrawBounds.Width + 2.0,
                        effectiveFrameRateTextLayout.DrawBounds.Height + 2.0
                    ),
                    Colors.Black
                );

                drawingSession.DrawTextLayout(
                    effectiveFrameRateTextLayout,
                    new Vector2(
                        (float)(1.0 - effectiveFrameRateTextLayout.DrawBounds.X),
                        (float)(1.0 - effectiveFrameRateTextLayout.DrawBounds.Y)
                    ),
                    Colors.White
                );
#endif
            }

            _lastFrameBounds = new(rows, columns);
            _terminalEngine.InvalidateCanvas();
        }

        /// <summary>
        /// Draws <paramref name="drawableCell"/>'s background to <paramref
        /// name="drawingSession"/>.
        /// </summary>
        /// <param name="drawingSession">The draw loop's <see
        /// cref="CanvasDrawingSession"/>.</param>
        /// <param name="defaultBackgroundColor">The default background color
        /// with which to draw.</param>
        /// <param name="backgroundIsInvisible">Whether the background, if
        /// <paramref name="defaultBackgroundColor"/>, should be drawn as
        /// transparent.</param>
        /// <param name="drawableCell">The cell to draw.</param>
        private void DrawBackground(CanvasDrawingSession drawingSession, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, DrawableCell drawableCell)
        {
            Color calculatedColor = drawableCell.Cell.GraphicRendition.Inverse ^ drawableCell.Cell.Selected
                ? drawableCell.Cell.GraphicRendition.CalculatedForegroundColor()
                : drawableCell.Cell.GraphicRendition.CalculatedBackgroundColor(
                    defaultBackgroundColor,
                    backgroundIsInvisible,
                    honorBackgroundIsInvisible: !_terminalEngine.UseAlternateScreenBuffer
                );

            drawingSession.Blend = CanvasBlend.Copy;

            drawingSession.FillRectangle(
                MathF.Round(drawableCell.Point.X * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                MathF.Round(drawableCell.Point.Y * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                MathF.Round(CellSize.Width * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                MathF.Round(CellSize.Height * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
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
        /// <param name="textAntialiasing">The text antialiasing style with
        /// which to draw <paramref name="drawableCell"/>.</param>
        /// <param name="defaultBackgroundColor">The default background color
        /// with which to draw.</param>
        /// <param name="backgroundIsInvisible">Whether the background, if
        /// <paramref name="defaultBackgroundColor"/>, should be drawn as
        /// transparent.</param>
        /// <param name="drawableCell">The cell to draw.</param>
        /// <param name="logger">An <see cref="ILogger"/>.</param>
        /// <exception cref="ArgumentException"></exception>
        private void DrawForeground(CanvasDrawingSession drawingSession, TextAntialiasingStyle textAntialiasing, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, DrawableCell drawableCell, ILogger? logger = null)
        {
            if (drawableCell.Cell.GraphemeCluster is null)
            {
                return;
            }

            if (logger != null)
            {
                StringBuilder drawnGraphemeCluster = new();

                drawnGraphemeCluster.Append('\'');
                drawnGraphemeCluster.Append(drawableCell.Cell.GraphemeCluster);
                drawnGraphemeCluster.Append("\' => [ ");

                foreach (char @char in drawableCell.Cell.GraphemeCluster)
                {
                    drawnGraphemeCluster.Append((int)@char);
                    drawnGraphemeCluster.Append(", ");
                }

                drawnGraphemeCluster.Append(']');

                logger.LogTrace("Drawing character {character}", drawnGraphemeCluster.ToString());
            }

            // Always present box-drawing characters (U+2500 through U+257f)
            // and block elements (U+2580 through U+259f) as aliased
            drawingSession.TextAntialiasing = drawableCell.Cell.GraphemeCluster.Length == 1
                && drawableCell.Cell.GraphemeCluster[0] >= '─'
                && drawableCell.Cell.GraphemeCluster[0] <= '▟'
                    ? CanvasTextAntialiasing.Aliased
                    : textAntialiasing switch
                    {
                        (TextAntialiasingStyle)(-1) => CanvasTextAntialiasing.Aliased,
                        TextAntialiasingStyle.None => CanvasTextAntialiasing.Aliased,
                        TextAntialiasingStyle.Grayscale => CanvasTextAntialiasing.Grayscale,
                        TextAntialiasingStyle.ClearType => CanvasTextAntialiasing.ClearType,
                        _ => throw new ArgumentException($"Invalid TextAntialiasingStyle {textAntialiasing}.", nameof(textAntialiasing))
                    };

            if (_terminalEngine.FullColorEmoji && drawableCell.Cell.GraphemeCluster.IsEmoji())
            {
                drawingSession.DrawText(
                    drawableCell.Cell.GraphemeCluster,
                    MathF.Round(drawableCell.Point.X * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                    MathF.Round(drawableCell.Point.Y * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                    Colors.Black,
                    TextFormats[0x0f]
                );
            }
            else
            {
                drawingSession.DrawTextLayout(
                    drawableCell.CanvasTextLayout,
                    MathF.Round(drawableCell.Point.X * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                    MathF.Round(drawableCell.Point.Y * (drawingSession.Dpi / DpiConstant)) / (drawingSession.Dpi / DpiConstant),
                    drawableCell.Cell.GraphicRendition.Inverse ^ drawableCell.Cell.Selected
                        ? drawableCell.Cell.GraphicRendition.CalculatedBackgroundColor(defaultBackgroundColor, backgroundIsInvisible, honorBackgroundIsInvisible: false)
                        : drawableCell.Cell.GraphicRendition.CalculatedForegroundColor()
                );
            }
        }

        /// <summary>
        /// Draws <paramref name="drawableCell"/>'s decorations to <paramref
        /// name="drawingSession"/>.
        /// </summary>
        /// <param name="drawingSession">The draw loop's <see
        /// cref="CanvasDrawingSession"/>.</param>
        /// <param name="drawableCell">The cell to draw.</param>
        private void DrawDecoration(CanvasDrawingSession drawingSession, DrawableCell drawableCell)
        {
            // Single underline (or double underline)
            if (
                (
                    drawableCell.Cell.GraphicRendition.Underline
                    && (
                        drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyle.Single
                        || drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyle.Double
                    )
                ) || drawableCell.Cell.GraphicRendition.DoubleUnderline
            )
            {
                DrawTextLine(
                    drawingSession,
                    drawableCell,
                    (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationBottom),
                    useUnderlineColor: true
                );
            }

            // Crossed-out
            if (drawableCell.Cell.GraphicRendition.CrossedOut)
            {
                DrawTextLine(
                    drawingSession,
                    drawableCell,
                    (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationMidpoint),
                    useUnderlineColor: false
                );
            }

            // Double underline
            if (
                drawableCell.Cell.GraphicRendition.DoubleUnderline
                || (
                    drawableCell.Cell.GraphicRendition.Underline
                    && drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyle.Double
                )
            )
            {
                DrawTextLine(
                    drawingSession,
                    drawableCell,
                    (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationAlmostBottom),
                    useUnderlineColor: true
                );
            }

            // Undercurl
            if (
                drawableCell.Cell.GraphicRendition.Underline
                && drawableCell.Cell.GraphicRendition.UnderlineStyle == UnderlineStyle.Undercurl
            )
            {
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
        /// path="/param[@name='drawableCell']"/></param>
        /// <param name="underlineY">The Y location at which to draw the
        /// underline.</param>
        /// <param name="useUnderlineColor">Whether to use the underline color
        /// (<see langword="true"/>) or the foreground color (<see
        /// langword="false"/>).</param>
        private void DrawTextLine(CanvasDrawingSession drawingSession, DrawableCell drawableCell, float underlineY, bool useUnderlineColor)
        {
            drawingSession.DrawLine(
                drawableCell.Caret.Column * CellSize.Width,
                underlineY,
                (drawableCell.Caret.Column * CellSize.Width) + CellSize.Width,
                underlineY,
                useUnderlineColor
                    ? drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor()
                    : drawableCell.Cell.GraphicRendition.CalculatedForegroundColor(),
                (float)_terminalEngine.FontSize * DecorationWeight
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
        /// path="/param[@name='drawableCell']"/></param>
        private void DrawUndercurl(CanvasDrawingSession drawingSession, DrawableCell drawableCell)
        {
            // The top-left point of our w
            Vector2 pointA = new(
                drawableCell.Caret.Column * CellSize.Width,
                (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationAlmostBottom)
            );

            // The bottom of the first half of our w
            Vector2 pointB = new(
                (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.25f),
                (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationBottom)
            );

            // The top midpoint of our w
            Vector2 pointC = new(
                (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.5f),
                (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationAlmostBottom)
            );

            // The bottom of the second half of our w
            Vector2 pointD = new(
                (drawableCell.Caret.Column * CellSize.Width) + (CellSize.Width * 0.75f),
                (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationBottom)
            );

            // The top-right point of our w
            Vector2 pointE = new(
                (drawableCell.Caret.Column * CellSize.Width) + CellSize.Width,
                (drawableCell.Caret.Row * CellSize.Height) + (CellSize.Height * DecorationAlmostBottom)
            );

            drawingSession.DrawLine(
                pointA,
                pointB,
                drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
                (float)_terminalEngine.FontSize * DecorationUndercurlWeight
            );

            drawingSession.DrawLine(
                pointB,
                pointC,
                drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
                (float)_terminalEngine.FontSize * DecorationUndercurlWeight
            );

            drawingSession.DrawLine(
                pointC,
                pointD,
                drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
                (float)_terminalEngine.FontSize * DecorationUndercurlWeight
            );

            drawingSession.DrawLine(
                pointD,
                pointE,
                drawableCell.Cell.GraphicRendition.UnderlineColor.ToWindowsUIColor(),
                (float)_terminalEngine.FontSize * DecorationUndercurlWeight
            );
        }
    }
}
