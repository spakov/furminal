namespace Spakov.AnsiProcessor.TermCap
{
    /// <summary>
    /// Terminal capabilities definition.
    /// </summary>
    public class TerminalCapabilities
    {
        /// <summary>
        /// Control characters configuration.
        /// </summary>
        /// <remarks>If <see langword="null"/>, <see
        /// cref="AnsiReader.OnControlCharacter"/> and <see
        /// cref="AnsiReader.OnEscapeSequence"/> will never be
        /// invoked.</remarks>
        public ControlCharacters? ControlCharacters { get; set; } = new();

        /// <summary>
        /// Input configuration.
        /// </summary>
        public Input Input { get; set; } = new();
    }
}
