using Spakov.AnsiProcessor.AnsiColors;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Spakov.AnsiProcessor.Helpers
{
    /// <summary>
    /// Methods for converting an extended 8-bit SGR color to an RGB color.
    /// </summary>
    /// <remarks>Source: <see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#8-bit"/></remarks>
    internal static class Color8Helper
    {
        private static readonly byte[] s_levels = [0x00, 0x5f, 0x87, 0xaf, 0xd7, 0xff];

        /// <summary>
        /// Converts <paramref name="parameters"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="parameters">The SGR parameters, which are separated by
        /// either <c>;</c> or <c>:</c>, split.</param>
        /// <param name="palette">The <see cref="Palette"/> to use for ANSI
        /// colors.</param>
        /// <returns>A <see cref="Color"/> representing <paramref
        /// name="parameters"/>.</returns>
        /// <exception cref="ArgumentException">Unable to convert <paramref
        /// name="parameters"/> to a <see cref="Color"/>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Negatively impacts readability")]
        internal static Color Color8(List<string> parameters, Palette palette)
        {
            if (parameters.Count != 3)
            {
                throw new ArgumentException("Exactly three parameters required.", nameof(parameters));
            }

            if (parameters[1] != Ansi.EscapeSequences.SGR.COLOR_8)
            {
                throw new ArgumentException("Parameters do not represent an 8-bit color.", nameof(parameters));
            }

            return Type5Color(parameters[2], palette);
        }

        /// <summary>
        /// Converts <paramref name="color"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="color">The SGR type-5 color parameter.</param>
        /// <param name="palette"><inheritdoc cref="Color8"
        /// path="/param[@name='palette']"/></param>
        /// <returns>A <see cref="Color"/> representing <paramref
        /// name="color"/>.</returns>
        /// <exception cref="ArgumentException">Unable to convert <paramref
        /// name="color"/> to a <see cref="Color"/>.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        private static Color Type5Color(string color, Palette palette)
        {
            if (!byte.TryParse(color, out byte colorByte))
            {
                throw new ArgumentException("Color was not parseable as a byte.", nameof(color));
            }

            if (colorByte < 0x10)
            {
                return colorByte switch
                {
                    0x0 => palette.Black,
                    0x1 => palette.Red,
                    0x2 => palette.Green,
                    0x3 => palette.Yellow,
                    0x4 => palette.Blue,
                    0x5 => palette.Magenta,
                    0x6 => palette.Cyan,
                    0x7 => palette.White,
                    0x8 => palette.BrightBlack,
                    0x9 => palette.BrightRed,
                    0xa => palette.BrightGreen,
                    0xb => palette.BrightYellow,
                    0xc => palette.BrightBlue,
                    0xd => palette.BrightMagenta,
                    0xe => palette.BrightCyan,
                    0xf => palette.BrightWhite,
                    _ => throw new InvalidOperationException()
                };
            }
            else if (colorByte is >= 0x10 and < 0xe8)
            {
                byte red = (byte)((colorByte - 0x10) / 36);
                byte green = (byte)((colorByte - 0x10) / 6 % 6);
                byte blue = (byte)((colorByte - 0x10) % 6);

                return Color.FromArgb(s_levels[red], s_levels[green], s_levels[blue]);
            }
            else
            {
                byte gray = (byte)(8 + ((colorByte - 0xe8) * 10));

                return Color.FromArgb(gray, gray, gray);
            }
        }
    }
}
