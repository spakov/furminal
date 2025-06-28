using System;
using System.Diagnostics.CodeAnalysis;

namespace Terminal {
  /// <summary>
  /// <see cref="Cell"/> coordinates.
  /// </summary>
  internal struct Caret(int row, int column) {
    public int Row = row;
    public int Column = column;

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(Caret a, Caret b) {
      if (a.Row != b.Row) return false;
      if (a.Column != b.Column) return false;

      return true;
    }

    public static bool operator !=(Caret a, Caret b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Caret other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Row, Column);
  }
}
