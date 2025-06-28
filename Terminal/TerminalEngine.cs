using AnsiProcessor;
using AnsiProcessor.AnsiColors;
using AnsiProcessor.Input;
using AnsiProcessor.Output.EscapeSequences;
using AnsiProcessor.TermCap;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Input;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace Terminal {
  /// <summary>
  /// Manages interaction between the UI thread and <see
  /// cref="TerminalRenderer"/>.
  /// </summary>
  /// <remarks>Also handles events from <see cref="AnsiReader"/>.</remarks>
  internal class TerminalEngine {
#if DEBUG
    internal readonly ILogger logger;
#endif

    private readonly TerminalControl terminalControl;
    private readonly CanvasControl canvas;

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
    private TextAntialiasingStyles _textAntialiasing;
    private bool _useVisualBell;
    private CursorStyles _cursorStyle;
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

    private readonly TerminalRenderer terminalRenderer;

    private readonly VideoTerminal videoTerminal;
    private readonly ConcurrentQueue<object> vtQueue;
    private readonly AutoResetEvent vtQueueReady;

    /// <summary>
    /// The <see cref="TerminalControl"/>'s <see cref="CanvasControl"/>.
    /// </summary>
    internal CanvasControl Canvas => canvas;

    /// <inheritdoc cref="TerminalControl.HWnd"/>
    internal nint HWnd => terminalControl.HWnd;

    /// <inheritdoc cref="TerminalControl.AnsiColors"/>
    internal Palette Palette {
      get => _palette;
      set => _palette = value;
    }

    /// <inheritdoc cref="TerminalControl.TerminalCapabilities"/>
    internal TerminalCapabilities TerminalCapabilities {
      get => _terminalCapabilities;
      set => _terminalCapabilities = value;
    }

    /// <inheritdoc cref="TerminalControl.DefaultWindowTitle"/>
    internal string DefaultWindowTitle {
      get => _defaultWindowTitle;
      set => _defaultWindowTitle = value;
    }

    /// <inheritdoc cref="TerminalControl.Rows"/>
    internal int Rows {
      get => _rows;

      set {
        if (_rows != value) {
          lock (ScreenBufferLock) {
            _rows = value;

            terminalRenderer.ResizeOffscreenBuffer();
            videoTerminal.Resize();
          }
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.Columns"/>
    internal int Columns {
      get => _columns;

      set {
        if (_columns != value) {
          lock (ScreenBufferLock) {
            _columns = value;

            terminalRenderer.ResizeOffscreenBuffer();
            videoTerminal.Resize();
          }
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.Scrollback"/>
    internal int Scrollback {
      get => _scrollback;

      set {
        if (_scrollback != value) {
          _scrollback = value;

          videoTerminal.ResizeScrollback();
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.FontFamily"/>
    internal string FontFamily {
      get => _fontFamily;

      set {
        if (_fontFamily != value) {
          _fontFamily = value;

          terminalRenderer.InitializeTextFormats();
          terminalRenderer.InvalidateLayoutCaches();
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.FontSize"/>
    internal double FontSize {
      get => _fontSize;

      set {
        if (_fontSize != value) {
          _fontSize = value;

          terminalRenderer.InitializeTextFormats();
          terminalRenderer.InvalidateLayoutCaches();
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.TextAntialiasing"/>
    internal TextAntialiasingStyles TextAntialiasing {
      get => _textAntialiasing;

      set {
        _textAntialiasing = value;
        terminalRenderer.OffscreenBufferDirty = true;
      }
    }

    /// <inheritdoc cref="TerminalControl.UseBackgroundColorErase"/>
    internal bool UseBackgroundColorErase {
      get => _useBackgroundColorErase;
      set => _useBackgroundColorErase = value;
    }

    /// <inheritdoc cref="TerminalControl.BackgroundIsInvisible"/>
    internal bool BackgroundIsInvisible {
      get => _backgroundIsInvisible;

      set {
        _backgroundIsInvisible = value;
        terminalRenderer.OffscreenBufferDirty = true;
      }
    }

    /// <inheritdoc cref="TerminalControl.UseVisualBell"/>
    internal bool UseVisualBell {
      get => _useVisualBell;
      set => _useVisualBell = value;
    }

    /// <inheritdoc cref="TerminalControl.CursorStyle"/>
    /// <remarks>Note that this can be changed by <see
    /// cref="Terminal.VideoTerminal"/> and must be reported back to <see
    /// cref="TerminalControl"/>.</remarks>
    internal CursorStyles CursorStyle {
      get => _cursorStyle;

      set {
        if (_cursorStyle != value) {
          _cursorStyle = value;

          terminalControl.DispatcherQueue.TryEnqueue(() => terminalControl.CursorStyle = _cursorStyle);
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.CursorBlink"/>
    /// <remarks>Note that this can be changed by <see
    /// cref="Terminal.VideoTerminal"/> and must be reported back to <see
    /// cref="TerminalControl"/>.</remarks>
    internal bool CursorBlink {
      get => _cursorBlink;

      set {
        if (_cursorBlink != value) {
          _cursorBlink = value;

          terminalControl.DispatcherQueue.TryEnqueue(() => terminalControl.CursorBlink = _cursorBlink);
        }
      }
    }

    /// <inheritdoc cref="TerminalControl.TabWidth"/>
    internal int TabWidth {
      get => _tabWidth;
      set => _tabWidth = value;
    }

    /// <inheritdoc cref="TerminalControl.CopyOnMouseUp"/>
    internal bool CopyOnMouseUp {
      get => _copyOnMouseUp;
      set => _copyOnMouseUp = value;
    }

    /// <inheritdoc cref="TerminalControl.CopyNewline"/>
    internal string CopyNewline {
      get => _copyNewline;
      set => _copyNewline = value;
    }

    /// <inheritdoc cref="TerminalControl.ShiftPressed"/>
    internal bool ShiftPressed => terminalControl.ShiftPressed;

    /// <inheritdoc cref="TerminalControl.ControlPressed"/>
    internal bool ControlPressed => terminalControl.ControlPressed;

    /// <inheritdoc cref="TerminalControl.AltPressed"/>
    internal bool AltPressed => terminalControl.AltPressed;

    /// <inheritdoc cref="TerminalControl.LastMouseButton"/>
    internal MouseButtons LastMouseButton => terminalControl.LastMouseButton;

    /// <inheritdoc cref="TerminalControl.WindowTitle"/>
    internal string? WindowTitle {
      get => terminalControl.WindowTitle;
      set => terminalControl.DispatcherQueue.TryEnqueue(() => terminalControl.WindowTitle = value);
    }

    /// <summary>
    /// Whether auto-repeat keys (DECARM) is in effect.
    /// </summary>
    internal bool AutoRepeatKeys {
      get => _autoRepeatKeys;
      set => _autoRepeatKeys = value;
    }

    /// <summary>
    /// Whether application cursor keys mode (DECCKM) is in effect.
    /// </summary>
    internal bool ApplicationCursorKeys {
      get => _applicationCursorKeys;
      set => _applicationCursorKeys = value;
    }

    /// <summary>
    /// Whether bracketed paste mode is in effect.
    /// </summary>
    internal bool BracketedPasteMode {
      get => _bracketedPasteMode;
      set => _bracketedPasteMode = value;
    }

    /// <summary>
    /// Whether in-band window resize notifications (DECSET 2048) are in
    /// effect.
    /// </summary>
    internal bool ReportResize {
      get => _reportResize;
      set {
        if (_reportResize != value) {
          _reportResize = value;
          terminalControl.ReportWindowSize();
        }
      }
    }

    /// <summary>
    /// The mouse tracking mode that is in effect.
    /// </summary>
    internal MouseTrackingModes MouseTrackingMode {
      get => _mouseTrackingMode;
      set => _mouseTrackingMode = value;
    }

    /// <summary>
    /// The terminal cell size.
    /// </summary>
    internal SizeF CellSize {
      get => terminalRenderer.CellSize;

      set {
        if (terminalRenderer.CellSize != value) {
          terminalRenderer.CellSize = value;
        }
      }
    }

    /// <inheritdoc cref="TerminalRenderer.CellSizeDirty"/>
    internal bool CellSizeDirty => terminalRenderer.CellSizeDirty;

    /// <inheritdoc cref="TerminalRenderer.OffscreenBuffer"/>
    internal CanvasRenderTarget? OffscreenBuffer => terminalRenderer.OffscreenBuffer;

    /// <inheritdoc cref="TerminalRenderer.CursorDisplayed"/>
    internal bool CursorDisplayed {
      get => terminalRenderer.CursorDisplayed;
      set => terminalRenderer.CursorDisplayed = value;
    }

    /// <inheritdoc cref="TerminalRenderer.CursorVisible"/>
    internal bool CursorVisible {
      get => terminalRenderer.CursorVisible;
      set => terminalRenderer.CursorVisible = value;
    }

    /// <inheritdoc cref="VideoTerminal.Caret"/>
    internal Caret Caret => videoTerminal.Caret;

    /// <inheritdoc cref="VideoTerminal.ScrollbackMode"/>
    internal bool ScrollbackMode => videoTerminal.ScrollbackMode;

    /// <inheritdoc cref="VideoTerminal.TextIsSelected"/>
    internal bool TextIsSelected => videoTerminal.TextIsSelected;

    /// <inheritdoc cref="VideoTerminal.SelectionMode"/>
    internal bool SelectionMode {
      get => videoTerminal.SelectionMode;
      set => videoTerminal.SelectionMode = value;
    }

    /// <summary>
    /// The <see cref="AnsiProcessor.AnsiReader"/> associated with the
    /// terminal.
    /// </summary>
    internal AnsiReader? AnsiReader {
      get => _ansiReader;

      set {
        if (_ansiReader != value) {
          if (value is not null) {
            value.OnText += AnsiReader_OnText;
            value.OnControlCharacter += AnsiReader_OnControlCharacter;
            value.OnEscapeSequence += AnsiReader_OnEscapeSequence;
          } else {
            if (_ansiReader is not null) {
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
    internal AnsiWriter? AnsiWriter {
      get => _ansiWriter;

      set {
        if (_ansiWriter != value) {
          _ansiWriter = value;
        }
      }
    }

    /// <summary>
    /// A screen buffer lock.
    /// </summary>
    /// <remarks>It is very important to ensure a lock on this object is in
    /// place when reading from or writing to <see
    /// cref="VideoTerminal.screenBuffer"/> to avoid a race
    /// condition!</remarks>
    internal object ScreenBufferLock => _screenBufferLock;

    /// <summary>
    /// A <see cref="Terminal.VideoTerminal"/>.
    /// </summary>
    internal VideoTerminal VideoTerminal => videoTerminal;

    /// <summary>
    /// The VT queue.
    /// </summary>
    internal ConcurrentQueue<object> VTQueue => vtQueue;

    /// <summary>
    /// The VT queue ready event.
    /// </summary>
    internal AutoResetEvent VTQueueReady => vtQueueReady;

    /// <summary>
    /// Initializes a <see cref="TerminalEngine"/>.
    /// </summary>
    /// <param name="terminalControl">A <see cref="TerminalControl"/>.</param>
    /// <param name="canvas"><inheritdoc cref="Canvas"
    /// path="/summary"/></param>
    public TerminalEngine(TerminalControl terminalControl, CanvasControl canvas) {
#if DEBUG
      using ILoggerFactory factory = LoggerFactory.Create(
        builder => {
          builder.AddDebug();
          builder.SetMinimumLevel(TerminalControl.logLevel);
        }
      );

      logger = factory.CreateLogger<TerminalEngine>();
#endif

      this.terminalControl = terminalControl;
      this.canvas = canvas;

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

      terminalRenderer = new(this);
      vtQueue = new();
      vtQueueReady = new(false);

      videoTerminal = new(this);
      videoTerminal.ResizeScrollback();
    }

    /// <summary>
    /// The nominal size of the terminal, in pixels.
    /// </summary>
    internal SizeF NominalSizeInPixels => new(Columns * CellSize.Width, Rows * CellSize.Height);

    /// <summary>
    /// Instantiates the offscreen buffer.
    /// </summary>
    internal void InstantiateOffscreenBuffer() => terminalRenderer.ResizeOffscreenBuffer(force: true);

    /// <inheritdoc cref="TerminalRenderer.UpdateRefreshRate"/>
    internal void UpdateRefreshRate() => terminalRenderer.UpdateRefreshRate();

    /// <summary>
    /// Invalidates <see cref="TerminalControl.Canvas"/>, asking it to redraw
    /// itself.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0200:Remove unnecessary lambda expression", Justification = "Suggestion is incorrect, changes meaning")]
    internal void InvalidateCanvas() => terminalControl.DispatcherQueue.TryEnqueue(() => Canvas.Invalidate());

    /// <inheritdoc cref="TerminalRenderer.CleanCellSize"/>
    internal void CleanCellSize() => terminalRenderer.CleanCellSize();

    /// <summary>
    /// Copies the selection to the clipboard.
    /// </summary>
    internal void CopySelection() {
      videoTerminal.EndSelectionMode(copy: true);
      SelectionMode = false;
    }

    /// <inheritdoc cref="AnsiWriter.SendText"/>
    internal void SendText(string? text) {
      AnsiWriter?.SendText(text);

#if DEBUG
      logger.LogInformation("Sent text \"{text}\"", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(text));
#endif
    }

    /// <inheritdoc cref="AnsiWriter.SendEscapeSequence"/>
    internal void SendEscapeSequence(byte[] escapeSequence, bool brokenMode = false) {
      AnsiWriter?.SendEscapeSequence(escapeSequence, brokenMode);

#if DEBUG
      logger.LogInformation("Sent escape sequence \"{escapeSequence}\"", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(Encoding.ASCII.GetString(escapeSequence)));
#endif
    }

    /// <inheritdoc cref="AnsiWriter.SendKeystroke"/>
    internal void SendKeystroke(Keystroke keystroke) {
      AnsiWriter?.SendKeystroke(keystroke);

#if DEBUG
      logger.LogTrace("Sent keystroke:");
      logger.LogTrace("  Key: {key}", keystroke.Key);
      logger.LogTrace("  Modifier keys: {modifierKeys}", keystroke.ModifierKeys);
      logger.LogTrace("  Is repeat: {isRepeat}", keystroke.IsRepeat);
      logger.LogTrace("  DECARM: {decarm}", keystroke.AutoRepeatKeys);
      logger.LogTrace("  DECCKM: {decckm}", keystroke.ApplicationCursorKeys);
      logger.LogTrace("  Caps Lock: {capsLock}", keystroke.CapsLock);
#endif
    }

    /// <summary>
    /// Processes a pointer press event by passing <paramref
    /// name="pointerPoint"/> to <see cref="VideoTerminal.PointerPressed"/>.
    /// </summary>
    /// <param name="pointerPoint"><inheritdoc
    /// cref="VideoTerminal.PointerPressed"
    /// path="/param[@name='pointerPoint']"/></param>
    internal void PointerPressed(PointerPoint pointerPoint) => videoTerminal.PointerPressed(pointerPoint);

    /// <summary>
    /// Process a pointer move event by passing <paramref name="pointerPoint"/>
    /// to <see cref="VideoTerminal.PointerMoved"/>.
    /// </summary>
    /// <param name="pointerPoint"><inheritdoc
    /// cref="VideoTerminal.PointerMoved"
    /// path="/param[@name='pointerPoint']"/></param>
    internal void PointerMoved(PointerPoint pointerPoint) => videoTerminal.PointerMoved(pointerPoint);

    /// <summary>
    /// Process a pointer release event by passing <paramref
    /// name="pointerPoint"/> to <see cref="VideoTerminal.PointerReleased"/>.
    /// </summary>
    /// <param name="pointerPoint"><inheritdoc
    /// cref="VideoTerminal.PointerReleased"
    /// path="/param[@name='pointerPoint']"/></param>
    internal void PointerReleased(PointerPoint pointerPoint) => videoTerminal.PointerReleased(pointerPoint);

    /// <summary>
    /// Invoked when text is received by <see cref="AnsiReader"/>.
    /// </summary>
    /// <param name="text">The received text.</param>
    private void AnsiReader_OnText(string text) {
#if DEBUG
      logger.LogInformation("Received text \"{text}\"", text);
#endif

      vtQueue.Enqueue(text);
      vtQueueReady.Set();
      AnsiReader!.Resume();
    }

    /// <summary>
    /// Invoked when a control character is received by <see
    /// cref="AnsiReader"/>.
    /// </summary>
    /// <param name="controlCharacter">The received control character.</param>
    private void AnsiReader_OnControlCharacter(char controlCharacter) {
#if DEBUG
      logger.LogInformation("Received control character '{controlCharacter}'", AnsiProcessor.Helpers.PrintableHelper.MakePrintable(controlCharacter));
#endif

      if (controlCharacter == AnsiProcessor.Ansi.C0.BEL) {
        if (!UseVisualBell) {
          System.Media.SystemSounds.Beep.Play();
        } else {
          terminalControl.DispatcherQueue.TryEnqueue(() => terminalControl.VisualBell = true);
        }

        AnsiReader!.Resume();
        return;
      }

      vtQueue.Enqueue(controlCharacter);
      vtQueueReady.Set();
      AnsiReader!.Resume();
    }

    /// <summary>
    /// Invoked when an escape sequence is received by <see
    /// cref="AnsiReader"/>.
    /// </summary>
    /// <param name="escapeSequence">The received <see
    /// cref="EscapeSequence"/>.</param>
    private void AnsiReader_OnEscapeSequence(EscapeSequence escapeSequence) {
#if DEBUG
      logger.LogInformation("Received escape sequence \"{escapeSequence}\"", escapeSequence.RawEscapeSequence);
#endif

      vtQueue.Enqueue(escapeSequence);
      vtQueueReady.Set();
      AnsiReader!.Resume();
    }
  }
}
