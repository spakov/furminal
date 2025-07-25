namespace Spakov.AnsiProcessor.Output.EscapeSequences.Fs
{
    /// <summary>
    /// A representation of an ANSI <see cref="Ansi.EscapeSequences.Fs"/>
    /// escape sequence.
    /// </summary>
    public class FsEscapeSequence : EscapeSequence
    {
        /// <summary>
        /// Initializes an <see cref="FsEscapeSequence"/>.
        /// </summary>
        /// <param name="rawFsEscapeSequence">The raw Fs escape
        /// sequence.</param>
        protected FsEscapeSequence(string rawFsEscapeSequence) : base(rawFsEscapeSequence) {
        }

        /// <summary>
        /// Initializes an <see cref="FsEscapeSequence"/>.
        /// </summary>
        /// <param name="rawFsEscapeSequence">The raw Fs escape sequence from
        /// which to initialize an object.</param>
        /// <returns>An <see cref="FsEscapeSequence"/>.</returns>
        internal static FsEscapeSequence InitializeFsEscapeSequence(string rawFsEscapeSequence) => new(rawFsEscapeSequence);

        /// <summary>
        /// Determines whether an Fs escape sequence is complete to facilitate
        /// building the sequence.
        /// </summary>
        /// <remarks>
        /// <para>Since an Fs escape sequence is only two characters, the
        /// sequence is always complete.</para>
        /// <para>Source: <see
        /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fs_Escape_sequences"
        /// /></para></remarks>
        /// <returns><see langword="true"/> if the escape sequence is complete
        /// or <see langword="false"/> otherwise.</returns>
        internal static bool IsFsEscapeSequenceComplete() => true;
    }
}
