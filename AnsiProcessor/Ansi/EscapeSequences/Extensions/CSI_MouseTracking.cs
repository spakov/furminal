namespace AnsiProcessor.Ansi.EscapeSequences.Extensions {
  /// <summary>
  /// The CSI mouse tracking extension escape sequences, which are either (for
  /// X10/11) <see cref="C0.ESC"/>, followed by <see cref="C1.CSI"/>, followed
  /// by <see cref="CSI_MouseTracking.MOUSE_TRACKING_LEADER"/>, followed by
  /// <c>Cb</c>, followed by <c>Cx</c>, followed by <c>Cy</c>, or (for SGR)
  /// <see cref="C0.ESC"/>, followed by <see cref="C1.CSI"/>, followed by <see
  /// cref="CSI_MouseTracking.MOUSE_TRACKING_SGR_LEADER"/>, followed by
  /// <c>Cb</c>, followed by <see
  /// cref="CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR"/>, followed by
  /// <c>Cx</c>, followed by <see
  /// cref="CSI_MouseTracking.MOUSE_TRACKING_SGR_SEPARATOR"/>, followed by
  /// <c>Cy</c>, followed by either <see
  /// cref="CSI_MouseTracking.MOUSE_TRACKING_SGR_PRESS_TERMINATOR"/> or <see
  /// cref="CSI_MouseTracking.MOUSE_TRACKING_SGR_RELEASE_TERMINATOR"/>.
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Mouse-Tracking"
  /// /></item>
  /// </list>
  /// </remarks>
  public static class CSI_MouseTracking {
    /// <summary>
    /// The X10/X11 mouse tracking leader character.
    /// </summary>
    public const char MOUSE_TRACKING_LEADER = 'M';

    /// <summary>
    /// The SGR mouse tracking leader character.
    /// </summary>
    public const char MOUSE_TRACKING_SGR_LEADER = '<';

    /// <summary>
    /// The SGR mouse tracking separator character.
    /// </summary>
    public const char MOUSE_TRACKING_SGR_SEPARATOR = ';';

    /// <summary>
    /// The SGR mouse tracking button press termination character.
    /// </summary>
    public const char MOUSE_TRACKING_SGR_PRESS_TERMINATOR = 'M';

    /// <summary>
    /// The SGR mouse tracking button release termination character.
    /// </summary>
    public const char MOUSE_TRACKING_SGR_RELEASE_TERMINATOR = 'm';
  }
}
