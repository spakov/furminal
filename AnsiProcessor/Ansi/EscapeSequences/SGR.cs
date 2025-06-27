namespace AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The select graphic rendition (SGR) escape sequences, which are <see
  /// cref="C0.ESC"/>, followed by <see cref="Fe.CSI"/>, followed by an encoded
  /// integer (the constants in <see cref="SGR"/>), followed by <see
  /// cref="SGR.TERMINATOR"/>.
  /// </summary>
  /// <remarks>
  /// <para>Sources:</para>
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Select_Graphic_Rendition_parameters"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// </list>
  /// <para>I see absolutely no non-nefarious reason for choosing to use 8 and
  /// 28 these days, so I am explicitly deciding not to support those.</para>
  /// </remarks>
  public static class SGR {
    /// <summary>
    /// The empty SGR character sequence.
    /// </summary>
    /// <remarks>This is functionally equivalent to <see
    /// cref="RESET"/>.</remarks>
    public const string EMPTY = "";

    /// <summary>
    /// The SGR reset character sequence.
    /// </summary>
    public const string RESET = "0";

    /// <summary>
    /// The SGR bold font weight character sequence.
    /// </summary>
    public const string BOLD = "1";

    /// <summary>
    /// The SGR faint character sequence.
    /// </summary>
    public const string FAINT = "2";

    /// <summary>
    /// The SGR italic character sequence.
    /// </summary>
    public const string ITALIC = "3";

    /// <summary>
    /// The SGR underline character sequence.
    /// </summary>
    public const string UNDERLINE = "4";

    /// <summary>
    /// The SGR blink character sequence.
    /// </summary>
    public const string BLINK = "5";

    /// <summary>
    /// The SGR inverse character sequence.
    /// </summary>
    public const string INVERSE = "7";

    /// <summary>
    /// The SGR crossed-out character sequence.
    /// </summary>
    public const string CROSSED_OUT = "9";

    /// <summary>
    /// The SGR double-underline character sequence.
    /// </summary>
    public const string DOUBLE_UNDERLINE = "21";

    /// <summary>
    /// The SGR normal font weight and non-faint character sequence.
    /// </summary>
    public const string NORMAL = "22";

    /// <summary>
    /// The SGR no italic character sequence.
    /// </summary>
    public const string NO_ITALIC = "23";

    /// <summary>
    /// The SGR no underline character sequence.
    /// </summary>
    public const string NO_UNDERLINE = "24";

    /// <summary>
    /// The SGR no blink character sequence.
    /// </summary>
    public const string NO_BLINK = "25";

    /// <summary>
    /// The SGR no inverse character sequence.
    /// </summary>
    public const string NO_INVERSE = "27";

    /// <summary>
    /// The SGR no crossed-out character sequence.
    /// </summary>
    public const string NO_CROSSED_OUT = "29";

    /// <summary>
    /// The SGR black foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BLACK = "30";

    /// <summary>
    /// The SGR red foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_RED = "31";

    /// <summary>
    /// The SGR green foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_GREEN = "32";

    /// <summary>
    /// The SGR yellow foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_YELLOW = "33";

    /// <summary>
    /// The SGR blue foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BLUE = "34";

    /// <summary>
    /// The SGR magenta foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_MAGENTA = "35";

    /// <summary>
    /// The SGR cyan foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_CYAN = "36";

    /// <summary>
    /// The SGR white foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_WHITE = "37";

    /// <summary>
    /// The SGR extended foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_EXTENDED = "38";

    /// <summary>
    /// The SGR default foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_DEFAULT = "39";

    /// <summary>
    /// The SGR black background color character sequence.
    /// </summary>
    public const string BACKGROUND_BLACK = "40";

    /// <summary>
    /// The SGR red background color character sequence.
    /// </summary>
    public const string BACKGROUND_RED = "41";

    /// <summary>
    /// The SGR green background color character sequence.
    /// </summary>
    public const string BACKGROUND_GREEN = "42";

    /// <summary>
    /// The SGR yellow background color character sequence.
    /// </summary>
    public const string BACKGROUND_YELLOW = "43";

    /// <summary>
    /// The SGR blue background color character sequence.
    /// </summary>
    public const string BACKGROUND_BLUE = "44";

    /// <summary>
    /// The SGR magenta background color character sequence.
    /// </summary>
    public const string BACKGROUND_MAGENTA = "45";

    /// <summary>
    /// The SGR cyan background color character sequence.
    /// </summary>
    public const string BACKGROUND_CYAN = "46";

    /// <summary>
    /// The SGR white background color character sequence.
    /// </summary>
    public const string BACKGROUND_WHITE = "47";

    /// <summary>
    /// The SGR extended background color character sequence.
    /// </summary>
    public const string BACKGROUND_EXTENDED = "48";

    /// <summary>
    /// The SGR default background color character sequence.
    /// </summary>
    public const string BACKGROUND_DEFAULT = "49";

    /// <summary>
    /// The SGR underline color.
    /// </summary>
    public const string UNDERLINE_COLOR = "58";

    /// <summary>
    /// The SGR default underline color.
    /// </summary>
    public const string DEFAULT_UNDERLINE_COLOR = "59";

    /// <summary>
    /// The SGR bright black foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_BLACK = "90";

    /// <summary>
    /// The SGR bright red foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_RED = "91";

    /// <summary>
    /// The SGR bright green foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_GREEN = "92";

    /// <summary>
    /// The SGR bright yellow foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_YELLOW = "93";

    /// <summary>
    /// The SGR bright blue foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_BLUE = "94";

    /// <summary>
    /// The SGR bright magenta foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_MAGENTA = "95";

    /// <summary>
    /// The SGR bright cyan foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_CYAN = "96";

    /// <summary>
    /// The SGR bright white foreground color character sequence.
    /// </summary>
    public const string FOREGROUND_BRIGHT_WHITE = "97";

    /// <summary>
    /// The SGR bright black background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_BLACK = "100";

    /// <summary>
    /// The SGR bright red background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_RED = "101";

    /// <summary>
    /// The SGR bright green background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_GREEN = "102";

    /// <summary>
    /// The SGR bright yellow background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_YELLOW = "103";

    /// <summary>
    /// The SGR bright blue background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_BLUE = "104";

    /// <summary>
    /// The SGR bright magenta background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_MAGENTA = "105";

    /// <summary>
    /// The SGR bright cyan background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_CYAN = "106";

    /// <summary>
    /// The SGR bright white background color character sequence.
    /// </summary>
    public const string BACKGROUND_BRIGHT_WHITE = "107";

    /// <summary>
    /// The SGR parameter for 8-bit colors.
    /// </summary>
    internal const string COLOR_8 = "5";

    /// <summary>
    /// The SGR parameter for 24-bit colors.
    /// </summary>
    internal const string COLOR_24 = "2";

    /// <summary>
    /// The SGR termination character sequence.
    /// </summary>
    internal const string TERMINATOR = "m";
  }
}
