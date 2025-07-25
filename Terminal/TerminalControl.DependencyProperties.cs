using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.TermCap;
using Spakov.Terminal.Helpers;
using System.IO;
using Windows.UI;

namespace Spakov.Terminal
{
    public sealed partial class TerminalControl : UserControl
    {
        /// <summary>
        /// <inheritdoc cref="ConsoleOutputProperty"/>
        /// </summary>
        public FileStream? ConsoleOutput
        {
            get => (FileStream?)GetValue(ConsoleOutputProperty);
            set => SetValue(ConsoleOutputProperty, value);
        }

        /// <summary>
        /// The console's output <see cref="FileStream"/>.
        /// </summary>
        public static readonly DependencyProperty ConsoleOutputProperty = DependencyProperty.Register(
            nameof(ConsoleOutput),
            typeof(FileStream),
            typeof(TerminalControl),
            new PropertyMetadata(null, OnConsoleOutputChanged)
        );

        /// <summary>
        /// Invoked when the console output changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnConsoleOutputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.AnsiReader = terminalControl.ConsoleOutput is not null
                ? new(
                    terminalControl.ConsoleOutput,
                    terminalControl.TerminalCapabilities,
                    terminalControl.AnsiColors
                )
                : null;
        }

        /// <summary>
        /// <inheritdoc cref="ConsoleInputProperty"/>
        /// </summary>
        public FileStream? ConsoleInput
        {
            get => (FileStream?)GetValue(ConsoleInputProperty);
            set => SetValue(ConsoleInputProperty, value);
        }

        /// <summary>
        /// The console's input <see cref="FileStream"/>.
        /// </summary>
        public static readonly DependencyProperty ConsoleInputProperty = DependencyProperty.Register(
            nameof(ConsoleInput),
            typeof(FileStream),
            typeof(TerminalControl),
            new PropertyMetadata(null, OnConsoleInputChanged)
        );

