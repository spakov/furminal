using System;
using System.Diagnostics.CodeAnalysis;

namespace Terminal {
  /// <summary>
  /// Like <see cref="Windows.Foundation.Size"/>, but with <see
  /// langword="float"/>s.
  /// </summary>
  public struct SizeF {
    public float Width;
    public float Height;

    /// <summary>
    /// Initializes a <see cref="SizeF"/> with <paramref name="width"/> and
    /// <paramref name="height"/>.
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public SizeF(float width, float height) {
      Width = width;
      Height = height;
    }

    /// <inheritdoc cref="SizeF(float, float)" />
    public SizeF(double width, double height) {
      Width = (float) width;
      Height = (float) height;
    }

    /// <summary>
    /// Converts <see cref="SizeF"/> to <see cref="Windows.Foundation.Size"/>.
    /// </summary>
    /// <returns>A <see cref="Windows.Foundation.Size"/>.</returns>
    public readonly Windows.Foundation.Size ToSize() => new(Width, Height);

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(SizeF a, SizeF b) {
      if (a.Width != b.Width) return false;
      if (a.Height != b.Height) return false;

      return true;
    }

    public static bool operator !=(SizeF a, SizeF b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is SizeF other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Width, Height);
  }
}
