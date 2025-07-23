using Spakov.Terminal;
using Spakov.W6t.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.W6t.Settings.Json {
  [Description("The w6t cursor settings.")]
  internal class Cursor {
    [Description("The cursor style to use.")]
    [DefaultString("Underline")]
    public CursorStyles? CursorStyle { get; set; }

    [Description("The cursor thickness to use, as a fraction of the font size. Not applicable to the block cursor.")]
    [DefaultDoubleNumber(0.1)]
    [MinimumDouble(0.0)]
    [MaximumDouble(1.0)]
    public double? CursorThickness { get; set; }

    [Description("Whether to blink the cursor.")]
    [DefaultBoolean(true)]
    public bool? CursorBlink { get; set; }

    [Description("The cursor blink rate, in milliseconds. Has no effect if CursorBlink is false.")]
    [DefaultIntNumber(500)]
    [MinimumInt(0)]
    public int? CursorBlinkRate { get; set; }

    [Description("The color to use to draw the cursor.")]
    [DefaultString("#ffbac2de")]
    public Color? CursorColor { get; set; }
  }
}
