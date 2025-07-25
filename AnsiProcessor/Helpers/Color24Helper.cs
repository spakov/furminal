using System;
using System.Collections.Generic;
using System.Drawing;

namespace Spakov.AnsiProcessor.Helpers
{
    /// <summary>
    /// Methods for converting an extended 24-bit SGR color to an RGB color.
    /// </summary>
    /// <remarks>
    /// <para>Sources:</para>
    /// <list type="bullet">
    /// <item><see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#24-bit"/></item>
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#:~:text=The%20color%20space%20identifier%20Pi%20is%20ignored."
    /// /></item>
    /// </list>
    /// </remarks>
    internal static class Color24Helper
    {
        /// <summary>
        /// Converts <paramref name="parameters"/> to a <see cref="Color"/>.
        /// </summary>
        /// <remarks>We ignore the ODA colorspace ID, since its behavior is
        /// undefined, which is what Mr. Dickey does as well.</remarks>
        /// <param name="parameters">The SGR parameters, which are separated by
        /// either <c>;</c> or <c>:</c>, split.</param>
        /// <returns>A <see cref="Color"/> representing <paramref
        /// name="parameters"/>.</returns>
        /// <exception cref="ArgumentException">Unable to convert <paramref
        /// name="parameters"/> to a <see cref="Color"/>.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Negatively impacts readability")]
        internal static Color Color24(List<string> parameters)
        {
            if (parameters.Count != 5)
            {
                throw new ArgumentException("Exactly five parameters required.", nameof(parameters));
            }

            if (parameters[1] != Ansi.EscapeSequences.SGR.COLOR_24)
            {
                throw new ArgumentException("Parameters do not represent a 24-bit color.", nameof(parameters));
            }

            return Type2Color(parameters[2], parameters[3], parameters[4]);
        }

        /// <summary>
        /// Converts <paramref name="red"/>, <paramref name="green"/>, and
        /// <paramref name="blue"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="red">The red SGR type-2 color parameter.</param>
        /// <param name="green">The green SGR type-2 color parameter.</param>
        /// <param name="blue">The blue SGR type-2 color parameter.</param>
        /// <returns>A <see cref="Color"/> representing <paramref
        /// name="color"/>.</returns>
        /// <exception cref="ArgumentException">Unable to convert <paramref
        /// name="color"/> to a <see cref="Color"/>.</exception>
        /// <exception cref="InvalidOperationException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Negatively impacts readability")]
        private static Color Type2Color(string red, string green, string blue)
        {
            if (!byte.TryParse(red, out byte redByte))
            {
                throw new ArgumentException("Red color was not parseable as a byte.", nameof(red));
            }

            if (!byte.TryParse(green, out byte greenByte))
            {
                throw new ArgumentException("Green color was not parseable as a byte.", nameof(green));
            }

            if (!byte.TryParse(blue, out byte blueByte))
            {
                throw new ArgumentException("Blue color was not parseable as a byte.", nameof(blue));
            }

            return Color.FromArgb(redByte, greenByte, blueByte);
        }
    }
}
