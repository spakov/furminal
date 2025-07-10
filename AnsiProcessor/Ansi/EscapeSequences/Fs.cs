namespace Spakov.AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The Fs escape sequences, which are <see cref="C0.ESC"/> followed by
  /// <c>0x60-0x7e</c> (the constants in <see cref="Fs"/>).
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fs_Escape_sequences"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// </list>
  /// </remarks>
  public static class Fs {
    /// <summary>
    /// The full reset (RIS) character.
    /// </summary>
    public const char RIS = 'c';

    /// <summary>
    /// The memory lock character.
    /// </summary>
    public const char MEMORY_LOCK = 'l';

    /// <summary>
    /// The memory unlock character.
    /// </summary>
    public const char MEMORY_UNLOCK = 'm';

    /// <summary>
    /// The G2 character set as GL (LS2) character.
    /// </summary>
    public const char LS2 = 'n';

    /// <summary>
    /// The G3 character set as GL (LS3) character.
    /// </summary>
    public const char LS3 = 'o';

    /// <summary>
    /// The G3 character set as GR (LS3R) character.
    /// </summary>
    public const char LS3R = '|';

    /// <summary>
    /// The G2 character set as GR (LS2R) character.
    /// </summary>
    public const char LS2R = '}';

    /// <summary>
    /// The G1 character set as GR (LS1R) character.
    /// </summary>
    public const char LS1R = '~';
  }
}
