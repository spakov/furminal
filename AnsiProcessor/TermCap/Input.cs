namespace AnsiProcessor.TermCap {
  /// <summary>
  /// Allows defining input behavior.
  /// </summary>
  public class Input {
    /// <summary>
    /// Whether the Backspace key sends a <c>DEL</c> (<see langword="true"/>)
    /// or a <c>BS</c> (<see langword="false"/>).
    /// </summary>
    public bool BackspaceIsDel { get; set; } = true;

    /// <summary>
    /// The escape sequence to emit for Home and End key presses.
    /// </summary>
    /// <remarks>
    /// <para>This can be one of the following:</para>
    /// <list type="bullet">
    /// <item><see cref="Ansi.EscapeSequences.Fe.CSI"/>, for modern
    /// behavior</item>
    /// <item><see cref="Ansi.EscapeSequences.Fe.SS3"/>, for xterm-like
    /// behavior</item>
    /// </list>
    /// </remarks>
    public char HomeAndEndKeyEscapeSequence { get; set; } = Ansi.EscapeSequences.Fe.CSI;

    /// <summary>
    /// The escape sequence to emit for F1, F2, F3, and F4 key presses.
    /// </summary>
    /// <remarks>
    /// <para>This can be one of the following:</para>
    /// <list type="bullet">
    /// <item><see cref="Ansi.EscapeSequences.Fe.CSI"/>, for modern
    /// behavior</item>
    /// <item><see cref="Ansi.EscapeSequences.Fe.SS3"/>, for xterm-like
    /// behavior</item>
    /// </list>
    /// </remarks>
    public char F1ThroughF4KeysEscapeSequence { get; set; } = Ansi.EscapeSequences.Fe.CSI;
  }
}
