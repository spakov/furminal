namespace Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions {
  /// <summary>
  /// The CSI DECSET private mode escape sequences, which are <see
  /// cref="C0.ESC"/>, followed by <see cref="C1.CSI"/>, followed by <see
  /// cref="DECSET_LEADER"/>, followed by a DECSET sequence, followed by either
  /// <see cref="CSI.DECSET_HIGH"/> (on) or <see cref="CSI.DECSET_LOW"/> (off).
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Control_Sequence_Introducer_commands"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// <item><see href="https://github.com/microsoft/terminal/"/></item>
  /// <item><see
  /// href="https://gist.github.com/rockorager/e695fb2924d36b2bcf1fff4a3704bd83"
  /// /></item>
  /// <item><see href="https://github.com/neovim/neovim/pull/31350"/></item>
  /// <item><see
  /// href="https://github.com/contour-terminal/contour/blob/master/docs/vt-extensions/color-palette-update-notifications.md"
  /// /></item>
  /// </list>
  /// </remarks>
  public static class CSI_DECSET {
    /// <summary>
    /// The DEC private mode set (DECSET) leader character.
    /// </summary>
    public const char DECSET_LEADER = '?';

    /// <summary>
    /// The DEC application cursor keys (DECCKM) value.
    /// </summary>
    public const int DECSET_DECCKM = 1;

    /// <summary>
    /// The DEC designate USASCII for character sets G0-G3 (DECANM), VT100, and
    /// set VT100 mode value.
    /// </summary>
    public const int DECSET_DECANM = 2;

    /// <summary>
    /// The DEC 132 column mode (DECCOLM) value.
    /// </summary>
    public const int DECSET_DECCOLM = 3;

    /// <summary>
    /// The DEC smooth (slow) scroll (DECSCLM) value.
    /// </summary>
    public const int DECSET_DECSCLM = 4;

    /// <summary>
    /// The DEC reverse video (DECSCNM) value.
    /// </summary>
    public const int DECSET_DECSCNM = 5;

    /// <summary>
    /// The DEC origin mode (DECOM) value.
    /// </summary>
    public const int DECSET_DECOM = 6;

    /// <summary>
    /// The DEC auto-wrap mode (DECAWM) value.
    /// </summary>
    public const int DECSET_DECAWM = 7;

    /// <summary>
    /// The DEC auto-repeat keys (DECARM) value.
    /// </summary>
    public const int DECSET_DECARM = 8;

    /// <summary>
    /// The xterm send mouse X &amp; Y on button press value.
    /// </summary>
    public const int DECSET_XTERM_X10_MOUSE = 9;

    /// <summary>
    /// The rxvt show toolbar value.
    /// </summary>
    public const int DECSET_RXVT_SHOW_TOOLBAR = 10;

    /// <summary>
    /// The text cursor enable blinking (ATT160) value.
    /// </summary>
    public const int DECSET_ATT160 = 12;

    /// <summary>
    /// The xterm start blinkg cursor value.
    /// </summary>
    public const int DECSET_XTERM_START_BLINKING_CURSOR = 13;

    /// <summary>
    /// The xterm enable XOR of blinking cursor value.
    /// </summary>
    public const int DECSET_XTERM_XOR_BLINKING_CURSOR = 14;

    /// <summary>
    /// The DEC print form feed (DECPFF) value.
    /// </summary>
    public const int DECSET_DECPFF = 18;

    /// <summary>
    /// The DEC print extent to full screen (DECPEX) value.
    /// </summary>
    public const int DECSET_DECPEX = 19;

    /// <summary>
    /// The DEC text cursor enable mode (DECTCEM) value.
    /// </summary>
    public const int DECSET_DECTCEM = 25;

    /// <summary>
    /// The rxvt show scrollbar value.
    /// </summary>
    public const int DECSET_RXVT_SHOW_SCROLLBAR = 30;

    /// <summary>
    /// The rxvt enable font-shifting functions value.
    /// </summary>
    public const int DECSET_RXVT_ENABLE_FONT_SHIFTING = 35;

    /// <summary>
    /// The DEC enable Tektronix mode (DECTEK) value.
    /// </summary>
    public const int DECSET_DECTEK = 38;

    /// <summary>
    /// The xterm allow 80 ⇒ 132 mode value.
    /// </summary>
    public const int DECSET_XTERM_80_132 = 40;

    /// <summary>
    /// The xterm more fix value.
    /// </summary>
    public const int DECSET_XTERM_MORE_FIX = 41;

    /// <summary>
    /// The DEC enable national replacement character sets (DECNRCM) value.
    /// </summary>
    public const int DECSET_DECNRCM = 42;

    /// <summary>
    /// The DEC enable graphic expanded print mode (DECGEPM) value.
    /// </summary>
    public const int DECSET_DECGEPM = 43;

    /// <summary>
    /// The xterm turn on margin bell value.
    /// </summary>
    public const int DECSET_XTERM_MARGIN_BELL = 44;

    /// <summary>
    /// The DEC enable graphic print color mode (DECGPCM) value.
    /// </summary>
    public const int DECSET_DECGPCM = 44;

    /// <summary>
    /// The xterm reverse-wraparound mode (XTREVWRAP) value.
    /// </summary>
    public const int DECSET_XTREVWRAP = 45;

    /// <summary>
    /// The DEC enable graphic print color syntax (DECGPCS) value.
    /// </summary>
    public const int DECSET_DECGPCS = 45;

    /// <summary>
    /// The xterm start logging (XTLOGGING) value.
    /// </summary>
    public const int DECSET_XTLOGGING = 46;

    /// <summary>
    /// The DEC graphic print background mode value.
    /// </summary>
    public const int DECSET_GRAPHIC_PRINT_BACKGROUND_MODE = 46;

    /// <summary>
    /// The xterm use alternate screen buffer value.
    /// </summary>
    public const int DECSET_ALTERNATE_SCREEN_BUFFER = 47;

    /// <summary>
    /// The DEC enable graphic rotated print mode (DECGRPM) value.
    /// </summary>
    public const int DECSET_DECGRPM = 47;

    /// <summary>
    /// The DEC application keypad mode (DECNKM) value.
    /// </summary>
    public const int DECSET_DECNKM = 66;

    /// <summary>
    /// The DEC backarrow key sends backspace (DECBKM) value.
    /// </summary>
    public const int DECSET_DECBKM = 67;

    /// <summary>
    /// The DEC enable left and right margin mode (DECLRMM) value.
    /// </summary>
    public const int DECSET_DECLRMM = 69;

    /// <summary>
    /// The DEC enable sixel display mode (DECSDM) value.
    /// </summary>
    public const int DECSET_DECSDM = 80;

    /// <summary>
    /// The DEC do not clear screen when DECCOLM is set/reset (DECNCSM) value.
    /// </summary>
    public const int DECSET_DECNCSM = 95;

    /// <summary>
    /// The xterm send mouse X &amp; Y on button press and release (X11) value.
    /// </summary>
    public const int DECSET_XTERM_X11_MOUSE = 1000;

    /// <summary>
    /// The xterm hilite mouse tracking value.
    /// </summary>
    public const int DECSET_XTERM_HILITE_MOUSE_TRACKING = 1001;

    /// <summary>
    /// The xterm cell motion mouse tracking value.
    /// </summary>
    public const int DECSET_XTERM_CELL_MOTION_MOUSE_TRACKING = 1002;

    /// <summary>
    /// The xterm all motion mouse tracking value.
    /// </summary>
    public const int DECSET_XTERM_ALL_MOTION_MOUSE_TRACKING = 1003;

    /// <summary>
    /// The xterm FocusIn/FocusOut events value.
    /// </summary>
    public const int DECSET_XTERM_FOCUSIN_FOCUSOUT = 1004;

    /// <summary>
    /// The xterm UTF-8 mouse mode value.
    /// </summary>
    public const int DECSET_XTERM_UTF8_MOUSE_MODE = 1005;

    /// <summary>
    /// The xterm SGR mouse mode value.
    /// </summary>
    public const int DECSET_XTERM_SGR_MOUSE_MODE = 1006;

    /// <summary>
    /// The xterm alternate scroll mode value.
    /// </summary>
    public const int DECSET_XTERM_ALTERNATE_SCROLL_MODE = 1007;

    /// <summary>
    /// The rxvt scroll to bottom on tty output value.
    /// </summary>
    public const int DECSET_RXVT_SCROLL_TO_BOTTOM_ON_OUTPUT = 1010;

    /// <summary>
    /// The rxvt scroll to bottom on key press value.
    /// </summary>
    public const int DECSET_RXVT_SCROLL_TO_BOTTOM_ON_KEY_PRESS = 1011;

    /// <summary>
    /// The xterm enable fast scroll value.
    /// </summary>
    public const int DECSET_XTERM_FAST_SCROLL = 1014;

    /// <summary>
    /// The urxvt enable mouse mode value.
    /// </summary>
    public const int DECSET_URXVT_MOUSE_MODE = 1015;

    /// <summary>
    /// The xterm enable SGR mouse pixel mode value.
    /// </summary>
    public const int DECSET_XTERM_SGR_MOUSE_PIXEL_MODE = 1016;

    /// <summary>
    /// The xterm interpret meta key value.
    /// </summary>
    public const int DECSET_XTERM_INTERPRET_META_KEY = 1034;

    /// <summary>
    /// The xterm enable special modifiers for Alt and Num Lock keys value.
    /// </summary>
    public const int DECSET_XTERM_SPECIAL_MODIFIERS = 1035;

    /// <summary>
    /// The xterm send ESC when meta modifies a key value.
    /// </summary>
    public const int DECSET_XTERM_SEND_ESC_ON_META = 1036;

    /// <summary>
    /// The xterm send DEL from the editing-keypad Delete key value.
    /// </summary>
    public const int DECSET_XTERM_SEND_DEL = 1037;

    /// <summary>
    /// The xterm send ESC when Alt modifies a key value.
    /// </summary>
    public const int DECSET_XTERM_SEND_ESC_ON_ALT = 1039;

    /// <summary>
    /// The xterm keep selection even if not highlighted value.
    /// </summary>
    public const int DECSET_XTERM_KEEP_SELECTION = 1040;

    /// <summary>
    /// The xterm use the CLIPBOARD selection value.
    /// </summary>
    public const int DECSET_XTERM_SELECT_TO_CLIPBOARD = 1041;

    /// <summary>
    /// The xterm enable urgency window manager hint when Control-G is received
    /// value.
    /// </summary>
    public const int DECSET_XTERM_BELL_IS_URGENT = 1042;

    /// <summary>
    /// The xterm enable raising of the window when Control-G is received
    /// value.
    /// </summary>
    public const int DECSET_XTERM_POP_ON_BELL = 1043;

    /// <summary>
    /// The xterm reuse the most recent data copied to CLIPBOARD value.
    /// </summary>
    public const int DECSET_XTERM_KEEP_CLIPBOARD = 1044;

    /// <summary>
    /// The xterm extended reverse-wraparound mode (XTREVWRAP2) value.
    /// </summary>
    public const int DECSET_XTREVWRAP2 = 1045;

    /// <summary>
    /// The xterm enable alternate screen buffer value.
    /// </summary>
    public const int DECSET_XTERM_ALTERNATE_SCREEN_BUFFER = 1046;

    /// <summary>
    /// The xterm use alternate screen buffer value.
    /// </summary>
    public const int DECSET_XTERM_USE_ALTERNATE_SCREEN_BUFFER = 1047;

    /// <summary>
    /// The xterm save cursor as in DECSC value.
    /// </summary>
    public const int DECSET_XTERM_SAVE_CURSOR = 1048;

    /// <summary>
    /// The xterm save cursor as in DECSC and use alternate screen buffer
    /// value.
    /// </summary>
    public const int DECSET_XTERM_SAVE_CURSOR_AND_USE_ASB = 1049;

    /// <summary>
    /// The xterm set terminfo/termcap function-key mode value.
    /// </summary>
    public const int DECSET_XTERM_SET_TERMINFO_TERMCAP_F_KEY = 1050;

    /// <summary>
    /// The xterm set Sun function-key mode value.
    /// </summary>
    public const int DECSET_XTERM_SET_SUN_F_KEY = 1051;

    /// <summary>
    /// The xterm set HP function-key mode value.
    /// </summary>
    public const int DECSET_XTERM_SET_HP_F_KEY = 1052;

    /// <summary>
    /// The xterm set SCO function-key mode value.
    /// </summary>
    public const int DECSET_XTERM_SET_SCO_F_KEY = 1053;

    /// <summary>
    /// The xterm set legacy keyboard emulation (X11R6) value.
    /// </summary>
    public const int DECSET_XTERM_LEGACY_KEYBOARD_EMULATION = 1060;

    /// <summary>
    /// The xterm set VT220 keyboard emulation value.
    /// </summary>
    public const int DECSET_XTERM_VT220_KEYBOARD_EMULATION = 1061;

    /// <summary>
    /// The xterm enable readline mouse button-1 value.
    /// </summary>
    public const int DECSET_XTERM_READLINE_MOUSE_BUTTON_1 = 2001;

    /// <summary>
    /// The xterm enable readline mouse button-2 value.
    /// </summary>
    public const int DECSET_XTERM_READLINE_MOUSE_BUTTON_2 = 2002;

    /// <summary>
    /// The xterm enable readline mouse button-3 value.
    /// </summary>
    public const int DECSET_XTERM_READLINE_MOUSE_BUTTON_3 = 2003;

    /// <summary>
    /// The xterm set bracketed paste mode value.
    /// </summary>
    public const int DECSET_XTERM_BRACKETED_PASTE_MODE = 2004;

    /// <summary>
    /// The xterm enable readline character-quoting value.
    /// </summary>
    public const int DECSET_XTERM_READLINE_CHARACTER_QUOTING = 2005;

    /// <summary>
    /// The xterm enable readline newline pasting value.
    /// </summary>
    public const int DECSET_XTERM_READLINE_NEWLINE_PASTING = 2006;

    /// <summary>
    /// The theme change notification value.
    /// </summary>
    public const int DECSET_THEME_CHANGE = 2031;

    /// <summary>
    /// The in-band window resize notification value.
    /// </summary>
    public const int DECSET_WINDOW_RESIZE = 2048;

    /// <summary>
    /// The in-band window resize notification response separator character.
    /// </summary>
    public const char DECSET_WINDOW_RESIZE_SEPARATOR = ';';

    /// <summary>
    /// The in-band window resize notification response termination character.
    /// </summary>
    public const char DECSET_WINDOW_RESIZE_TERMINATOR = 't';

    /// <summary>
    /// The ConPTY win32-input-mode value.
    /// </summary>
    /// <remarks>
    /// <para>See <see
    /// href="https://github.com/microsoft/terminal/blob/main/doc/specs/%234999%20-%20Improved%20keyboard%20handling%20in%20Conpty.md"
    /// /> for background on this.</para>
    /// <para>Also be sure to see <see
    /// href="https://invisible-island.net/xterm/modified-keys.html"/> and <see
    /// href="https://www.leonerd.org.uk/hacks/fixterms/"/> for more on this.
    /// (It's fascinating to me that Microsoft developed yet another standard
    /// for keystroke encoding when there are already at least three others. I
    /// mean, I get why, but still.)</para>
    /// </remarks>
    public const int DECSET_WIN32_INPUT_MODE = 9001;
  }
}
