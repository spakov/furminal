namespace Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions
{
    /// <summary>
    /// The CSI xterm window manipulation (XTWINOPS) escape sequences, which
    /// are <see cref="C0.ESC"/>, followed by <see cref="C1.CSI"/>, followed by
    /// a first parameter (which is optionally followed by <see
    /// cref="XTWINOPS_SEPARATOR"/>, followed by a second parameter [which is
    /// optionally followed by <see cref="XTWINOPS_SEPARATOR"/>, followed by a
    /// third parameter]), followed by <see cref="CSI.XTWINOPS"/>.
    /// </summary>
    /// <remarks>
    /// Sources:
    /// <list type="bullet">
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// </list>
    /// </remarks>
    public static class CSI_XTWINOPS
    {
        /// <summary>
        /// The XTWINOPS text area size in characters value.
        /// </summary>
        public const int XTWINOPS_TEXT_AREA_SIZE = 18;

        /// <summary>
        /// The XTWINOPS text area size in characters response value.
        /// </summary>
        /// <remarks>Like <c>CSI  8 ;  height ;  width t</c>.</remarks>
        public const int XTWINOPS_TEXT_AREA_SIZE_RESPONSE = 8;

        /// <summary>
        /// The XTWINOPS push to stack value.
        /// </summary>
        public const int XTWINOPS_STACK_PUSH = 22;

        /// <summary>
        /// The XTWINOPS pop from stack value.
        /// </summary>
        public const int XTWINOPS_STACK_POP = 23;

        /// <summary>
        /// The XTWINOPS separator character.
        /// </summary>
        public const char XTWINOPS_SEPARATOR = ';';
    }
}
