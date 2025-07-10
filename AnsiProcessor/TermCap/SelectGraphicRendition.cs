using System.Collections.Generic;

namespace Spakov.AnsiProcessor.TermCap {
  /// <summary>
  /// Allows defining supported SGR escape sequences to handle via callbacks.
  /// </summary>
  public class SelectGraphicRendition {
    /// <summary>
    /// <see cref="Ansi.EscapeSequences.SGR"/> escape sequences configuration.
    /// </summary>
    /// <remarks>Add escape sequences to the list that should cause
    /// <see cref="AnsiReader.OnEscapeSequence"/> to be invoked.</remarks>
    public List<string> SGR { get; } = [
      Ansi.EscapeSequences.SGR.EMPTY,
      Ansi.EscapeSequences.SGR.RESET,
      Ansi.EscapeSequences.SGR.FAINT,
      Ansi.EscapeSequences.SGR.BOLD,
      Ansi.EscapeSequences.SGR.ITALIC,
      Ansi.EscapeSequences.SGR.UNDERLINE,
      Ansi.EscapeSequences.SGR.INVERSE,
      Ansi.EscapeSequences.SGR.CROSSED_OUT,
      Ansi.EscapeSequences.SGR.DOUBLE_UNDERLINE,
      Ansi.EscapeSequences.SGR.NORMAL,
      Ansi.EscapeSequences.SGR.NO_ITALIC,
      Ansi.EscapeSequences.SGR.NO_UNDERLINE,
      Ansi.EscapeSequences.SGR.NO_INVERSE,
      Ansi.EscapeSequences.SGR.NO_CROSSED_OUT,
      Ansi.EscapeSequences.SGR.FOREGROUND_BLACK,
      Ansi.EscapeSequences.SGR.FOREGROUND_RED,
      Ansi.EscapeSequences.SGR.FOREGROUND_GREEN,
      Ansi.EscapeSequences.SGR.FOREGROUND_YELLOW,
      Ansi.EscapeSequences.SGR.FOREGROUND_BLUE,
      Ansi.EscapeSequences.SGR.FOREGROUND_MAGENTA,
      Ansi.EscapeSequences.SGR.FOREGROUND_CYAN,
      Ansi.EscapeSequences.SGR.FOREGROUND_WHITE,
      Ansi.EscapeSequences.SGR.FOREGROUND_EXTENDED,
      Ansi.EscapeSequences.SGR.FOREGROUND_DEFAULT,
      Ansi.EscapeSequences.SGR.BACKGROUND_BLACK,
      Ansi.EscapeSequences.SGR.BACKGROUND_RED,
      Ansi.EscapeSequences.SGR.BACKGROUND_GREEN,
      Ansi.EscapeSequences.SGR.BACKGROUND_YELLOW,
      Ansi.EscapeSequences.SGR.BACKGROUND_BLUE,
      Ansi.EscapeSequences.SGR.BACKGROUND_MAGENTA,
      Ansi.EscapeSequences.SGR.BACKGROUND_CYAN,
      Ansi.EscapeSequences.SGR.BACKGROUND_WHITE,
      Ansi.EscapeSequences.SGR.BACKGROUND_EXTENDED,
      Ansi.EscapeSequences.SGR.BACKGROUND_DEFAULT,
      Ansi.EscapeSequences.SGR.UNDERLINE_COLOR,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_BLACK,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_RED,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_GREEN,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_YELLOW,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_BLUE,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_MAGENTA,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_CYAN,
      Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_WHITE,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_BLACK,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_RED,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_GREEN,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_YELLOW,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_BLUE,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_MAGENTA,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_CYAN,
      Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_WHITE
    ];
  }
}