        /// <summary>
        /// Invoked when the console input changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnConsoleInputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.AnsiWriter = terminalControl.ConsoleInput is not null
                ? new(
                    terminalControl.ConsoleInput,
                    terminalControl.TerminalCapabilities
                )
                : null;
        }

        /// <summary>
        /// <inheritdoc cref="AnsiColorsProperty"/>
        /// </summary>
        public Palette AnsiColors
        {
            get => (Palette)GetValue(AnsiColorsProperty);
            set => SetValue(AnsiColorsProperty, value);
        }

        /// <summary>
        /// The <see cref="Palette"/> that should be used for ANSI colors.
        /// </summary>
        public static readonly DependencyProperty AnsiColorsProperty = DependencyProperty.Register(
            nameof(AnsiColors),
            typeof(Palette),
            typeof(TerminalControl),
            new PropertyMetadata(new Palette(), OnAnsiColorsChanged)
        );

        /// <summary>
        /// Invoked when the ANSI colors change.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnAnsiColorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            if (terminalControl._terminalEngine.AnsiReader is not null)
            {
                terminalControl._terminalEngine.AnsiReader.Palette = terminalControl.AnsiColors;
            }

            terminalControl._terminalEngine.Palette = terminalControl.AnsiColors;
            terminalControl._terminalEngine.MarkOffscreenBufferDirty();
        }

        /// <summary>
        /// <inheritdoc cref="TerminalCapabilitiesProperty"/>
        /// </summary>
        public TerminalCapabilities TerminalCapabilities
        {
            get => (TerminalCapabilities)GetValue(TerminalCapabilitiesProperty);
            set => SetValue(TerminalCapabilitiesProperty, value);
        }

        /// <summary>
        /// The <see cref="AnsiProcessor.TermCap.TerminalCapabilities"/>
        /// associated with the terminal.
        /// </summary>
        public static readonly DependencyProperty TerminalCapabilitiesProperty = DependencyProperty.Register(
            nameof(TerminalCapabilities),
            typeof(TerminalCapabilities),
            typeof(TerminalControl),
            new PropertyMetadata(new TerminalCapabilities(), OnTerminalCapabilitiesChanged)
        );

        /// <summary>
        /// Invoked when the <see
        /// cref="AnsiProcessor.TermCap.TerminalCapabilities"/> changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnTerminalCapabilitiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.TerminalCapabilities = terminalControl.TerminalCapabilities;
        }

        /// <summary>
        /// <inheritdoc cref="DefaultWindowTitleProperty"/>
        /// </summary>
        public string DefaultWindowTitle
        {
            get => (string)GetValue(DefaultWindowTitleProperty);
            set => SetValue(DefaultWindowTitleProperty, value);
        }

        /// <summary>
        /// The terminal default window title.
        /// </summary>
        public static readonly DependencyProperty DefaultWindowTitleProperty = DependencyProperty.Register(
            nameof(DefaultWindowTitle),
            typeof(string),
            typeof(TerminalControl),
            new PropertyMetadata("TerminalControl", OnDefaultWindowTitleChanged)
        );

        /// <summary>
        /// Invoked when the terminal default window title changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnDefaultWindowTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl.WindowTitle ??= terminalControl.DefaultWindowTitle;
        }

        /// <summary>
        /// <inheritdoc cref="RowsProperty"/>
        /// </summary>
        public int Rows
        {
            get => (int)GetValue(RowsProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(RowsProperty, 1);
                }
                else
                {
                    SetValue(RowsProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of terminal rows.
        /// </summary>
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            nameof(Rows),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(24, OnRowsChanged)
        );

        /// <summary>
        /// Invoked when the number of rows changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnRowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl.logger?.LogDebug("Rows changed: {rows}", terminalControl.Rows);
            terminalControl._terminalEngine.Rows = terminalControl.Rows;
            terminalControl.InvalidateMeasure();
            terminalControl.ReportWindowSize();
            terminalControl.TerminalResize?.Invoke();
        }

        /// <summary>
        /// <inheritdoc cref="ColumnsProperty"/>
        /// </summary>
        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(ColumnsProperty, 1);
                }
                else
                {
                    SetValue(ColumnsProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of terminal columns.
        /// </summary>
        public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
            nameof(Columns),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(80, OnColumnsChanged)
        );

        /// <summary>
        /// Invoked when the number of columns changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnColumnsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl.logger?.LogDebug("Columns changed: {columns}", terminalControl.Columns);
            terminalControl._terminalEngine.Columns = terminalControl.Columns;
            terminalControl.InvalidateMeasure();
            terminalControl.ReportWindowSize();
            terminalControl.TerminalResize?.Invoke();
        }

        /// <summary>
        /// <inheritdoc cref="ScrollbackProperty"/>
        /// </summary>
        public int Scrollback
        {
            get => (int)GetValue(ScrollbackProperty);

            set
            {
                if (value < 0)
                {
                    SetValue(ScrollbackProperty, 0);
                }
                else
                {
                    SetValue(ScrollbackProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of scrollback lines.
        /// </summary>
        public static readonly DependencyProperty ScrollbackProperty = DependencyProperty.Register(
            nameof(Scrollback),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(5000, OnScrollbackChanged)
        );

        /// <summary>
        /// Invoked when the scrollback line count changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnScrollbackChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.Scrollback = terminalControl.Scrollback;
        }

        /// <summary>
        /// <inheritdoc cref="LinesPerScrollbackProperty"/>
        /// </summary>
        public int LinesPerScrollback
        {
            get => (int)GetValue(LinesPerScrollbackProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(LinesPerScrollbackProperty, 1);
                }
                else
                {
                    SetValue(LinesPerScrollbackProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of lines to shift when using scrollback via Shift-Page
        /// Up and Shift-Page Down.
        /// </summary>
        public static readonly DependencyProperty LinesPerScrollbackProperty = DependencyProperty.Register(
            nameof(LinesPerScrollback),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(12)
        );

        /// <summary>
        /// <inheritdoc cref="LinesPerSmallScrollbackProperty"/>
        /// </summary>
        public int LinesPerSmallScrollback
        {
            get => (int)GetValue(LinesPerSmallScrollbackProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(LinesPerSmallScrollbackProperty, 1);
                }
                else
                {
                    SetValue(LinesPerSmallScrollbackProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of lines to shift when using scrollback via Shift-Up and
        /// and Shift-Down.
        /// </summary>
        public static readonly DependencyProperty LinesPerSmallScrollbackProperty = DependencyProperty.Register(
            nameof(LinesPerSmallScrollback),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(1)
        );

        /// <summary>
        /// <inheritdoc cref="LinesPerWheelScrollbackProperty"/>
        /// </summary>
        public int LinesPerWheelScrollback
        {
            get => (int)GetValue(LinesPerWheelScrollbackProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(LinesPerWheelScrollbackProperty, 1);
                }
                else
                {
                    SetValue(LinesPerWheelScrollbackProperty, value);
                }
            }
        }

        /// <summary>
        /// The number of lines to shift when using scrollback via the mouse
        /// wheel.
        /// </summary>
        public static readonly DependencyProperty LinesPerWheelScrollbackProperty = DependencyProperty.Register(
            nameof(LinesPerWheelScrollback),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(8)
        );

        /// <summary>
        /// <inheritdoc cref="FontFamilyProperty"/>
        /// </summary>
        public new string FontFamily
        {
            get => (string)GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// The terminal font family.
        /// </summary>
        public static new readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
            nameof(FontFamily),
            typeof(string),
            typeof(TerminalControl),
            new PropertyMetadata("0xProto Nerd Font Propo", OnFontFamilyChanged)
        );

        /// <summary>
        /// Invoked when the font family changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.FontFamily = terminalControl.FontFamily;
            terminalControl.InvalidateMeasure();
        }

        /// <summary>
        /// <inheritdoc cref="FontSizeProperty"/>
        /// </summary>
        public new double FontSize
        {
            get => (double)GetValue(FontSizeProperty);

            set
            {
                if (value < 1.0)
                {
                    SetValue(FontSizeProperty, 1.0);
                }
                else
                {
                    SetValue(FontSizeProperty, value);
                }
            }
        }

        /// <summary>
        /// The terminal font size.
        /// </summary>
        public static new readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
            nameof(FontSize),
            typeof(double),
            typeof(TerminalControl),
            new PropertyMetadata(14.0, OnFontSizeChanged)
        );

        /// <summary>
        /// Invoked when the font size changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.FontSize = terminalControl.FontSize;
            terminalControl.InvalidateMeasure();
        }

        /// <summary>
        /// <inheritdoc cref="TextAntialiasingProperty"/>
        /// </summary>
        public TextAntialiasingStyle TextAntialiasing
        {
            get => (TextAntialiasingStyle)GetValue(TextAntialiasingProperty);
            set => SetValue(TextAntialiasingProperty, value);
        }

        /// <summary>
        /// The terminal text antialiasing style.
        /// </summary>
        public static readonly DependencyProperty TextAntialiasingProperty = DependencyProperty.Register(
            nameof(TextAntialiasing),
            typeof(TextAntialiasingStyle),
            typeof(TerminalControl),
            new PropertyMetadata(TextAntialiasingStyle.Grayscale, OnTextAntialiasingChanged)
        );

        /// <summary>
        /// Invoked when the text antialiasing changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnTextAntialiasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl.TerminalEngine.TextAntialiasing = terminalControl.TextAntialiasing;
            terminalControl._terminalEngine.MarkOffscreenBufferDirty();
        }

        /// <summary>
        /// <inheritdoc cref="FullColorEmojiProperty"/>
        /// </summary>
        public bool FullColorEmoji
        {
            get => (bool)GetValue(FullColorEmojiProperty);
            set => SetValue(FullColorEmojiProperty, value);
        }

        /// <summary>
        /// Whether the terminal should draw full-color emoji.
        /// </summary>
        public static readonly DependencyProperty FullColorEmojiProperty = DependencyProperty.Register(
            nameof(FullColorEmoji),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(false, OnFullColorEmojiChanged)
        );

        /// <summary>
        /// Invoked when the full-color emoji property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnFullColorEmojiChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl.TerminalEngine.FullColorEmoji = terminalControl.FullColorEmoji;
            terminalControl._terminalEngine.MarkOffscreenBufferDirty();
        }

        /// <summary>
        /// <inheritdoc cref="UseBackgroundColorEraseProperty"/>
        /// </summary>
        public bool UseBackgroundColorErase
        {
            get => (bool)GetValue(UseBackgroundColorEraseProperty);
            set => SetValue(UseBackgroundColorEraseProperty, value);
        }

        /// <summary>
        /// Whether escape sequences that erase characters should cause the
        /// cell to use the current graphic rendition background color.
        /// </summary>
        /// <remarks>An excellent visualization of what this means can be found
        /// here: <see
        /// href="https://sunaku.github.io/vim-256color-bce.html"/></remarks>
        public static readonly DependencyProperty UseBackgroundColorEraseProperty = DependencyProperty.Register(
            nameof(UseBackgroundColorErase),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnUseBackgroundColorEraseChanged)
        );

        /// <summary>
        /// Invoked when use background color erase changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnUseBackgroundColorEraseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.UseBackgroundColorErase = terminalControl.UseBackgroundColorErase;
            terminalControl._terminalEngine.MarkOffscreenBufferDirty();
        }

        /// <summary>
        /// <inheritdoc cref="BackgroundIsInvisibleProperty"/>
        /// </summary>
        public bool BackgroundIsInvisible
        {
            get => (bool)GetValue(BackgroundIsInvisibleProperty);
            set => SetValue(BackgroundIsInvisibleProperty, value);
        }

        /// <summary>
        /// Whether backgrounds should be rendered invisible and "show through"
        /// to the terminal background.
        /// </summary>
        public static readonly DependencyProperty BackgroundIsInvisibleProperty = DependencyProperty.Register(
            nameof(BackgroundIsInvisible),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnBackgroundIsInvisibleChanged)
        );

        /// <summary>
        /// Invoked when the background is invisible property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnBackgroundIsInvisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.BackgroundIsInvisible = terminalControl.BackgroundIsInvisible;

            if (terminalControl.BackgroundIsInvisibleMenuItem is not null)
            {
                terminalControl.BackgroundIsInvisibleMenuItem.IsChecked = terminalControl.BackgroundIsInvisible;
            }
        }

        /// <summary>
        /// <inheritdoc cref="UseVisualBellProperty"/>
        /// </summary>
        public bool UseVisualBell
        {
            get => (bool)GetValue(UseVisualBellProperty);
            set => SetValue(UseVisualBellProperty, value);
        }

        /// <summary>
        /// Whether to use a visual bell.
        /// </summary>
        public static readonly DependencyProperty UseVisualBellProperty = DependencyProperty.Register(
            nameof(UseVisualBell),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnUseVisualBellChanged)
        );

        /// <summary>
        /// Invoked when the use visual bell property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnUseVisualBellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.UseVisualBell = terminalControl.UseVisualBell;

            if (terminalControl.UseVisualBellMenuItem is not null)
            {
                terminalControl.UseVisualBellMenuItem.IsChecked = terminalControl.UseVisualBell;
            }
        }

        /// <summary>
        /// <inheritdoc cref="CursorStyleProperty"/>
        /// </summary>
        public CursorStyle CursorStyle
        {
            get => (CursorStyle)GetValue(CursorStyleProperty);
            set => SetValue(CursorStyleProperty, value);
        }

        /// <summary>
        /// The cursor style.
        /// </summary>
        public static readonly DependencyProperty CursorStyleProperty = DependencyProperty.Register(
            nameof(CursorStyle),
            typeof(CursorStyle),
            typeof(TerminalControl),
            new PropertyMetadata(CursorStyle.Block, OnCursorStyleChanged)
        );

        /// <summary>
        /// Invoked when the cursor style property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnCursorStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.CursorStyle = terminalControl.CursorStyle;

            if (terminalControl.BlockCursorMenuItem is not null)
            {
                terminalControl.BlockCursorMenuItem.IsChecked = terminalControl.CursorStyle == CursorStyle.Block;
            }

            if (terminalControl.UnderlineCursorMenuItem is not null)
            {
                terminalControl.UnderlineCursorMenuItem.IsChecked = terminalControl.CursorStyle == CursorStyle.Underline;
            }

            if (terminalControl.BarCursorMenuItem is not null)
            {
                terminalControl.BarCursorMenuItem.IsChecked = terminalControl.CursorStyle == CursorStyle.Bar;
            }
        }

        /// <summary>
        /// <inheritdoc cref="CursorThicknessProperty"/>
        /// </summary>
        public double CursorThickness
        {
            get => (double)GetValue(CursorThicknessProperty);

            set
            {
                if (value < 0.0)
                {
                    SetValue(CursorThicknessProperty, 0.0);
                }
                else if (value > 1.0)
                {
                    SetValue(CursorThicknessProperty, 1.0);
                }
                else
                {
                    SetValue(CursorThicknessProperty, value);
                }
            }
        }

        /// <summary>
        /// The thickness of the non-bar cursor, as a font size multiplier.
        /// </summary>
        public static readonly DependencyProperty CursorThicknessProperty = DependencyProperty.Register(
            nameof(CursorThickness),
            typeof(double),
            typeof(TerminalControl),
            new PropertyMetadata(0.1)
        );

        /// <summary>
        /// <inheritdoc cref="CursorBlinkProperty"/>
        /// </summary>
        public bool CursorBlink
        {
            get => (bool)GetValue(CursorBlinkProperty);
            set => SetValue(CursorBlinkProperty, value);
        }

        /// <summary>
        /// Whether the cursor should blink.
        /// </summary>
        public static readonly DependencyProperty CursorBlinkProperty = DependencyProperty.Register(
            nameof(CursorBlink),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnCursorBlinkChanged)
        );

        /// <summary>
        /// <inheritdoc cref="CursorBlinkRateProperty"/>
        /// </summary>
        public int CursorBlinkRate
        {
            get => (int)GetValue(CursorBlinkRateProperty);

            set
            {
                if (value < 0)
                {
                    SetValue(CursorBlinkRateProperty, 0);
                }
                else
                {
                    SetValue(CursorBlinkRateProperty, value);
                }
            }
        }

        /// <summary>
        /// How many milliseconds should elapse between cursor blinks.
        /// </summary>
        public static readonly DependencyProperty CursorBlinkRateProperty = DependencyProperty.Register(
          nameof(CursorBlinkRate),
          typeof(int),
          typeof(TerminalControl),
          new PropertyMetadata(500, OnCursorBlinkChanged)
        );

        /// <summary>
        /// Invoked when the cursor blink property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnCursorBlinkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.CursorBlink = terminalControl.CursorBlink;

            if (terminalControl.CursorTimer is not null)
            {
                CursorHelper.DestroyCursorTimer(terminalControl);
            }

            CursorHelper.SetUpCursorTimer(terminalControl);

            if (terminalControl.CursorBlinkMenuItem is not null)
            {
                terminalControl.CursorBlinkMenuItem.IsChecked = terminalControl.CursorBlink;
            }
        }

        /// <summary>
        /// <inheritdoc cref="CursorColorProperty"/>
        /// </summary>
        public Color CursorColor
        {
            get => (Color)GetValue(CursorColorProperty);
            set => SetValue(CursorColorProperty, value);
        }

        /// <summary>
        /// The color of the cursor.
        /// </summary>
        public static readonly DependencyProperty CursorColorProperty = DependencyProperty.Register(
            nameof(CursorColor),
            typeof(Color),
            typeof(TerminalControl),
            new PropertyMetadata(new Color() { A = 0xff, R = 0xba, G = 0xc2, B = 0xde })
        );

        /// <summary>
        /// <inheritdoc cref="TabWidthProperty"/>
        /// </summary>
        public int TabWidth
        {
            get => (int)GetValue(TabWidthProperty);

            set
            {
                if (value < 1)
                {
                    SetValue(TabWidthProperty, 1);
                }
                else
                {
                    SetValue(TabWidthProperty, value);
                }
            }
        }

        /// <summary>
        /// The width of a tab, in spaces.
        /// </summary>
        public static readonly DependencyProperty TabWidthProperty = DependencyProperty.Register(
            nameof(TabWidth),
            typeof(int),
            typeof(TerminalControl),
            new PropertyMetadata(8)
        );

        /// <summary>
        /// <inheritdoc cref="CopyOnMouseUpProperty"/>
        /// </summary>
        public bool CopyOnMouseUp
        {
            get => (bool)GetValue(CopyOnMouseUpProperty);
            set => SetValue(CopyOnMouseUpProperty, value);
        }

        /// <summary>
        /// Whether to copy text immediately after releasing the mouse button
        /// after selecting it.
        /// </summary>
        public static readonly DependencyProperty CopyOnMouseUpProperty = DependencyProperty.Register(
            nameof(CopyOnMouseUp),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnCopyOnMouseUpChanged)
        );

        /// <summary>
        /// Invoked when the copy on mouse up property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnCopyOnMouseUpChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.CopyOnMouseUp = terminalControl.CopyOnMouseUp;

            if (terminalControl.CopyOnMouseUp)
            {
                if (terminalControl.ContextMenu is not null)
                {
                    if (terminalControl.ContextMenu.Items.Contains(terminalControl.CopyMenuItem))
                    {
                        terminalControl.ContextMenu.Items.RemoveAt(0);
                    }
                }
            }
            else
            {
                if (terminalControl.ContextMenu is not null)
                {
                    if (!terminalControl.ContextMenu.Items.Contains(terminalControl.CopyMenuItem))
                    {
                        terminalControl.ContextMenu.Items.Insert(0, terminalControl.CopyMenuItem);
                    }
                }
            }

            if (terminalControl.CopyOnMouseUpMenuItem is not null)
            {
                terminalControl.CopyOnMouseUpMenuItem.IsChecked = terminalControl.CopyOnMouseUp;
            }
        }

        /// <summary>
        /// <inheritdoc cref="CopyNewlineProperty"/>
        /// </summary>
        public string CopyNewline
        {
            get => (string)GetValue(CopyNewlineProperty);
            set => SetValue(CopyNewlineProperty, value);
        }

        /// <summary>
        /// The newline string to insert between copied lines.
        /// </summary>
        public static readonly DependencyProperty CopyNewlineProperty = DependencyProperty.Register(
            nameof(CopyNewline),
            typeof(string),
            typeof(TerminalControl),
            new PropertyMetadata("\r\n", OnCopyNewlineChanged)
        );

        /// <summary>
        /// Invoked when the copy newline property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnCopyNewlineChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            terminalControl._terminalEngine.CopyNewline = terminalControl.CopyNewline;
        }

        /// <summary>
        /// <inheritdoc cref="PasteOnRightClickProperty"/>
        /// </summary>
        public bool PasteOnRightClick
        {
            get => (bool)GetValue(PasteOnRightClickProperty);
            set => SetValue(PasteOnRightClickProperty, value);
        }

        /// <summary>
        /// Whether to paste text when right-clicking the terminal.
        /// </summary>
        /// <remarks>If <see langword="true"/>, the context menu is displayed
        /// with Ctrl+Right Click. If <see langword="false"/>, the context menu
        /// is displayed with Right Click.</remarks>
        public static readonly DependencyProperty PasteOnRightClickProperty = DependencyProperty.Register(
            nameof(PasteOnRightClick),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(false, OnPasteOnRightClickChanged)
        );

        /// <summary>
        /// Invoked when the paste on right click property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnPasteOnRightClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            if (terminalControl.PasteOnRightClickMenuItem is not null)
            {
                terminalControl.PasteOnRightClickMenuItem.IsChecked = terminalControl.PasteOnRightClick;
            }
        }

        /// <summary>
        /// <inheritdoc cref="PasteOnMiddleClickProperty"/>
        /// </summary>
        public bool PasteOnMiddleClick
        {
            get => (bool)GetValue(PasteOnMiddleClickProperty);
            set => SetValue(PasteOnMiddleClickProperty, value);
        }

        /// <summary>
        /// Whether to paste text when middle-clicking the terminal.
        /// </summary>
        public static readonly DependencyProperty PasteOnMiddleClickProperty = DependencyProperty.Register(
            nameof(PasteOnMiddleClick),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnPasteOnMiddleClickChanged)
        );

        /// <summary>
        /// Invoked when the paste on middle click property changes.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnPasteOnMiddleClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TerminalControl terminalControl = (TerminalControl)d;

            if (terminalControl.PasteOnMiddleClickMenuItem is not null)
            {
                terminalControl.PasteOnMiddleClickMenuItem.IsChecked = terminalControl.PasteOnMiddleClick;
            }
        }

        /// <summary>
        /// <inheritdoc cref="UseContextMenuProperty"/>
        /// </summary>
        public bool UseContextMenu
        {
            get => (bool)GetValue(UseContextMenuProperty);
            set => SetValue(UseContextMenuProperty, value);
        }

        /// <summary>
        /// Whether to display a context menu on Right Click (or Ctrl+Right
        /// Click, if <see cref="PasteOnRightClick"/> is <see
        /// langword="true"/>).
        /// </summary>
        public static readonly DependencyProperty UseContextMenuProperty = DependencyProperty.Register(
            nameof(UseContextMenu),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnUseContextMenuChanged)
        );

        /// <summary>
        /// <inheritdoc cref="UseExtendedContextMenuProperty"/>
        /// </summary>
        public bool UseExtendedContextMenu
        {
            get => (bool)GetValue(UseExtendedContextMenuProperty);
            set => SetValue(UseExtendedContextMenuProperty, value);
        }

        /// <summary>
        /// Whether to display an extended context menu if <see
        /// cref="UseContextMenu"/> is <see langword="true"/>.
        /// </summary>
        public static readonly DependencyProperty UseExtendedContextMenuProperty = DependencyProperty.Register(
            nameof(UseExtendedContextMenu),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(true, OnUseContextMenuChanged)
        );

        /// <summary>
        /// Invoked when the use context menu property or the use extended context
        /// menu properties change.
        /// </summary>
        /// <param name="d"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='sender']"/></param>
        /// <param name="e"><inheritdoc
        /// cref="DependencyPropertyChangedEventHandler"
        /// path="/param[@name='e']"/></param>
        private static void OnUseContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => MenuHelper.InitializeContextMenu((TerminalControl)d);

        /// <summary>
        /// <inheritdoc cref="ShowSettingsSaveAsDefaultsButtonProperty"/>
        /// </summary>
        public bool ShowSettingsSaveAsDefaultsButton
        {
            get => (bool)GetValue(ShowSettingsSaveAsDefaultsButtonProperty);
            set => SetValue(ShowSettingsSaveAsDefaultsButtonProperty, value);
        }

        /// <summary>
        /// Whether to show the save defaults button in the settings window.
        /// </summary>
        public static readonly DependencyProperty ShowSettingsSaveAsDefaultsButtonProperty = DependencyProperty.Register(
            nameof(ShowSettingsSaveAsDefaultsButton),
            typeof(bool),
            typeof(TerminalControl),
            new PropertyMetadata(false)
        );
    }
}
