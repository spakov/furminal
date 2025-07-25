namespace Spakov.AnsiProcessor.Ansi.EscapeSequences
{
    /// <summary>
    /// The SS3 (SS3) escape sequences, which are <see
    /// cref="C0.ESC"/>, followed by <see cref="Fe.SS3"/>, followed by a
    /// keycode.
    /// </summary>
    /// <remarks>
    /// <para>Note that these are used by <see cref="AnsiWriter"/>, not <see
    /// cref="AnsiReader"/>.</para>
    /// <para>Sources:
    /// <list type="bullet">
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// <item><see
    /// href="https://invisible-island.net/xterm/xterm-function-keys.html"
    /// /></item>
    /// </list></para>
    /// </remarks>
    public static class SS3
    {
        /// <summary>
        /// The cursor up (CUU) character.
        /// </summary>
        public const char CUU = 'A';

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
        /// The home character.
        /// </summary>
        public const char KEYCODE_HOME = 'H';

        /// <summary>
        /// The end character.
        /// </summary>
        public const char KEYCODE_END = 'F';

        /// <summary>
        /// The F1 character.
        /// </summary>
        public const char KEYCODE_F1 = 'P';

        /// <summary>
        /// The F2 character.
        /// </summary>
        public const char KEYCODE_F2 = 'Q';

        /// <summary>
        /// The F3 character.
        /// </summary>
        public const char KEYCODE_F3 = 'R';

        /// <summary>
        /// The F4 character.
        /// </summary>
        public const char KEYCODE_F4 = 'S';
    }
}
