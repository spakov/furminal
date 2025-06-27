using AnsiProcessor.Ansi.EscapeSequences.Extensions;
using AnsiProcessor.AnsiColors;
using AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR;
using AnsiProcessor.TermCap;
using System.Collections.Generic;
using System.Text;

namespace AnsiProcessor.Output.EscapeSequences.Fe.CSI {
  /// <summary>
  /// An ANSI <see cref="Ansi.EscapeSequences.CSI"/> escape sequence.
  /// </summary>
  /// <remarks>
  /// <para><see cref="CSIEscapeSequence.Type"/> is set and <see
  /// cref="CSIEscapeSequence.Ps"/> is set with sequence parameters for
  /// supported <see cref="CSIEscapeSequence.Type"/>s. <see
  /// cref="CSIEscapeSequence.Variant"/> is also set, if applicable.</para>
  /// <para>There is no guarantee that any of the properties make sense or are
  /// valid—they are merely what was sent to <see cref="AnsiReader"/>.</para>
  /// <para>The set of handled sequences is based largely on those described
  /// in <see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-VT100-Mode"
  /// />, along with some additions identified through testing.</para>
  /// <para>Unsupported CSI sequences, per the reference below, either because
  /// they're highly xterm-specific, highly non-Windows-specific, highly
  /// DEC-specific, or just plain not used these days:</para>
  /// <list type="bullet">
  /// <item><c>CSI ? Ps J</c>, <c>DECSED</c></item>
  /// <item><c>CSI ? Ps K</c>, <c>DECSEL</c></item>
  /// <item><c>CSI # P</c>, <c>CSI Pm # P</c>, <c>XTPUSHCOLORS</c></item>
  /// <item><c>CSI # Q</c>, <c>CSI Pm # Q</c>, <c>XTPOPCOLORS</c></item>
  /// <item><c>CSI # R</c>, <c>XTREPORTCOLORS</c></item>
  /// <item><c>CSI ? Pi ; Pa ; Pv S</c>, <c>XTSMGRAPHICS</c></item>
  /// <item><c>CSI Ps ; Ps ; Ps ; Ps ; Ps T</c>, <c>XTHIMOUSE</c></item>
  /// <item><c>CSI &gt; Pm T</c>, <c>XTRMTITLE</c></item>
  /// <item><c>CSI ? 5 W</c>, <c>DECST8C</c></item>
  /// <item><c>CSI Ps ^</c></item>
  /// <item><c>CSI Ps c</c></item>
  /// <item><c>CSI = Ps c</c></item>
  /// <item><c>CSI &gt; Ps c</c></item>
  /// <item><c>CSI &gt; Pp ; Pv f</c>, <c>CSI > Pp f</c>,
  /// <c>XTFMTKEYS</c></item>
  /// <item><c>CSI ? Pp g</c>, <c>XTQFMTKEYS</c></item>
  /// <item><c>CSI Pm h</c> (not to be confused with <c>CSI ? Pm h</c>, which
  /// is supported)</item>
  /// <item><c>CSI Ps i</c></item>
  /// <item><c>CSI ? Ps i</c></item>
  /// <item><c>CSI Pm l</c></item>
  /// <item><c>CSI &gt; Pp ; Pv m</c>, <c>CSI &gt; Pp m</c>,
  /// <c>XTMODKEYS</c></item>
  /// <item><c>CSI ? Pp m</c>, <c>XTQMODKEYS</c></item>
  /// <item><c>CSI &gt; Ps n</c></item>
  /// <item><c>CSI ? Ps n</c></item>
  /// <item><c>CSI &gt; Ps p</c>, <c>XTSMPOINTER</c></item>
  /// <item><c>CSI Pl ; Pc " p</c>, <c>DECSCL</c></item>
  /// <item><c>CSI Ps $ p</c>, <c>DECRQM</c></item>
  /// <item><c>CSI ? Ps $ p</c>, <c>DECRQM</c></item>
  /// <item><c>CSI # p</c>, <c>CSI Pm # p</c>, <c>XTPUSHSGR</c></item>
  /// <item><c>CSI &gt; Ps q</c>, <c>XTVERSION</c></item>
  /// <item><c>CSI Ps " q</c>, <c>DECSCA</c></item>
  /// <item><c>CSI # q</c>, <c>XTPOPSGR</c></item>
  /// <item><c>CSI ? Pm r</c>, <c>XTRESTORE</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr ; Pm $ r</c>, <c>DECCARA</c></item>
  /// <item><c>CSI Pl ; Pr s</c>, <c>DECSLRM</c></item>
  /// <item><c>CSI &gt; Ps s</c>, <c>XTSHIFTESCAPE</c></item>
  /// <item><c>CSI ? Pm s</c>, <c>XTSAVE</c></item>
  /// <item><c>CSI &gt; Pm t</c>, <c>XTSMTITLE</c></item>
  /// <item><c>CSI Ps SP t</c>, <c>DECSWBV</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr ; Pm $ t</c>, <c>DECRARA</c></item>
  /// <item><c>CSI &amp; u</c>, <c>DECRQUPSS</c></item>
  /// <item><c>CSI Ps SP u</c>, <c>DECSMBV</c></item>
  /// <item><c>CSI " v</c>, <c>DECRQDE</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr ; Pp ; Pt ; Pl ; Pp $ v</c>,
  /// <c>DECCRA</c></item>
  /// <item><c>CSI Ps $ w</c>, <c>DECRQPSR</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr ' w</c>, <c>DECEFR</c></item>
  /// <item><c>CSI Ps x</c>, <c>DECREQTPARM</c></item>
  /// <item><c>CSI Ps * x</c>, <c>DECSACE</c></item>
  /// <item><c>CSI Pc ; Pt ; Pl ; Pb ; Pr $ x</c>, <c>DECFRA</c></item>
  /// <item><c>CSI Ps # y</c>, <c>XTCHECKSUM</c></item>
  /// <item><c>CSI Pi ; Pg ; Pt ; Pl ; Pb ; Pr * y</c>, <c>DECRQCRA</c></item>
  /// <item><c>CSI Ps ; Pu ' z</c>, <c>DECELR</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr $ z</c>, <c>DECERA</c></item>
  /// <item><c>CSI Pm ' {</c>, <c>DECSLE</c></item>
  /// <item><c>CSI # {</c>, <c>CSI Pm # {</c>, <c>XTPUSHSGR</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr $ {</c>, <c>DECSERA</c></item>
  /// <item><c>CSI Pt ; Pl ; Pb ; Pr # |</c>, <c>XTREPORTSGR</c></item>
  /// <item><c>CSI Ps $ |</c>, <c>DECSCPP</c></item>
  /// <item><c>CSI Ps ' |</c>, <c>DECRQLP</c></item>
  /// <item><c>CSI Ps * |</c>, <c>DECSNLS</c></item>
  /// <item><c>CSI # }</c>, <c>XTPOPSGR</c></item>
  /// <item><c>CSI Ps ; Pf ; Pb , |</c>, <c>DECAC</c></item>
  /// <item><c>CSI Ps ; Pf ; Pb , }</c>, <c>DECATC</c></item>
  /// <item><c>CSI Ps ' }</c>, <c>DECIC</c></item>
  /// <item><c>CSI Ps $ }</c>, <c>DECSASD</c></item>
  /// <item><c>CSI Ps ' ~</c>, <c>DECDC</c></item>
  /// <item><c>CSI Ps $ ~</c>, <c>DECSSDT</c></item>
  /// </list>
  /// <para>Source: <see
  /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Functions-using-CSI-_-ordered-by-the-final-character_s_"
  /// /></para>
  /// </remarks>
  public class CSIEscapeSequence : FeEscapeSequence {
    /// <summary>
    /// <c>Ps</c>, as in <see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h3-Operating-System-Commands"
    /// />.
    /// </summary>
    /// <remarks><see langword="null"/> if the OSC sequence is
    /// invalid.</remarks>
    public List<int>? Ps { get; private set; }

