using System.Drawing;

namespace Spakov.AnsiProcessor.AnsiColors
{
    /// <summary>
    /// An ANSI color palette.
    /// </summary>
    /// <remarks>Defaults are Catppuccin v1.7.1 Mocha ANSI colors.</remarks>
    public class Palette
    {
        /// <summary>
        /// The black color.
        /// </summary>
        public virtual Color Black { get; set; } = Color.FromArgb(69, 71, 90);

        /// <summary>
        /// The red color.
        /// </summary>
        public virtual Color Red { get; set; } = Color.FromArgb(243, 139, 168);

        /// <summary>
        /// The green color.
        /// </summary>
        public virtual Color Green { get; set; } = Color.FromArgb(166, 227, 161);

        /// <summary>
        /// The yellow color.
        /// </summary>
        public virtual Color Yellow { get; set; } = Color.FromArgb(249, 226, 175);

        /// <summary>
        /// The blue color.
        /// </summary>
        public virtual Color Blue { get; set; } = Color.FromArgb(137, 180, 250);

        /// <summary>
        /// The magenta color.
        /// </summary>
        public virtual Color Magenta { get; set; } = Color.FromArgb(246, 194, 231);

        /// <summary>
        /// The cyan color.
        /// </summary>
        public virtual Color Cyan { get; set; } = Color.FromArgb(148, 226, 213);

        /// <summary>
        /// The white color.
        /// </summary>
        public virtual Color White { get; set; } = Color.FromArgb(166, 173, 200);

        /// <summary>
        /// The bright black color.
        /// </summary>
        public virtual Color BrightBlack { get; set; } = Color.FromArgb(88, 91, 112);

        /// <summary>
        /// The bright red color.
        /// </summary>
        public virtual Color BrightRed { get; set; } = Color.FromArgb(243, 119, 153);

        /// <summary>
        /// The bright green color.
        /// </summary>
        public virtual Color BrightGreen { get; set; } = Color.FromArgb(137, 216, 139);

        /// <summary>
        /// The bright yellow color.
        /// </summary>
        public virtual Color BrightYellow { get; set; } = Color.FromArgb(235, 211, 145);

        /// <summary>
        /// The bright blue color.
        /// </summary>
        public virtual Color BrightBlue { get; set; } = Color.FromArgb(116, 168, 252);

        /// <summary>
        /// The bright magenta color.
        /// </summary>
        public virtual Color BrightMagenta { get; set; } = Color.FromArgb(242, 174, 222);

        /// <summary>
        /// The bright cyan color.
        /// </summary>
        public virtual Color BrightCyan { get; set; } = Color.FromArgb(107, 215, 202);

        /// <summary>
        /// The bright white color.
        /// </summary>
        public virtual Color BrightWhite { get; set; } = Color.FromArgb(186, 194, 222);

        /// <summary>
        /// The default foreground color.
        /// </summary>
        public virtual Color DefaultForegroundColor { get; set; }

        /// <summary>
        /// The default background color.
        /// </summary>
        public virtual Color DefaultBackgroundColor { get; set; }

        /// <summary>
        /// The default underline color.
        /// </summary>
        public virtual Color DefaultUnderlineColor { get; set; }

        /// <summary>
        /// Initializes a <see cref="Palette"/>.
        /// </summary>
        public Palette()
        {
            DefaultForegroundColor = White;
            DefaultBackgroundColor = Black;
            DefaultUnderlineColor = White;
        }
    }
}
