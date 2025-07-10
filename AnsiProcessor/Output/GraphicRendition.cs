using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;

namespace Spakov.AnsiProcessor.Output {
  /// <summary>
  /// A graphic rendition, which is what an SGR escape sequence modifies.
  /// </summary>
  public struct GraphicRendition {
    /// <summary>
    /// Whether bold font weight is enabled.
    /// </summary>
    public bool Bold;

    /// <summary>
    /// Whether faint is enabled.
    /// </summary>
    public bool Faint;

    /// <summary>
    /// Whether italic is enabled.
    /// </summary>
    public bool Italic;

    /// <summary>
    /// Whether underline is enabled.
    /// </summary>
    public bool Underline;

    /// <summary>
    /// Whether blink is enabled.
    /// </summary>
    public bool Blink;

    /// <summary>
    /// Whether inverse is enabled.
    /// </summary>
    public bool Inverse;

    /// <summary>
    /// Whether crossed-out is enabled.
    /// </summary>
    public bool CrossedOut;

    /// <summary>
    /// Whether double-underline is enabled.
    /// </summary>
    public bool DoubleUnderline;

    /// <summary>
    /// The foreground color.
    /// </summary>
    public Color ForegroundColor;

    /// <summary>
    /// The background color.
    /// </summary>
    public Color BackgroundColor;

    /// <summary>
    /// The underline color.
    /// </summary>
    public Color UnderlineColor;

    /// <summary>
    /// The underline style associated with the extension to <see
    /// cref="Ansi.EscapeSequences.SGR.UNDERLINE"/>.
    /// </summary>
    public UnderlineStyles UnderlineStyle;

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(GraphicRendition a, GraphicRendition b) {
      if (a.Bold != b.Bold) return false;
      if (a.Faint != b.Faint) return false;
      if (a.Italic != b.Italic) return false;
      if (a.Underline != b.Underline) return false;
      if (a.Blink != b.Blink) return false;
      if (a.Inverse != b.Inverse) return false;
      if (a.CrossedOut != b.CrossedOut) return false;
      if (a.DoubleUnderline != b.DoubleUnderline) return false;
      if (a.ForegroundColor != b.ForegroundColor) return false;
      if (a.BackgroundColor != b.BackgroundColor) return false;
      if (a.UnderlineColor != b.UnderlineColor) return false;
      if (a.UnderlineStyle != b.UnderlineStyle) return false;

      return true;
    }

    public static bool operator !=(GraphicRendition a, GraphicRendition b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is GraphicRendition other && this == other;

    public override readonly int GetHashCode() {
      return HashCode.Combine(
        HashCode.Combine(
          Bold,
          Faint,
          Italic,
          Underline,
          Blink,
          Inverse,
          CrossedOut,
          DoubleUnderline
        ),
        ForegroundColor,
        BackgroundColor,
        UnderlineColor,
        UnderlineStyle
      );
    }
  }
}
