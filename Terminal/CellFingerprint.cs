using System;
using System.Diagnostics.CodeAnalysis;

namespace Terminal {
  /// <summary>
  /// A cell fingerprint, which uniquely identifies a <see cref="Cell"/>'s
  /// drawing shape.
  /// </summary>
  internal struct CellFingerprint(Cell cell) {
    /// <summary>
    /// The <see cref="Cell"/>'s grapheme cluster.
    /// </summary>
    public string? GraphemeCluster = cell.GraphemeCluster;

    /// <summary>
    /// Whether the cell is to be presented as bold.
    /// </summary>
    public bool Bold = cell.GraphicRendition.Bold;

    /// <summary>
    /// Whether the cell is to be presented as faint.
    /// </summary>
    public bool Faint = cell.GraphicRendition.Faint;

    /// <summary>
    /// Whether the cell is to be presented as italicized.
    /// </summary>
    public bool Italic = cell.GraphicRendition.Italic;

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(CellFingerprint a, CellFingerprint b) {
      if (a.GraphemeCluster != b.GraphemeCluster) return false;
      if (a.Bold != b.Bold) return false;
      if (a.Faint != b.Faint) return false;
      if (a.Italic != b.Italic) return false;

      return true;
    }

    public static bool operator !=(CellFingerprint a, CellFingerprint b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is CellFingerprint other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(GraphemeCluster, Bold, Faint, Italic);
  }
}
