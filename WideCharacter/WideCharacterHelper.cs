using System.Runtime.InteropServices;
using System.Text;

namespace Spakov.WideCharacter
{
    /// <summary>
    /// Contains extension methods on <see langword="string"/>? to calculate
    /// the wide-character width of grapheme clusters.
    /// </summary>
    public static partial class WideCharacterHelper
    {
        /// <summary>
        /// return a character width analogous to <c>wcwidth</c> (except
        /// portable and hopefully less buggy than most system <c>wcwidth</c>
        /// functions).
        /// </summary>
        /// <param name="c">A character.</param>
        /// <returns></returns>
        [LibraryImport("utf8proc.dll")]
        [UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        private static partial int utf8proc_charwidth(int c);

        /// <summary>
        /// Returns the wide-character width of <paramref
        /// name="graphemeCluster"/>.
        /// </summary>
        /// <param name="graphemeCluster">A grapheme cluster.</param>
        /// <returns>The wide-character width of <paramref
        /// name="graphemeCluster"/>. Returns 1 if <paramref
        /// name="graphemeCluster"/> is <see langword="null"/>.</returns>
        public static int WideCharacterWidth(this string? graphemeCluster)
        {
            if (graphemeCluster == null)
            {
                return 1;
            }

            if (graphemeCluster.IsEmoji())
            {
                return 2;
            }

            int runeCount = 0;
            int maxWidth = 0;

            foreach (Rune rune in graphemeCluster.EnumerateRunes())
            {
                runeCount++;

                int width = utf8proc_charwidth(rune.Value);

                if (width > maxWidth)
                {
                    maxWidth = width;
                }
            }

            return maxWidth;
        }

        /// <summary>
        /// Returns a value indicating whether <paramref name="graphemeCluster"/>
        /// represents an emoji.
        /// </summary>
        /// <param name="graphemeCluster">A grapheme cluster.</param>
        /// <returns><see langword="true"/> if <paramref name="graphemeCluster"/>
        /// represents an emoji or <see langword="false"/> otherwise.</returns>
        public static bool IsEmoji(this string? graphemeCluster) => graphemeCluster != null && (Emoji.s_emojiSequences.Contains(graphemeCluster) || Emoji.s_emojiZwjSequences.Contains(graphemeCluster));
    }
}
