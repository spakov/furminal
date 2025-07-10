using System.Collections.Generic;

namespace Spakov.AnsiProcessor.TermCap {
  /// <summary>
  /// Allows defining supported control characters to handle via callbacks.
  /// </summary>
  public class ControlCharacters {
    /// <summary>
    /// <see cref="Ansi.C0"/> control characters configuration.
    /// </summary>
    /// <remarks>
    /// <para>Add control characters to the list that should cause
    /// <see cref="AnsiReader.OnControlCharacter"/> to be invoked.</para>
    /// <para>If this does not contain <see cref="Ansi.C0.ESC"/>, <see
    /// cref="AnsiReader.OnEscapeSequence"/> will never be invoked.</para>
    /// </remarks>
    public List<char> C0 { get; } = [
      Ansi.C0.BEL,
      Ansi.C0.BS,
      Ansi.C0.HT,
      Ansi.C0.LF,
      Ansi.C0.CR,
      Ansi.C0.ESC
    ];

    /// <summary>
    /// Whether to allow bare <see cref="Ansi.C1"/> control characters to cause
    /// <see cref="AnsiReader.OnControlCharacter"/> to be invoked, rather than
    /// only <see cref="Ansi.C1"/> control characters preceded by <see
    /// cref="Ansi.C0.ESC"/> (which are delivered via <see
    /// cref="AnsiReader.OnEscapeSequence"/>).
    /// </summary>
    public bool AllowBareC1 { get; set; } = false;

    /// <summary>
    /// <see cref="Ansi.C1"/> control characters configuration.
    /// </summary>
    /// <remarks>
    /// <para>Add control characters to the list that should cause
    /// <see cref="AnsiReader.OnControlCharacter"/> to be invoked.</para>
    /// <para>If this does not contain <see cref="Ansi.C0.ESC"/>, <see
    /// cref="AnsiReader.OnEscapeSequence"/> will never be invoked.</para>
    /// </remarks>
    public List<char> C1 { get; } = [
      Ansi.C1.CSI
    ];

    /// <summary>
    /// Escape sequences configuration.
    /// </summary>
    /// <remarks>Has no effect if <see cref="C0"/> does not include <see
    /// cref="Ansi.C0.ESC"/>. If <see langword="null"/>, <see
    /// cref="AnsiReader.OnEscapeSequence"/> will never be invoked.</remarks>
    public EscapeSequences? EscapeSequences { get; set; } = new();
  }
}
