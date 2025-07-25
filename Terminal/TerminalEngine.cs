using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Spakov.AnsiProcessor;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.Input;
using Spakov.AnsiProcessor.Output.EscapeSequences;
using Spakov.AnsiProcessor.TermCap;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Spakov.Terminal
{
    /// <summary>
    /// Manages interaction between the UI thread and <see
    /// cref="TerminalRenderer"/>.
    /// </summary>
    /// <remarks>Also handles events from <see cref="AnsiReader"/>.</remarks>
    internal class TerminalEngine
    {
        internal readonly ILogger? logger;

        private readonly TerminalControl _terminalControl;
        private readonly CanvasControl _canvas;

        private Palette _palette;
        private TerminalCapabilities _terminalCapabilities;

        private string _defaultWindowTitle;
        private int _rows;
        private int _columns;
        private int _scrollback;

        private string _fontFamily;
        private double _fontSize;

        private bool _useBackgroundColorErase;
        private bool _backgroundIsInvisible;
        private TextAntialiasingStyle _textAntialiasing;
        private bool _fullColorEmoji;
        private bool _useVisualBell;
        private CursorStyle _cursorStyle;
        private bool _cursorBlink;
        private int _tabWidth;
        private bool _copyOnMouseUp;
        private string _copyNewline;

        private bool _autoRepeatKeys;
        private bool _applicationCursorKeys;
        private bool _bracketedPasteMode;
        private bool _reportResize;

        private MouseTrackingModes _mouseTrackingMode;

        private AnsiReader? _ansiReader;
        private AnsiWriter? _ansiWriter;

        private readonly object _screenBufferLock;

        private readonly TerminalRenderer _terminalRenderer;

        private readonly VideoTerminal _videoTerminal;
        private readonly ConcurrentQueue<object> _vtQueue;
        private readonly AutoResetEvent _vtQueueReady;

        /// <summary>
        /// The <see cref="TerminalControl"/>'s <see cref="CanvasControl"/>.
        /// </summary>
        internal CanvasControl Canvas => _canvas;

        /// <inheritdoc cref="TerminalControl.HWnd"/>
        internal nint HWnd => _terminalControl.HWnd;

        /// <inheritdoc cref="TerminalControl.DispatcherQueue"/>
        internal DispatcherQueue DispatcherQueue => _terminalControl.DispatcherQueue;

        /// <inheritdoc cref="TerminalControl.AnsiColors"/>
        internal Palette Palette
        {
            get => _palette;
            set => _palette = value;
        }

        /// <inheritdoc cref="TerminalControl.TerminalCapabilities"/>
        internal TerminalCapabilities TerminalCapabilities
        {
            get => _terminalCapabilities;
            set => _terminalCapabilities = value;
        }

        /// <inheritdoc cref="TerminalControl.DefaultWindowTitle"/>
        internal string DefaultWindowTitle
        {
            get => _defaultWindowTitle;
            set => _defaultWindowTitle = value;
        }

        /// <inheritdoc cref="TerminalControl.Rows"/>
        internal int Rows
        {
            get => _rows;

            set
            {
                if (_rows != value)
                {
                    lock (ScreenBufferLock)
                    {
                        _rows = value;
                        _terminalRenderer.ResizeOffscreenBuffer();
                        _videoTerminal.Resize();
                    }
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.Columns"/>
        internal int Columns
        {
            get => _columns;

            set
            {
                if (_columns != value)
                {
                    lock (ScreenBufferLock)
                    {
                        _columns = value;
                        _terminalRenderer.ResizeOffscreenBuffer();
                        _videoTerminal.Resize();
                    }
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.Scrollback"/>
        internal int Scrollback
        {
            get => _scrollback;

            set
            {
                if (_scrollback != value)
                {
                    _scrollback = value;
                    _videoTerminal.ResizeScrollback();
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.FontFamily"/>
        internal string FontFamily
        {
            get => _fontFamily;

            set
            {
                if (_fontFamily != value)
                {
                    _fontFamily = value;
                    _terminalRenderer.InitializeTextFormats();
                    _terminalRenderer.InvalidateLayoutCaches();
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.FontSize"/>
        internal double FontSize
        {
            get => _fontSize;

            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    _terminalRenderer.InitializeTextFormats();
                    _terminalRenderer.InvalidateLayoutCaches();
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.TextAntialiasing"/>
        internal TextAntialiasingStyle TextAntialiasing
        {
            get => _textAntialiasing;

            set
            {
                if (_textAntialiasing != value)
                {
                    _textAntialiasing = value;
                    _terminalRenderer.OffscreenBufferDirty = true;
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.FullColorEmoji"/>
        internal bool FullColorEmoji
        {
            get => _fullColorEmoji;

            set
            {
                if (_fullColorEmoji != value)
                {
                    _fullColorEmoji = value;
                    _terminalRenderer.InitializeTextFormats();
                    _terminalRenderer.InvalidateLayoutCaches();
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.UseBackgroundColorErase"/>
        internal bool UseBackgroundColorErase
        {
            get => _useBackgroundColorErase;
            set => _useBackgroundColorErase = value;
        }

        /// <inheritdoc cref="TerminalControl.BackgroundIsInvisible"/>
        internal bool BackgroundIsInvisible
        {
            get => _backgroundIsInvisible;

            set
            {
                _backgroundIsInvisible = value;
                _terminalRenderer.OffscreenBufferDirty = true;
            }
        }

        /// <inheritdoc cref="TerminalControl.UseVisualBell"/>
        internal bool UseVisualBell
        {
            get => _useVisualBell;
            set => _useVisualBell = value;
        }

        /// <inheritdoc cref="TerminalControl.CursorStyle"/>
        /// <remarks>Note that this can be changed by <see
        /// cref="Terminal.VideoTerminal"/> and must be reported back to <see
        /// cref="TerminalControl"/>.</remarks>
        internal CursorStyle CursorStyle
        {
            get => _cursorStyle;

            set
            {
                if (_cursorStyle != value)
                {
                    _cursorStyle = value;
                    _terminalControl.DispatcherQueue.TryEnqueue(() => _terminalControl.CursorStyle = _cursorStyle);
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.CursorBlink"/>
        /// <remarks>Note that this can be changed by <see
        /// cref="Terminal.VideoTerminal"/> and must be reported back to <see
        /// cref="TerminalControl"/>.</remarks>
        internal bool CursorBlink
        {
            get => _cursorBlink;

            set
            {
                if (_cursorBlink != value)
                {
                    _cursorBlink = value;
                    _terminalControl.DispatcherQueue.TryEnqueue(() => _terminalControl.CursorBlink = _cursorBlink);
                }
            }
        }

        /// <inheritdoc cref="TerminalControl.TabWidth"/>
        internal int TabWidth
        {
            get => _tabWidth;
            set => _tabWidth = value;
        }

        /// <inheritdoc cref="TerminalControl.CopyOnMouseUp"/>
        internal bool CopyOnMouseUp
        {
            get => _copyOnMouseUp;
            set => _copyOnMouseUp = value;
        }

        /// <inheritdoc cref="TerminalControl.CopyNewline"/>
        internal string CopyNewline
        {
            get => _copyNewline;
            set => _copyNewline = value;
        }

        /// <inheritdoc cref="TerminalControl.ShiftPressed"/>
        internal bool ShiftPressed => _terminalControl.ShiftPressed;

        /// <inheritdoc cref="TerminalControl.ControlPressed"/>
        internal bool ControlPressed => _terminalControl.ControlPressed;

        /// <inheritdoc cref="TerminalControl.AltPressed"/>
        internal bool AltPressed => _terminalControl.AltPressed;

        /// <inheritdoc cref="TerminalControl.LastMouseButton"/>
        internal MouseButton LastMouseButton => _terminalControl.LastMouseButton;

        /// <inheritdoc cref="TerminalControl.WindowTitle"/>
        internal string? WindowTitle
        {
            get => _terminalControl.WindowTitle;
            set => _terminalControl.DispatcherQueue.TryEnqueue(() => _terminalControl.WindowTitle = value);
        }

        /// <summary>
        /// Whether auto-repeat keys (DECARM) is in effect.
        /// </summary>
        internal bool AutoRepeatKeys
        {
            get => _autoRepeatKeys;
            set => _autoRepeatKeys = value;
        }

        /// <summary>
        /// Whether application cursor keys mode (DECCKM) is in effect.
        /// </summary>
        internal bool ApplicationCursorKeys
        {
            get => _applicationCursorKeys;
            set => _applicationCursorKeys = value;
        }

        /// <summary>
        /// Whether bracketed paste mode is in effect.
        /// </summary>
        internal bool BracketedPasteMode
        {
            get => _bracketedPasteMode;
            set => _bracketedPasteMode = value;
        }

        /// <summary>
        /// Whether in-band window resize notifications (DECSET 2048) are in
        /// effect.
        /// </summary>
        internal bool ReportResize
        {
            get => _reportResize;
            set
            {
                if (_reportResize != value)
                {
                    _reportResize = value;
                    _terminalControl.ReportWindowSize();
                }
            }
        }

        /// <summary>
        /// The mouse tracking mode that is in effect.
        /// </summary>
        internal MouseTrackingModes MouseTrackingMode
        {
            get => _mouseTrackingMode;
            set => _mouseTrackingMode = value;
        }

        /// <summary>
        /// The terminal cell size.
        /// </summary>
        internal SizeF CellSize
        {
            get => _terminalRenderer.CellSize;

            set
            {
                if (_terminalRenderer.CellSize != value)
                {
                    _terminalRenderer.CellSize = value;
                }
            }
        }

        /// <inheritdoc cref="TerminalRenderer.CellSizeDirty"/>
        internal bool CellSizeDirty => _terminalRenderer.CellSizeDirty;

        /// <inheritdoc cref="TerminalRenderer.OffscreenBuffer"/>
        internal CanvasRenderTarget? OffscreenBuffer => _terminalRenderer.OffscreenBuffer;

        /// <inheritdoc cref="TerminalRenderer.CursorDisplayed"/>
        internal bool CursorDisplayed
        {
            get => _terminalRenderer.CursorDisplayed;
            set => _terminalRenderer.CursorDisplayed = value;
        }

        /// <inheritdoc cref="TerminalRenderer.CursorVisible"/>
        internal bool CursorVisible
        {
            get => _terminalRenderer.CursorVisible;
            set => _terminalRenderer.CursorVisible = value;
        }

        /// <inheritdoc cref="VideoTerminal.Caret"/>
        internal Caret Caret => _videoTerminal.Caret;

        /// <inheritdoc cref="VideoTerminal.ScrollbackMode"/>
        internal bool ScrollbackMode => _videoTerminal.ScrollbackMode;

        /// <inheritdoc cref="VideoTerminal.TextIsSelected"/>
        internal bool TextIsSelected => _videoTerminal.TextIsSelected;

        /// <inheritdoc cref="VideoTerminal.SelectionMode"/>
        internal bool SelectionMode
        {
            get => _videoTerminal.SelectionMode;
            set => _videoTerminal.SelectionMode = value;
        }

        /// <inheritdoc cref="VideoTerminal.UseAlternateScreenBuffer"/>
        internal bool UseAlternateScreenBuffer => _videoTerminal.UseAlternateScreenBuffer;

        /// <summary>
        /// The <see cref="AnsiProcessor.AnsiReader"/> associated with the
        /// terminal.
        /// </summary>
        internal AnsiReader? AnsiReader
        {
            get => _ansiReader;

            set
            {
                if (_ansiReader != value)
                {
                    if (value is not null)
                    {
                        value.OnText += AnsiReader_OnText;
                        value.OnControlCharacter += AnsiReader_OnControlCharacter;
                        value.OnEscapeSequence += AnsiReader_OnEscapeSequence;
                    }
                    else
                    {
                        if (_ansiReader is not null)
                        {
                            _ansiReader.OnText -= AnsiReader_OnText;
                            _ansiReader.OnControlCharacter -= AnsiReader_OnControlCharacter;
                            _ansiReader.OnEscapeSequence -= AnsiReader_OnEscapeSequence;
                        }
                    }

                    _ansiReader = value;
                }
            }
        }

        /// <summary>
        /// The <see cref="AnsiProcessor.AnsiWriter"/> associated with the
        /// terminal.
        /// </summary>
        internal AnsiWriter? AnsiWriter
        {
            get => _ansiWriter;

            set
            {
                if (_ansiWriter != value)
                {
                    _ansiWriter = value;
                }
            }
        }

        /// <summary>
        /// A screen buffer lock.
        /// </summary>
        /// <remarks>It is very important to ensure a lock on this object is in
        /// place when reading from or writing to <see
        /// cref="VideoTerminal._screenBuffer"/> to avoid a race
        /// condition!</remarks>
        internal object ScreenBufferLock => _screenBufferLock;

        /// <summary>
        /// A <see cref="Terminal.VideoTerminal"/>.
        /// </summary>
        internal VideoTerminal VideoTerminal => _videoTerminal;

        /// <summary>
        /// The VT queue.
        /// </summary>
        internal ConcurrentQueue<object> VTQueue => _vtQueue;

        /// <summary>
        /// The VT queue ready event.
        /// </summary>
        internal AutoResetEvent VTQueueReady => _vtQueueReady;

        /// <summary>
        /// Initializes a <see cref="TerminalEngine"/>.
        /// </summary>
        /// <param name="terminalControl">A <see
        /// cref="TerminalControl"/>.</param>
        /// <param name="canvas"><inheritdoc cref="Canvas"
        /// path="/summary"/></param>
        public TerminalEngine(TerminalControl terminalControl, CanvasControl canvas)
        {
            logger = LoggerHelper.CreateLogger<TerminalEngine>();

            _terminalControl = terminalControl;
            _canvas = canvas;

            _palette = terminalControl.AnsiColors;
            _terminalCapabilities = terminalControl.TerminalCapabilities;

            _defaultWindowTitle = terminalControl.DefaultWindowTitle;
            _rows = terminalControl.Rows;
            _columns = terminalControl.Columns;
            _scrollback = terminalControl.Scrollback;

            _fontFamily = terminalControl.FontFamily;
            _fontSize = terminalControl.FontSize;

            _textAntialiasing = terminalControl.TextAntialiasing;
            _useBackgroundColorErase = terminalControl.UseBackgroundColorErase;
            _backgroundIsInvisible = terminalControl.BackgroundIsInvisible;
            _useVisualBell = terminalControl.UseVisualBell;
            _cursorBlink = terminalControl.CursorBlink;
            _tabWidth = terminalControl.TabWidth;
            _copyOnMouseUp = terminalControl.CopyOnMouseUp;
            _copyNewline = terminalControl.CopyNewline;

            _screenBufferLock = new();

            _terminalRenderer = new(this);
            _vtQueue = new();
            _vtQueueReady = new(false);

            _videoTerminal = new(this);
            _videoTerminal.ResizeScrollback();
        }

        /// <summary>
        /// The nominal size of the terminal, in pixels.
        /// </summary>
        internal SizeF NominalSizeInPixels => new(Columns * CellSize.Width, Rows * CellSize.Height);

        /// <summary>
        /// Instantiates the offscreen buffer.
        /// </summary>
        internal void InstantiateOffscreenBuffer() => _terminalRenderer.ResizeOffscreenBuffer(force: true);

        /// <inheritdoc cref="TerminalRenderer.UpdateRefreshRate"/>
        internal void UpdateRefreshRate() => _terminalRenderer.UpdateRefreshRate();

        /// <summary>
        /// Marks the offscreen buffer as dirty, forcing a frame draw.
        /// </summary>
        internal void MarkOffscreenBufferDirty() => _terminalRenderer.OffscreenBufferDirty = true;

        /// <summary>
        /// Invalidates <see cref="TerminalControl.Canvas"/>, asking it to
        /// redraw itself.
        /// </summary>
        /// <remarks>This must be called from the UI thread!</remarks>
        internal void InvalidateCanvas() => Canvas.Invalidate();

        /// <inheritdoc cref="TerminalRenderer.CleanCellSize"/>
        internal void CleanCellSize() => _terminalRenderer.CleanCellSize();

        /// <summary>
        /// Copies the selection to the clipboard.
        /// </summary>
        internal void CopySelection()
        {
            _videoTerminal.EndSelectionMode(copy: true);
            SelectionMode = false;
        }

        /// <inheritdoc cref="AnsiWriter.SendText"/>
        internal void SendText(string? text)
        {
            AnsiWriter?.SendText(text);
            logger?.LogInformation("Sent text \"{text}\"", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(text));
        }

        /// <inheritdoc cref="AnsiWriter.SendEscapeSequence"/>
        internal void SendEscapeSequence(byte[] escapeSequence)
        {
            AnsiWriter?.SendEscapeSequence(escapeSequence);
            logger?.LogInformation("Sent escape sequence \"{escapeSequence}\"", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(Encoding.ASCII.GetString(escapeSequence)));
        }

        /// <inheritdoc cref="AnsiWriter.SendKeystroke"/>
        internal void SendKeystroke(Keystroke keystroke)
        {
            AnsiWriter?.SendKeystroke(keystroke);

            logger?.LogTrace("Sent keystroke:");
            logger?.LogTrace("  Key: {key}", keystroke.Key);
            logger?.LogTrace("  Modifier keys: {modifierKeys}", keystroke.ModifierKeys);
            logger?.LogTrace("  Is repeat: {isRepeat}", keystroke.IsRepeat);
            logger?.LogTrace("  DECARM: {decarm}", keystroke.AutoRepeatKeys);
            logger?.LogTrace("  DECCKM: {decckm}", keystroke.ApplicationCursorKeys);
            logger?.LogTrace("  Caps Lock: {capsLock}", keystroke.CapsLock);
        }

        /// <summary>
        /// Processes a pointer press event by passing <paramref
        /// name="pointerPoint"/> to <see
        /// cref="VideoTerminal.PointerPressed"/>.
        /// </summary>
        /// <param name="pointerPoint"><inheritdoc
        /// cref="VideoTerminal.PointerPressed"
        /// path="/param[@name='pointerPoint']"/></param>
        internal void PointerPressed(PointerPoint pointerPoint) => _videoTerminal.PointerPressed(pointerPoint);

        /// <summary>
        /// Process a pointer move event by passing <paramref
        /// name="pointerPoint"/> to <see cref="VideoTerminal.PointerMoved"/>.
        /// </summary>
        /// <param name="pointerPoint"><inheritdoc
        /// cref="VideoTerminal.PointerMoved"
        /// path="/param[@name='pointerPoint']"/></param>
        internal void PointerMoved(PointerPoint pointerPoint) => _videoTerminal.PointerMoved(pointerPoint);

        /// <summary>
        /// Process a pointer release event by passing <paramref
        /// name="pointerPoint"/> to <see
        /// cref="VideoTerminal.PointerReleased"/>.
        /// </summary>
        /// <param name="pointerPoint"><inheritdoc
        /// cref="VideoTerminal.PointerReleased"
        /// path="/param[@name='pointerPoint']"/></param>
        internal void PointerReleased(PointerPoint pointerPoint) => _videoTerminal.PointerReleased(pointerPoint);

        /// <summary>
        /// Invoked when text is received by <see cref="AnsiReader"/>.
        /// </summary>
        /// <param name="text">The received text.</param>
        private void AnsiReader_OnText(string text)
        {
            logger?.LogInformation("Received text \"{text}\"", text);
            _vtQueue.Enqueue(text);
            _vtQueueReady.Set();
            AnsiReader!.Resume();
        }

        /// <summary>
        /// Invoked when a control character is received by <see
        /// cref="AnsiReader"/>.
        /// </summary>
        /// <param name="controlCharacter">The received control
        /// character.</param>
        private void AnsiReader_OnControlCharacter(char controlCharacter)
        {
            logger?.LogInformation("Received control character '{controlCharacter}'", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(controlCharacter));

            if (controlCharacter == AnsiProcessor.Ansi.C0.BEL)
            {
                if (!UseVisualBell)
                {
                    System.Media.SystemSounds.Beep.Play();
                }
                else
                {
                    _terminalControl.DispatcherQueue.TryEnqueue(() => _terminalControl.VisualBell = true);
                }

                AnsiReader!.Resume();
                return;
            }

            _vtQueue.Enqueue(controlCharacter);
            _vtQueueReady.Set();
            AnsiReader!.Resume();
        }

        /// <summary>
        /// Invoked when an escape sequence is received by <see
        /// cref="AnsiReader"/>.
        /// </summary>
        /// <param name="escapeSequence">The received <see
        /// cref="EscapeSequence"/>.</param>
        private void AnsiReader_OnEscapeSequence(EscapeSequence escapeSequence)
        {
            logger?.LogInformation("Received escape sequence \"{escapeSequence}\"", escapeSequence.RawEscapeSequence);
            _vtQueue.Enqueue(escapeSequence);
            _vtQueueReady.Set();
            AnsiReader!.Resume();
        }
    }
}
