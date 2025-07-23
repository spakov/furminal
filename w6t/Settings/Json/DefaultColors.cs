using Spakov.W6t.Settings.Json.SchemaAttributes;
using Windows.UI;

namespace Spakov.W6t.Settings.Json {
  [Description("The w6t default color settings.")]
  internal class DefaultColors {
    [Description("The default background color.")]
    [DefaultString("#ff45475a")]
    public Color? DefaultBackgroundColor { get; set; }

    [Description("The default foreground color.")]
    [DefaultString("#ffa6adc8")]
    public Color? DefaultForegroundColor { get; set; }

    [Description("The default underline color.")]
    [DefaultString("#ffa6adc8")]
    public Color? DefaultUnderlineColor { get; set; }
  }
}
