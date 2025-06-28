using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Terminal {
  /// <summary>
  /// A cell fingerprint, which uniquely identifies a <see cref="Cell"/>'s
  /// drawing shape.
  /// </summary>
  internal struct CellFingerprint(Cell cell) {
    /// <summary>
    /// The <see cref="Cell"/>'s <see cref="Rune"/> index.
    /// </summary>
    /// <remarks><c>-1</c> if the rune is <see langword="null"/>.</remarks>
    public int RuneIndex = cell.Rune is null ? -1 : ((Rune) cell.Rune).Value;

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
      if (a.RuneIndex != b.RuneIndex) return false;
      if (a.Bold != b.Bold) return false;
      if (a.Faint != b.Faint) return false;
      if (a.Italic != b.Italic) return false;

      return true;
    }

    public static bool operator !=(CellFingerprint a, CellFingerprint b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is CellFingerprint other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(RuneIndex, Bold, Faint, Italic);
  }
}
