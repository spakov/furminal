namespace Spakov.AnsiProcessor.Ansi {
  /// <summary>
  /// The C0 control characters.
  /// </summary>
  /// <remarks>Source: <see
  /// href="https://en.wikipedia.org/wiki/C0_and_C1_control_codes#C0_controls"
  /// /></remarks>
  public static class C0 {
    /// <summary>
    /// The null (NUL) character.
    /// </summary>
    public const char NUL = '\x00';

    /// <summary>
    /// The start of heading (SOH) character.
    /// </summary>
    public const char SOH = '\x01';

    /// <summary>
    /// The start of text (STX) character.
    /// </summary>
    public const char STX = '\x02';

    /// <summary>
    /// The end of text (ETX) character.
    /// </summary>
    public const char ETX = '\x03';

    /// <summary>
    /// The end of transmission (EOT) character.
    /// </summary>
    public const char EOT = '\x04';

    /// <summary>
    /// The enquiry (ENQ) character.
    /// </summary>
    public const char ENQ = '\x05';

    /// <summary>
    /// The acknowledge (ACK) character.
    /// </summary>
    public const char ACK = '\x06';

    /// <summary>
    /// The bell (BEL) character.
    /// </summary>
    public const char BEL = '\a';

    /// <summary>
    /// The backspace (BS) character.
    /// </summary>
    public const char BS = '\b';

    /// <summary>
    /// The horiontal tabulation (HT) character.
    /// </summary>
    public const char HT = '\t';

    /// <summary>
    /// The line feed (LF) character.
    /// </summary>
    public const char LF = '\n';

    /// <summary>
    /// The vertical tabulation (VT) character.
    /// </summary>
    public const char VT = '\v';

    /// <summary>
    /// The form feed (FF) character.
    /// </summary>
    public const char FF = '\f';

    /// <summary>
    /// The carriage return (CR) character.
    /// </summary>
    public const char CR = '\r';

    /// <summary>
    /// The shift out (SO) character.
    /// </summary>
    public const char SO = '\x0e';

    /// <summary>
    /// The shift in (SI) character.
    /// </summary>
    public const char SI = '\x0f';

    /// <summary>
    /// The data link escape (DLE) character.
    /// </summary>
    public const char DLE = '\x10';

    /// <summary>
    /// The device control one (DC1) character.
    /// </summary>
    public const char DC1 = '\x11';

    /// <summary>
    /// The device control two (DC2) character.
    /// </summary>
    public const char DC2 = '\x12';

    /// <summary>
    /// The device control three (DC3) character.
    /// </summary>
    public const char DC3 = '\x13';

    /// <summary>
    /// The device control four (DC4) character.
    /// </summary>
    public const char DC4 = '\x14';

    /// <summary>
    /// The negative acknowledge (NAK) character.
    /// </summary>
    public const char NAK = '\x15';

    /// <summary>
    /// The synchronous idle (SYN) character.
    /// </summary>
    public const char SYN = '\x16';

    /// <summary>
    /// The end of transmission block (ETB) character.
    /// </summary>
    public const char ETB = '\x17';

    /// <summary>
    /// The cancel (CAN) character.
    /// </summary>
    public const char CAN = '\x18';

    /// <summary>
    /// The end of medium (EM) character.
    /// </summary>
    public const char EM = '\x19';

    /// <summary>
    /// The substitute (SUB) character.
    /// </summary>
    public const char SUB = '\x20';

    /// <summary>
    /// The escape (ESC) character.
    /// </summary>
    public const char ESC = '\x1b';

    /// <summary>
    /// The file separator (FS) character.
    /// </summary>
    public const char FS = '\x1c';

    /// <summary>
    /// The group separator (GS) character.
    /// </summary>
    public const char GS = '\x1d';

    /// <summary>
    /// The record separator (RS) character.
    /// </summary>
    public const char RS = '\x1e';

    /// <summary>
    /// The unit separator (US) character.
    /// </summary>
    public const char US = '\x1f';
  }
}
