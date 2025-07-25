using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Spakov.EmojiGenerator
{
    /// <summary>
    /// A parser for <c>emoji-sequences.txt</c>.
    /// </summary>
    internal class EmojiSequencesParser
    {
        /// <summary>
        /// Base 16.
        /// </summary>
        private const int Base16 = 0x10;

        private readonly TextReader _emojiSequencesStream;

        /// <summary>
        /// Initializes a <see cref="EmojiSequencesParser"/>.
        /// </summary>
        /// <param name="emojiSequencesStream">A <see cref="TextReader"/> for
        /// <c>emoji-sequences.txt</c>.</param>
        internal EmojiSequencesParser(TextReader emojiSequencesStream)
        {
            _emojiSequencesStream = emojiSequencesStream;
        }

        /// <summary>
        /// Returns a <see cref="HashSet{T}"/> of <see langword="string"/>
        /// grapheme clusters containing the contents of <see
        /// cref="_emojiSequencesStream"/>.
        /// </summary>
        /// <returns>Emoji sequences.</returns>
        internal HashSet<string> Parse()
        {
            HashSet<string> emojiSequences = [];

            string? line;

            while ((line = _emojiSequencesStream.ReadLine()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                {
                    continue;
                }

                string[] fields = line.Split(';');
                string codepointField = fields[0].Trim();

                if (codepointField.Contains(".."))
                {
                    string[] codepointStrings = codepointField.Split("..");
                    List<Rune> emojiSequence = [];

                    uint firstCodepoint = Convert.ToUInt32(codepointStrings[0], Base16);
                    uint lastCodepoint = Convert.ToUInt32(codepointStrings[1], Base16);

                    for (uint i = firstCodepoint; i <= lastCodepoint; i++)
                    {
                        emojiSequence.Add(new(i));
                    }
                }
                else
                {
                    string[] codepointStrings = codepointField.Split(' ');
                    List<Rune> emojiSequence = [];

                    foreach (string codepointString in codepointStrings)
                    {
                        emojiSequence.Add(new(Convert.ToUInt32(codepointString, Base16)));
                    }

                    emojiSequences.Add(emojiSequence.ToGraphemeCluster());
                }
            }

            return emojiSequences;
        }
    }
}
