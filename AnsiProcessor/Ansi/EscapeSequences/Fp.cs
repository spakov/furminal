namespace Spakov.AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The Fp escape sequences, which are <see cref="C0.ESC"/> followed by
  /// <c>0x30-0x3f</c> (the constants in <see cref="Fp"/>).
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fp_Escape_sequences"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// </list>
  /// </remarks>
  public static class Fp {
    /// <summary>
    /// The DEC save cursor (DECSC) character.
    /// </summary>
    public const char DECSC = '7';

    /// <summary>
    /// The DEC restore cursor (DECRC) character.
    /// </summary>
    public const char DECRC = '8';
  }
}
