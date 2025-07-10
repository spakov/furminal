using System.Collections.Generic;

namespace Spakov.AnsiProcessor.TermCap {
  /// <summary>
  /// Allows defining supported escape sequences to handle via callbacks.
  /// </summary>
  public class EscapeSequences {
    /// <summary>
    /// <see cref="Ansi.EscapeSequences.Fe"/> escape sequences configuration.
    /// </summary>
    /// <remarks>Add escape sequences to the list that should cause
    /// <see cref="AnsiReader.OnEscapeSequence"/> to be invoked.</remarks>
    public List<char> Fe { get; } = [
      Ansi.EscapeSequences.Fe.HTS,
      Ansi.EscapeSequences.Fe.CSI,
      Ansi.EscapeSequences.Fe.OSC
    ];

    /// <summary>
    /// <see cref="Ansi.EscapeSequences.Fp"/> escape sequences configuration.
    /// </summary>
    /// <remarks>Add escape sequences to the list that should cause
    /// <see cref="AnsiReader.OnEscapeSequence"/> to be invoked.</remarks>
    public List<char> Fp { get; } = [
      Ansi.EscapeSequences.Fp.DECSC,
      Ansi.EscapeSequences.Fp.DECRC,
    ];

    /// <summary>
    /// <see cref="Ansi.EscapeSequences.Fs"/> escape sequences configuration.
    /// </summary>
    /// <remarks>Add escape sequences to the list that should cause
    /// <see cref="AnsiReader.OnEscapeSequence"/> to be invoked.</remarks>
    public List<char> Fs { get; } = [
      Ansi.EscapeSequences.Fs.RIS
    ];

    /// <summary>
    /// <see cref="Ansi.EscapeSequences.NF"/> escape sequences configuration.
    /// </summary>
    /// <remarks>Add escape sequences to the list that should cause
    /// <see cref="AnsiReader.OnEscapeSequence"/> to be invoked.</remarks>
    public List<string> NF { get; } = [];

    /// <summary>
    /// Control sequence introducer (CSI) escape sequences configuration.
    /// </summary>
    /// <remarks>Has no effect if <see cref="Fe"/> does not include <see
    /// cref="Ansi.EscapeSequences.Fe.CSI"/></remarks>
    public ControlSequenceIntroducer? CSI { get; set; } = new();
  }
}
