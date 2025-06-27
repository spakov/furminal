using System.Diagnostics.CodeAnalysis;

namespace Terminal {
  /// <summary>
  /// A rectangle, used for <see cref="Cell"/> overfill tracking.
  /// </summary>
  public struct RectF {
    public float Top;
    public float Left;
    public float Right;
    public float Bottom;

    /// <summary>
    /// Initializes a <see cref="RectF"/> with <paramref name="top"/>,
    /// <paramref name="left"/>, <paramref name="right"/>, and <paramref
    /// name="bottom"/>.
    /// </summary>
    /// <param name="top">The top.</param>
    /// <param name="left">The left.</param>
    /// <param name="right">The right.</param>
    /// <param name="bottom">The bottom.</param>
    public RectF(float top, float left, float right, float bottom) {
      Top = top;
      Left = left;
      Right = right;
      Bottom = bottom;
    }

    /// <inheritdoc cref="RectF(float, float, float, float)"/>
    public RectF(double top, double left, double right, double bottom) {
      Top = (float) top;
      Left = (float) left;
      Right = (float) right;
      Bottom = (float) bottom;
    }

    public static bool operator ==(RectF a, RectF b) => a.Top == b.Top && a.Left == b.Left && a.Right == b.Right && a.Bottom == b.Bottom;

    public static bool operator !=(RectF a, RectF b) => a.Top != b.Top || a.Left != b.Left || a.Right != b.Right || a.Bottom != b.Bottom;

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    public override readonly int GetHashCode() => base.GetHashCode();
  }
}
