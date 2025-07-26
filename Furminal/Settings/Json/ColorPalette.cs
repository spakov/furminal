using Spakov.Furminal.Settings.Json.SchemaAttributes;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal color palette.")]
    internal class ColorPalette
    {
        [Description("The Furminal default colors.")]
        public DefaultColors? DefaultColors { get; set; }

        [Description("The Furminal standard colors.")]
        public StandardColors? StandardColors { get; set; }

        [Description("The Furminal bright colors.")]
        public BrightColors? BrightColors { get; set; }
    }
}