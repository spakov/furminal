namespace Spakov.AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The nF escape sequences, which are <see cref="C0.ESC"/> [followed by
  /// <c>0x20-0x2f</c>, followed by additional bytes], where the bracketed text
  /// refers to the constants defined in <see cref="NF"/>.
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#nF_Escape_sequences"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// </list>
  /// </remarks>
  public static class NF {
    /// <summary>
    /// The 7-bit controls (S7C1T) character sequence.
    /// </summary>
    public const string S7C1T = " F";

    /// <summary>
    /// The 8-bit controls (S8C1T) character sequence.
    /// </summary>
    public const string S8C1T = " G";

    /// <summary>
    /// The ANSI conformance level 1 character sequence.
    /// </summary>
    public const string ANSI_CONFORMANCE_LEVEL_1 = " L";

    /// <summary>
    /// The ANSI conformance level 2 character sequence.
    /// </summary>
    public const string ANSI_CONFORMANCE_LEVEL_2 = " M";

    /// <summary>
    /// The ANSI conformance level 3 character sequence.
    /// </summary>
    public const string ANSI_CONFORMANCE_LEVEL_3 = " N";

    /// <summary>
    /// The DEC double-height line (DECDHL), top half character sequence.
    /// </summary>
    public const string DECDHL_TOP = "#3";

    /// <summary>
    /// The DEC double-height line (DECDHL), bottom half character sequence.
    /// </summary>
    public const string DECDHL_BOTTOM = "#4";

    /// <summary>
    /// The DEC single-width line (DECSWL) character sequence.
    /// </summary>
    public const string DECSWL = "#5";

    /// <summary>
    /// The DEC double-width line (DECDWL) character sequence.
    /// </summary>
    public const string DECDWL = "#6";

    /// <summary>
    /// The DEC screen alignment test (DECALN) character sequence.
    /// </summary>
    public const string DECALN = "#8";

    /// <summary>
    /// The select default character set character sequence.
    /// </summary>
    public const string SELECT_DEFAULT_CHARACTER_SET = "%@";

    /// <summary>
    /// The select UTF-8 character set character sequence.
    /// </summary>
    public const string SELECT_UTF8_CHARACTER_SET = "%G";

    /// <summary>
    /// The designate G0 character set for VT100 character sequence.
    /// </summary>
    public const string DESIGNATE_G0_CHARACTER_SET_VT100 = "(";

    /// <summary>
    /// The designate G1 character set for VT100 character sequence.
    /// </summary>
    public const string DESIGNATE_G1_CHARACTER_SET_VT100 = ")";

    /// <summary>
    /// The designate G2 character set for VT220 character sequence.
    /// </summary>
    public const string DESIGNATE_G2_CHARACTER_SET_VT220 = "*";

    /// <summary>
    /// The designate G3 character set for VT220 character sequence.
    /// </summary>
    public const string DESIGNATE_G3_CHARACTER_SET_VT220 = "+";

    /// <summary>
    /// The designate G1 character set for VT300 character sequence.
    /// </summary>
    public const string DESIGNATE_G1_CHARACTER_SET_VT300 = "-";

    /// <summary>
    /// The designate G2 character set for VT300 character sequence.
    /// </summary>
    public const string DESIGNATE_G2_CHARACTER_SET_VT300 = ".";

    /// <summary>
    /// The designate G3 character set for VT300 character sequence.
    /// </summary>
    public const string DESIGNATE_G3_CHARACTER_SET_VT300 = "/";
  }
}
