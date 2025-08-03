namespace Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions
{
    /// <summary>
    /// The CSI xterm key modifier options (XTMODKEYS) escape sequences, which
    /// are <see cref="C0.ESC"/>, followed by <see cref="Fe.CSI"/>, followed by
    /// <see cref="XTMODKEYS"/>, followed by a first parameter (which is
    /// optionally followed by <see cref="XTMODKEYS_SEPARATOR"/>, followed by a
    /// second parameter), followed by <see cref="CSI.XTMODKEYS"/>.
    /// </summary>
    /// <remarks>
    /// Sources:
    /// <list type="bullet">
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// </list>
    /// </remarks>
    public static class CSI_XTMODKEYS
    {
        /// <summary>
        /// The xterm key modifier options (XTMODKEYS) character.
        /// </summary>
        public const char XTMODKEYS = '>';

        /// <summary>
        /// The xterm query key modifier options (XTQMODKEYS) character.
        /// </summary>
        public const char XTQMODKEYS = '?';

        /// <summary>
        /// The XTMODKEYS modify keyboard value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_KEYBOARD = 0;

        /// <summary>
        /// The XTMODKEYS modify cursor keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_CURSOR_KEYS = 1;

        /// <summary>
        /// The XTMODKEYS modify function keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_FUNCTION_KEYS = 2;

        /// <summary>
        /// The XTMODKEYS modify keypad keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_KEYPAD_KEYS = 3;

        /// <summary>
        /// The XTMODKEYS modify other keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_OTHER_KEYS = 4;

        /// <summary>
        /// The XTMODKEYS modify modifier keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_MODIFIER_KEYS = 6;

        /// <summary>
        /// The XTMODKEYS modify special keys value.
        /// </summary>
        public const int XTMODKEYS_MODIFY_SPECIAL_KEYS = 7;

        /// <summary>
        /// The XTMODKEYS separator character.
        /// </summary>
        public const char XTMODKEYS_SEPARATOR = ';';
    }
}
