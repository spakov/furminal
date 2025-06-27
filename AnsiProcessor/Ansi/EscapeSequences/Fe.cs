namespace AnsiProcessor.Ansi.EscapeSequences {
  /// <summary>
  /// The Fe escape sequences, which are <see cref="C0.ESC"/> followed by
  /// <c>0x40-0x5f</c> (the constants in <see cref="Fe"/>).
  /// </summary>
  /// <remarks>
  /// Sources:
  /// <list type="bullet">
  /// <item><see
  /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fe_Escape_sequences"
  /// /></item>
  /// <item><see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
  /// </list>
  /// </remarks>
  public static class Fe {
    /// <summary>
    /// The printable representation of the padding (PAD) character.
    /// </summary>
    public const char PAD = (char) ((byte) C1.PAD - C1.offset);

    /// <summary>
    /// The printable representation of the high octet preset (HOP) character.
    /// </summary>
    public const char HOP = (char) ((byte) C1.HOP - C1.offset);

    /// <summary>
    /// The printable representation of the break permitted here (BPH)
    /// character.
    /// </summary>
    public const char BPH = (char) ((byte) C1.BPH - C1.offset);

    /// <summary>
    /// The printable representation of the no break here (NBH) character.
    /// </summary>
    public const char NBH = (char) ((byte) C1.NBH - C1.offset);

    /// <summary>
    /// The printable representation of the index (IND) character.
    /// </summary>
    public const char IND = (char) ((byte) C1.IND - C1.offset);

    /// <summary>
    /// The printable representation of the next line (NEL) character.
    /// </summary>
    public const char NEL = (char) ((byte) C1.NEL - C1.offset);

    /// <summary>
    /// The printable representation of the start of selected area (SSA)
    /// character.
    /// </summary>
    public const char SSA = (char) ((byte) C1.SSA - C1.offset);

    /// <summary>
    /// The printable representation of the end of selected area (ESA)
    /// character.
    /// </summary>
    public const char ESA = (char) ((byte) C1.ESA - C1.offset);

    /// <summary>
    /// The printable representation of the horizontal tabulation set (HTS)
    /// character.
    /// </summary>
    public const char HTS = (char) ((byte) C1.HTS - C1.offset);

    /// <summary>
    /// The printable representation of the horizontal tabulation with
    /// justification (HTJ) character.
    /// </summary>
    public const char HTJ = (char) ((byte) C1.HTJ - C1.offset);

    /// <summary>
    /// The printable representation of the vertical tabulation set (VTS)
    /// character.
    /// </summary>
    public const char VTS = (char) ((byte) C1.VTS - C1.offset);

    /// <summary>
    /// The printable representation of the partial line down (PLD) character.
    /// </summary>
    public const char PLD = (char) ((byte) C1.PLD - C1.offset);

    /// <summary>
    /// The printable representation of the partial line up (PLU) character.
    /// </summary>
    public const char PLU = (char) ((byte) C1.PLU - C1.offset);

    /// <summary>
    /// The printable representation of the reverse index (RI) character.
    /// </summary>
    public const char RI = (char) ((byte) C1.RI - C1.offset);

    /// <summary>
    /// The printable representation of the single-shift 2 (SS2) character.
    /// </summary>
    public const char SS2 = (char) ((byte) C1.SS2 - C1.offset);

    /// <summary>
    /// The printable representation of the single-shift 3 (SS3) character.
    /// </summary>
    public const char SS3 = (char) ((byte) C1.SS3 - C1.offset);

    /// <summary>
    /// The printable representation of the device control string (DCS)
    /// character.
    /// </summary>
    public const char DCS = (char) ((byte) C1.DCS - C1.offset);

    /// <summary>
    /// The printable representation of the private use 1 (PU1) character.
    /// </summary>
    public const char PU1 = (char) ((byte) C1.PU1 - C1.offset);

    /// <summary>
    /// The printable representation of the private use 2 (PU2) character.
    /// </summary>
    public const char PU2 = (char) ((byte) C1.PU2 - C1.offset);

    /// <summary>
    /// The printable representation of the set transmit state (STS) character.
    /// </summary>
    public const char STS = (char) ((byte) C1.STS - C1.offset);

    /// <summary>
    /// The printable representation of the cancel character (CCH).
    /// </summary>
    public const char CCH = (char) ((byte) C1.CCH - C1.offset);

    /// <summary>
    /// The printable representation of the message waiting (MW) character.
    /// </summary>
    public const char MW = (char) ((byte) C1.MW - C1.offset);

    /// <summary>
    /// The printable representation of the start of protected area (SPA)
    /// character.
    /// </summary>
    public const char SPA = (char) ((byte) C1.SPA - C1.offset);

    /// <summary>
    /// The printable representation of the end of protected area (EPA)
    /// character.
    /// </summary>
    public const char EPA = (char) ((byte) C1.EPA - C1.offset);

    /// <summary>
    /// The printable representation of the start of string (SOS) character.
    /// </summary>
    public const char SOS = (char) ((byte) C1.SOS - C1.offset);

    /// <summary>
    /// The printable representation of the single graphic character (SGC)
    /// introducer character.
    /// </summary>
    public const char SGC = (char) ((byte) C1.SGC - C1.offset);

    /// <summary>
    /// The printable representation of the single character introducer (SCI)
    /// character.
    /// </summary>
    public const char SCI = (char) ((byte) C1.SCI - C1.offset);

    /// <summary>
    /// The printable representation of the control sequence introducer (CSI)
    /// character.
    /// </summary>
    public const char CSI = (char) ((byte) C1.CSI - C1.offset);

    /// <summary>
    /// The printable representation of the string terminator (ST) character.
    /// </summary>
    public const char ST = (char) ((byte) C1.ST - C1.offset);

    /// <summary>
    /// The printable representation of the operating system command character.
    /// </summary>
    public const char OSC = (char) ((byte) C1.OSC - C1.offset);

    /// <summary>
    /// The printable representation of the privacy message (PM) character.
    /// </summary>
    public const char PM = (char) ((byte) C1.PM - C1.offset);

    /// <summary>
    /// The printable representation of the application program command (APC)
    /// character.
    /// </summary>
    public const char APC = (char) ((byte) C1.APC - C1.offset);
  }
}
