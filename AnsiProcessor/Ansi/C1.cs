namespace Spakov.AnsiProcessor.Ansi
{
    /// <summary>
    /// The C1 control characters.
    /// </summary>
    /// <remarks>Source: <see
    /// href="https://en.wikipedia.org/wiki/C0_and_C1_control_codes#C1_controls"
    /// /></remarks>
    public static class C1
    {
        /// <summary>
        /// The offset between the printable representation of PAD and its
        /// value.
        /// </summary>
        internal const byte Offset = (byte)'@';

        /// <summary>
        /// The padding (PAD) character.
        /// </summary>
        public const char PAD = (char)((byte)'@' + Offset);

        /// <summary>
        /// The high octet preset (HOP) character.
        /// </summary>
        public const char HOP = (char)((byte)'A' + Offset);

        /// <summary>
        /// The break permitted here (BPH) character.
        /// </summary>
        public const char BPH = (char)((byte)'B' + Offset);

        /// <summary>
        /// The no break here (NBH) character.
        /// </summary>
        public const char NBH = (char)((byte)'C' + Offset);

        /// <summary>
        /// The index (IND) character.
        /// </summary>
        public const char IND = (char)((byte)'D' + Offset);

        /// <summary>
        /// The next line (NEL) character.
        /// </summary>
        public const char NEL = (char)((byte)'E' + Offset);

        /// <summary>
        /// The start of selected area (SSA) character.
        /// </summary>
        public const char SSA = (char)((byte)'F' + Offset);

        /// <summary>
        /// The end of selected area (ESA) character.
        /// </summary>
        public const char ESA = (char)((byte)'G' + Offset);

        /// <summary>
        /// The horizontal tabulation set (HTS) character.
        /// </summary>
        public const char HTS = (char)((byte)'H' + Offset);

        /// <summary>
        /// The horizontal tabulation with justification (HTJ) character.
        /// </summary>
        public const char HTJ = (char)((byte)'I' + Offset);

        /// <summary>
        /// The vertical tabulation set (VTS) character.
        /// </summary>
        public const char VTS = (char)((byte)'J' + Offset);

        /// <summary>
        /// The partial line down (PLD) character.
        /// </summary>
        public const char PLD = (char)((byte)'K' + Offset);

        /// <summary>
        /// The partial line up (PLU) character.
        /// </summary>
        public const char PLU = (char)((byte)'L' + Offset);

        /// <summary>
        /// The reverse index (RI) character.
        /// </summary>
        public const char RI = (char)((byte)'M' + Offset);

        /// <summary>
        /// The single-shift 2 (SS2) character.
        /// </summary>
        public const char SS2 = (char)((byte)'N' + Offset);

        /// <summary>
        /// The single-shift 3 (SS3) character.
        /// </summary>
        public const char SS3 = (char)((byte)'O' + Offset);

        /// <summary>
        /// The device control string (DCS) character.
        /// </summary>
        public const char DCS = (char)((byte)'P' + Offset);

        /// <summary>
        /// The private use 1 (PU1) character.
        /// </summary>
        public const char PU1 = (char)((byte)'Q' + Offset);

        /// <summary>
        /// The private use 2 (PU2) character.
        /// </summary>
        public const char PU2 = (char)((byte)'R' + Offset);

        /// <summary>
        /// The set transmit state (STS) character.
        /// </summary>
        public const char STS = (char)((byte)'S' + Offset);

        /// <summary>
        /// The cancel character (CCH).
        /// </summary>
        public const char CCH = (char)((byte)'T' + Offset);

        /// <summary>
        /// The message waiting (MW) character.
        /// </summary>
        public const char MW = (char)((byte)'U' + Offset);

        /// <summary>
        /// The start of protected area (SPA) character.
        /// </summary>
        public const char SPA = (char)((byte)'V' + Offset);

        /// <summary>
        /// The end of protected area (EPA) character.
        /// </summary>
        public const char EPA = (char)((byte)'W' + Offset);

        /// <summary>
        /// The start of string (SOS) character.
        /// </summary>
        public const char SOS = (char)((byte)'X' + Offset);

        /// <summary>
        /// The single graphic character (SGC) introduceer character.
        /// </summary>
        public const char SGC = (char)((byte)'Y' + Offset);

        /// <summary>
        /// The single character introducer (SCI) character.
        /// </summary>
        public const char SCI = (char)((byte)'Z' + Offset);

        /// <summary>
        /// The control sequence introducer (CSI) character.
        /// </summary>
        public const char CSI = (char)((byte)'[' + Offset);

        /// <summary>
        /// The string terminator (ST) character.
        /// </summary>
        public const char ST = (char)((byte)'\\' + Offset);

        /// <summary>
        /// The operating system command character.
        /// </summary>
        public const char OSC = (char)((byte)']' + Offset);

        /// <summary>
        /// The privacy message (PM) character.
        /// </summary>
        public const char PM = (char)((byte)'^' + Offset);

        /// <summary>
        /// The application program command (APC) character.
        /// </summary>
        public const char APC = (char)((byte)'_' + Offset);
    }
}
