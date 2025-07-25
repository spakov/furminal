namespace Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions
{
    /// <summary>
    /// The CSI DSR extension escape sequences, which are <see
    /// cref="C0.ESC"/>, followed by <see cref="Fe.CSI"/>, followed by <see
    /// cref="CSI_DECSET.DECSET_LEADER"/>, followed by a DECSET sequence,
    /// followed by either <see cref="CSI.DECSET_HIGH"/> (on) or <see
    /// cref="CSI.DECSET_LOW"/> (off).
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
    /// <item><see
    /// href="https://github.com/contour-terminal/contour/issues/1659"/></item>
    /// <item><see href="https://github.com/neovim/neovim/pull/31350"/></item>
    /// <item><see
    /// href="https://github.com/contour-terminal/contour/blob/master/docs/vt-extensions/color-palette-update-notifications.md"
    /// /></item>
    /// </list>
    /// </remarks>
    public static class CSI_DSR
    {
        /// <summary>
        /// The DSR status report query value.
        /// </summary>
        public const int DSR_STATUS_REPORT = 5;

        /// <summary>
        /// The DSR report cursor position (RCP) query value.
        /// </summary>
        public const int DSR_RCP = 6;

        /// <summary>
        /// The DSR theme query value.
        /// </summary>
        public const int DSR_THEME_QUERY = 996;

        /// <summary>
        /// The DSR theme query response value.
        /// </summary>
        public const int DSR_THEME_RESPONSE = 997;

        /// <summary>
        /// The DSR theme query dark theme response character.
        /// </summary>
        public const char DSR_THEME_DARK = '1';

        /// <summary>
        /// The DSR theme query light theme response character.
        /// </summary>
        public const char DSR_THEME_LIGHT = '2';

        /// <summary>
        /// The DSR theme query response separator character.
        /// </summary>
        public const char DSR_THEME_SEPARATOR = ';';
    }
}
