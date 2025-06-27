namespace AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The control sequence introducer (CSI) escape sequences, which are <see
  /// cref="C0.ESC"/>, followed by <see cref="Fe.CSI"/>, followed by parameter
  /// bytes, followed by intermediate bytes, followed by a final byte (the
  /// constants defined in <see cref="CSI"/>).
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Control_Sequence_Introducer_commands"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// <item><see
  /// href="https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences"
  /// /></item>
  /// </list>
  /// </remarks>
  public static class CSI {
    /// <summary>
    /// The insert character (ICH) character.
    /// </summary>
    public const char ICH = '@';

    /// <summary>
    /// The scroll left (SL) string.
    /// </summary>
    public const string SL = " @";

    /// <summary>
    /// The cursor up (CUU) character.
    /// </summary>
    public const char CUU = 'A';

    /// <summary>
    /// The scroll right (SR) string.
    /// </summary>
    public const string SR = " A";

    /// <summary>
    /// The cursor down (CUD) character.
    /// </summary>
    public const char CUD = 'B';

    /// <summary>
    /// The cursor forward (CUF) character.
    /// </summary>
    public const char CUF = 'C';

    /// <summary>
    /// The cursor back (CUB) character.
    /// </summary>
    public const char CUB = 'D';

    /// <summary>
    /// The cusor next line (CNL) character.
    /// </summary>
    public const char CNL = 'E';

    /// <summary>
    /// The cursor previous line (CPL) character.
    /// </summary>
    public const char CPL = 'F';

    /// <summary>
    /// The cursor horizontal absolute (CHA) character.
    /// </summary>
    public const char CHA = 'G';

    /// <summary>
    /// The cursor position (CUP) character.
    /// </summary>
    public const char CUP = 'H';

    /// <summary>
    /// The cursor forward tabulation (CHT) character.
    /// </summary>
    public const char CHT = 'I';

    /// <summary>
    /// The erase in display (ED) character.
    /// </summary>
    public const char ED = 'J';

    /// <summary>
    /// The erase in line (EL) character.
    /// </summary>
    public const char EL = 'K';

    /// <summary>
    /// The insert line (IL) character.
    /// </summary>
    public const char IL = 'L';

    /// <summary>
    /// The delete line (DL) character.
    /// </summary>
    public const char DL = 'M';

    /// <summary>
    /// The delete character (DCH) character.
    /// </summary>
    public const char DCH = 'P';

    /// <summary>
    /// The scroll up (SU) character.
    /// </summary>
    public const char SU = 'S';

    /// <summary>
    /// The xterm report position on title-stack (XTTITLEPOS) string.
    /// </summary>
    public const string XTTITLEPOS = "#S";

    /// <summary>
    /// The xterm report position on title-stack (XTTITLEPOS) separator character.
    /// </summary>
    public const char XTTITLEPOS_SEPARATOR = ';';

    /// <summary>
    /// The scroll down (SD) character.
    /// </summary>
    public const char SD = 'T';

    /// <summary>
    /// The erase character (ECH) character.
    /// </summary>
    public const char ECH = 'X';

    /// <summary>
    /// The cursor backward tabulation (CBT) character.
    /// </summary>
    public const char CBT = 'Z';

    /// <summary>
    /// The character position absolute (HPA) character.
    /// </summary>
    public const char HPA = '`';

    /// <summary>
    /// The character position relative (HPR) character.
    /// </summary>
    public const char HPR = 'a';

    /// <summary>
    /// The repetition (REP) character.
    /// </summary>
    public const char REP = 'b';

    /// <summary>
    /// The send device attributes (DA) character.
    /// </summary>
    public const char DA = 'c';

    /// <summary>
    /// The line position abslute (VPA) character.
    /// </summary>
    public const char VPA = 'd';

    /// <summary>
    /// The line position relative (VPR) character.
    /// </summary>
    public const char VPR = 'e';

    /// <summary>
    /// The horizontal and vertical position (HVP) character.
    /// </summary>
    public const char HVP = 'f';

    /// <summary>
    /// The tab clear (TBC) character.
    /// </summary>
    public const char TBC = 'g';

    /// <summary>
    /// The TBC clear current column value.
    /// </summary>
    public const int TBC_CLEAR_CURRENT_COLUMN = 0;

    /// <summary>
    /// The TBC clear all value.
    /// </summary>
    public const int TBC_CLEAR_ALL = 3;

    /// <summary>
    /// The DEC private mode set (DECSET) "high" character (properly known as
    /// DECSET).
    /// </summary>
    public const char DECSET_HIGH = 'h';

    /// <summary>
    /// The media copy (MC) character.
    /// </summary>
    public const char MC = 'i';

    /// <summary>
    /// The DEC private mode set (DECSET) "low" character (properly known as
    /// DECRST).
    /// </summary>
    public const char DECSET_LOW = 'l';

    /// <summary>
    /// The select graphic rendition (SGR) character.
    /// </summary>
    public const char SGR = 'm';

    /// <summary>
    /// The xterm key modifier options (XTMODKEYS) character (which is indeed
    /// the same as <see cref="SGR"/>).
    /// </summary>
    public const char XTMODKEYS = 'm';

    /// <summary>
    /// The device status report (DSR) character.
    /// </summary>
    public const char DSR = 'n';

    /// <summary>
    /// The DEC soft terminal reset (DECSTR) character.
    /// </summary>
    public const char DECSTR = 'p';

    /// <summary>
    /// The DEC load LEDs (DECLL) character.
    /// </summary>
    public const char DECLL = 'q';

    /// <summary>
    /// The DEC set cursor style (DECSCUSR) string.
    /// </summary>
    public const string DECSCUSR = " q";

    /// <summary>
    /// The DEC set scrolling region (DECSTBM) character.
    /// </summary>
    public const char DECSTBM = 'r';

    /// <summary>
    /// The save cursor character.
    /// </summary>
    public const char SAVE_CURSOR = 's';

    /// <summary>
    /// The xterm window manipulation (XTWINOPS) character.
    /// </summary>
    public const char XTWINOPS = 't';

    /// <summary>
    /// The restore cursor character.
    /// </summary>
    public const char RESTORE_CURSOR = 'u';

    /// <summary>
    /// The keycode for the Home key.
    /// </summary>
    public const string KEYCODE_HOME = "1";

    /// <summary>
    /// The keycode for the Insert key.
    /// </summary>
    public const string KEYCODE_INSERT = "2";

    /// <summary>
    /// The keycode for the Delete key.
    /// </summary>
    public const string KEYCODE_DELETE = "3";

    /// <summary>
    /// The keycode for the End key.
    /// </summary>
    public const string KEYCODE_END = "4";

    /// <summary>
    /// The keycode for the Page Up key.
    /// </summary>
    public const string KEYCODE_PAGE_UP = "5";

    /// <summary>
    /// The keycode for the Page Down key.
    /// </summary>
    public const string KEYCODE_PAGE_DOWN = "6";

    /// <summary>
    /// The keycode for the F1 key.
    /// </summary>
    public const string KEYCODE_F1 = "11";

    /// <summary>
    /// The keycode for the F2 key.
    /// </summary>
    public const string KEYCODE_F2 = "12";

    /// <summary>
    /// The keycode for the F3 key.
    /// </summary>
    public const string KEYCODE_F3 = "13";

    /// <summary>
    /// The keycode for the F4 key.
    /// </summary>
    public const string KEYCODE_F4 = "14";

    /// <summary>
    /// The keycode for the F5 key.
    /// </summary>
    public const string KEYCODE_F5 = "15";

    /// <summary>
    /// The keycode for the F6 key.
    /// </summary>
    public const string KEYCODE_F6 = "17";

    /// <summary>
    /// The keycode for the F7 key.
    /// </summary>
    public const string KEYCODE_F7 = "18";

    /// <summary>
    /// The keycode for the F8 key.
    /// </summary>
    public const string KEYCODE_F8 = "19";

    /// <summary>
    /// The keycode for the F9 key.
    /// </summary>
    public const string KEYCODE_F9 = "20";

    /// <summary>
    /// The keycode for the F10 key.
    /// </summary>
    public const string KEYCODE_F10 = "21";

    /// <summary>
    /// The keycode for the F11 key.
    /// </summary>
    public const string KEYCODE_F11 = "23";

    /// <summary>
    /// The keycode for the F12 key.
    /// </summary>
    public const string KEYCODE_F12 = "24";

    /// <summary>
    /// The keycode parameter separation character.
    /// </summary>
    public const char KEYCODE_PARAMETER_SEPARATOR = ';';

    /// <summary>
    /// The keycode terminating character.
    /// </summary>
    public const char KEYCODE_TERMINATOR = '~';
  }
}
