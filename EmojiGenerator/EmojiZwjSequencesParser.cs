using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Spakov.EmojiGenerator
{
    /// <summary>
    /// A parser for <c>emoji-zwj-sequences.txt</c>.
    /// </summary>
    internal class EmojiZwjSequencesParser
    {
        /// <summary>
        /// Base 16.
        /// </summary>
        private const int Base16 = 0x10;

        private readonly TextReader _emojiZwjSequencesStream;

        /// <summary>
        /// Initializes a <see cref="EmojiZwjSequencesParser"/>.
        /// </summary>
        /// <param name="emojiZwjSequencesStream">A <see cref="TextReader"/>
        /// for <c>emoji-zwj-sequences.txt</c>.</param>
        internal EmojiZwjSequencesParser(TextReader emojiZwjSequencesStream)
        {
            _emojiZwjSequencesStream = emojiZwjSequencesStream;
        }

        /// <summary>
        /// Returns a <see cref="HashSet{T}"/> of <see langword="string"/>
        /// grapheme clusters containing the contents of <see
        /// cref="_emojiZwjSequencesStream"/>.
        /// </summary>
        /// <returns>Emoji sequences.</returns>
        internal HashSet<string> Parse()
        {
            HashSet<string> emojiSequences = [];

            string? line;

            while ((line = _emojiZwjSequencesStream.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                string[] fields = line.Split(';');
                string codepointField = fields[0].Trim();

                string[] codepointStrings = codepointField.Split(' ');
                List<Rune> emojiSequence = [];

                foreach (string codepointString in codepointStrings)
                {
                    emojiSequence.Add(new(Convert.ToUInt32(codepointString, Base16)));
                }

                emojiSequences.Add(emojiSequence.ToGraphemeCluster());
            }

            return emojiSequences;
        }
    }
}
