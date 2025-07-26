using Spakov.Furminal.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal default color settings.")]
    internal class DefaultColors
    {
        [Description("The default background color.")]
        [DefaultString("#ff45475a")]
        public Color? DefaultBackgroundColor { get; set; }

        [Description("The default foreground color.")]
        [DefaultString("#ffa6adc8")]
        public Color? DefaultForegroundColor { get; set; }

        [Description("The default underline color.")]
        [DefaultString("#ffa6adc8")]
        public Color? DefaultUnderlineColor { get; set; }
    }
}