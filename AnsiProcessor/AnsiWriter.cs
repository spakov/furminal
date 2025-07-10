using Spakov.AnsiProcessor.Helpers;
using Spakov.AnsiProcessor.Input;
using Spakov.AnsiProcessor.TermCap;
using System.IO;
using System.Text;

namespace Spakov.AnsiProcessor {
  /// <summary>
  /// An ANSI writer.
  /// </summary>
  /// <param name="consoleInStream">The input stream to which to process
  /// ANSI.</param>
  /// <param name="terminalCapabilities">A <see cref="TerminalCapabilities"/>
  /// configuration.</param>
  public class AnsiWriter(FileStream consoleInStream, TerminalCapabilities terminalCapabilities) {
    /// <summary>
    /// Sends <paramref name="text"/> to the console input stream.
    /// </summary>
    /// <remarks>
    /// <para>Does nothing if <paramref name="text"/> is <see
    /// langword="null"/>.</para>
    /// </remarks>
    /// <param name="text">The text to send to the console input
    /// stream.</param>
    public void SendText(string? text) {
      if (text is not null) {
        byte[] toSend = Encoding.UTF8.GetBytes(text);

        consoleInStream.Write(toSend, 0, toSend.Length);
        consoleInStream.Flush();
      }
    }

    /// <summary>
    /// Sends <paramref name="escapeSequence"/> to the console input stream.
    /// </summary>
    /// <remarks>Regarding <paramref name="brokenMode"/>, I have <em>absolutely
    /// no idea</em> what is going on. I'm assuming this must be a
    /// ConPTY-ism.</remarks>
    /// <param name="escapeSequence">The escape sequence (as 7-bit ASCII, with
    /// no leading ESC) to send to the console input stream.</param>
    /// <param name="brokenMode">Certain sequences seem to require a "broken"
    /// sequence to be sent; this enables that functionality.</param>
    public void SendEscapeSequence(byte[] escapeSequence, bool brokenMode = false) {
      byte esc = (byte) Ansi.C0.ESC;
      byte[] brokenEsc = [esc, (byte) Ansi.C0.NUL];
      byte[] toSend;

      if (!brokenMode) {
        toSend = new byte[1 + escapeSequence.Length];
        toSend[0] = esc;
        System.Buffer.BlockCopy(escapeSequence, 0, toSend, 1, escapeSequence.Length);
      } else {
        toSend = new byte[brokenEsc.Length + escapeSequence.Length];
        System.Buffer.BlockCopy(brokenEsc, 0, toSend, 0, brokenEsc.Length);
        System.Buffer.BlockCopy(escapeSequence, 0, toSend, brokenEsc.Length, escapeSequence.Length);
      }

      consoleInStream.Write(toSend, 0, toSend.Length);
      consoleInStream.Flush();
    }

    /// <summary>
    /// Sends a <paramref name="keystroke"/> to the console input stream.
    /// </summary>
    /// <param name="keystroke">A keystroke.</param>
    public void SendKeystroke(Keystroke keystroke) {
      string? toSend = KeystrokeHelper.KeystrokeToAnsi(keystroke, terminalCapabilities);
      SendText(toSend);
    }
  }
}
