using Spakov.W6t.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.W6t.Settings.Json
{
    [Description("The w6t standard color settings.")]
    internal class StandardColors
    {
        [Description("Black.")]
        [DefaultString("#ff585b70")]
        public Color? Black { get; set; }

        [Description("Red.")]
        [DefaultString("#fff38ba8")]
        public Color? Red { get; set; }

        [Description("Green.")]
        [DefaultString("#ffa6e3a1")]
        public Color? Green { get; set; }

        [Description("Yellow.")]
        [DefaultString("#fff9e2af")]
        public Color? Yellow { get; set; }

        [Description("Blue.")]
        [DefaultString("#ff89b4fa")]
        public Color? Blue { get; set; }

        [Description("Magenta.")]
        [DefaultString("#fff5c2e7")]
        public Color? Magenta { get; set; }

        [Description("Cyan.")]
        [DefaultString("#ff94e2d5")]
        public Color? Cyan { get; set; }

        [Description("White.")]
        [DefaultString("#ffa6adc8")]
        public Color? White { get; set; }
    }
}
