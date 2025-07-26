using Spakov.Furminal.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal bright color settings.")]
    internal class BrightColors
    {
        [Description("Bright black.")]
        [DefaultString("#ff585b70")]
        public Color? BrightBlack { get; set; }

        [Description("Bright red.")]
        [DefaultString("#fff37799")]
        public Color? BrightRed { get; set; }

        [Description("Bright green.")]
        [DefaultString("#ff89d88b")]
        public Color? BrightGreen { get; set; }

        [Description("Bright yellow.")]
        [DefaultString("#ffebd391")]
        public Color? BrightYellow { get; set; }

        [Description("Bright blue.")]
        [DefaultString("#ff74a8fc")]
        public Color? BrightBlue { get; set; }

        [Description("Bright magenta.")]
        [DefaultString("#fff2aede")]
        public Color? BrightMagenta { get; set; }

        [Description("Bright cyan.")]
        [DefaultString("#ff6bd7ca")]
        public Color? BrightCyan { get; set; }

        [Description("Bright white.")]
        [DefaultString("#ffbac2de")]
        public Color? BrightWhite { get; set; }
    }
}