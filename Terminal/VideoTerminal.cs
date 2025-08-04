using Microsoft.Extensions.Logging;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace Spakov.Terminal
{
    /// <summary>
    /// The screen buffer and its state, which is analogous to a VT100.
    /// </summary>
    /// <remarks>
    /// <para>Does not interact with the UI thread.</para>
    /// <para><c>VideoTerminal.ProcessEscapeSequence.cs</c> contains the
    /// escape-sequence-processing parts of <see
    /// cref="VideoTerminal"/>.</para>
    /// </remarks>
    internal partial class VideoTerminal
    {
        private readonly ILogger? _logger;

        private readonly TerminalEngine _terminalEngine;

        private readonly List<Cell[]> _screenBuffer;
        private Caret _caret;

        private bool _selectionMode;
        private bool _lazySelectionMode;
        private Caret _lastSelection;

        private List<Cell[]>? _scrollbackBuffer;
        private List<Cell[]>? _scrollforwardBuffer;

        private GraphicRendition _graphicRendition;
        private Palette _palette;
        private bool _transparentEligible;
        private System.Drawing.Color _backgroundColorErase;

        private readonly List<Cell[]> _alternateScreenBuffer;
        private bool _useAlternateScreenBuffer;

        // For HTS, TBC, and HT
        private readonly List<int> _tabStops;

        // For CSI DECSET DECAWM
        private bool _autoWrapMode;
        private bool _wrapPending;

        // For CSI DECSTBM, CSI DECSET DECOM
        private int _scrollRegionTop;
        private int _scrollRegionBottom;
        private bool _originMode;

        // For Fp DECSC/DECRC
        private CursorState? _savedCursorState;

        // For CSI SAVE_CURSOR and RESTORE_CURSOR
        private Caret? _savedCursorPosition;

        // For CSI XTWINOPS 22 and 23
        private readonly string?[] _windowTitleStack;
        private int _windowTitleStackLength;

        // For DECSET 2031
        private bool _reportPaletteUpdate;

        /// <summary>
        /// The ANSI color palette being used.
        /// </summary>
        internal Palette Palette
        {
            get => _palette;

            set
            {
                if (_palette != value)
                {
                    _palette = value;
                    _graphicRendition.InitializeFromPalette(_palette);

                    if (_reportPaletteUpdate)
                    {
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
        internal bool SelectionMode
        {
            get => _selectionMode;

            set
            {
                if (_selectionMode != value)
                {
                    // Ending selection mode and in lazy selection mode
                    if (!value && _lazySelectionMode)
                    {
                        _lazySelectionMode = false;
                    }

                    // Ending selection mode and already in selection mode
                    if (!value && _selectionMode)
                    {
                        if (_terminalEngine.CopyOnMouseUp)
                        {
                            EndSelectionMode(copy: true);
                        }
                        else
                        {
                            _lazySelectionMode = true;
                        }
                    }

                    // Starting selection mode and in lazy selection mode
                    if (value && _lazySelectionMode)
                    {
                        EndSelectionMode(copy: false);
                        _lazySelectionMode = false;
                    }

                    // Starting selection mode but already in selection mode
                    if (value && _selectionMode)
                    {
                        EndSelectionMode(copy: false);
                    }

                    _selectionMode = value;
                }
            }
        }

        /// <summary>
        /// Whether the user has selected text.
        /// </summary>
        internal bool TextIsSelected
        {
            get
            {
                for (int row = 0; row < _screenBuffer.Count; row++)
                {
                    for (int col = 0; col < _screenBuffer[row].Length; col++)
                    {
                        if (_screenBuffer[row][col].Selected)
                        {
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
        /// <remarks>Set to <see langword="false"/> to force the terminal out
        /// of scrollback mode.</remarks>
        internal bool ScrollbackMode
        {
            get => _scrollforwardBuffer is not null && _scrollforwardBuffer.Count > 0;

            set
            {
                if (_scrollforwardBuffer is not null)
                {
                    if (!value)
                    {
                        if (ScrollbackMode)
                        {
                            ShiftToScrollback((uint)_scrollforwardBuffer.Count);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Whether to use the alternate screen buffer.
        /// </summary>
        internal bool UseAlternateScreenBuffer
        {
            get => _useAlternateScreenBuffer;

            private set
            {
                if (_useAlternateScreenBuffer != value)
                {
                    _useAlternateScreenBuffer = value;
                    SwapBuffers(_screenBuffer, _alternateScreenBuffer);
                }
            }
        }

        /// <summary>
        /// The caret row.
        /// </summary>
        private int Row
        {
            get => Caret.Row;
            set => _caret.Row = value;
        }

        /// <summary>
        /// The caret column.
        /// </summary>
        private int Column
        {
            get => Caret.Column;
            set => _caret.Column = value;
        }

        /// <summary>
        /// Whether a wrap is pending, for CSI DECSET DECAWM.
        /// </summary>
        private bool WrapPending
        {
            get => _wrapPending;

            set
            {
                if (_autoWrapMode)
                {
                    _wrapPending = value;
                    _logger?.LogDebug("{set} WrapPending", _wrapPending ? "Set" : "Cleared");
                }
            }
        }

        /// <summary>
        /// Initializes a <see cref="VideoTerminal"/>.
        /// </summary>
        /// <param name="terminalEngine">A <see
        /// cref="TerminalEngine"/>.</param>
        internal VideoTerminal(TerminalEngine terminalEngine)
        {
            _logger = LoggerHelper.CreateLogger<VideoTerminal>();

            _terminalEngine = terminalEngine;

            _screenBuffer = [];
            _alternateScreenBuffer = [];
            _tabStops = [];

            _graphicRendition = new();
            _palette = terminalEngine.Palette!;
            _graphicRendition.InitializeFromPalette(_palette);
            _transparentEligible = true;

            if (terminalEngine.UseBackgroundColorErase)
            {
                _backgroundColorErase = _graphicRendition.BackgroundColor;
            }

            terminalEngine.CursorVisible = true;
            terminalEngine.AutoRepeatKeys = true;
            _autoWrapMode = true;

            _windowTitleStack = new string?[10];
            _windowTitleStackLength = 0;

            // Initialize screen buffers
            Resize();

            InitializeTabStops();

            Task.Run(() =>
            {
                while (true)
                {
                    terminalEngine.VTQueueReady.WaitOne();

                    while (terminalEngine.VTQueue.TryDequeue(out object? vtInput))
                    {
                        if (vtInput is null)
                        {
                            continue;
                        }
                        else if (vtInput is string vtString)
                        {
                            lock (terminalEngine.ScreenBufferLock)
                            {
                                WriteText(vtString);
                            }
                        }
                        else if (vtInput is char vtCharacter)
                        {
                            lock (terminalEngine.ScreenBufferLock)
                            {
                                WriteGraphemeCluster(vtCharacter.ToString());
                            }
                        }
                        else if (vtInput is EscapeSequence vtEscapeSequence)
                        {
                            lock (terminalEngine.ScreenBufferLock)
                            {
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
        internal void Resize()
        {
            _logger?.LogDebug("Resizing to {rows} x {columns}", _terminalEngine.Rows, _terminalEngine.Columns);

            bool useScrollback = _scrollbackBuffer is not null && _scrollbackBuffer.Count > 0;

            Resize(_screenBuffer, useScrollback: useScrollback);
            Resize(_alternateScreenBuffer, useScrollback: false);

            _logger?.LogDebug("Caret is at {row}, {column}", Row, Column);

            if (Row > _terminalEngine.Rows - 1)
            {
                Row = _terminalEngine.Rows - 1;
                _logger?.LogDebug("Moved caret up to {row}", Row);
            }

            if (Column > _terminalEngine.Columns - 1)
            {
                Column = _terminalEngine.Columns - 1;
                _logger?.LogDebug("Moved caret left to {column}", Column);
            }

            _tabStops.Sort();
            for (int i = _tabStops.Count - 1; i >= 0; i--)
            {
                if (_tabStops[i] > Column)
                {
                    _tabStops.RemoveAt(i);
                }
            }

            WrapPending = Column == _terminalEngine.Columns - 1;

            _scrollRegionTop = 0;
            _scrollRegionBottom = _terminalEngine.Rows - 1;
        }

        /// <summary>
        /// Initializes the scrollback buffer to account for a resize.
        /// </summary>
        /// <remarks>Intended to be invoked after the scrollback is
        /// changed.</remarks>
        internal void ResizeScrollback()
        {
            _scrollbackBuffer ??= [];
            _scrollforwardBuffer ??= [];

            if (_scrollbackBuffer.Count > _terminalEngine.Scrollback)
            {
                _scrollbackBuffer.RemoveRange(
                    _terminalEngine.Scrollback - 1,
                    _scrollbackBuffer.Count - _terminalEngine.Scrollback
                );
            }
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the terminal.
        /// </summary>
        /// <param name="message">The message to write.</param>
        internal void Write(string message)
        {
            lock (_terminalEngine.ScreenBufferLock)
            {
                _graphicRendition.ForegroundColor = System.Drawing.Color.White;
                _graphicRendition.BackgroundColor = System.Drawing.Color.Navy;

                NextRow();
                WriteText(message);
                NextRow();
            }
        }

        /// <summary>
        /// Writes <paramref name="message"/> to the terminal in very
        /// pronounced colors.
        /// </summary>
        /// <remarks>This is meant to be used in exceptional cases that prevent
        /// the terminal from working at all.</remarks>
        /// <param name="message">The message to write.</param>
        internal void WriteError(string message)
        {
            lock (_terminalEngine.ScreenBufferLock)
            {
                _graphicRendition.ForegroundColor = System.Drawing.Color.White;
                _graphicRendition.BackgroundColor = System.Drawing.Color.Red;

                NextRow();
                WriteText(message);
                NextRow();
            }
        }

        /// <summary>
        /// Writes <paramref name="text"/> to the screen buffer.
        /// </summary>
        /// <param name="text">The text to write.</param>
        private void WriteText(string text)
        {
            _logger?.LogInformation("Processing text \"{text}\"", text);

            TextElementEnumerator graphemeClusterEnumerator = StringInfo.GetTextElementEnumerator(text);

            while (graphemeClusterEnumerator.MoveNext())
            {
                WriteGraphemeCluster(graphemeClusterEnumerator.GetTextElement());
            }
        }

        /// <summary>
        /// Writes <paramref name="graphemeCluster"/> to the screen buffer.
        /// </summary>
        /// <param name="graphemeCluster">The grapheme cluster to
        /// write.</param>
        private void WriteGraphemeCluster(string? graphemeCluster)
        {
            // Snap out of scrollback mode
            ScrollbackMode = false;

            _logger?.LogTrace("Handling grapheme cluster {graphemeCluster}", PrintableHelper.MakePrintable(graphemeCluster));

            if (graphemeCluster is not null)
            {
                if (graphemeCluster[0] < 0x20)
                {
                    if (_autoWrapMode)
                    {
                        WrapPending = false;
                    }
                }

                switch (graphemeCluster[0])
                {
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
                        if (Row == _terminalEngine.Rows - 1)
                        {
                            ShiftToScrollback(1, force: true);
                        }
                        else
                        {
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
            if (_autoWrapMode && WrapPending)
            {
                _logger?.LogDebug("Auto-wrap initiated");

                NextRow();
                WrapPending = false;
            }

            int graphemeClusterWidth = graphemeCluster.WideCharacterWidth();

            // Wrap if we don't have space to write a wide character
            if (graphemeClusterWidth > 1 && _autoWrapMode && Column == _terminalEngine.Columns - 2)
            {
                _logger?.LogDebug("Auto-wrap initiated (wide character)");

                NextRow();
                WrapPending = false;
            }

            _screenBuffer[Row][Column] = new()
            {
                GraphemeCluster = graphemeCluster,
                GraphicRendition = _graphicRendition,
                TransparentEligible = _transparentEligible
            };

            if (graphemeClusterWidth > 1)
            {
                if (_autoWrapMode && Column == _terminalEngine.Columns - 1)
                {
                    WrapPending = true;
                }
                else
                {
                    CaretRight();
                }

                if (_autoWrapMode && WrapPending)
                {
                    _logger?.LogDebug("Auto-wrap initiated (wide character)");

                    NextRow();
                    WrapPending = false;
                }

                _screenBuffer[Row][Column] = new()
                {
                    GraphemeCluster = null,
                    GraphicRendition = _graphicRendition,
                    TransparentEligible = _transparentEligible
                };
            }

            _logger?.LogDebug("screenBuffer[{Row}][{Column}] = '{graphemeCluster}'", Row, Column, graphemeCluster);

            if (_autoWrapMode && Column == _terminalEngine.Columns - 1)
            {
                WrapPending = true;
            }
            else
            {
                CaretRight();
            }
        }

        /// <summary>
        /// Handles pointer presses.
        /// </summary>
        /// <remarks>
        /// Intended to be invoked by <see
        /// cref="TerminalEngine.PointerPressed"/> to handle the event.
        /// </remarks>
        /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
        /// cref="TerminalControl.Canvas_PointerPressed"/>.</param>
        /// <param name="leftClickCount">The number of multi-click left clicks
        /// in this multi-click operation.</param>
        internal void PointerPressed(PointerPoint pointerPoint, int leftClickCount)
        {
            (int row, int column) = PointToCellIndices(pointerPoint.Position);
            if (row < 0 || column < 0)
            {
                return;
            }

            // Handle mouse tracking
            if (
                _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X10)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
            )
            {
                if (
                    !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
                    && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
                )
                {
                    // For mouse tracking
                    byte cb = 0x20;

                    if (pointerPoint.Properties.IsLeftButtonPressed)
                    {
                        cb += 0x00;
                    }
                    else if (pointerPoint.Properties.IsMiddleButtonPressed)
                    {
                        cb += 0x01;
                    }
                    else if (pointerPoint.Properties.IsRightButtonPressed)
                    {
                        cb += 0x02;
                    }

                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11))
                    {
                        if (_terminalEngine.ShiftPressed)
                        {
                            cb += 0x04;
                        }

                        if (_terminalEngine.AltPressed)
                        {
                            cb += 0x08;
                        }

                        if (_terminalEngine.ControlPressed)
                        {
                            cb += 0x10;
                        }
                    }

                    if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20)
                    {
                        cb = byte.MaxValue;
                    }

                    if (cb < byte.MaxValue)
                    {
                        _terminalEngine.AnsiWriter?.SendEscapeSequence(
                            [
                                (byte)Fe.CSI,
                                (byte)CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                                cb,
                                (byte)(column + 1 + 0x20),
                                (byte)(row + 1 + 0x20)
                            ]
                        );
                    }
                }
                else
                {
                    // For mouse tracking
                    uint cb = 0x00;

                    if (pointerPoint.Properties.IsLeftButtonPressed)
                    {
                        cb += 0x00;
                    }
                    else if (pointerPoint.Properties.IsMiddleButtonPressed)
                    {
                        cb += 0x01;
                    }
                    else if (pointerPoint.Properties.IsRightButtonPressed)
                    {
                        cb += 0x02;
                    }

                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11))
                    {
                        if (_terminalEngine.ShiftPressed)
                        {
                            cb += 0x04;
                        }

                        if (_terminalEngine.AltPressed)
                        {
                            cb += 0x08;
                        }

                        if (_terminalEngine.ControlPressed)
                        {
                            cb += 0x10;
                        }
                    }

                    StringBuilder mouseReport = new();

                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(column + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(row + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                    }
                    else if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.X);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.Y);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                    }

                    _terminalEngine.AnsiWriter?.SendEscapeSequence(
                        Encoding.ASCII.GetBytes(mouseReport.ToString())
                    );
                }
            }

            // Handle selection changes
            if (!UseAlternateScreenBuffer && pointerPoint.Properties.IsLeftButtonPressed)
            {
                if (leftClickCount == 2)
                {
                    _screenBuffer[row][column].Selected = true;

                    StringBuilder selection = new(_screenBuffer[row][column].GraphemeCluster);

                    for (int j = column; j >= 0; j--)
                    {
                        selection.Insert(0, _screenBuffer[row][j].GraphemeCluster);

                        if (_screenBuffer[row][j].GraphemeCluster is null || !Word().IsMatch(selection.ToString()))
                        {
                            if (_screenBuffer[row][j].GraphemeCluster is not null)
                            {
                                selection.Remove(0, _screenBuffer[row][j].GraphemeCluster!.Length);
                            }

                            break;
                        }
                        else
                        {
                            _screenBuffer[row][j].Selected = true;
                        }
                    }

                    for (int j = column; j < _screenBuffer[row].Length; j++)
                    {
                        selection.Append(_screenBuffer[row][j].GraphemeCluster);

                        if (_screenBuffer[row][j].GraphemeCluster is null || !Word().IsMatch(selection.ToString()))
                        {
                            if (_screenBuffer[row][j].GraphemeCluster is not null)
                            {
                                selection.Remove(selection.Length - _screenBuffer[row][j].GraphemeCluster!.Length - 1, _screenBuffer[row][j].GraphemeCluster!.Length);
                            }

                            break;
                        }
                        else
                        {
                            _screenBuffer[row][j].Selected = true;
                        }
                    }
                }
                else if (leftClickCount == 3)
                {
                    for (int j = 0; j < _screenBuffer[row].Length; j++)
                    {
                        _screenBuffer[row][j].Selected = true;
                    }
                }

                _lastSelection.Column = column;
                _lastSelection.Row = row;

                SelectionMode = true;
            }
        }

        /// <summary>
        /// Handles pointer movement.
        /// </summary>
        /// <remarks>
        /// Intended to be invoked by <see
        /// cref="TerminalEngine.PointerMoved"/> to handle the event.
        /// </remarks>
        /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
        /// cref="TerminalControl.Canvas_PointerMoved"/>.</param>
        internal void PointerMoved(PointerPoint pointerPoint)
        {
            (int row, int column) = PointToCellIndices(pointerPoint.Position);
            if (row < 0 || column < 0)
            {
                return;
            }

            // Handle mouse tracking
            if (
                _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
            )
            {
                bool trackMouse = true;

                if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion))
                {
                    // Cell motion occurs only when a button is pressed
                    if (
                        !pointerPoint.Properties.IsLeftButtonPressed
                        && !pointerPoint.Properties.IsMiddleButtonPressed
                        && !pointerPoint.Properties.IsRightButtonPressed
                    )
                    {
                        trackMouse = false;
                    }
                }

                if (trackMouse)
                {
                    if (
                        !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
                        && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
                    )
                    {
                        // For mouse tracking
                        byte cb = 0x20;

                        if (pointerPoint.Properties.IsLeftButtonPressed)
                        {
                            cb += 0x00;
                        }
                        else if (pointerPoint.Properties.IsMiddleButtonPressed)
                        {
                            cb += 0x01;
                        }
                        else if (pointerPoint.Properties.IsRightButtonPressed)
                        {
                            cb += 0x02;
                        }
                        else
                        {
                            // No button pressed
                            cb += 0x03;
                        }

                        // This is a mouse move
                        cb += 0x20;

                        if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20)
                        {
                            cb = byte.MaxValue;
                        }

                        if (cb < byte.MaxValue)
                        {
                            _terminalEngine.AnsiWriter?.SendEscapeSequence(
                                [
                                    (byte)Fe.CSI,
                                    (byte)CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                                    cb,
                                    (byte)(column + 1 + 0x20),
                                    (byte)(row + 1 + 0x20)
                                ]
                            );
                        }
                    }
                    else
                    {
                        // For mouse tracking
                        uint cb = 0x00;

                        if (pointerPoint.Properties.IsLeftButtonPressed)
                        {
                            cb += 0x00;
                        }
                        else if (pointerPoint.Properties.IsMiddleButtonPressed)
                        {
                            cb += 0x01;
                        }
                        else if (pointerPoint.Properties.IsRightButtonPressed)
                        {
                            cb += 0x02;
                        }
                        else
                        {
                            // No button pressed
                            cb += 0x03;
                        }

                        // This is a mouse move
                        cb += 0x20;

                        StringBuilder mouseReport = new();

                        if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR))
                        {
                            mouseReport.Append(Fe.CSI);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                            mouseReport.Append(cb);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                            mouseReport.Append(column + 1);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                            mouseReport.Append(row + 1);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                        }
                        else if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel))
                        {
                            mouseReport.Append(Fe.CSI);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                            mouseReport.Append(cb);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                            mouseReport.Append(pointerPoint.Position.X);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                            mouseReport.Append(pointerPoint.Position.Y);
                            mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR);
                        }

                        _terminalEngine.AnsiWriter?.SendEscapeSequence(
                            Encoding.ASCII.GetBytes(mouseReport.ToString())
                        );
                    }
                }
            }

            // Handle selection changes
            if (!SelectionMode)
            {
                return;
            }

            if (_lastSelection.Row == -1 && _lastSelection.Column == -1)
            {
                _lastSelection.Row = row;
                _lastSelection.Column = column;
            }

            // Use Bresenham's line algorithm to interpolate missing points in
            // the mouse movement and account for all selection addition and
            // subtraction operations
            foreach ((int deltaRow, int deltaColumn) in FourConnectedBresenhamInterpolation(_lastSelection, (row, column)))
            {
                HorizontalSelectionChange(deltaRow, deltaColumn);
                VerticalSelectionChange(deltaRow, deltaColumn);

                _screenBuffer[deltaRow][deltaColumn].Selected = true;

                _lastSelection.Row = deltaRow;
                _lastSelection.Column = deltaColumn;
            }
        }

        /// <summary>
        /// Handles pointer releases.
        /// </summary>
        /// <remarks>
        /// <para>Intended to be invoked by <see
        /// cref="TerminalEngine.PointerReleased"/> to handle the
        /// event.</para>
        /// <para><c>SelectionMode = false</c> is handled by the caller.</para>
        /// </remarks>
        /// <param name="pointerPoint">The <see cref="PointerPoint"/> from <see
        /// cref="TerminalControl.Canvas_PointerReleased"/>.</param>
        internal void PointerReleased(PointerPoint pointerPoint)
        {
            (int row, int column) = PointToCellIndices(pointerPoint.Position);
            if (row < 0 || column < 0)
            {
                return;
            }

            // Handle mouse tracking
            if (
                _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.X11)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.CellMotion)
                || _terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.AllMotion)
            )
            {
                if (
                    !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR)
                    && !_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel)
                )
                {
                    // For mouse tracking
                    byte cb = 0x20;

                    // This is a mouse button release
                    cb += 0x03;

                    if (row + 1 > 0xff - 0x20 || column + 1 > 0xff - 0x20)
                    {
                        cb = byte.MaxValue;
                    }

                    if (cb < byte.MaxValue)
                    {
                        _terminalEngine.AnsiWriter?.SendEscapeSequence(
                            [
                                (byte)Fe.CSI,
                                (byte)CSI_MouseTracking.MOUSE_TRACKING_LEADER,
                                cb,
                                (byte)(column + 1 + 0x20),
                                (byte)(row + 1 + 0x20)
                            ]
                        );
                    }
                }
                else
                {
                    // For mouse tracking
                    uint cb = 0x00;

                    if (_terminalEngine.LastMouseButton == MouseButton.Left)
                    {
                        cb += 0x00;
                    }
                    else if (_terminalEngine.LastMouseButton == MouseButton.Middle)
                    {
                        cb += 0x01;
                    }
                    else if (_terminalEngine.LastMouseButton == MouseButton.Right)
                    {
                        cb += 0x02;
                    }

                    StringBuilder mouseReport = new();

                    if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.SGR))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(column + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(row + 1);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_RELEASE_TERMINATOR);
                    }
                    else if (_terminalEngine.MouseTrackingMode.HasFlag(MouseTrackingModes.Pixel))
                    {
                        mouseReport.Append(Fe.CSI);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER);
                        mouseReport.Append(cb);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.X);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR);
                        mouseReport.Append(pointerPoint.Position.Y);
                        mouseReport.Append(CSI_MouseTracking.MOUSE_TRACKING_SGR_RELEASE_TERMINATOR);
                    }

                    _terminalEngine.AnsiWriter?.SendEscapeSequence(
                        Encoding.ASCII.GetBytes(mouseReport.ToString())
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
        private void HorizontalSelectionChange(int row, int column)
        {
            if (_lastSelection.Column != column)
            {
                int delta = _lastSelection.Column > -1
                    ? column - _lastSelection.Column
                    : 0;

                int firstSelectedRowInLastSelectionColumn = int.MaxValue;
                int lastSelectedRowInLastSelectionColumn = int.MinValue;

                if (_lastSelection.Column > -1)
                {
                    for (int i = 0; i < _screenBuffer.Count; i++)
                    {
                        if (_screenBuffer[i].Length > _lastSelection.Column)
                        {
                            if (_screenBuffer[i][_lastSelection.Column].Selected)
                            {
                                if (i < firstSelectedRowInLastSelectionColumn)
                                {
                                    firstSelectedRowInLastSelectionColumn = i;
                                }

                                if (i > lastSelectedRowInLastSelectionColumn)
                                {
                                    lastSelectedRowInLastSelectionColumn = i;
                                }
                            }
                        }
                    }
                }

                if (!_terminalEngine.AltPressed)
                {
                    // Line selection mode
                    if (delta > 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaColumn = _lastSelection.Column; deltaColumn < column; deltaColumn++)
                            {
                                _screenBuffer[row][deltaColumn].Selected = false;
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaColumn = _lastSelection.Column + 1; deltaColumn <= column; deltaColumn++)
                            {
                                _screenBuffer[row][deltaColumn].Selected = true;
                            }
                        }
                    }
                    else if (delta < 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaColumn = _lastSelection.Column; deltaColumn > column; deltaColumn--)
                            {
                                _screenBuffer[row][deltaColumn].Selected = false;
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaColumn = _lastSelection.Column - 1; deltaColumn >= column; deltaColumn--)
                            {
                                _screenBuffer[row][deltaColumn].Selected = true;
                            }
                        }
                    }
                }
                else
                {
                    // Block selection mode
                    if (delta > 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaColumn = _lastSelection.Column; deltaColumn < column; deltaColumn++)
                            {
                                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaColumn = _lastSelection.Column + 1; deltaColumn <= column; deltaColumn++)
                            {
                                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                }
                            }
                        }
                    }
                    else if (delta < 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaColumn = _lastSelection.Column; deltaColumn > column; deltaColumn--)
                            {
                                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaColumn = _lastSelection.Column - 1; deltaColumn >= column; deltaColumn--)
                            {
                                for (int deltaRow = firstSelectedRowInLastSelectionColumn; deltaRow <= lastSelectedRowInLastSelectionColumn; deltaRow++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = true;
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
        private void VerticalSelectionChange(int row, int column)
        {
            if (_lastSelection.Row != row)
            {
                int delta = _lastSelection.Row > -1
                  ? row - _lastSelection.Row
                  : 0;

                int firstSelectedColumnInLastSelectionRow = int.MaxValue;
                int lastSelectedColumnInLastSelectionRow = int.MinValue;

                if (_lastSelection.Row > -1)
                {
                    for (int i = 0; i < _screenBuffer[_lastSelection.Row].Length; i++)
                    {
                        if (_screenBuffer[_lastSelection.Row][i].Selected)
                        {
                            if (i < firstSelectedColumnInLastSelectionRow)
                            {
                                firstSelectedColumnInLastSelectionRow = i;
                            }

                            if (i > lastSelectedColumnInLastSelectionRow)
                            {
                                lastSelectedColumnInLastSelectionRow = i;
                            }
                        }
                    }
                }

                if (!_terminalEngine.AltPressed)
                {
                    // Line selection mode
                    if (delta > 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow <= row; deltaRow++)
                            {
                                if (deltaRow == row)
                                {
                                    // The first selected row
                                    for (int deltaColumn = 0; deltaColumn <= firstSelectedColumnInLastSelectionRow; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                    }
                                }
                                else
                                {
                                    // Any other row
                                    for (int deltaColumn = 0; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow <= row; deltaRow++)
                            {
                                if (deltaRow == _lastSelection.Row)
                                {
                                    // The first selected row
                                    for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                                else if (deltaRow == row)
                                {
                                    // The last selected row
                                    for (int deltaColumn = 0; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                                else
                                {
                                    // Any other row
                                    for (int deltaColumn = 0; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                            }
                        }
                    }
                    else if (delta < 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow >= row; deltaRow--)
                            {
                                if (deltaRow == row)
                                {
                                    // The first selected row
                                    for (int deltaColumn = lastSelectedColumnInLastSelectionRow; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                    }
                                }
                                else
                                {
                                    // Any other row
                                    for (int deltaColumn = 0; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow >= row; deltaRow--)
                            {
                                if (deltaRow == _lastSelection.Row)
                                {
                                    // The first selected row
                                    for (int deltaColumn = 0; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                                else if (deltaRow == row)
                                {
                                    // The last selected row
                                    for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                                else
                                {
                                    // Any other row
                                    for (int deltaColumn = 0; deltaColumn < _screenBuffer[deltaRow].Length; deltaColumn++)
                                    {
                                        _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Block selection mode
                    if (delta > 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow < row; deltaRow++)
                            {
                                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaRow = _lastSelection.Row + 1; deltaRow <= row; deltaRow++)
                            {
                                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = true;
                                }
                            }
                        }
                    }
                    else if (delta < 0)
                    {
                        if (_screenBuffer[row][column].Selected)
                        {
                            // The user is removing from the selection
                            for (int deltaRow = _lastSelection.Row; deltaRow > row; deltaRow--)
                            {
                                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = false;
                                }
                            }
                        }
                        else
                        {
                            // The user is adding to the selection
                            for (int deltaRow = _lastSelection.Row - 1; deltaRow >= row; deltaRow--)
                            {
                                for (int deltaColumn = firstSelectedColumnInLastSelectionRow; deltaColumn <= lastSelectedColumnInLastSelectionRow; deltaColumn++)
                                {
                                    _screenBuffer[deltaRow][deltaColumn].Selected = true;
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
        /// cref="TerminalControl.CopyOnMouseUp"/> is <see langword="false"/>,
        /// and then immediately set <see cref="SelectionMode"/> to <see
        /// langword="false"/>. In this case, ensure to invoke with <paramref
        /// name="copy"/> set to <see langword="true"/> to copy the
        /// selection.</remarks>
        /// <param name="copy">Whether to copy the selection or simply clear
        /// it.</param>
        internal void EndSelectionMode(bool copy)
        {
            StringBuilder selection = new();

            for (int row = 0; row < _screenBuffer.Count; row++)
            {
                bool hadSelection = false;

                for (int col = 0; col < _screenBuffer[row].Length; col++)
                {
                    if (_screenBuffer[row][col].Selected)
                    {
                        hadSelection = true;
                        _screenBuffer[row][col].Selected = false;

                        if (copy)
                        {
                            selection.Append(_screenBuffer[row][col].GraphemeCluster);
                        }
                    }
                }

                if (copy && hadSelection)
                {
                    selection.Append(_terminalEngine.CopyNewline);
                }
            }

            if (copy && selection.Length > 0)
            {
                selection.Remove(selection.Length - _terminalEngine.CopyNewline.Length, _terminalEngine.CopyNewline.Length);

                DataPackage dataPackage = new()
                {
                    RequestedOperation = DataPackageOperation.Copy
                };

                dataPackage.SetText(selection.ToString());
                Clipboard.SetContent(dataPackage);
            }

            _lastSelection.Row = -1;
            _lastSelection.Column = -1;
        }

        /// <summary>
        /// Shifts <paramref name="rows"/> rows from the top of the screen
        /// buffer.
        /// </summary>
        /// <remarks>
        /// <para>If the scrollback and scrollforward buffers are initialized,
        /// shifts into the scrollback buffer, shifting out of the
        /// scrollforward buffer as needed.</para>
        /// <para>If <see cref="UseAlternateScreenBuffer"/> is <see
        /// langword="true"/>, behaves the same as if the scrollback and
        /// scrollforward buffers are not initialized.</para>
        /// </remarks>
        /// <param name="rows">The number of rows to shift.</param>
        /// <param name="force">Whether to force the shift to scrollback, even
        /// if it will result in empty lines.</param>
        /// <param name="callerMemberName">The member that invoked <see
        /// cref="ShiftToScrollback"/>, used for debug logging.</param>
        internal void ShiftToScrollback(uint rows = 1, bool force = false, [System.Runtime.CompilerServices.CallerMemberName] string? callerMemberName = null)
        {
            bool useScrollback = _scrollbackBuffer is not null && _scrollforwardBuffer is not null && _terminalEngine.Scrollback > 0 && !UseAlternateScreenBuffer;

            SelectionMode = false;

            if (useScrollback)
            {
                if (!force)
                {
                    if (rows > _scrollforwardBuffer!.Count)
                    {
                        rows = (uint)_scrollforwardBuffer.Count;
                    }
                }
            }

            _logger?.LogInformation("{callerMemberName} => ShiftToScrollback({rows}, force: {force})", callerMemberName, rows, force);

            if (!useScrollback && !force)
            {
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                if (useScrollback)
                {
                    if (_scrollbackBuffer!.Count == _terminalEngine.Scrollback)
                    {
                        _scrollbackBuffer.RemoveAt(0);
                    }
                }

                if (useScrollback)
                {
                    _scrollbackBuffer!.Add(_screenBuffer[0]);
                }

                _screenBuffer.RemoveAt(0);

                if (useScrollback && _scrollforwardBuffer!.Count > 0)
                {
                    _screenBuffer.Add(_scrollforwardBuffer[0]);

                    _scrollforwardBuffer.RemoveAt(0);
                }
                else
                {
                    _screenBuffer.Add(new Cell[_terminalEngine.Columns]);
                    _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                    for (int col = 0; col < _terminalEngine.Columns; col++)
                    {
                        _screenBuffer[_terminalEngine.Rows - 1][col] = new()
                        {
                            GraphicRendition = _graphicRendition,
                            TransparentEligible = _transparentEligible
                        };

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            _screenBuffer[_terminalEngine.Rows - 1][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
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
        /// shifts from the bottom of the scrollback buffer into the screen
        /// buffer, shifting into the scrollforward buffer as needed.</para>
        /// <para>If <see cref="UseAlternateScreenBuffer"/> is <see
        /// langword="true"/>, behaves the same as if the scrollback and
        /// scrollforward buffers are not initialized.</para>
        /// </remarks>
        /// <param name="rows">The number of rows to shift.</param>
        /// <param name="force">Whether to force the shift to scrollforward,
        /// even if it will result in empty lines.</param>
        /// <param name="callerMemberName">The member that invoked <see
        /// cref="ShiftFromScrollback"/>, used for debug logging.</param>
        internal void ShiftFromScrollback(uint rows = 1, bool force = false, [System.Runtime.CompilerServices.CallerMemberName] string? callerMemberName = null)
        {
            bool useScrollback = _scrollbackBuffer is not null && _scrollforwardBuffer is not null && _terminalEngine.Scrollback > 0 && !UseAlternateScreenBuffer;

            SelectionMode = false;

            if (useScrollback)
            {
                if (!force)
                {
                    if (rows > _scrollbackBuffer!.Count)
                    {
                        rows = (uint)_scrollbackBuffer.Count;
                    }
                }
            }

            _logger?.LogInformation("{callerMemberName} => ShiftFromScrollback({rows}, force: {force})", callerMemberName, rows, force);

            if (!useScrollback && !force)
            {
                return;
            }

            for (int row = 0; row < rows; row++)
            {
                if (useScrollback)
                {
                    _scrollforwardBuffer!.Insert(0, _screenBuffer[_terminalEngine.Rows - 1]);
                }

                _screenBuffer.RemoveAt(_terminalEngine.Rows - 1);

                if (useScrollback && _scrollbackBuffer!.Count > 0)
                {
                    _screenBuffer.Insert(0, _scrollbackBuffer[^1]);

                    _scrollbackBuffer.RemoveAt(_scrollbackBuffer.Count - 1);
                }
                else
                {
                    _screenBuffer.Insert(0, new Cell[_terminalEngine.Columns]);
                    _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                    for (int col = 0; col < _terminalEngine.Columns; col++)
                    {
                        _screenBuffer[0][col] = new()
                        {
                            GraphicRendition = _graphicRendition,
                            TransparentEligible = _transparentEligible
                        };

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            _screenBuffer[0][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the indices of the <see cref="Cell"/> within <see
        /// cref="_screenBuffer"/> corresponding to <paramref name="point"/>.
        /// </summary>
        /// <param name="point">A <see cref="Point"/>.</param>
        /// <returns>A <see cref="ValueTuple{T1, T2}"/> containing the row and
        /// the column indices within <see cref="_screenBuffer"/> of the
        /// matching <see cref="Cell"/>, or <c>-1</c> if there is no <see
        /// cref="Cell"/> at <paramref name="point"/>.</returns>
        internal (int row, int column) PointToCellIndices(Point point)
        {
            int row = (int)(point.Y / (_terminalEngine.Rows * _terminalEngine.CellSize.Height) * _terminalEngine.Rows);
            int column = (int)(point.X / (_terminalEngine.Columns * _terminalEngine.CellSize.Width) * _terminalEngine.Columns);

            if (row < 0 || column < 0)
            {
                return (-1, -1);
            }

            return row > _screenBuffer.Count - 1
                ? (-1, -1)
                : column > _screenBuffer[row].Length - 1
                    ? (row, -1)
                    : (row, column);
        }

        /// <summary>
        /// Row indexer.
        /// </summary>
        /// <param name="row">The requested row.</param>
        /// <returns>The row at index <paramref name="row"/>.</returns>
        internal Cell[] this[int row] => _screenBuffer[row];

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
        private void Resize(List<Cell[]> buffer, bool useScrollback)
        {
            // Resize columns in each row
            for (int row = 0; row < buffer.Count; row++)
            {
                Cell[] newRow = new Cell[_terminalEngine.Columns];
                _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

                for (int col = 0; col < Math.Min(buffer[row].Length, _terminalEngine.Columns); col++)
                {
                    newRow[col] = buffer[row][col];
                }

                for (int col = buffer[row].Length - 1; col < _terminalEngine.Columns; col++)
                {
                    newRow[col] = new()
                    {
                        GraphicRendition = _graphicRendition,
                        TransparentEligible = _transparentEligible
                    };

                    if (_terminalEngine.UseBackgroundColorErase)
                    {
                        newRow[col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                    }
                }

                buffer[row] = newRow;
            }

            // Adjust number of rows
            if (_terminalEngine.Rows > buffer.Count)
            { // Adding rows
                for (int row = buffer.Count; row < _terminalEngine.Rows; row++)
                {
                    buffer.Add(new Cell[_terminalEngine.Columns]);

                    for (int col = 0; col < _terminalEngine.Columns; col++)
                    {
                        // Note that we do *not* adjust the new cell's graphic
                        // rendition here; these appeared out of nowhere in a
                        // place that doesn't match "now"
                        buffer[row][col] = new();

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            buffer[row][col].GraphicRendition.BackgroundColor = _backgroundColorErase;
                        }
                    }
                }
            }
            else if (_terminalEngine.Rows < buffer.Count)
            { // Removing rows
                SelectionMode = false;

                for (int row = buffer.Count - 1; row >= _terminalEngine.Rows; row--)
                {
                    if (useScrollback)
                    {
                        if (_scrollbackBuffer!.Count == _terminalEngine.Scrollback)
                        {
                            _scrollbackBuffer.RemoveAt(0);
                        }

                        _scrollbackBuffer!.Add(buffer[0]);
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
        /// name="bufferB"/> are both <see cref="TerminalControl.Rows"/> by
        /// <see cref="TerminalControl.Columns"/>.</remarks>
        /// <param name="bufferA">A <see cref="List{T}"/> of an array of <see
        /// cref="Cell"/>s.</param>
        /// <param name="bufferB">A <see cref="List{T}"/> of an array of <see
        /// cref="Cell"/>s.</param>
        private void SwapBuffers(List<Cell[]> bufferA, List<Cell[]> bufferB)
        {
            Cell swap;

            for (int row = 0; row < _terminalEngine.Rows; row++)
            {
                for (int col = 0; col < _terminalEngine.Columns; col++)
                {
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
        private void ClearScreen(ScreenClearType screenClearType)
        {
            _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

            switch (screenClearType)
            {
                case ScreenClearType.Before:
                    for (int i = 0; i <= Row; i++)
                    {
                        if (i < Row)
                        {
                            for (int j = 0; j < _terminalEngine.Columns; j++)
                            {
                                _screenBuffer[i][j] = new()
                                {
                                    GraphicRendition = _graphicRendition,
                                    TransparentEligible = _transparentEligible
                                };

                                if (_terminalEngine.UseBackgroundColorErase)
                                {
                                    _screenBuffer[i][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                }
                            }
                        }
                        else
                        {
                            for (int j = 0; j < Column; j++)
                            {
                                _screenBuffer[i][j] = new()
                                {
                                    GraphicRendition = _graphicRendition,
                                    TransparentEligible = _transparentEligible
                                };

                                if (_terminalEngine.UseBackgroundColorErase)
                                {
                                    _screenBuffer[i][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                }
                            }
                        }
                    }

                    break;

                case ScreenClearType.After:
                    for (int i = Row; i < _terminalEngine.Rows; i++)
                    {
                        if (i > Row)
                        {
                            for (int j = 0; j < _terminalEngine.Columns; j++)
                            {
                                _screenBuffer[i][j] = new()
                                {
                                    GraphicRendition = _graphicRendition,
                                    TransparentEligible = _transparentEligible
                                };

                                if (_terminalEngine.UseBackgroundColorErase)
                                {
                                    _screenBuffer[i][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                }
                            }
                        }
                        else
                        {
                            for (int j = Column; j < _terminalEngine.Columns; j++)
                            {
                                _screenBuffer[i][j] = new()
                                {
                                    GraphicRendition = _graphicRendition,
                                    TransparentEligible = _transparentEligible
                                };

                                if (_terminalEngine.UseBackgroundColorErase)
                                {
                                    _screenBuffer[i][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                                }
                            }
                        }
                    }

                    break;

                case ScreenClearType.Entire:
                case ScreenClearType.EntireWithScrollback:
                    for (int i = Row; i < _terminalEngine.Rows; i++)
                    {
                        for (int j = 0; j < _terminalEngine.Columns; j++)
                        {
                            _screenBuffer[i][j] = new()
                            {
                                GraphicRendition = _graphicRendition,
                                TransparentEligible = _transparentEligible
                            };

                            if (_terminalEngine.UseBackgroundColorErase)
                            {
                                _screenBuffer[i][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                            }
                        }
                    }

                    if (screenClearType == ScreenClearType.EntireWithScrollback)
                    {
                        ScrollbackMode = false;
                        _scrollbackBuffer?.Clear();
                    }

                    break;
            }
        }

        /// <summary>
        /// Clears the line.
        /// </summary>
        /// <param name="lineClearType">The type of line clear.</param>
        private void ClearLine(LineClearType lineClearType)
        {
            _transparentEligible = _graphicRendition.BackgroundColor == Palette.DefaultBackgroundColor;

            switch (lineClearType)
            {
                case LineClearType.Before:
                    for (int j = 0; j < Column; j++)
                    {
                        _screenBuffer[Row][j] = new()
                        {
                            GraphicRendition = _graphicRendition,
                            TransparentEligible = _transparentEligible
                        };

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            _screenBuffer[Row][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                        }
                    }

                    break;

                case LineClearType.After:
                    for (int j = Column; j < _terminalEngine.Columns; j++)
                    {
                        _screenBuffer[Row][j] = new()
                        {
                            GraphicRendition = _graphicRendition,
                            TransparentEligible = _transparentEligible
                        };

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            _screenBuffer[Row][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                        }
                    }

                    break;

                case LineClearType.Entire:
                    for (int j = 0; j < _terminalEngine.Columns; j++)
                    {
                        _screenBuffer[Row][j] = new()
                        {
                            GraphicRendition = _graphicRendition,
                            TransparentEligible = _transparentEligible
                        };

                        if (_terminalEngine.UseBackgroundColorErase)
                        {
                            _screenBuffer[Row][j].GraphicRendition.BackgroundColor = _backgroundColorErase;
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Initializes tab stops based on the current terminal size.
        /// </summary>
        private void InitializeTabStops()
        {
            _tabStops.Clear();

            for (int i = 0; i < _terminalEngine.Columns; i += _terminalEngine.TabWidth)
            {
                _tabStops.Add(i);
            }
        }

        /// <summary>
        /// Moves the caret to the next tab stop.
        /// </summary>
        private void NextTabStop()
        {
            _tabStops.Sort();

            foreach (int tabStop in _tabStops)
            {
                if (tabStop <= Column)
                {
                    continue;
                }

                Column = tabStop;
                return;
            }
        }

        /// <summary>
        /// Moves the caret to the previous tab stop.
        /// </summary>
        private void PreviousTabStop()
        {
            _tabStops.Sort((a, b) => b.CompareTo(a));

            foreach (int tabStop in _tabStops)
            {
                if (tabStop >= Column)
                {
                    continue;
                }

                Column = tabStop;
                return;
            }
        }

        /// <summary>
        /// Moves the caret to the beginning of the next row.
        /// </summary>
        private void NextRow()
        {
            Column = 0;

            if (++Row == _terminalEngine.Rows)
            {
                Row--;
                ShiftToScrollback(1, force: true);
            }
        }

        /// <summary>
        /// Moves the caret to the beginning of the previous row.
        /// </summary>
        private void PreviousRow()
        {
            if (--Row < 0)
            {
                Row = 0;
            }

            Column = 0;
        }

        /// <summary>
        /// Moves the caret to the left.
        /// </summary>
        private void CaretLeft()
        {
            if (--Column < 0)
            {
                Column++;
            }
        }

        /// <summary>
        /// Moves the caret to the right.
        /// </summary>
        private void CaretRight()
        {
            if (++Column == _screenBuffer[Row].Length)
            {
                Column--;
            }
        }

        /// <summary>
        /// Moves the caret up.
        /// </summary>
        private void CaretUp()
        {
            if (--Row < 0)
            {
                Row++;
            }
        }

        /// <summary>
        /// Moves the caret down.
        /// </summary>
        private void CaretDown()
        {
            if (++Row == _terminalEngine.Rows)
            {
                Row--;
            }
        }

        /// <summary>
        /// Generates all (row, column) points along a 4-connected Bresenham
        /// line from <paramref name="lastSelection"/> to <paramref
        /// name="newSelection"/>.
        /// </summary>
        /// <remarks>
        /// <para>Intended for cell-based selection where coordinates are
        /// discrete.</para>
        /// <para>Source: <see
        /// href="https://stackoverflow.com/a/14506390"/></para>
        /// </remarks>
        /// <param name="lastSelection">The last selection <see
        /// cref="Terminal.Caret"/>.</param>
        /// <param name="newSelection">A <see cref="ValueTuple{T1, T2}"/> of
        /// the target row and column.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see
        /// cref="ValueTuple{T1, T2}"/> of row and column.</returns>
        private static IEnumerable<(int row, int column)> FourConnectedBresenhamInterpolation(Caret lastSelection, ValueTuple<int, int> newSelection)
        {
            int column0 = lastSelection.Column;
            int column1 = newSelection.Item2;
            int row0 = lastSelection.Row;
            int row1 = newSelection.Item1;

            int deltaColumn = Math.Abs(column1 - column0);
            int deltaRow = Math.Abs(row1 - row0);
            int signColumn = column0 < column1 ? 1 : -1;
            int signRow = row0 < row1 ? 1 : -1;
            int err = 0;

            for (int i = 0; i <= deltaColumn + deltaRow; i++)
            {
                yield return (row0, column0);

                int err1 = err + deltaRow;
                int err2 = err - deltaColumn;

                if (Math.Abs(err1) < Math.Abs(err2))
                {
                    column0 += signColumn;
                    err = err1;
                }
                else
                {
                    row0 += signRow;
                    err = err2;
                }
            }
        }

        /// <summary>
        /// Responds to a <see cref="CSI_XTMODKEYS.XTQMODKEYS"/>.
        /// </summary>
        /// <param name="pp">The <c>modify*Keys</c> resource to query.</param>
        private void XTMODKEYSQueryResponse(int pp)
        {
            int? value = pp switch
            {
                1 => (int)_terminalEngine.XTMODKEYS.ModifyCursorKeysValue,
                2 => (int)_terminalEngine.XTMODKEYS.ModifyFunctionKeysValue,
                3 => (int)_terminalEngine.XTMODKEYS.ModifyKeypadKeysValue,
                4 => (int)_terminalEngine.XTMODKEYS.ModifyOtherKeysValue,
                _ => null
            };

            if (value is null)
            {
                return;
            }

            StringBuilder themeResponse = new();

            themeResponse.Append(Fe.CSI);
            themeResponse.Append(CSI_XTMODKEYS.XTMODKEYS);
            themeResponse.Append(value);
            themeResponse.Append(CSI.XTMODKEYS);

            _terminalEngine.AnsiWriter?.SendEscapeSequence(
                Encoding.ASCII.GetBytes(themeResponse.ToString())
            );
        }

        /// <summary>
        /// Responds to a <see cref="CSI_DSR.DSR_THEME_QUERY"/>.
        /// </summary>
        private void DSRThemeQueryResponse()
        {
            float backgroundColorGamma = 0.0f;

            backgroundColorGamma += (float)Palette.DefaultBackgroundColor.R / byte.MaxValue;
            backgroundColorGamma += (float)Palette.DefaultBackgroundColor.G / byte.MaxValue;
            backgroundColorGamma += (float)Palette.DefaultBackgroundColor.B / byte.MaxValue;

            backgroundColorGamma /= 3;

            StringBuilder themeResponse = new();

            themeResponse.Append(Fe.CSI);
            themeResponse.Append('?');
            themeResponse.Append(CSI_DSR.DSR_THEME_RESPONSE);
            themeResponse.Append(CSI_DSR.DSR_THEME_SEPARATOR);
            themeResponse.Append(backgroundColorGamma > 0.5 ? CSI_DSR.DSR_THEME_LIGHT : CSI_DSR.DSR_THEME_DARK);
            themeResponse.Append(CSI.DSR);

            _terminalEngine.AnsiWriter?.SendEscapeSequence(
                Encoding.ASCII.GetBytes(themeResponse.ToString())
            );
        }

        [GeneratedRegex(@"^\w+$")]
        private static partial Regex Word();

        /// <summary>
        /// The cursor state, as in DECSC/DECRC.
        /// </summary>
        private readonly struct CursorState
        {
            /// <summary>
            /// A snapshot of <see cref="VideoTerminal.Caret"/>.
            /// </summary>
            public readonly Caret Caret;

            /// <summary>
            /// A snapshot of <see cref="TerminalEngine.CursorVisible"/>.
            /// </summary>
            public readonly bool CursorVisible;

            /// <summary>
            /// A snapshot of <see cref="_autoWrapMode"/>.
            /// </summary>
            public readonly bool AutoWrapMode;

            /// <summary>
            /// A snapshot of <see cref="VideoTerminal.WrapPending"/>.
            /// </summary>
            public readonly bool WrapPending;

            /// <summary>
            /// A snapshot of <see cref="_originMode"/>.
            /// </summary>
            public readonly bool OriginMode;

            /// <summary>
            /// A snapshot of <see cref="_graphicRendition"/>.
            /// </summary>
            public readonly GraphicRendition GraphicRendition;

            /// <summary>
            /// Initializes a <see cref="CursorState"/> based on <paramref
            /// name="screenBuffer"/>.
            /// </summary>
            /// <param name="screenBuffer">A <see
            /// cref="VideoTerminal"/>.</param>
            public CursorState(VideoTerminal screenBuffer)
            {
                Caret = screenBuffer.Caret;
                CursorVisible = screenBuffer._terminalEngine.CursorVisible;
                AutoWrapMode = screenBuffer._autoWrapMode;
                WrapPending = screenBuffer.WrapPending;
                OriginMode = screenBuffer._originMode;
                GraphicRendition = screenBuffer._graphicRendition;

                screenBuffer._logger?.LogDebug("Saving cursor state:");
                screenBuffer._logger?.LogDebug("  Row = {row}", Caret.Row);
                screenBuffer._logger?.LogDebug("  Column = {column}", Caret.Column);
                screenBuffer._logger?.LogDebug("  CursorVisible = {cursorVisible}", CursorVisible);
                screenBuffer._logger?.LogDebug("  AutoWrapMode = {autoWrapMode}", AutoWrapMode);
                screenBuffer._logger?.LogDebug("  WrapPending = {wrapPending}", WrapPending);
                screenBuffer._logger?.LogDebug("  OriginMode = {originMode}", OriginMode);
                screenBuffer._logger?.LogDebug("  GraphicRendition = {graphicRendition}", GraphicRendition);
            }

            /// <summary>
            /// Restores a <see cref="CursorState"/> to <paramref
            /// name="screenBuffer"/>.
            /// </summary>
            /// <param name="screenBuffer"></param>
            public readonly void Restore(VideoTerminal screenBuffer)
            {
                screenBuffer.Row = Caret.Row;
                screenBuffer.Column = Caret.Column;
                screenBuffer._terminalEngine.CursorVisible = CursorVisible;
                screenBuffer._autoWrapMode = AutoWrapMode;
                screenBuffer.WrapPending = WrapPending;
                screenBuffer._originMode = OriginMode;
                screenBuffer._graphicRendition = GraphicRendition;

                screenBuffer._logger?.LogDebug("Restoring cursor state:");
                screenBuffer._logger?.LogDebug("  Row = {row}", screenBuffer.Row);
                screenBuffer._logger?.LogDebug("  Column = {column}", screenBuffer.Column);
                screenBuffer._logger?.LogDebug("  CursorVisible = {cursorVisible}", screenBuffer._terminalEngine.CursorVisible);
                screenBuffer._logger?.LogDebug("  AutoWrapMode = {autoWrapMode}", screenBuffer._autoWrapMode);
                screenBuffer._logger?.LogDebug("  WrapPending = {wrapPending}", screenBuffer.WrapPending);
                screenBuffer._logger?.LogDebug("  OriginMode = {originMode}", screenBuffer._originMode);
                screenBuffer._logger?.LogDebug("  GraphicRendition = {graphicRendition}", screenBuffer._graphicRendition);
            }
        }

        /// <summary>
        /// Represents the last selection.
        /// </summary>
        private struct LastSelection
        {
            /// <summary>
            /// The row index of <see cref="_screenBuffer"/>.
            /// </summary>
            public int Row;

            /// <summary>
            /// The column index of <see cref="_screenBuffer"/>.
            /// </summary>
            public int Column;

            /// <summary>
            /// Initializes a <see cref="LastSelection"/>.
            /// </summary>
            public LastSelection()
            {
                Row = -1;
                Column = -1;
            }
        }

        /// <summary>
        /// Types of screen clears.
        /// </summary>
        private enum ScreenClearType
        {
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
        private enum LineClearType
        {
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
