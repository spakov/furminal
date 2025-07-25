using Spakov.AnsiProcessor.AnsiColors;
using Spakov.AnsiProcessor.Helpers;
using Spakov.AnsiProcessor.TermCap;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Spakov.AnsiProcessor.Output.EscapeSequences.Fe.CSI.SGR
{
    /// <summary>
    /// Represents an <see cref="Ansi.EscapeSequences.SGR"/> escape sequence as
    /// a set of useful properties.
    /// </summary>
    /// <remarks>Sources:
    /// <list type="bullet">
    /// <item><see
    /// href="https://en.wikipedia.org/wiki/ANSI_escape_code"/></item>
    /// <item><see
    /// href="https://invisible-island.net/xterm/ctlseqs/ctlseqs.html"/></item>
    /// <item><see href="https://st.suckless.org/patches/undercurl/"/></item>
    /// </list>
    /// </remarks>
    public class SGREscapeSequence : CSIEscapeSequence
    {
        private readonly Palette _palette;
        private GraphicRendition _graphicRendition;

        /// <summary>
        /// The <see cref="Palette"/> intended to be used by this <see
        /// cref="SGREscapeSequence"/>.
        /// </summary>
        public Palette Palette => _palette;

        /// <summary>
        /// The <see cref="GraphicRendition"/> associated with this <see
        /// cref="SGREscapeSequence"/>.
        /// </summary>
        public GraphicRendition GraphicRendition
        {
            get => _graphicRendition;
            private set => _graphicRendition = value;
        }

        /// <summary>
        /// A list of <see cref="SGREscapeSequence"/> properties.
        /// </summary>
        [Flags]
        public enum Properties
        {
            None,
            Bold = 0x0001,
            Faint = 0x0002,
            Italic = 0x0004,
            Underline = 0x0008,
            Blink = 0x0010,
            Inverse = 0x0020,
            CrossedOut = 0x0040,
            DoubleUnderline = 0x0080,
            ForegroundColor = 0x0100,
            DefaultForegroundColor = 0x0200,
            BackgroundColor = 0x0400,
            DefaultBackgroundColor = 0x0800,
            UnderlineColor = 0x1000,
            DefaultUnderlineColor = 0x2000,
            UnderlineStyle = 0x4000
        }

        /// <summary>
        /// The properties that were modified by this <see
        /// cref="SGREscapeSequence"/>.
        /// </summary>
        public Properties ModifiedProperties { get; private set; }

        /// <inheritdoc cref="GraphicRendition.Bold"/>
        public bool Bold
        {
            get => _graphicRendition.Bold;

            set
            {
                _graphicRendition.Bold = value;
                ModifiedProperties |= Properties.Bold;
            }
        }

        /// <inheritdoc cref="GraphicRendition.Faint"/>
        public bool Faint
        {
            get => _graphicRendition.Faint;

            set
            {
                _graphicRendition.Faint = value;
                ModifiedProperties |= Properties.Faint;
            }
        }

        /// <inheritdoc cref="GraphicRendition.Italic"/>
        public bool Italic
        {
            get => _graphicRendition.Italic;

            set
            {
                _graphicRendition.Italic = value;
                ModifiedProperties |= Properties.Italic;
            }
        }

        /// <inheritdoc cref="GraphicRendition.Underline"/>
        public bool Underline
        {
            get => _graphicRendition.Underline;

            set
            {
                _graphicRendition.Underline = value;
                ModifiedProperties |= Properties.Underline;
            }
        }

        /// <inheritdoc cref="GraphicRendition.Blink"/>
        public bool Blink
        {
            get => _graphicRendition.Blink;

            set
            {
                _graphicRendition.Blink = value;
                ModifiedProperties |= Properties.Blink;
            }
        }

        /// <inheritdoc cref="GraphicRendition.Inverse"/>
        public bool Inverse
        {
            get => _graphicRendition.Inverse;

            set
            {
                _graphicRendition.Inverse = value;
                ModifiedProperties |= Properties.Inverse;
            }
        }

        /// <inheritdoc cref="GraphicRendition.CrossedOut"/>
        public bool CrossedOut
        {
            get => _graphicRendition.CrossedOut;

            set
            {
                _graphicRendition.CrossedOut = value;
                ModifiedProperties |= Properties.CrossedOut;
            }
        }

        /// <inheritdoc cref="GraphicRendition.DoubleUnderline"/>
        public bool DoubleUnderline
        {
            get => _graphicRendition.DoubleUnderline;

            set
            {
                _graphicRendition.DoubleUnderline = value;
                ModifiedProperties |= Properties.DoubleUnderline;
            }
        }

        /// <inheritdoc cref="GraphicRendition.ForegroundColor"/>
        public Color ForegroundColor
        {
            get => _graphicRendition.ForegroundColor;

            set
            {
                _graphicRendition.ForegroundColor = value;
                ModifiedProperties |= Properties.ForegroundColor;
            }
        }

        /// <inheritdoc cref="GraphicRendition.BackgroundColor"/>
        public Color BackgroundColor
        {
            get => _graphicRendition.BackgroundColor;

            set
            {
                _graphicRendition.BackgroundColor = value;
                ModifiedProperties |= Properties.BackgroundColor;
            }
        }

        /// <inheritdoc cref="GraphicRendition.UnderlineColor"/>
        public Color UnderlineColor
        {
            get => _graphicRendition.UnderlineColor;

            set
            {
                _graphicRendition.UnderlineColor = value;
                ModifiedProperties |= Properties.UnderlineColor;
            }
        }

        /// <inheritdoc cref="GraphicRendition.UnderlineStyle"/>
        public UnderlineStyle UnderlineStyle
        {
            get => _graphicRendition.UnderlineStyle;

            set
            {
                _graphicRendition.UnderlineStyle = value;
                ModifiedProperties |= Properties.UnderlineStyle;
            }
        }

        /// <summary>
        /// Initializes a <see cref="SGREscapeSequence"/>.
        /// </summary>
        /// <param name="sgrEscapeSequence">An <see
        /// cref="Ansi.EscapeSequences.SGR"/> escape sequence.</param>
        /// <param name="palette">The <see cref="Palette"/> to use for ANSI
        /// colors.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref
        /// name="sgrEscapeSequence"/> is not an <see
        /// cref="Ansi.EscapeSequences.SGR"/> escape sequence.</exception>
        private SGREscapeSequence(string sgrEscapeSequence, Palette palette) : base(sgrEscapeSequence)
        {
            if (
                !sgrEscapeSequence.StartsWith(Ansi.EscapeSequences.Fe.CSI)
                && !sgrEscapeSequence.EndsWith(Ansi.EscapeSequences.SGR.TERMINATOR)
            )
            {
                throw new ArgumentException("Not an SGR escape sequence.", nameof(sgrEscapeSequence));
            }

            _palette = palette;

            GraphicRendition = new();

            // Discard the leading CSI and the trailing control character, then
            // split by ;
            List<string> sequenceParts = [.. sgrEscapeSequence[1..^1].Split(';')];

            // SGR escape sequences can support a ;-separated list of
            // parameters for extended color parameters
            if (sequenceParts.Count is 3 or 5)
            {
                if (sequenceParts[0].Equals(Ansi.EscapeSequences.SGR.FOREGROUND_EXTENDED))
                {
                    ForegroundColor = SGREscapeSequenceToColor(sequenceParts, palette);

                    return;
                }
                else if (sequenceParts[0].Equals(Ansi.EscapeSequences.SGR.BACKGROUND_EXTENDED))
                {
                    BackgroundColor = SGREscapeSequenceToColor(sequenceParts, palette);

                    return;
                }
                else if (sequenceParts[0].Equals(Ansi.EscapeSequences.SGR.UNDERLINE_COLOR))
                {
                    UnderlineColor = SGREscapeSequenceToColor(sequenceParts, palette);

                    return;
                }
            }

            // But normally, SGR escape sequences can be chained together,
            // separated by ;
            foreach (string sequencePart in sequenceParts)
            {
                // Check for :-separated parameters, which are an extension for
                // colors and for underline
                if (sequencePart.Contains(':'))
                {
                    List<string> parameters = [.. sequencePart.Split(':')];

                    switch (parameters[0])
                    {
                        case Ansi.EscapeSequences.SGR.UNDERLINE:
                            if (int.TryParse(parameters[1], out int underlineStyle))
                            {
                                if (underlineStyle is >= ((int)UnderlineStyle.None) and <= ((int)UnderlineStyle.Undercurl))
                                {
                                    Underline = true;
                                    UnderlineStyle = (UnderlineStyle)underlineStyle;
                                }
                            }

                            break;

                        case Ansi.EscapeSequences.SGR.FOREGROUND_EXTENDED:
                            ForegroundColor = SGREscapeSequenceToColor(parameters, palette);

                            break;

                        case Ansi.EscapeSequences.SGR.BACKGROUND_EXTENDED:
                            BackgroundColor = SGREscapeSequenceToColor(parameters, palette);

                            break;

                        case Ansi.EscapeSequences.SGR.UNDERLINE_COLOR:
                            UnderlineColor = SGREscapeSequenceToColor(parameters, palette);

                            break;
                    }
                    // No parameters
                }
                else
                {
                    if (
                        sequencePart.Equals(Ansi.EscapeSequences.SGR.RESET)
                        || sequencePart.Equals(Ansi.EscapeSequences.SGR.EMPTY)
                    )
                    {
                        Bold = false;
                        Faint = false;
                        Italic = false;
                        Underline = false;
                        Blink = false;
                        Inverse = false;
                        CrossedOut = false;
                        DoubleUnderline = false;
                        ForegroundColor = palette.DefaultForegroundColor;
                        BackgroundColor = palette.DefaultBackgroundColor;
                        UnderlineColor = palette.DefaultUnderlineColor;
                        UnderlineStyle = UnderlineStyle.None;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BOLD))
                    {
                        Bold = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FAINT))
                    {
                        Faint = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.ITALIC))
                    {
                        Italic = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.UNDERLINE))
                    {
                        Underline = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BLINK))
                    {
                        Blink = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.INVERSE))
                    {
                        Inverse = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.CROSSED_OUT))
                    {
                        CrossedOut = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.DOUBLE_UNDERLINE))
                    {
                        DoubleUnderline = true;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NORMAL))
                    {
                        Bold = false;
                        Faint = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NO_ITALIC))
                    {
                        Italic = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NO_UNDERLINE))
                    {
                        Underline = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NO_BLINK))
                    {
                        Blink = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NO_INVERSE))
                    {
                        Inverse = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.NO_CROSSED_OUT))
                    {
                        CrossedOut = false;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BLACK))
                    {
                        ForegroundColor = palette.Black;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_RED))
                    {
                        ForegroundColor = palette.Red;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_GREEN))
                    {
                        ForegroundColor = palette.Green;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_YELLOW))
                    {
                        ForegroundColor = palette.Yellow;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BLUE))
                    {
                        ForegroundColor = palette.Blue;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_MAGENTA))
                    {
                        ForegroundColor = palette.Magenta;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_CYAN))
                    {
                        ForegroundColor = palette.Cyan;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_WHITE))
                    {
                        ForegroundColor = palette.White;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_DEFAULT))
                    {
                        ForegroundColor = palette.DefaultForegroundColor;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BLACK))
                    {
                        BackgroundColor = palette.Black;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_RED))
                    {
                        BackgroundColor = palette.Red;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_GREEN))
                    {
                        BackgroundColor = palette.Green;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_YELLOW))
                    {
                        BackgroundColor = palette.Yellow;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BLUE))
                    {
                        BackgroundColor = palette.Blue;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_MAGENTA))
                    {
                        BackgroundColor = palette.Magenta;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_CYAN))
                    {
                        BackgroundColor = palette.Cyan;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_WHITE))
                    {
                        BackgroundColor = palette.White;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_DEFAULT))
                    {
                        BackgroundColor = palette.DefaultBackgroundColor;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.DEFAULT_UNDERLINE_COLOR))
                    {
                        UnderlineColor = palette.DefaultUnderlineColor;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_BLACK))
                    {
                        ForegroundColor = palette.BrightBlack;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_RED))
                    {
                        ForegroundColor = palette.BrightRed;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_GREEN))
                    {
                        ForegroundColor = palette.BrightGreen;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_YELLOW))
                    {
                        ForegroundColor = palette.BrightYellow;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_BLUE))
                    {
                        ForegroundColor = palette.BrightBlue;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_MAGENTA))
                    {
                        ForegroundColor = palette.BrightMagenta;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_CYAN))
                    {
                        ForegroundColor = palette.BrightCyan;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.FOREGROUND_BRIGHT_WHITE))
                    {
                        ForegroundColor = palette.BrightWhite;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_BLACK))
                    {
                        BackgroundColor = palette.BrightBlack;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_RED))
                    {
                        BackgroundColor = palette.BrightRed;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_GREEN))
                    {
                        BackgroundColor = palette.BrightGreen;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_YELLOW))
                    {
                        BackgroundColor = palette.BrightYellow;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_BLUE))
                    {
                        BackgroundColor = palette.BrightBlue;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_MAGENTA))
                    {
                        BackgroundColor = palette.BrightMagenta;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_CYAN))
                    {
                        BackgroundColor = palette.BrightCyan;
                    }
                    else if (sequencePart.Equals(Ansi.EscapeSequences.SGR.BACKGROUND_BRIGHT_WHITE))
                    {
                        BackgroundColor = palette.BrightWhite;
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether an escape sequence is an SGR escape sequence and
        /// is wanted by the <see cref="TerminalCapabilities"/>.
        /// </summary>
        /// <remarks>
        /// <para>Assumes that <paramref name="rawCSIEscapeSequence"/> is a CSI
        /// escape sequence.</para>
        /// <para>Source: <see
        /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Control_Sequence_Introducer_commands"
        /// /></para>
        /// </remarks>
        /// <param name="terminalCapabilities">A <see
        /// cref="TerminalCapabilities"/>.</param>
        /// <param name="rawCSIEscapeSequence">The complete CSI escape
        /// sequence.</param>
        /// <returns><see langword="true"/> if the escape sequence is an SGR
        /// escape sequence or <see langword="false"/> otherwise.</returns>
        internal static bool IsWantedSGREscapeSequence(TerminalCapabilities terminalCapabilities, string rawCSIEscapeSequence)
        {
            if (!rawCSIEscapeSequence.StartsWith(Ansi.EscapeSequences.Fe.CSI))
            {
                return false;
            }

            if (!rawCSIEscapeSequence.EndsWith(Ansi.EscapeSequences.CSI.SGR))
            {
                return false;
            }

            // Discard the leading CSI and the trailing control character, then
            // split by ;
            List<string> sequenceParts = [.. rawCSIEscapeSequence[1..^1].Split(';')];

            if (sequenceParts.Count > 0)
            {
                if (terminalCapabilities.ControlCharacters?.EscapeSequences?.CSI?.SGR is not null)
                {
                    if (terminalCapabilities.ControlCharacters.EscapeSequences.CSI.SGR.SGR.Contains(sequenceParts[0]))
                    {
                        return true;
                    }
                    else
                    {
                        // Some extended CSI sequences are separated by :
                        // instead
                        List<string> colonSequenceParts = [.. rawCSIEscapeSequence[1..^1].Split(':')];

                        if (terminalCapabilities.ControlCharacters.EscapeSequences.CSI.SGR.SGR.Contains(colonSequenceParts[0]))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Initializes an <see cref="SGREscapeSequence"/>.
        /// </summary>
        /// <remarks>Source: <see
        /// href="https://en.wikipedia.org/wiki/ANSI_escape_code#Select_Graphic_Rendition_parameters"
        /// /></remarks>
        /// <param name="sgrEscapeSequence">The complete SGR escape
        /// sequence.</param>
        /// <param name="sgrEscapeSequenceObject">The SGR escape
        /// sequence.</param>
        /// <param name="palette">The <see cref="Palette"/> to use for ANSI
        /// colors.</param>
        /// <returns>An <see cref="SGREscapeSequence"/> if the sequence was
        /// processed successfully, or <see langword="null"/>
        /// otherwise.</returns>
        internal static SGREscapeSequence InitializeSGREscapeSequence(string sgrEscapeSequence, Palette palette) => new(sgrEscapeSequence, palette);

        /// <summary>
        /// Converts <paramref name="parameters"/> to a <see cref="Color"/>.
        /// </summary>
        /// <param name="parameters">SGR sequence parameters.</param>
        /// <param name="palette"><inheritdoc
        /// cref="SGREscapeSequence.SGREscapeSequence"
        /// path="/param[@name='palette']"/></param>
        /// <returns>The <see cref="Color"/> corresponding to <paramref
        /// name="palette"/>, or <see cref="Palette.BrightRed"/> if the
        /// sequence could not be converted into a color.</returns>
        private static Color SGREscapeSequenceToColor(List<string> parameters, Palette palette)
        {
            return parameters.Count == 3
                ? parameters[1].Equals(Ansi.EscapeSequences.SGR.COLOR_8)
                    ? Color8Helper.Color8(parameters, palette)
                    : palette.BrightRed
                : parameters.Count == 5
                ? parameters[1].Equals(Ansi.EscapeSequences.SGR.COLOR_24)
                    ? Color24Helper.Color24(parameters)
                    : palette.BrightRed
                : palette.BrightRed;
        }
    }
}
