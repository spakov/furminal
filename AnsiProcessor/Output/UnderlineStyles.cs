namespace AnsiProcessor.Output {
  /// <summary>
  /// A list of underline styles as an extension to <see
  /// cref="Ansi.EscapeSequences.SGR.UNDERLINE"/>.
  /// </summary>
  public enum UnderlineStyles {
    None,
    Single = 1,
    Double,
    Undercurl
  }
}
