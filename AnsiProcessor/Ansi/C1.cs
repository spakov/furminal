namespace AnsiProcessor.Ansi {
  /// <summary>
  /// The C1 control characters.
  /// </summary>
  /// <remarks>Source: <see
  /// href="https://en.wikipedia.org/wiki/C0_and_C1_control_codes#C1_controls"
  /// /></remarks>
  public static class C1 {
    /// <summary>
    /// The offset between the printable representation of PAD and its value.
    /// </summary>
    internal const byte offset = (byte) '@';

    /// <summary>
    /// The padding (PAD) character.
    /// </summary>
    public const char PAD = (char) ((byte) '@' + offset);

    /// <summary>
    /// The high octet preset (HOP) character.
    /// </summary>
    public const char HOP = (char) ((byte) 'A' + offset);

    /// <summary>
    /// The break permitted here (BPH) character.
    /// </summary>
    public const char BPH = (char) ((byte) 'B' + offset);

    /// <summary>
    /// The no break here (NBH) character.
    /// </summary>
    public const char NBH = (char) ((byte) 'C' + offset);

    /// <summary>
    /// The index (IND) character.
    /// </summary>
    public const char IND = (char) ((byte) 'D' + offset);

    /// <summary>
    /// The next line (NEL) character.
    /// </summary>
    public const char NEL = (char) ((byte) 'E' + offset);

    /// <summary>
    /// The start of selected area (SSA) character.
    /// </summary>
    public const char SSA = (char) ((byte) 'F' + offset);

    /// <summary>
    /// The end of selected area (ESA) character.
    /// </summary>
    public const char ESA = (char) ((byte) 'G' + offset);

    /// <summary>
    /// The horizontal tabulation set (HTS) character.
    /// </summary>
    public const char HTS = (char) ((byte) 'H' + offset);

    /// <summary>
    /// The horizontal tabulation with justification (HTJ) character.
    /// </summary>
    public const char HTJ = (char) ((byte) 'I' + offset);

    /// <summary>
    /// The vertical tabulation set (VTS) character.
    /// </summary>
    public const char VTS = (char) ((byte) 'J' + offset);

    /// <summary>
    /// The partial line down (PLD) character.
    /// </summary>
    public const char PLD = (char) ((byte) 'K' + offset);

    /// <summary>
    /// The partial line up (PLU) character.
    /// </summary>
    public const char PLU = (char) ((byte) 'L' + offset);

    /// <summary>
    /// The reverse index (RI) character.
    /// </summary>
    public const char RI = (char) ((byte) 'M' + offset);

    /// <summary>
    /// The single-shift 2 (SS2) character.
    /// </summary>
    public const char SS2 = (char) ((byte) 'N' + offset);

    /// <summary>
    /// The single-shift 3 (SS3) character.
    /// </summary>
    public const char SS3 = (char) ((byte) 'O' + offset);

    /// <summary>
    /// The device control string (DCS) character.
    /// </summary>
    public const char DCS = (char) ((byte) 'P' + offset);

    /// <summary>
    /// The private use 1 (PU1) character.
    /// </summary>
    public const char PU1 = (char) ((byte) 'Q' + offset);

    /// <summary>
    /// The private use 2 (PU2) character.
    /// </summary>
    public const char PU2 = (char) ((byte) 'R' + offset);

    /// <summary>
    /// The set transmit state (STS) character.
    /// </summary>
    public const char STS = (char) ((byte) 'S' + offset);

    /// <summary>
    /// The cancel character (CCH).
    /// </summary>
    public const char CCH = (char) ((byte) 'T' + offset);

    /// <summary>
    /// The message waiting (MW) character.
    /// </summary>
    public const char MW = (char) ((byte) 'U' + offset);

    /// <summary>
    /// The start of protected area (SPA) character.
    /// </summary>
    public const char SPA = (char) ((byte) 'V' + offset);

    /// <summary>
    /// The end of protected area (EPA) character.
    /// </summary>
    public const char EPA = (char) ((byte) 'W' + offset);

    /// <summary>
    /// The start of string (SOS) character.
    /// </summary>
    public const char SOS = (char) ((byte) 'X' + offset);

    /// <summary>
    /// The single graphic character (SGC) introduceer character.
    /// </summary>
    public const char SGC = (char) ((byte) 'Y' + offset);

    /// <summary>
    /// The single character introducer (SCI) character.
    /// </summary>
    public const char SCI = (char) ((byte) 'Z' + offset);

    /// <summary>
    /// The control sequence introducer (CSI) character.
    /// </summary>
    public const char CSI = (char) ((byte) '[' + offset);

    /// <summary>
    /// The string terminator (ST) character.
    /// </summary>
    public const char ST = (char) ((byte) '\\' + offset);

    /// <summary>
    /// The operating system command character.
    /// </summary>
    public const char OSC = (char) ((byte) ']' + offset);

    /// <summary>
    /// The privacy message (PM) character.
    /// </summary>
    public const char PM = (char) ((byte) '^' + offset);

    /// <summary>
    /// The application program command (APC) character.
    /// </summary>
    public const char APC = (char) ((byte) '_' + offset);
  }
}
