using AnsiProcessor.Output;
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

    /// <summary>
    /// Whether this <see cref="Cell"/> contains overfill from the cell below
    /// it.
    /// </summary>
    public bool ContainsOverfillFromBelow;

    /// <summary>
    /// Whether this <see cref="Cell"/> contains overfill from the cell after
    /// it.
    /// </summary>
    public bool ContainsOverfillFromAfter;

    /// <summary>
    /// Whether this <see cref="Cell"/> contains overfill from the cell before
    /// it.
    /// </summary>
    public bool ContainsOverfillFromBefore;

    /// <summary>
    /// Whether this <see cref="Cell"/> contains overfill from the cell above
    /// it.
    /// </summary>
    public bool ContainsOverfillFromAbove;

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(Cell a, Cell b) {
      if (a.Rune != b.Rune) return false;
      if (a.GraphicRendition != b.GraphicRendition) return false;
      if (a.Selected != b.Selected) return false;

      return true;
    }

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator !=(Cell a, Cell b) {
      if (a.Rune != b.Rune) return true;
      if (a.GraphicRendition != b.GraphicRendition) return true;
      if (a.Selected != b.Selected) return true;

      return false;
    }

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    public override readonly int GetHashCode() => base.GetHashCode();
  }
}
