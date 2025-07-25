using System.Collections.Generic;
using System.Text;

namespace Spakov.AnsiProcessor.Output.EscapeSequences.Fe.OSC
{
    /// <summary>
    /// A representation of ANSI <see cref="Ansi.EscapeSequences.Fe.OSC"/>
    /// escape sequence.
    /// </summary>
    public class OSCEscapeSequence : FeEscapeSequence
    {
        /// <summary>
        /// <c>Ps</c>, as in <see
        /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Operating-System-Commands"
        /// />.
        /// </summary>
        /// <remarks><c>-1</c> if the OSC sequence is invalid.</remarks>
        public int Ps { get; private set; }

        /// <summary>
        /// <c>Pt</c>, as in <see
        /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Operating-System-Commands"
        /// />.
        /// </summary>
        /// <remarks><see langword="null"/> if the OSC sequence is
        /// invalid.</remarks>
        public List<string>? Pt { get; private set; }

        /// <summary>
        /// Initializes an <see cref="OSCEscapeSequence"/>.
        /// </summary>
        /// <param name="ps"><inheritdoc cref="Ps" path="/summary"/></param>
        /// <param name="pt"><inheritdoc cref="Pt" path="/summary"/></param>
        /// <param name="rawOSCEscapeSequence">The raw OSC escape
        /// sequence.</param>
        protected OSCEscapeSequence(string rawOSCEscapeSequence, int ps = -1, List<string>? pt = null) : base(rawOSCEscapeSequence)
        {
            Ps = ps;
            Pt = pt;
        }

        /// <summary>
        /// Initializes an <see cref="OSCEscapeSequence"/>.
        /// </summary>
        /// <param name="rawOSCEscapeSequence">The raw OSC escape sequence from
        /// which to initialize an object.</param>
        /// <returns>An <see cref="OSCEscapeSequence"/>.</returns>
        internal static OSCEscapeSequence InitializeOSCEscapeSequence(string rawOSCEscapeSequence)
        {
            if (rawOSCEscapeSequence.Length < 2)
            {
                // Invalid OSC escape sequence
                return new(rawOSCEscapeSequence);
            }

            string[] oscSequenceParameters = rawOSCEscapeSequence[1..].Split(';');
            if (!uint.TryParse(oscSequenceParameters[0], out uint ps))
            {
                // Invalid OSC escape sequence
                return new(rawOSCEscapeSequence);
            }

            // For 0 <= ps <= 2, Pt can conceivably contain ; characters
            if (ps < 3)
            {
                StringBuilder pt = new();

                for (int i = 1; i < oscSequenceParameters.Length; i++)
                {
                    pt.Append(oscSequenceParameters[i]);
                }

                return new(rawOSCEscapeSequence, (int)ps, [pt.ToString()]);
            }
            else
            {
                List<string> pt = [];

                for (int i = 1; i < oscSequenceParameters.Length; i++)
                {
                    pt.Add(oscSequenceParameters[i]);
                }

                return new(rawOSCEscapeSequence, (int)ps, pt);
            }
        }

        /// <summary>
        /// Determines whether <paramref name="rawFeEscapeSequence"/> is an OSC
        /// escape sequence.
        /// </summary>
        /// <remarks>Assumes that <paramref name="rawFeEscapeSequence"/> is an
        /// Fe escape sequence.</remarks>
        /// <param name="rawFeEscapeSequence"></param>
        /// <returns><see langword="true"/> if <paramref
        /// name="rawFeEscapeSequence"/> is an OSC escape sequence or <see
        /// langword="false"/> otherwise.</returns>
        internal static bool IsOSCEscapeSequence(string rawFeEscapeSequence) => rawFeEscapeSequence[0] == Ansi.EscapeSequences.Fe.OSC;
    }
}