    /// <summary>
    /// The CSI sequence type (i.e., the last character of the sequence).
    /// </summary>
    /// <remarks><see langword="null"/> if the CSI sequence is
    /// invalid.</remarks>
    public char? Type { get; private set; }

    /// <summary>
    /// The CSI sequence variant, if applicable.
    /// </summary>
    /// <remarks>
    /// <para><see langword="null"/> if the variant is not applicable.</para>
    /// <para>This can be used to distinguish between, e.g., <c>CSI Ps @</c>
    /// and <c>CSI Ps SP @</c>, where <see cref="Variant"/> would be a space
    /// character in the latter case, but <see langword="null"/> in the former
    /// case.</para>
    /// </remarks>
    public char? Variant { get; private set; }

    /// <summary>
    /// Initializes a <see cref="CSIEscapeSequence"/>.
    /// </summary>
    /// <param name="ps"><inheritdoc cref="Ps" path="/summary"/></param>
    /// <param name="type"><inheritdoc cref="Type" path="/summary"/></param>
    /// <param name="variant"><inheritdoc cref="Variant"
    /// path="/summary"/></param>
    /// <param name="rawCSIEscapeSequence">The raw CSI escape sequence.</param>
    protected CSIEscapeSequence(string rawCSIEscapeSequence, List<int>? ps = null, char? type = null, char? variant = null) : base(rawCSIEscapeSequence) {
      Ps = ps;
      Type = type;
      Variant = variant;
    }

