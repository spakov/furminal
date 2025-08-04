namespace Spakov.AnsiProcessor.Ansi
{
    /// <summary>
    /// ANSI keycodes.
    /// </summary>
    /// <remarks>
    /// Sources:
    /// <list type="bullet">
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// <item><see
    /// href="https://invisible-island.net/xterm/manpage/xterm.html"/></item>
    /// <item><see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Terminal_input_sequences"
    /// /></item>
    /// </list>
    /// </remarks>
    public static class Keycodes
    {
        /// <summary>
        /// The cursor up (CUU) character.
        /// </summary>
        public const char UP = 'A';

        /// <summary>
        /// The cursor down (CUD) character.
        /// </summary>
        public const char DOWN = 'B';

        /// <summary>
        /// The cursor forward (CUF) character.
        /// </summary>
        public const char RIGHT = 'C';

        /// <summary>
        /// The cursor back (CUB) character.
        /// </summary>
        public const char LEFT = 'D';

        /// <summary>
        /// The keycode for the Home key.
        /// </summary>
        public const char HOME = 'H';

        /// <summary>
        /// The keycode for the Insert key.
        /// </summary>
        public const string INSERT = "2";

        /// <summary>
        /// The keycode for the Delete key.
        /// </summary>
        public const string DELETE = "3";

        /// <summary>
        /// The keycode for the End key.
        /// </summary>
        public const char END = 'F';

        /// <summary>
        /// The keycode for the Page Up key.
        /// </summary>
        public const string PAGE_UP = "5";

        /// <summary>
        /// The keycode for the Page Down key.
        /// </summary>
        public const string PAGE_DOWN = "6";

        /// <summary>
        /// The keycode for the F1 key.
        /// </summary>
        public const string F1 = "11";

        /// <summary>
        /// The keycode for the F2 key.
        /// </summary>
        public const string F2 = "12";

        /// <summary>
        /// The keycode for the F3 key.
        /// </summary>
        public const string F3 = "13";

        /// <summary>
        /// The keycode for the F4 key.
        /// </summary>
        public const string F4 = "14";

        /// <summary>
        /// The keycode for the F5 key.
        /// </summary>
        public const string F5 = "15";

        /// <summary>
        /// The keycode for the F6 key.
        /// </summary>
        public const string F6 = "17";

        /// <summary>
        /// The keycode for the F7 key.
        /// </summary>
        public const string F7 = "18";

        /// <summary>
        /// The keycode for the F8 key.
        /// </summary>
        public const string F8 = "19";

        /// <summary>
        /// The keycode for the F9 key.
        /// </summary>
        public const string F9 = "20";

        /// <summary>
        /// The keycode for the F10 key.
        /// </summary>
        public const string F10 = "21";

        /// <summary>
        /// The keycode for the F11 key.
        /// </summary>
        public const string F11 = "23";

        /// <summary>
        /// The keycode for the F12 key.
        /// </summary>
        public const string F12 = "24";

        /// <summary>
        /// The keycode for "other" keys.
        /// </summary>
        public const string OTHER = "27";

        /// <summary>
        /// The DECCKM keycode for the Home key.
        /// </summary>
        public const string DECCKM_HOME = "1";

        /// <summary>
        /// The DECCKM keycode for the End key.
        /// </summary>
        public const string DECCKM_END = "4";

        /// <summary>
        /// The DECCKM keycode for the F1 key.
        /// </summary>
        public const char DECCKM_F1 = 'P';

        /// <summary>
        /// The DECCKM keycode for the F2 key.
        /// </summary>
        public const char DECCKM_F2 = 'Q';

        /// <summary>
        /// The DECCKM keycode for the F3 key.
        /// </summary>
        public const char DECCKM_F3 = 'R';

        /// <summary>
        /// The DECCKM keycode for the F4 key.
        /// </summary>
        public const char DECCKM_F4 = 'S';

        /// <summary>
        /// The keycode parameter separation character.
        /// </summary>
        public const char PARAMETER_SEPARATOR = ';';

        /// <summary>
        /// The keycode terminating character, used for "non-VT" keys.
        /// </summary>
        public const char TERMINATOR = '~';
    }
}
