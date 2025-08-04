using System;
using System.Text;

namespace Spakov.AnsiProcessor.Helpers
{
    /// <summary>
    /// Methods to facilitate printing strings containing control characters.
    /// </summary>
    public static class PrintableHelper
    {
        /// <summary>
        /// Converts all control characters in <paramref name="input"/> to a
        /// readable string representation.
        /// </summary>
        /// <param name="input">The string to convert.</param>
        /// <returns>The converted string.</returns>
        public static string MakePrintable(string? input)
        {
            StringBuilder output = new();

            if (input is null)
            {
                output.Append("┆(null)┆");
            }
            else
            {
                foreach (char character in input)
                {
                    output.Append(MakePrintable(character));
                }
            }

            return output.ToString();
        }

        /// <summary>
        /// Converts <paramref name="input"/> to a readable string
        /// representation, replacing all <see cref="Ansi.C0"/> and <see
        /// cref="Ansi.C1"/>
        /// characters.
        /// </summary>
        /// <param name="input">The character to convert.</param>
        /// <returns>The converted string.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string MakePrintable(char input)
        {
            return char.IsControl(input)
                ? (byte)input switch
                {
                    0x00 => "┆NUL┆",
                    0x01 => "┆SOH┆",
                    0x02 => "┆STX┆",
                    0x03 => "┆ETX┆",
                    0x04 => "┆EOT┆",
                    0x05 => "┆ENQ┆",
                    0x06 => "┆ACK┆",
                    0x07 => "┆BEL┆",
                    0x08 => "┆BS┆",
                    0x09 => "┆HT┆",
                    0x0a => "┆LF┆",
                    0x0b => "┆VT┆",
                    0x0c => "┆FF┆",
                    0x0d => "┆CR┆",
                    0x0e => "┆SO┆",
                    0x0f => "┆SI┆",
                    0x10 => "┆DLE┆",
                    0x11 => "┆DC1┆",
                    0x12 => "┆DC2┆",
                    0x13 => "┆DC3┆",
                    0x14 => "┆DC4┆",
                    0x15 => "┆NAK┆",
                    0x16 => "┆SYN┆",
                    0x17 => "┆ETB┆",
                    0x18 => "┆CAN┆",
                    0x19 => "┆EM┆",
                    0x1a => "┆SUB┆",
                    0x1b => "┆ESC┆",
                    0x1c => "┆FS┆",
                    0x1d => "┆GS┆",
                    0x1e => "┆RS┆",
                    0x1f => "┆US┆",
                    0x7f => "┆DEL┆",
                    0x80 => "┆PAD┆",
                    0x81 => "┆HOP┆",
                    0x82 => "┆BPH┆",
                    0x83 => "┆NBH┆",
                    0x84 => "┆IND┆",
                    0x85 => "┆NEL┆",
                    0x86 => "┆SSA┆",
                    0x87 => "┆ESA┆",
                    0x88 => "┆HTS┆",
                    0x89 => "┆HTJ┆",
                    0x8a => "┆VTS┆",
                    0x8b => "┆PLD┆",
                    0x8c => "┆PLU┆",
                    0x8d => "┆RI┆",
                    0x8e => "┆SS2┆",
                    0x8f => "┆SS3┆",
                    0x90 => "┆DCS┆",
                    0x91 => "┆PU1┆",
                    0x92 => "┆PU2┆",
                    0x93 => "┆STS┆",
                    0x94 => "┆CCH┆",
                    0x95 => "┆MW┆",
                    0x96 => "┆SPA┆",
                    0x97 => "┆EPA┆",
                    0x98 => "┆SOS┆",
                    0x99 => "┆SGC┆",
                    0x9a => "┆SCI┆",
                    0x9b => "┆CSI┆",
                    0x9c => "┆ST┆",
                    0x9d => "┆OSC┆",
                    0x9e => "┆PM┆",
                    0x9f => "┆APC┆",
                    _ => throw new InvalidOperationException()
                }
                : input.ToString();
        }
    }
}
