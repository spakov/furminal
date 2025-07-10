using System;
using Windows.UI;

namespace Spakov.W6t.Settings {
  /// <summary>
  /// Contains color-related conversion extension methods.
  /// </summary>
  internal static class ColorHelper {
    /// <summary>
    /// Converts <paramref name="color"/> to a <see
    /// cref="System.Drawing.Color"/>.
    /// </summary>
    /// <param name="color">A <see cref="Color"/>.</param>
    /// <returns>A <see cref="System.Drawing.Color"/>.</returns>
    internal static System.Drawing.Color ToSystemDrawingColor(this Color color) => System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);

    /// <summary>
    /// Converts <paramref name="color"/> to a hex code.
    /// </summary>
    /// <remarks>This is an extension method to <see cref="Color"/>.</remarks>
    /// <param name="color">A <see cref="Color"/>.</param>
    /// <returns>A hex code representing <paramref name="color"/>.</returns>
    internal static string ToHexCode(this Color color) => $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";

    /// <summary>
    /// Converts <paramref name="hexCode"/> to a <see cref="Color"/>.
    /// </summary>
    /// <remarks>
    /// <para>This is an extension method to <see cref="string"/>.</para>
    /// <para>Supports the following variants:</para>
    /// <list type="bullet">
    /// <item><c>#aarrggbb</c></item>
    /// <item><c>#rrggbb</c> -> <c>#FFrrggbb</c></item>
    /// <item><c>#argb</c> -> <c>#aarrggbb</c></item>
    /// <item><c>#rgb</c> -> <c>#FFrrggbb</c></item>
    /// </list>
    /// </remarks>
    /// <param name="hexCode">A hex code.</param>
    /// <returns>A <see cref="Color"/> represented by <paramref
    /// name="hexCode"/>.</returns>
    /// <exception cref="ArgumentException"></exception>
    internal static Color ToColor(this string hexCode) {
      if (!hexCode.StartsWith('#')) {
        throw new ArgumentException("Hex code must start with '#'.", nameof(hexCode));
      }

      byte a;
      byte r;
      byte g;
      byte b;

      try {
        if (hexCode.Length == 4) {
          a = 0xff;
          r = Convert.ToByte(hexCode.Substring(1, 1), 0x10);
          r += (byte) (r * 0x10);
          g = Convert.ToByte(hexCode.Substring(2, 1), 0x10);
          g += (byte) (g * 0x10);
          b = Convert.ToByte(hexCode.Substring(3, 1), 0x10);
          b += (byte) (b * 0x10);
        } else if (hexCode.Length == 5) {
#pragma warning disable IDE0057 // Use range operator
          a = Convert.ToByte(hexCode.Substring(0, 1), 0x10);
#pragma warning restore IDE0057 // Use range operator
          a += (byte) (a * 0x10);
          r = Convert.ToByte(hexCode.Substring(1, 1), 0x10);
          r += (byte) (r * 0x10);
          g = Convert.ToByte(hexCode.Substring(2, 1), 0x10);
          g += (byte) (g * 0x10);
          b = Convert.ToByte(hexCode.Substring(3, 1), 0x10);
          b += (byte) (b * 0x10);
        } else if (hexCode.Length == 7) {
          a = 0xff;
          r = Convert.ToByte(hexCode.Substring(1, 2), 0x10);
          g = Convert.ToByte(hexCode.Substring(3, 2), 0x10);
          b = Convert.ToByte(hexCode.Substring(5, 2), 0x10);
        } else if (hexCode.Length == 9) {
          a = Convert.ToByte(hexCode.Substring(1, 2), 0x10);
          r = Convert.ToByte(hexCode.Substring(3, 2), 0x10);
          g = Convert.ToByte(hexCode.Substring(5, 2), 0x10);
          b = Convert.ToByte(hexCode.Substring(7, 2), 0x10);
        } else {
          throw new ArgumentException("Hex code must have length of 4, 5, 7, or 9.", nameof(hexCode));
        }
      } catch (ArgumentException e) {
        throw new ArgumentException("Hex code contains negative signs.", nameof(hexCode), e);
      } catch (FormatException e) {
        throw new ArgumentException("Hex code contains non-hex characters.", nameof(hexCode), e);
      }

      return Color.FromArgb(a, r, g, b);
    }
  }
}
