using Spakov.Terminal;
using Spakov.Furminal.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal appearance settings.")]
    internal class Appearance
    {
        [Description("The window backdrop to apply.")]
        [DefaultString("Mica")]
        public WindowBackdrop? WindowBackdrop { get; set; }

        [Description("The color to apply if using the solid color window backdrop.")]
        [DefaultString("#ff000000")]
        public Color? SolidColorWindowBackdropColor { get; set; }

        [Description("The font family to use.")]
        [DefaultString("0xProto Nerd Font Propo")]
        public string? FontFamily { get; set; }

        [Description("The font size to use.")]
        [DefaultDoubleNumber(14.0)]
        [MinimumDouble(1.0)]
        public double? FontSize { get; set; }

        [Description("The antialiasing style to apply to use while rendering text.")]
        [DefaultString("Grayscale")]
        public TextAntialiasingStyle? TextAntialiasing { get; set; }

        [Description("Whether to render full-color emoji using the Segoe UI Emoji font.")]
        [DefaultBoolean(false)]
        public bool? FullColorEmoji { get; set; }

        [Description("Whether to use background color erase.")]
        [DefaultBoolean(true)]
        public bool? UseBackgroundColorErase { get; set; }

        [Description("Whether to render the default background color as transparent.")]
        [DefaultBoolean(true)]
        public bool? BackgroundIsInvisible { get; set; }
    }
}