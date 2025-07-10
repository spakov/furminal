using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Spakov.AnsiProcessor.Output;
using System;
using Windows.UI;

namespace Spakov.Terminal.Helpers {
  /// <summary>
  /// Methods for manipulating <see cref="GraphicRendition"/>s.
  /// </summary>
  internal static class GraphicRenditionHelper {
    /// <summary>
    /// The midpoint of a calculated gamma value.
    /// </summary>
    private const float gammaMidpoint = 0.5f;

    /// <summary>
    /// The coefficient for high-gamma "faint" adjustment.
    /// </summary>
    private const float gammaHighCoefficient = 1.3f;

    /// <summary>
    /// The coefficient for low-gamma "faint" adjustment.
    /// </summary>
    private const float gammaLowCoefficient = 0.7f;

    /// <summary>
    /// Gets the calculated foreground color, taking into account <see
    /// cref="GraphicRendition"/> attributes.
    /// </summary>
    /// <param name="graphicRendition">A <see
    /// cref="GraphicRendition"/>.</param>
    /// <returns>The calculated foreground color.</returns>
    internal static Color CalculatedForegroundColor(this GraphicRendition graphicRendition) {
      if (graphicRendition.Faint) {
        float backgroundColorGamma = 0.0f;

        backgroundColorGamma += (float) graphicRendition.BackgroundColor.R / byte.MaxValue;
        backgroundColorGamma += (float) graphicRendition.BackgroundColor.G / byte.MaxValue;
        backgroundColorGamma += (float) graphicRendition.BackgroundColor.B / byte.MaxValue;

        backgroundColorGamma /= 3;

        return backgroundColorGamma > gammaMidpoint
          ? new() {
            A = graphicRendition.ForegroundColor.A,
            R = (byte) Math.Min(graphicRendition.ForegroundColor.R * gammaHighCoefficient, byte.MaxValue),
            G = (byte) Math.Min(graphicRendition.ForegroundColor.G * gammaHighCoefficient, byte.MaxValue),
            B = (byte) Math.Min(graphicRendition.ForegroundColor.B * gammaHighCoefficient, byte.MaxValue)
          }
          : new() {
            A = graphicRendition.ForegroundColor.A,
            R = (byte) (graphicRendition.ForegroundColor.R * gammaLowCoefficient),
            G = (byte) (graphicRendition.ForegroundColor.G * gammaLowCoefficient),
            B = (byte) (graphicRendition.ForegroundColor.B * gammaLowCoefficient)
          };
      } else {
        return graphicRendition.ForegroundColor.ToWindowsUIColor();
      }
    }

    /// <summary>
    /// Returns the calculated background color, taking into account <see
    /// cref="TerminalControl.BackgroundIsInvisible"/> and <see
    /// cref="GraphicRendition"/> attributes.
    /// </summary>
    /// <remarks>This is an extension method on
    /// <see cref="GraphicRendition"/>.</remarks>
    /// <param name="graphicRendition">A <see
    /// cref="GraphicRendition"/>.</param>
    /// <param name="defaultBackgroundColor">The default background color with
    /// which to draw.</param>
    /// <param name="backgroundIsInvisible">Whether the background, if
    /// <paramref name="defaultBackgroundColor"/>, should be drawn as
    /// transparent.</param>
    /// <param name="honorBackgroundIsInvisible">Whether to honor <paramref
    /// name="backgroundIsInvisible"/>.</param>
    /// <returns>The calculated background color.</returns>
    internal static Color CalculatedBackgroundColor(this GraphicRendition graphicRendition, System.Drawing.Color defaultBackgroundColor, bool backgroundIsInvisible, bool honorBackgroundIsInvisible = true) {
      if (graphicRendition.BackgroundColor == defaultBackgroundColor) {
        if (backgroundIsInvisible && honorBackgroundIsInvisible) {
          return Colors.Transparent;
        }
      }

      if (graphicRendition.Faint) {
        float backgroundColorGamma = 0.0f;

        backgroundColorGamma += (float) graphicRendition.BackgroundColor.R / byte.MaxValue;
        backgroundColorGamma += (float) graphicRendition.BackgroundColor.G / byte.MaxValue;
        backgroundColorGamma += (float) graphicRendition.BackgroundColor.B / byte.MaxValue;

        backgroundColorGamma /= 3;

        return backgroundColorGamma < gammaMidpoint
          ? new() {
            A = graphicRendition.BackgroundColor.A,
            R = (byte) Math.Min(graphicRendition.BackgroundColor.R * gammaHighCoefficient, byte.MaxValue),
            G = (byte) Math.Min(graphicRendition.BackgroundColor.G * gammaHighCoefficient, byte.MaxValue),
            B = (byte) Math.Min(graphicRendition.BackgroundColor.B * gammaHighCoefficient, byte.MaxValue)
          }
          : new() {
            A = graphicRendition.BackgroundColor.A,
            R = (byte) (graphicRendition.BackgroundColor.R * gammaLowCoefficient),
            G = (byte) (graphicRendition.BackgroundColor.G * gammaLowCoefficient),
            B = (byte) (graphicRendition.BackgroundColor.B * gammaLowCoefficient)
          };
      } else {
        return graphicRendition.BackgroundColor.ToWindowsUIColor();
      }
    }

    /// <summary>
    /// Returns the correct <see cref="CanvasTextFormat"/> for a <see
    /// cref="GraphicRendition"/>.
    /// </summary>
    /// <remarks>This is an extension method on
    /// <see cref="GraphicRendition"/>.</remarks>
    /// <param name="graphicRendition">A <see
    /// cref="GraphicRendition"/>.</param>
    /// <param name="terminalRenderer">A <see
    /// cref="TerminalRenderer"/>.</param>
    /// <returns>The correct <see cref="CanvasTextFormat"/>.</returns>
    internal static CanvasTextFormat TextFormat(this GraphicRendition graphicRendition, TerminalRenderer terminalRenderer) {
      byte graphicRenditionTextVariant = 0;

      if (graphicRendition.Bold) {
        graphicRenditionTextVariant |= 0x1;
      }

      if (graphicRendition.Faint) {
        graphicRenditionTextVariant |= 0x2;
      }

      if (graphicRendition.Italic) {
        graphicRenditionTextVariant |= 0x4;
      }

      // If bold and faint, use bold
      if (graphicRenditionTextVariant == 0x03) {
        graphicRenditionTextVariant = 0x01;
      }

      // If bold, faint, and italic, use bold and italic
      if (graphicRenditionTextVariant == 0x07) {
        graphicRenditionTextVariant = 0x05;
      }

      return terminalRenderer.TextFormats[graphicRenditionTextVariant]!;
    }
  }
}
