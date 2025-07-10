using Spakov.Terminal;
using Windows.UI;

namespace Spakov.W6t.Settings.Json {
  internal class Cursor {
    public CursorStyles? CursorStyle { get; set; }
    public double? CursorThickness { get; set; }
    public bool? CursorBlink { get; set; }
    public int? CursorBlinkRate { get; set; }
    public Color? CursorColor { get; set; }
  }
}
