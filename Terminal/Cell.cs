using Spakov.AnsiProcessor.Output;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Spakov.Terminal {
  /// <summary>
  /// A terminal screen buffer cell.
  /// </summary>
  internal struct Cell {
    /// <summary>
    /// The grapheme cluster represented by this <see cref="Cell"/>.
    /// </summary>
    public string? GraphemeCluster;

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
      if (a.GraphemeCluster != b.GraphemeCluster) return false;
      if (a.GraphicRendition != b.GraphicRendition) return false;
      if (a.Selected != b.Selected) return false;

      return true;
    }

    public static bool operator !=(Cell a, Cell b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is Cell other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(GraphemeCluster, GraphicRendition, Selected);
  }
}
