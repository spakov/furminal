using Windows.UI;

namespace w6t.Settings.Json {
  internal class Cursor {
    public Terminal.CursorStyles? CursorStyle { get; set; }
    public double? CursorThickness { get; set; }
    public bool? CursorBlink { get; set; }
    public int? CursorBlinkRate { get; set; }
    public Color? CursorColor { get; set; }
  }
}
