using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Terminal.Helpers;

namespace Terminal {
  /// <summary>
  /// A cell to be drawn by the <see cref="TerminalRenderer"/>, plus metadata.
  /// </summary>
  internal struct DrawableCell {
    /// <summary>
    /// The caret position at which to draw the <see cref="Terminal.Cell"/>.
    /// </summary>
    public Caret Caret;

    /// <summary>
    /// The pixel position at which to draw the <see cref="Terminal.Cell"/>.
    /// </summary>
    public Vector2 Point;

    /// <summary>
    /// The <see cref="Terminal.Cell"/> to draw.
    /// </summary>
    public Cell Cell;

    /// <summary>
    /// The <see cref="Terminal.Cell"/>'s fingerprint.
    /// </summary>
    public CellFingerprint CellFingerprint;

    /// <summary>
    /// The <see cref="Terminal.Cell"/>'s <see
    /// cref="Microsoft.Graphics.Canvas.Text.CanvasTextLayout"/>.
    /// </summary>
    public CanvasTextLayout CanvasTextLayout;

    /// <summary>
    /// The <see cref="Terminal.Cell"/>'s overfill.
    /// </summary>
    public RectF Overfill;

    /// <summary>
    /// Initializes a <see cref="DrawableCell"/>.
    /// </summary>
    /// <param name="terminalRenderer">A <see
    /// cref="TerminalRenderer"/>.</param>
    /// <param name="drawingSession"><paramref name="terminalRenderer"/>'s
    /// draw loop's <see cref="CanvasDrawingSession"/>.</param>
    /// <param name="caret"><paramref name="cell"/>'s location.</param>
    /// <param name="cell">A <see cref="Terminal.Cell"/>.</param>
    public DrawableCell(TerminalRenderer terminalRenderer, CanvasDrawingSession drawingSession, Caret caret, Vector2 point, Cell cell) {
      Caret = caret;
      Point = point;
      Cell = cell;
      CellFingerprint = new(cell);

      if (!terminalRenderer.CanvasTextLayoutCache.TryGetValue(CellFingerprint, out CanvasTextLayout? canvasTextLayout)) {
        canvasTextLayout = new CanvasTextLayout(
          drawingSession,
          cell.Rune.ToString(),
          cell.GraphicRendition.TextFormat(terminalRenderer),
          0.0f,
          0.0f
        );

        terminalRenderer.CanvasTextLayoutCache.Add(CellFingerprint, canvasTextLayout);
      }

      CanvasTextLayout = canvasTextLayout!;

      // Take into account antialiasing
      float fudge = cell.Rune != null && !Rune.IsWhiteSpace((Rune) cell.Rune) ? 2.0f : 0.0f;

      if (!terminalRenderer.OverfillCache.TryGetValue(CellFingerprint, out RectF overfill)) {
        float overfillTop = Math.Abs(
          Math.Min(
            (float) CanvasTextLayout.DrawBounds.Y - fudge,
            0.0f
          )
        );

        float overfillLeft = Math.Abs(
          Math.Min(
            (float) CanvasTextLayout.DrawBounds.X - fudge,
            0.0f
          )
        );

        float overfillRight = Math.Max(
          Math.Max(
            (float) (CanvasTextLayout.DrawBounds.X + CanvasTextLayout.DrawBounds.Width) + fudge,
            (float) CanvasTextLayout.LayoutBounds.Width + fudge
          ) - terminalRenderer.CellSize.Width,
          0.0f
        );

        float overfillBottom = Math.Max(
          Math.Max(
            (float) (CanvasTextLayout.DrawBounds.Y + CanvasTextLayout.DrawBounds.Height) + fudge,
            (float) CanvasTextLayout.LayoutBounds.Height + fudge
          ) - terminalRenderer.CellSize.Height,
          0.0f
        );

        terminalRenderer.OverfillCache.Add(CellFingerprint, new RectF(overfillTop, overfillLeft, overfillRight, overfillBottom));
      }

      Overfill = overfill;
    }

    [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
    public static bool operator ==(DrawableCell a, DrawableCell b) {
      if (a.Caret != b.Caret) return false;
      if (a.CellFingerprint != b.CellFingerprint) return false;

      return true;
    }

    public static bool operator !=(DrawableCell a, DrawableCell b) => !(a == b);

    public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is DrawableCell other && this == other;

    public override readonly int GetHashCode() => HashCode.Combine(Caret, CellFingerprint);
  }
}
