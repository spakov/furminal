using System.Collections.Generic;
using System.Text;

namespace Spakov.EmojiGenerator {
  /// <summary>
  /// Extension methods on a <see cref="HashSet{T}"/> of <see
  /// langword="string"/> grapheme clusters.
  /// </summary>
  internal static class GraphemeClusterHashSetHelper {
    /// <summary>
    /// Converts <paramref name="emojiSequences"/> to code.
    /// </summary>
    /// <param name="emojiSequences">A hash set of grapheme clusters.</param>
    /// <param name="indent">Indent with this value after the first
    /// line.</param>
    /// <returns>Generated code.</returns>
    internal static string ToCode(this HashSet<string> emojiSequences, string indent) {
      StringBuilder emojiSequencesCode = new();
      bool firstLine = true;

      foreach (string emojiSequence in emojiSequences) {
        if (!firstLine) {
          emojiSequencesCode.Append(indent);
        }

        emojiSequencesCode.Append('"');
        emojiSequencesCode.Append(emojiSequence);
        emojiSequencesCode.Append("\",\r\n");

        if (firstLine) firstLine = false;
      }

      // Eat the trailing ,\r\n
      emojiSequencesCode.Remove(emojiSequencesCode.Length - 3, 3);

      return emojiSequencesCode.ToString();
    }
  }
}
