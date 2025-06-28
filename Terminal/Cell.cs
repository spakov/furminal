using AnsiProcessor.Output;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Terminal {
  /// <summary>
  /// A terminal screen buffer cell.
  /// </summary>
  internal struct Cell {
    /// <summary>
    /// The <see cref="System.Text.Rune"/> represented by this <see
    /// cref="Cell"/>.
    /// </summary>
    public Rune? Rune;

    /// <summary>
    /// The <see cref="AnsiProcessor.Output.GraphicRendition"/> used to draw
    /// this <see cref="Cell"/>.
    /// </summary>
    public GraphicRendition GraphicRendition;

    /// <summary>
    /// Whether this <see cref="Cell"/> is selected.
    /// </summary>
    public bool Selected;

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(Cell a, Cell b) {
      if ((a.Rune is null ? -1 : ((Rune) a.Rune).Value) != (b.Rune is null ? -1 : ((Rune) b.Rune).Value)) return false;
      if (a.GraphicRendition != b.GraphicRendition) return false;
      if (a.Selected != b.Selected) return false;

      return true;
    }

    public static bool operator !=(Cell a, Cell b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Cell other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Rune, GraphicRendition, Selected);
  }
}
