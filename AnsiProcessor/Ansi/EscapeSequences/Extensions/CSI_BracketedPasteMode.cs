namespace Spakov.AnsiProcessor.Ansi.EscapeSequences.Extensions {
  /// <summary>
  /// The CSI bracketed paste mode escape sequences.
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Bracketed-Paste-Mode"
  /// /></item>
  /// </list>
  /// </remarks>
  public static class CSI_BracketedPasteMode {
    /// <summary>
    /// The xterm bracketed paste mode start escape sequence string.
    /// </summary>
    public const string BRACKETED_PASTE_MODE_START = "\x1b[200~";

    /// <summary>
    /// The xterm bracketed paste mode end escape sequence string.
    /// </summary>
    public const string BRACKETED_PASTE_MODE_END = "\x1b[201~";
  }
}
