using System;
using System.Text;

namespace Spakov.AnsiProcessor.Helpers {
  /// <summary>
  /// Methods to facilitate printing strings containing control characters.
  /// </summary>
  public static class PrintableHelper {
    /// <summary>
    /// Converts all control characters in <paramref name="input"/> to a
    /// readable string representation.
    /// </summary>
    /// <param name="input">The string to convert.</param>
    /// <returns>The converted string.</returns>
    public static string MakePrintable(string? input) {
      StringBuilder output = new();

      if (input is null) {
        output.Append("┆(null)┆");
      } else {
        foreach (char character in input) output.Append(MakePrintable(character));
      }

      return output.ToString();
    }

    /// <summary>
    /// Converts <paramref name="input"/> to a readable string representation,
    /// replacing all <see cref="Ansi.C0"/> and <see cref="Ansi.C1"/>
    /// characters.
    /// </summary>
    /// <param name="input">The character to convert.</param>
    /// <returns>The converted string.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static string MakePrintable(char input) {
      return char.IsControl(input)
        ? (byte) input switch {
            0x00 => "␀",
            0x01 => "␁",
            0x02 => "␂",
            0x03 => "␃",
            0x04 => "␄",
            0x05 => "␅",
            0x06 => "␆",
            0x07 => @"\a",
            0x08 => @"\b",
            0x09 => @"\t",
            0x0a => @"\n",
            0x0b => @"\v",
            0x0c => @"\f",
            0x0d => @"\r",
            0x0e => "␎",
            0x0f => "␏",
            0x10 => "␐",
            0x11 => "␑",
            0x12 => "␒",
            0x13 => "␓",
            0x14 => "␔",
            0x15 => "␕",
            0x16 => "␖",
            0x17 => "␗",
            0x18 => "␘",
            0x19 => "␙",
            0x1a => "␚",
            0x1b => "␛",
            0x1c => "␜",
            0x1d => "␝",
            0x1e => "␞",
            0x1f => "␟",
            0x7f => "␡",
            0x80 => "┆PAD┆",
            0x81 => "┆HOP┆",
            0x82 => "┆BPH┆",
            0x83 => "┆NBH┆",
            0x84 => "┆IND┆",
            0x85 => "┆NEL┆",
            0x86 => "┆SSA┆",
            0x87 => "┆ESA┆",
            0x88 => "┆HTS┆",
            0x89 => "┆HTJ┆",
            0x8a => "┆VTS┆",
            0x8b => "┆PLD┆",
            0x8c => "┆PLU┆",
            0x8d => "┆RI┆",
            0x8e => "┆SS2┆",
            0x8f => "┆SS3┆",
            0x90 => "┆DCS┆",
            0x91 => "┆PU1┆",
            0x92 => "┆PU2┆",
            0x93 => "┆STS┆",
            0x94 => "┆CCH┆",
            0x95 => "┆MW┆",
            0x96 => "┆SPA┆",
            0x97 => "┆EPA┆",
            0x98 => "┆SOS┆",
            0x99 => "┆SGC┆",
            0x9a => "┆SCI┆",
            0x9b => "┆CSI┆",
            0x9c => "┆ST┆",
            0x9d => "┆OSC┆",
            0x9e => "┆PM┆",
            0x9f => "┆APC┆",
            _ => throw new InvalidOperationException()
          }
        : input.ToString();
    }
  }
}
