namespace Spakov.AnsiProcessor.Output.EscapeSequences.Fp {
  /// <summary>
  /// A representation of an ANSI <see cref="Ansi.EscapeSequences.Fp"/> escape
  /// sequence.
  /// </summary>
  public class FpEscapeSequence : EscapeSequence {
    /// <summary>
    /// Initializes an <see cref="FpEscapeSequence"/>.
    /// </summary>
    /// <param name="rawFpEscapeSequence">The raw Fp escape sequence.</param>
    protected FpEscapeSequence(string rawFpEscapeSequence) : base(rawFpEscapeSequence) { }

    /// <summary>
    /// Initializes an <see cref="FpEscapeSequence"/>.
    /// </summary>
    /// <param name="rawFpEscapeSequence">The raw Fp escape sequence from
    /// which to initialize an object.</param>
    /// <returns>An <see cref="FpEscapeSequence"/>.</returns>
    internal static FpEscapeSequence InitializeFpEscapeSequence(string rawFpEscapeSequence) => new(rawFpEscapeSequence);

    /// <summary>
    /// Determines whether an Fp escape sequence is complete to facilitate
    /// building the sequence.
    /// </summary>
    /// <remarks>
    /// <para>Since an Fp escape sequence is only two characters, the sequence
    /// is always complete.</para>
    /// <para>Source: <see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fp_Escape_sequences"
    /// /></para></remarks>
    /// <returns><see langword="true"/> if the escape sequence is complete or
    /// <see langword="false"/> otherwise.</returns>
    internal static bool IsFpEscapeSequenceComplete() => true;
  }
}
