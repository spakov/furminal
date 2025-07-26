using Spakov.Furminal.Settings.Json.SchemaAttributes;

namespace Spakov.Furminal.Settings.Json
{
    [Description("The Furminal settings.")]
    internal class Settings
    {
        [Description("Basic settings.")]
        public Basics? Basics { get; set; }

        [Description("Appearance settings.")]
        public Appearance? Appearance { get; set; }

        [Description("Behavior settings.")]
        public Behavior? Behavior { get; set; }

        [Description("Cursor settings.")]
        public Cursor? Cursor { get; set; }

        [Description("Scrollback settings.")]
        public Scrollback? Scrollback { get; set; }

        [Description("Copy and paste settings.")]
        public CopyAndPaste? CopyAndPaste { get; set; }

        [Description("Color palette settings.")]
        public ColorPalette? ColorPalette { get; set; }
    }
}