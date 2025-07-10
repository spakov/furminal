using Spakov.AnsiProcessor.TermCap;

namespace Spakov.AnsiProcessor.Output.EscapeSequences.NF {
  /// <summary>
  /// A representation of an ANSI <see cref="Ansi.EscapeSequences.NF"/> escape
  /// sequence.
  /// </summary>
  public class NFEscapeSequence : EscapeSequence {
    /// <summary>
    /// Initializes an <see cref="NFEscapeSequence"/>.
    /// </summary>
    /// <param name="rawNFEscapeSequence">The raw nF escape sequence.</param>
    protected NFEscapeSequence(string rawNFEscapeSequence) : base(rawNFEscapeSequence) { }

    /// <summary>
    /// Initializes an <see cref="NFEscapeSequence"/>.
    /// </summary>
    /// <param name="rawNFEscapeSequence">The raw nF escape sequence from
    /// which to initialize an object.</param>
    /// <returns>An <see cref="NFEscapeSequence"/>.</returns>
    internal static NFEscapeSequence InitializeNFEscapeSequence(string rawNFEscapeSequence) => new(rawNFEscapeSequence);

    /// <summary>
    /// Determines whether <paramref name="terminalCapabilities"/> states that
    /// <paramref name="character"/> should be handled.
    /// </summary>
    /// <param name="terminalCapabilities">A <see
    /// cref="TerminalCapabilities"/>.</param>
    /// <param name="character">The character to check.</param>
    /// <returns><see langword="true"/> if <paramref name="character"/> should
    /// be handled or <see langword="false"/> otherwise.</returns>
    internal static bool NFEscapeSequenceMatches(TerminalCapabilities terminalCapabilities, char character) {
      if (terminalCapabilities.ControlCharacters?.EscapeSequences is null) return false;

      foreach (string nF in terminalCapabilities.ControlCharacters.EscapeSequences.NF) {
        if (nF.StartsWith(character)) return true;
      }

      return false;
    }

    /// <summary>
    /// Determines whether an nF escape sequence is complete to facilitate
    /// building the sequence.
    /// </summary>
    /// <remarks>Source: <see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#nF_Escape_sequences"
    /// /></remarks>
    /// <returns><see langword="true"/> if the escape sequence is complete or
    /// <see langword="false"/> otherwise.</returns>
    internal static bool IsNFEscapeSequenceComplete(char character) => character is >= '0' and <= '~';
  }
}
