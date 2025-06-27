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

    public static bool operator ==(SizeF a, SizeF b) => a.Width == b.Width && a.Height == b.Height;

    public static bool operator !=(SizeF a, SizeF b) => a.Width != b.Width || a.Height != b.Height;

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    public override readonly int GetHashCode() => base.GetHashCode();
  }
}
