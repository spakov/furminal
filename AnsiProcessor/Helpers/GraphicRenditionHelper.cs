using AnsiProcessor.AnsiColors;
using AnsiProcessor.Output;
using AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR;

namespace AnsiProcessor.Helpers {
  /// <summary>
  /// Extension methods for <see cref="GraphicRendition"/>.
  /// </summary>
  public static class GraphicRenditionHelper {
    /// <summary>
    /// Initializes a <see cref="GraphicRendition"/> from the default colors
    /// defined in <paramref name="palette"/>.
    /// </summary>
    /// <remarks>
    /// <para>Also ensures that all attributes are disabled.</para>
    /// <para>This is an extension method to <see
    /// cref="GraphicRendition"/>.</para>
    /// </remarks>
    /// <param name="graphicRendition">A <see
    /// cref="GraphicRendition"/>.</param>.
    /// <param name="palette">A <see cref="Palette"/>.</param>
    public static void InitializeFromPalette(this ref GraphicRendition graphicRendition, Palette palette) {
      graphicRendition.Bold = false;
      graphicRendition.Faint = false;
      graphicRendition.Italic = false;
      graphicRendition.Underline = false;
      graphicRendition.Blink = false;
      graphicRendition.Inverse = false;
      graphicRendition.CrossedOut = false;
      graphicRendition.DoubleUnderline = false;
      graphicRendition.ForegroundColor = palette.DefaultForegroundColor;
      graphicRendition.BackgroundColor = palette.DefaultBackgroundColor;
      graphicRendition.UnderlineColor = palette.DefaultUnderlineColor;
      graphicRendition.UnderlineStyle = UnderlineStyles.None;
    }

    /// <summary>
    /// Merges in <paramref name="sgrEscapeSequence"/>'s <see
    /// cref="SGREscapeSequence.ModifiedProperties"/>.
    /// </summary>
    /// <remarks>This is an extension method to <see
    /// cref="GraphicRendition"/>.</remarks>
    /// <param name="graphicRendition">A <see
    /// cref="GraphicRendition"/>.</param>.
    /// <param name="sgrEscapeSequence">An <see
    /// cref="SGREscapeSequence"/>.</param>
    public static void MergeFrom(this ref GraphicRendition graphicRendition, SGREscapeSequence sgrEscapeSequence) {
      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Bold) != 0) {
        graphicRendition.Bold = sgrEscapeSequence.Bold;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Faint) != 0) {
        graphicRendition.Faint = sgrEscapeSequence.Faint;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Italic) != 0) {
        graphicRendition.Italic = sgrEscapeSequence.Italic;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Underline) != 0) {
        graphicRendition.Underline = sgrEscapeSequence.Underline;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Blink) != 0) {
        graphicRendition.Blink = sgrEscapeSequence.Blink;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.Inverse) != 0) {
        graphicRendition.Inverse = sgrEscapeSequence.Inverse;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.CrossedOut) != 0) {
        graphicRendition.CrossedOut = sgrEscapeSequence.CrossedOut;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.DoubleUnderline) != 0) {
        graphicRendition.DoubleUnderline = sgrEscapeSequence.DoubleUnderline;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.ForegroundColor) != 0) {
        graphicRendition.ForegroundColor = sgrEscapeSequence.ForegroundColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.DefaultForegroundColor) != 0) {
        graphicRendition.ForegroundColor = sgrEscapeSequence.Palette.DefaultForegroundColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.BackgroundColor) != 0) {
        graphicRendition.BackgroundColor = sgrEscapeSequence.BackgroundColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.DefaultBackgroundColor) != 0) {
        graphicRendition.BackgroundColor = sgrEscapeSequence.Palette.DefaultBackgroundColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.UnderlineColor) != 0) {
        graphicRendition.UnderlineColor = sgrEscapeSequence.UnderlineColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.DefaultUnderlineColor) != 0) {
        graphicRendition.UnderlineColor = sgrEscapeSequence.Palette.DefaultUnderlineColor;
      }

      if ((sgrEscapeSequence.ModifiedProperties & SGREscapeSequence.Properties.UnderlineStyle) != 0) {
        graphicRendition.UnderlineStyle = sgrEscapeSequence.UnderlineStyle;
      }

      if (graphicRendition.Underline && graphicRendition.UnderlineStyle == UnderlineStyles.None) {
        graphicRendition.UnderlineStyle = UnderlineStyles.Single;
      }
    }
  }
}