    /// <summary>
    /// Initializes a <see cref="CSIEscapeSequence"/>, or more likely, one of
    /// its subclasses.
    /// </summary>
    /// <param name="terminalCapabilities">A <see
    /// cref="TerminalCapabilities"/>.</param>
    /// <param name="palette">A <see cref="Palette"/>.</param>
    /// <param name="rawCSIEscapeSequence">The raw CSI escape sequence from
    /// which to initialize an object.</param>
    /// <returns>A <see cref="CSIEscapeSequence"/>.</returns>
    internal static CSIEscapeSequence InitializeCSIEscapeSequence(TerminalCapabilities terminalCapabilities, Palette palette, string rawCSIEscapeSequence) {
      if (SGREscapeSequence.IsWantedSGREscapeSequence(terminalCapabilities, rawCSIEscapeSequence)) {
        return SGREscapeSequence.InitializeSGREscapeSequence(rawCSIEscapeSequence, palette);
      } else if (rawCSIEscapeSequence.Length < 2) {
        // Invalid CSI escape sequence
        return new(rawCSIEscapeSequence);
      } else {
        string[] stringPs = rawCSIEscapeSequence[1..^1].Split(';');
        int? defaultFirst = null;
        char type = rawCSIEscapeSequence.Substring(rawCSIEscapeSequence.Length - 1, 1)[0];
        char? variant = null;

        if ( // Sequences that are of the form CSI Ps SP type
          type is Ansi.EscapeSequences.CSI.ICH // and also SL
          or Ansi.EscapeSequences.CSI.CUU // and also SR
          or Ansi.EscapeSequences.CSI.DECLL // and also DECSCUSR
        ) {
          if (stringPs[^1][^1] == ' ') {
            variant = ' ';
          } else {
            stringPs[^1] = stringPs[^1][..^1];
          }

          if (type is Ansi.EscapeSequences.CSI.DECLL && variant is null) {
            // DECLL has a default of 0 (but not DECSCUSR)
            defaultFirst = 0;
          } else {
            defaultFirst = 1;
          }
        } else if ( // Sequences that are of the form CSI # type
          type is Ansi.EscapeSequences.CSI.SU // and also XTTITLEPOS
        ) {
          if (stringPs[^1][^1] == '#') {
            variant = '#';
          } else {
            stringPs[^1] = stringPs[^1][..^1];
          }

          if (variant is null) {
            // SU has a default of 1 (but not XTTITLEPOS)
            defaultFirst = 1;
          } else {
            defaultFirst = null;
          }
        } else if ( // Sequences that are of the form CSI Ps type
          type is Ansi.EscapeSequences.CSI.CUD
          or Ansi.EscapeSequences.CSI.CUF
          or Ansi.EscapeSequences.CSI.CUB
          or Ansi.EscapeSequences.CSI.CNL
          or Ansi.EscapeSequences.CSI.CPL
          or Ansi.EscapeSequences.CSI.CHA
          or Ansi.EscapeSequences.CSI.CHT
          or Ansi.EscapeSequences.CSI.ED
          or Ansi.EscapeSequences.CSI.EL
          or Ansi.EscapeSequences.CSI.IL
          or Ansi.EscapeSequences.CSI.DL
          or Ansi.EscapeSequences.CSI.DCH
          or Ansi.EscapeSequences.CSI.SD
          or Ansi.EscapeSequences.CSI.ECH
          or Ansi.EscapeSequences.CSI.CBT
          or Ansi.EscapeSequences.CSI.HPA
          or Ansi.EscapeSequences.CSI.HPR
          or Ansi.EscapeSequences.CSI.REP
          or Ansi.EscapeSequences.CSI.VPA
          or Ansi.EscapeSequences.CSI.VPR
          or Ansi.EscapeSequences.CSI.TBC
          or Ansi.EscapeSequences.CSI.DSR
          or Ansi.EscapeSequences.CSI.XTMODKEYS
          or Ansi.EscapeSequences.CSI.XTWINOPS
        ) {
          if ( // Sequences that have a default Ps of 0
            type is Ansi.EscapeSequences.CSI.ED
            or Ansi.EscapeSequences.CSI.EL
            or Ansi.EscapeSequences.CSI.TBC
          ) {
            defaultFirst = 0;
          } else if ( // Sequences that require a parameter
            type is Ansi.EscapeSequences.CSI.DSR
            or Ansi.EscapeSequences.CSI.XTMODKEYS
            or Ansi.EscapeSequences.CSI.XTWINOPS
          ) {
            if (type == Ansi.EscapeSequences.CSI.XTMODKEYS) {
              variant = rawCSIEscapeSequence[1];
            }

            defaultFirst = null;
          } else { // Sequences that have a default Ps of 1 (most of them)
            defaultFirst = 1;
          }
        } else if ( // Sequences that are of the form CSI Ps [ ; Ps ] type
          type is Ansi.EscapeSequences.CSI.CUP
          or Ansi.EscapeSequences.CSI.HVP
          or Ansi.EscapeSequences.CSI.DECSTBM
        ) {
          List<int> ps = ParsePs(stringPs, 1);

          if (ps.Count == 1) {
            ps.Add(1);
          }

          return new(rawCSIEscapeSequence, ps, type, variant);
        } else if ( // Sequences that require specific parameters
          type is Ansi.EscapeSequences.CSI.DECSTR
          or Ansi.EscapeSequences.CSI.SAVE_CURSOR
          or Ansi.EscapeSequences.CSI.RESTORE_CURSOR
        ) {
          if (type is Ansi.EscapeSequences.CSI.DECSTR) {
            if (rawCSIEscapeSequence[1] == '!') {
              return new(rawCSIEscapeSequence, null, type, '!');
            }
          } else if (
            type is Ansi.EscapeSequences.CSI.SAVE_CURSOR
            or Ansi.EscapeSequences.CSI.RESTORE_CURSOR
          ) {
            return new(rawCSIEscapeSequence, [], type);
          }
        } else if ( // DECSET sequences
          type is Ansi.EscapeSequences.CSI.DECSET_HIGH
          or Ansi.EscapeSequences.CSI.DECSET_LOW
        ) {
          if (rawCSIEscapeSequence.Length > 2 && rawCSIEscapeSequence[1] == CSI_DECSET.DECSET_LEADER) {
            if (int.TryParse(rawCSIEscapeSequence[2..^1], out int decsetSequence)) {
              return new(rawCSIEscapeSequence, [decsetSequence], type, rawCSIEscapeSequence[1]);
            }

            defaultFirst = null;
          }
        } else {
          // This is an unspported sequence
          return new(rawCSIEscapeSequence);
        }

        return new(rawCSIEscapeSequence, ParsePs(stringPs, defaultFirst), type, variant);
      }
    }

