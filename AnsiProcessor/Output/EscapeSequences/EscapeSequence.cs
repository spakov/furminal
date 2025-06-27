using AnsiProcessor.AnsiColors;
using AnsiProcessor.Output.EscapeSequences.Fe;
using AnsiProcessor.Output.EscapeSequences.Fp;
using AnsiProcessor.Output.EscapeSequences.Fs;
using AnsiProcessor.Output.EscapeSequences.NF;
using AnsiProcessor.TermCap;
using System;
using static AnsiProcessor.AnsiReader;

namespace AnsiProcessor.Output.EscapeSequences {
  /// <summary>
  /// An ANSI escape sequence.
  /// </summary>
  public class EscapeSequence {
    /// <summary>
    /// The raw escape sequence.
    /// </summary>
    public string RawEscapeSequence { get; private init; }

    /// <summary>
    /// Initializes an <see cref="EscapeSequence"/>.
    /// </summary>
    /// <param name="rawEscapeSequence">The raw escape sequence.</param>
    protected EscapeSequence(string rawEscapeSequence) {
      RawEscapeSequence = rawEscapeSequence;
    }

    /// <summary>
    /// Initializes an <see cref="EscapeSequence"/>, or more likely, one of its
    /// subclasses.
    /// </summary>
    /// <param name="terminalCapabilities">A <see
    /// cref="TerminalCapabilities"/>.</param>
    /// <param name="palette">A <see cref="Palette"/>.</param>
    /// <param name="rawEscapeSequence">The raw escape sequence from which to
    /// initialize an object.</param>
    /// <param name="escapeSequenceState">The <see cref="EscapeSequenceState"/>
    /// corresponding to the reader upon processing <paramref
    /// name="rawEscapeSequence"/>.</param>
    /// <returns>An <see cref="EscapeSequence"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal static EscapeSequence InitializeEscapeSequence(TerminalCapabilities terminalCapabilities, Palette palette, string rawEscapeSequence, EscapeSequenceState escapeSequenceState) {
      return escapeSequenceState switch {
        EscapeSequenceState.Fe => FeEscapeSequence.InitializeFeEscapeSequence(terminalCapabilities, palette, rawEscapeSequence),
        EscapeSequenceState.Fp => FpEscapeSequence.InitializeFpEscapeSequence(rawEscapeSequence),
        EscapeSequenceState.Fs => FsEscapeSequence.InitializeFsEscapeSequence(rawEscapeSequence),
        EscapeSequenceState.nF => NFEscapeSequence.InitializeNFEscapeSequence(rawEscapeSequence),
        _ => throw new InvalidOperationException()
      };
    }

    /// <summary>
    /// Determines the type of escape sequence represented by <paramref
    /// name="character"/>, based on <paramref name="terminalCapabilities"/>.
    /// </summary>
    /// <param name="terminalCapabilities">A <see
    /// cref="TerminalCapabilities"/>.</param>
    /// <param name="character">The character to check.</param>
    /// <returns>An <see cref="EscapeSequenceState"/>, or <see
    /// langword="null"/> if <paramref name="character"/> represents an escape
    /// sequence this is not configured in <paramref
    /// name="terminalCapabilities"/>.</returns>
    internal static EscapeSequenceState? DetermineEscapeSequenceType(TerminalCapabilities terminalCapabilities, char character) {
      if (terminalCapabilities.ControlCharacters?.EscapeSequences is not null) {
        if (terminalCapabilities.ControlCharacters.EscapeSequences.Fe.Contains(character)) {
          return EscapeSequenceState.Fe;
        } else if (terminalCapabilities.ControlCharacters.EscapeSequences.Fp.Contains(character)) {
          return EscapeSequenceState.Fp;
        } else if (terminalCapabilities.ControlCharacters.EscapeSequences.Fs.Contains(character)) {
          return EscapeSequenceState.Fs;
        } else if (NFEscapeSequence.NFEscapeSequenceMatches(terminalCapabilities, character)) {
          return EscapeSequenceState.nF;
        }
      }

      return null;
    }
  }
}
