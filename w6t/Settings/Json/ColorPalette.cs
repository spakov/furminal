using Spakov.W6t.Settings.Json.SchemaAttributes;

namespace Spakov.W6t.Settings.Json
{
    [Description("The w6t color palette.")]
    internal class ColorPalette
    {
        [Description("The w6t default colors.")]
        public DefaultColors? DefaultColors { get; set; }

        [Description("The w6t standard colors.")]
        public StandardColors? StandardColors { get; set; }

        [Description("The w6t bright colors.")]
        public BrightColors? BrightColors { get; set; }
    }
}
