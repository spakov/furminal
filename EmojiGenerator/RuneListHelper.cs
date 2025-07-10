using System.Collections.Generic;
using System.Text;

namespace Spakov.EmojiGenerator {
  /// <summary>
  /// Extension methods on an <see cref="IEnumerable{T}"/> of <see
  /// cref="Rune"/>s.
  /// </summary>
  internal static class RuneListHelper {
    /// <summary>
    /// Converts <paramref name="runes"/> to a grapheme cluster.
    /// </summary>
    /// <remarks>Returns an empty string if <paramref name="runes"/> is
    /// empty.</remarks>
    /// <param name="runes">A list of <see cref="Rune"/>s.</param>
    /// <returns>A grapheme cluster.</returns>
    internal static string ToGraphemeCluster(this IEnumerable<Rune> runes) {
      StringBuilder graphemeCluster = new();

      foreach (Rune rune in runes) {
        graphemeCluster.Append(rune);
      }

      return graphemeCluster.ToString();
    }
  }
}