    /// <summary>
    /// Determines whether <paramref name="rawFeEscapeSequence"/> is a CSI
    /// escape sequence.
    /// </summary>
    /// <remarks>Assumes that <paramref name="rawFeEscapeSequence"/> is an Fe
    /// escape sequence.</remarks>
    /// <param name="rawFeEscapeSequence"></param>
    /// <returns><see langword="true"/> if <paramref
    /// name="rawFeEscapeSequence"/> is a CSI escape sequence or <see
    /// langword="false"/> otherwise.</returns>
    internal static bool IsCSIEscapeSequence(string rawFeEscapeSequence) => rawFeEscapeSequence[0] == Ansi.EscapeSequences.Fe.CSI;

    /// <summary>
    /// Determines whether a CSI escape sequence is complete to facilitate
    /// building the sequence.
    /// </summary>
    /// <remarks>
    /// Source:
    /// <list type="bullet">
    /// <item><see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Control_Sequence_Introducer_commands"
    /// /></item>
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// </list>
    /// </remarks>
    /// <param name="escapeSequenceBuilder">The <see cref="StringBuilder"/>
    /// that contains the escape sequence's characters received so far.</param>
    /// <param name="character">The most recently received character.</param>
    /// <returns><see langword="true"/> if the escape sequence is complete or
    /// <see langword="false"/> otherwise.</returns>
    internal static bool IsCSIEscapeSequenceComplete(StringBuilder escapeSequenceBuilder, char character) {
      // Check for terminators
      if (character is >= '@' and <= '~') return true;

      char controlCharacter = escapeSequenceBuilder.ToString()[0];

      return false;
    }

    /// <summary>
    /// Converts <paramref name="stringPs"/> to a <see cref="List{T}"/> of <see
    /// langword="int"/>s.
    /// </summary>
    /// <remarks>Replaces values that could not be parsed with an <see
    /// langword="int"/> <c>-1</c>.</remarks>
    /// <param name="stringPs">An array of strings.</param>
    /// <param name="defaultFirst">If not <see langword="null"/>, ensure the
    /// returned <see cref="List{T}"/> will contain a single <paramref
    /// name="defaultFirst"/> instead of being an empty list.</param>
    /// <returns>A <see cref="List{T}"/> of <see langword="int"/>s.</returns>
    private static List<int> ParsePs(string[] stringPs, int? defaultFirst = null) {
      List<int> ps = [];

      foreach (string _stringPs in stringPs) {
        if (uint.TryParse(_stringPs, out uint _ps)) {
          ps.Add((int) _ps);
        } else {
          // Use -1 to indicate that parsing failed
          ps.Add(-1);
        }
      }

      if (defaultFirst is not null && ps.Count == 1 && ps[0] < 0) {
        ps[0] = (int) defaultFirst;
      } else if (defaultFirst is not null && ps.Count == 0) {
        ps.Add((int) defaultFirst);
      }

      return ps;
    }
  }
}
