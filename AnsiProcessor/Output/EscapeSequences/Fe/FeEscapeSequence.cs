using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.CSI;
using Spakov.AnsiProcessor.Output.EscapeSequences.Fe.OSC;
using Spakov.AnsiProcessor.TermCap;
using System.Text;

namespace Spakov.AnsiProcessor.Output.EscapeSequences.Fe
{
    /// <summary>
    /// A representation of an ANSI <see cref="Ansi.EscapeSequences.Fe"/>
    /// escape sequence.
    /// </summary>
    public class FeEscapeSequence : EscapeSequence
    {
        /// <summary>
        /// Initializes an <see cref="FeEscapeSequence"/>.
        /// </summary>
        /// <param name="rawFeEscapeSequence">The raw Fe escape
        /// sequence.</param>
        protected FeEscapeSequence(string rawFeEscapeSequence) : base(rawFeEscapeSequence) {
        }

        /// <summary>
        /// Initializes an <see cref="FeEscapeSequence"/>, or more likely, one
        /// of its subclasses.
        /// </summary>
        /// <param name="terminalCapabilities">A <see
        /// cref="TerminalCapabilities"/>.</param>
        /// <param name="palette">A <see cref="Palette"/>.</param>
        /// <param name="rawFeEscapeSequence">The raw Fe escape sequence from
        /// which to initialize an object.</param>
        /// <returns>An <see cref="FeEscapeSequence"/>.</returns>
        internal static FeEscapeSequence InitializeFeEscapeSequence(TerminalCapabilities terminalCapabilities, Palette palette, string rawFeEscapeSequence)
        {
            if (CSIEscapeSequence.IsCSIEscapeSequence(rawFeEscapeSequence))
            {
                return CSIEscapeSequence.InitializeCSIEscapeSequence(terminalCapabilities, palette, rawFeEscapeSequence);
            }
            else if (OSCEscapeSequence.IsOSCEscapeSequence(rawFeEscapeSequence))
            {
                return OSCEscapeSequence.InitializeOSCEscapeSequence(rawFeEscapeSequence);
            }

            return new(rawFeEscapeSequence);
        }

        /// <summary>
        /// Determines whether an Fe escape sequence is complete to facilitate
        /// building the sequence.
        /// </summary>
        /// <remarks>
        /// <para>Strips off the trailing ST or BEL.</para>
        /// <para>Sources:
        /// <list type="bullet">
        /// <item><see
        /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Fe_Escape_sequences"
        /// /></item>
        /// <item><see
        /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"
        /// /></item>
        /// </list></para>
        /// </remarks>
        /// <param name="escapeSequenceBuilder">The <see cref="StringBuilder"/>
        /// that contains the escape sequence's characters received so
        /// far.</param>
        /// <param name="character">The most recently received
        /// character.</param>
        /// <returns><see langword="true"/> if the escape sequence is complete
        /// or <see langword="false"/> otherwise.</returns>
        internal static bool IsFeEscapeSequenceComplete(StringBuilder escapeSequenceBuilder, char character)
        {
            // Check for string terminator
            if (character == Ansi.C1.ST)
            {
                escapeSequenceBuilder.Remove(escapeSequenceBuilder.ToString().Length - 1, 1);

                return true;
            }

            // An escaped ST is also acceptable, which is slightly trickier
            if (character == Ansi.EscapeSequences.Fe.ST)
            {
                string escapeSequence = escapeSequenceBuilder.ToString();

                if (escapeSequence.Substring(escapeSequence.Length - 2, 1)[0] == Ansi.C0.ESC)
                {
                    escapeSequenceBuilder.Remove(escapeSequence.Length - 2, 2);

                    return true;
                }
            }

            // Check for control characters that have no parameters
            if (escapeSequenceBuilder.Length == 1)
            {
                switch (character)
                {
                    case Ansi.EscapeSequences.Fe.PAD:
                    case Ansi.EscapeSequences.Fe.HOP:
                    case Ansi.EscapeSequences.Fe.BPH:
                    case Ansi.EscapeSequences.Fe.NBH:
                    case Ansi.EscapeSequences.Fe.IND:
                    case Ansi.EscapeSequences.Fe.NEL:
                    case Ansi.EscapeSequences.Fe.SSA:
                    case Ansi.EscapeSequences.Fe.ESA:
                    case Ansi.EscapeSequences.Fe.HTS:
                    case Ansi.EscapeSequences.Fe.HTJ:
                    case Ansi.EscapeSequences.Fe.VTS:
                    case Ansi.EscapeSequences.Fe.PLD:
                    case Ansi.EscapeSequences.Fe.PLU:
                    case Ansi.EscapeSequences.Fe.RI:
                    case Ansi.EscapeSequences.Fe.PU1:
                    case Ansi.EscapeSequences.Fe.PU2:
                    case Ansi.EscapeSequences.Fe.STS:
                    case Ansi.EscapeSequences.Fe.CCH:
                    case Ansi.EscapeSequences.Fe.MW:
                    case Ansi.EscapeSequences.Fe.SPA:
                    case Ansi.EscapeSequences.Fe.EPA:
                    case Ansi.EscapeSequences.Fe.SOS:
                    case Ansi.EscapeSequences.Fe.SGC:
                    case Ansi.EscapeSequences.Fe.SCI:
                    case Ansi.EscapeSequences.Fe.PM:
                        return true;
                }
            }

            char controlCharacter = escapeSequenceBuilder.ToString()[0];

            // Handle control characters with parameters that terminate with
            // something other than ST
            switch (controlCharacter)
            {
                case Ansi.EscapeSequences.Fe.SS2:
                case Ansi.EscapeSequences.Fe.SS3:
                    if (escapeSequenceBuilder.Length == 2)
                    {
                        return true;
                    }

                    break;

                case Ansi.EscapeSequences.Fe.CSI:
                    return CSIEscapeSequence.IsCSIEscapeSequenceComplete(escapeSequenceBuilder, character);

                case Ansi.EscapeSequences.Fe.OSC:
                    if (character == Ansi.C0.BEL)
                    {
                        escapeSequenceBuilder.Remove(escapeSequenceBuilder.ToString().Length - 1, 1);

                        return true;
                    }

                    break;
            }

            return false;
        }
    }
}