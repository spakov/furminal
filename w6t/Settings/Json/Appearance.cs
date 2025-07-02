using Terminal;
using Windows.UI;

namespace w6t.Settings.Json {
  internal class Appearance {
    public WindowBackdrops? WindowBackdrop { get; set; }
    public Color? SolidColorWindowBackdropColor { get; set; }
    public string? FontFamily { get; set; }
    public double? FontSize { get; set; }
    public TextAntialiasingStyles? TextAntialiasing { get; set; }
    public bool? FullColorEmoji { get; set; }
    public bool? UseBackgroundColorErase { get; set; }
    public bool? BackgroundIsInvisible { get; set; }
  }
}
