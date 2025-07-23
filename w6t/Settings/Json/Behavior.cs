using Spakov.W6t.Settings.Json.SchemaAttributes;

namespace Spakov.W6t.Settings.Json {
  [Description("The w6t behavior settings.")]
  internal class Behavior {
    [Description("Whether to use the visual bell or play a sound when the terminal bell rings.")]
    [DefaultBoolean(false)]
    public bool? UseVisualBell { get; set; }

    [Description("The number of seconds to display the visual bell. Has no effect if UseVisualBell is false.")]
    [DefaultIntNumber(1)]
    public int? VisualBellDisplayTime { get; set; }

    [Description("Whether to display the context menu when right clicking the terminal.")]
    [DefaultBoolean(true)]
    public bool? UseContextMenu { get; set; }

    [Description("Whether to display an extended context menu when right clicking the terminal. Has no effect if UseContextMenu is false.")]
    [DefaultBoolean(true)]
    public bool? UseExtendedContextMenu { get; set; }
  }
}
