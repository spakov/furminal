using Spakov.W6t.Settings.Json.SchemaAttributes;

namespace Spakov.W6t.Settings.Json
{
    [Description("The w6t scrollback settings.")]
    internal class Scrollback
    {
        [Description("The number of lines to use for scrollback.")]
        [DefaultIntNumber(5000)]
        [MinimumInt(0)]
        public int? ScrollbackLines { get; set; }

        [Description("The number of lines to use for a large scrollback.")]
        [DefaultIntNumber(12)]
        [MinimumInt(1)]
        public int? LinesPerScrollback { get; set; }

        [Description("The number of lines to use for a small scrollback.")]
        [DefaultIntNumber(1)]
        [MinimumInt(1)]
        public int? LinesPerSmallScrollback { get; set; }

        [Description("The number of lines to use for a mouse wheel scrollback.")]
        [DefaultIntNumber(8)]
        [MinimumInt(1)]
        public int? LinesPerWheelScrollback { get; set; }
    }
}
