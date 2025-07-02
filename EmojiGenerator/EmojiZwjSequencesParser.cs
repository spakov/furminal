using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EmojiGenerator {
  /// <summary>
  /// A parser for <c>emoji-zwj-sequences.txt</c>.
  /// </summary>
  internal class EmojiZwjSequencesParser {
    private const int base16 = 0x10;

    private readonly TextReader emojiZwjSequencesStream;

    /// <summary>
    /// Initializes a <see cref="EmojiZwjSequencesParser"/>.
    /// </summary>
    /// <param name="emojiZwjSequencesStream">A <see cref="TextReader"/> for
    /// <c>emoji-zwj-sequences.txt</c>.</param>
    internal EmojiZwjSequencesParser(TextReader emojiZwjSequencesStream) {
      this.emojiZwjSequencesStream = emojiZwjSequencesStream;
    }

    /// <summary>
    /// Returns a <see cref="HashSet{T}"/> of <see langword="string"/>
    /// grapheme clusters containing the contents of <see
    /// cref="emojiZwjSequencesStream"/>.
    /// </summary>
    /// <returns>Emoji sequences.</returns>
    internal HashSet<string> Parse() {
      HashSet<string> emojiSequences = [];

      string? line;

      while ((line = emojiZwjSequencesStream.ReadLine()) is not null) {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;

        string[] fields = line.Split(';');
        string codepointField = fields[0].Trim();

        string[] codepointStrings = codepointField.Split(' ');
        List<Rune> emojiSequence = [];

        foreach (string codepointString in codepointStrings) {
          emojiSequence.Add(new(Convert.ToUInt32(codepointString, base16)));
        }

        emojiSequences.Add(emojiSequence.ToGraphemeCluster());
      }

      return emojiSequences;
    }
  }
}
