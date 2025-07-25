using Spakov.AnsiProcessor.Input;
using Spakov.AnsiProcessor.TermCap;
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace Spakov.AnsiProcessor.Helpers
{
    /// <summary>
    /// Methods for converting keystrokes to ANSI.
    /// </summary>
    public static class KeystrokeHelper
    {
        /// <summary>
        /// Types of keystrokes.
        /// </summary>
        private enum KeystrokeType
        {
            /// <summary>
            /// An easily-representable plain-text keystroke.
            /// </summary>
            Text,

            /// <summary>
            /// A control character.
            /// </summary>
            ControlCharacter,

            /// <summary>
            /// A complex keystroke that isn't trivial to represent.
            /// </summary>
            ComplexKeystroke
        };

        /// <summary>
        /// Converts <paramref name="keystroke"/> to its corresponding ANSI
        /// string.
        /// </summary>
        /// <param name="keystroke">A keystroke.</param>
        /// <param name="terminalCapabilities">A <see
        /// cref="TerminalCapabilities"/> configuration.</param>
        /// <returns>The ANSI string representation of <paramref
        /// name="keystroke"/>, or <see langword="null"/> if one could not be
        /// determined.</returns>
        internal static string? KeystrokeToAnsi(Keystroke keystroke, TerminalCapabilities terminalCapabilities)
        {
            // Ignore repeated key presses if DECARM is not in effect
            if (!keystroke.AutoRepeatKeys && keystroke.IsRepeat)
            {
                return null;
            }

            KeystrokeType? keyType = keystroke.Key switch
            {
                Key.Up => KeystrokeType.ComplexKeystroke,
                Key.Down => KeystrokeType.ComplexKeystroke,
                Key.Right => KeystrokeType.ComplexKeystroke,
                Key.Left => KeystrokeType.ComplexKeystroke,
                Key.Home => KeystrokeType.ComplexKeystroke,
                Key.Insert => KeystrokeType.ComplexKeystroke,
                Key.Delete => KeystrokeType.ComplexKeystroke,
                Key.End => KeystrokeType.ComplexKeystroke,
                Key.PageUp => KeystrokeType.ComplexKeystroke,
                Key.PageDown => KeystrokeType.ComplexKeystroke,
                Key.F1 => KeystrokeType.ComplexKeystroke,
                Key.F2 => KeystrokeType.ComplexKeystroke,
                Key.F3 => KeystrokeType.ComplexKeystroke,
                Key.F4 => KeystrokeType.ComplexKeystroke,
                Key.F5 => KeystrokeType.ComplexKeystroke,
                Key.F6 => KeystrokeType.ComplexKeystroke,
                Key.F7 => KeystrokeType.ComplexKeystroke,
                Key.F8 => KeystrokeType.ComplexKeystroke,
                Key.F9 => KeystrokeType.ComplexKeystroke,
                Key.F10 => KeystrokeType.ComplexKeystroke,
                Key.F11 => KeystrokeType.ComplexKeystroke,
                Key.F12 => KeystrokeType.ComplexKeystroke,
                Key.Tab => KeystrokeType.ComplexKeystroke,
                _ => null
            };

            keyType ??= (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
              || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
              || (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
              || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
              || (keystroke.ModifierKeys & ModifierKeys.LeftMeta) != 0
              || (keystroke.ModifierKeys & ModifierKeys.RightMeta) != 0
              ? KeystrokeType.ControlCharacter
              : KeystrokeType.Text;

            if (keyType == KeystrokeType.Text && (int)keystroke.Key < 0x20)
            {
                keyType = KeystrokeType.ControlCharacter;
            }

            return keyType switch
            {
                KeystrokeType.Text => keystroke.ToStringRepresentation(),
                KeystrokeType.ControlCharacter => ControlCharacterKeystrokeToAnsi(keystroke, terminalCapabilities),
                KeystrokeType.ComplexKeystroke => ComplexKeystrokeToAnsiEscapeSequence(keystroke, terminalCapabilities),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Converts <paramref name="keystroke"/> to an ANSI escape sequence.
        /// </summary>
        /// <remarks>Assumes Alt, Control, and/or Meta are high in <paramref
        /// name="keystroke"/>'s <see cref="ModifierKeys"/>.</remarks>
        /// <param name="keystroke"><inheritdoc cref="KeystrokeToAnsi"
        /// path="/param[@name='keystroke']"/></param>
        /// <param name="terminalCapabilities"><inheritdoc cref="KeystrokeToAnsi"
        /// path="/param[@name='terminalCapabilities']"/></param>
        /// <returns>The generated ANSI escape sequence, or <see
        /// langword="null"/> if one could not be determined.</returns>
        private static string? ControlCharacterKeystrokeToAnsi(Keystroke keystroke, TerminalCapabilities terminalCapabilities)
        {
            StringBuilder escapeSequenceBuilder = new();

            byte keystrokeToSend = (byte)keystroke.Key;

            // Control subtracts 0x40 to convert to C0
            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
            )
            {
                if (keystroke.Key is >= Key.A and <= Key.GraveAccent)
                {
                    keystrokeToSend -= 0x40;
                }
            }

            // Alt says we use an ESC encoding
            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
            )
            {
                escapeSequenceBuilder = new();
                escapeSequenceBuilder.Append(Ansi.C0.ESC);
                escapeSequenceBuilder.Append((char)keystrokeToSend);

                return escapeSequenceBuilder.ToString();
            }

            // Special case: backspace
            if (keystrokeToSend == Ansi.C0.BS && terminalCapabilities.Input.BackspaceIsDel)
            {
                keystrokeToSend = 0x7f; // DEL
            }

            escapeSequenceBuilder.Append((char)keystrokeToSend);
            return escapeSequenceBuilder.ToString();
        }

        /// <summary>
        /// Converts <paramref name="keystroke"/> to an ANSI escape sequence.
        /// </summary>
        /// <remarks>Used in the case of non-printable keys.</remarks>
        /// <param name="keystroke"><inheritdoc cref="KeystrokeToAnsi"
        /// path="/param[@name='keystroke']"/></param>
        /// <param name="terminalCapabilities"><inheritdoc cref="KeystrokeToAnsi"
        /// path="/param[@name='terminalCapabilities']"/></param>
        /// <returns>The generated ANSI escape sequence.</returns>
        /// <exception cref="ArgumentException"><paramref name="keystroke"/> is
        /// not representable as an escape sequence.</exception>
        private static string ComplexKeystrokeToAnsiEscapeSequence(Keystroke keystroke, TerminalCapabilities terminalCapabilities)
        {
            StringBuilder escapeSequenceBuilder = new();

            escapeSequenceBuilder.Append(Ansi.C0.ESC);

            char escapeSequenceType = keystroke.Key switch
            {
                Key.Up => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,
                Key.Down => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,
                Key.Right => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,
                Key.Left => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,

                Key.Home => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,
                Key.Insert => Ansi.EscapeSequences.Fe.CSI,
                Key.Delete => Ansi.EscapeSequences.Fe.CSI,
                Key.End => keystroke.ApplicationCursorKeys
                    ? Ansi.EscapeSequences.Fe.SS3
                    : Ansi.EscapeSequences.Fe.CSI,
                Key.PageUp => Ansi.EscapeSequences.Fe.CSI,
                Key.PageDown => Ansi.EscapeSequences.Fe.CSI,

                // See https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#:~:text=the%20SS3%20%20sent%20before%20F1%20through%20F4%20is%20altered%20to%20CSI
                Key.F1 => keystroke.ModifierKeys != 0
                    ? Ansi.EscapeSequences.Fe.CSI
                    : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
                Key.F2 => keystroke.ModifierKeys != 0
                    ? Ansi.EscapeSequences.Fe.CSI
                    : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
                Key.F3 => keystroke.ModifierKeys != 0
                    ? Ansi.EscapeSequences.Fe.CSI
                    : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
                Key.F4 => keystroke.ModifierKeys != 0
                    ? Ansi.EscapeSequences.Fe.CSI
                    : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,

                Key.F5 => Ansi.EscapeSequences.Fe.CSI,
                Key.F6 => Ansi.EscapeSequences.Fe.CSI,
                Key.F7 => Ansi.EscapeSequences.Fe.CSI,
                Key.F8 => Ansi.EscapeSequences.Fe.CSI,
                Key.F9 => Ansi.EscapeSequences.Fe.CSI,
                Key.F10 => Ansi.EscapeSequences.Fe.CSI,
                Key.F11 => Ansi.EscapeSequences.Fe.CSI,
                Key.F12 => Ansi.EscapeSequences.Fe.CSI,

                Key.Tab => Ansi.C0.HT,

                _ => throw new ArgumentException("Keystroke not representable as an escape sequence.", nameof(keystroke))
            };

            if (escapeSequenceType == Ansi.EscapeSequences.Fe.CSI)
            {
                if (keystroke.ModifierKeys != 0)
                {
                    byte modifier = keystroke.ToCSIModifier();

                    escapeSequenceBuilder.Append(modifier);
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.KEYCODE_PARAMETER_SEPARATOR);
                }
            }

            // Special case: tab is different than the other keys
            if (escapeSequenceType == Ansi.C0.HT)
            {
                if (keystroke.ToCSIModifier() == 1)
                {
                    return escapeSequenceType.ToString();
                    // The only other supported case for Tab is Shift-Tab
                }
                else
                {
                    escapeSequenceBuilder.Clear();
                    escapeSequenceBuilder.Append(Ansi.C0.ESC);
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.Fe.CSI);
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.CBT);
                    return escapeSequenceBuilder.ToString();
                }
            }

            escapeSequenceBuilder.Append(escapeSequenceType);

            escapeSequenceBuilder.Append(keystroke.Key switch
            {
                Key.Up => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.CUU
                    : Ansi.EscapeSequences.SS3.CUU,
                Key.Down => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.CUD
                    : Ansi.EscapeSequences.SS3.CUD,
                Key.Right => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.CUF
                    : Ansi.EscapeSequences.SS3.CUF,
                Key.Left => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.CUB
                    : Ansi.EscapeSequences.SS3.CUB,

                Key.Home => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_HOME
                    : Ansi.EscapeSequences.SS3.KEYCODE_HOME,
                Key.Insert => Ansi.EscapeSequences.CSI.KEYCODE_INSERT,
                Key.Delete => Ansi.EscapeSequences.CSI.KEYCODE_DELETE,
                Key.End => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_END
                    : Ansi.EscapeSequences.SS3.KEYCODE_END,
                Key.PageUp => Ansi.EscapeSequences.CSI.KEYCODE_PAGE_UP,
                Key.PageDown => Ansi.EscapeSequences.CSI.KEYCODE_PAGE_DOWN,

                Key.F1 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_F1
                    : Ansi.EscapeSequences.SS3.KEYCODE_F1,
                Key.F2 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_F2
                    : Ansi.EscapeSequences.SS3.KEYCODE_F2,
                Key.F3 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_F3
                    : Ansi.EscapeSequences.SS3.KEYCODE_F3,
                Key.F4 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
                    ? Ansi.EscapeSequences.CSI.KEYCODE_F4
                    : Ansi.EscapeSequences.SS3.KEYCODE_F4,

                Key.F5 => Ansi.EscapeSequences.CSI.KEYCODE_F5,
                Key.F6 => Ansi.EscapeSequences.CSI.KEYCODE_F6,
                Key.F7 => Ansi.EscapeSequences.CSI.KEYCODE_F7,
                Key.F8 => Ansi.EscapeSequences.CSI.KEYCODE_F8,
                Key.F9 => Ansi.EscapeSequences.CSI.KEYCODE_F9,
                Key.F10 => Ansi.EscapeSequences.CSI.KEYCODE_F10,
                Key.F11 => Ansi.EscapeSequences.CSI.KEYCODE_F11,
                Key.F12 => Ansi.EscapeSequences.CSI.KEYCODE_F12,

                _ => throw new ArgumentException("Keystroke not representable as an escape sequence.", nameof(keystroke))
            });

            if (escapeSequenceType == Ansi.EscapeSequences.Fe.CSI)
            {
                if (
                    keystroke.Key is not Key.Up
                    and not Key.Down
                    and not Key.Right
                    and not Key.Left
                )
                {
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.KEYCODE_TERMINATOR);
                }
            }

            return escapeSequenceBuilder.ToString();
        }

        /// <summary>
        /// Converts a <see cref="Keystroke"/> to the character it represents.
        /// </summary>
        /// <remarks>This is an extension method to <see
        /// cref="Keystroke"/>.</remarks>
        /// <param name="keystroke">The <see cref="Keystroke"/> to
        /// convert.</param>
        /// <returns>A <see cref="char"/>, or <see langword="null"/> if the
        /// conversion failed.</returns>
        private static string? ToStringRepresentation(this Keystroke keystroke)
        {
            char[] representation = new char[8];
            byte[] keyboardState = new byte[256];

            if ((keystroke.ModifierKeys & ModifierKeys.LeftShift) != 0)
            {
                keyboardState[(int)VIRTUAL_KEY.VK_SHIFT] = 0x80;
                keyboardState[(int)VIRTUAL_KEY.VK_LSHIFT] = 0x80;
            }

            if ((keystroke.ModifierKeys & ModifierKeys.RightShift) != 0)
            {
                keyboardState[(int)VIRTUAL_KEY.VK_SHIFT] = 0x80;
                keyboardState[(int)VIRTUAL_KEY.VK_RSHIFT] = 0x80;
            }

            if (keystroke.CapsLock)
            {
                keyboardState[(int)VIRTUAL_KEY.VK_CAPITAL] = 0x01;
            }

            uint virtualKey = (uint)keystroke.Key;
            uint scanCode = PInvoke.MapVirtualKey(virtualKey, MAP_VIRTUAL_KEY_TYPE.MAPVK_VK_TO_VSC);
            UnloadKeyboardLayoutSafeHandle keyboardLayout = PInvoke.GetKeyboardLayout_SafeHandle(0);

            int result = PInvoke.ToUnicodeEx(
                virtualKey,
                scanCode,
                keyboardState,
                representation,
                0,
                keyboardLayout
            );

            return result > 0
                ? new string(representation, 0, Math.Min(result, 8))
                : null;
        }

        /// <summary>
        /// Converts a <see cref="Keystroke"/> to its CSI escape sequence
        /// modifier parameter.
        /// </summary>
        /// <remarks>This is an extension method to <see
        /// cref="Keystroke"/>.</remarks>
        /// <param name="keystroke">The <see cref="Keystroke"/> to
        /// convert.</param>
        /// <returns>A <see cref="byte"/> containing the modifier
        /// parameter.</returns>
        private static byte ToCSIModifier(this Keystroke keystroke)
        {
            byte modifier = 1;

            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftShift) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightShift) != 0
            )
            {
                modifier += 1;
            }

            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
            )
            {
                modifier += 2;
            }

            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
            )
            {
                modifier += 4;
            }

            if (
                (keystroke.ModifierKeys & ModifierKeys.LeftMeta) != 0
                || (keystroke.ModifierKeys & ModifierKeys.RightMeta) != 0
            )
            {
                modifier += 8;
            }

            return modifier;
        }

        private static List<ExtendedModifierKeys>? s_extendedModifiers;

        /// <summary>
        /// Determines whether a <paramref name="keystroke"/> is equivalent to
        /// <paramref name="key"/> with <paramref name="modifierKeys"/>.
        /// </summary>
        /// <remarks>This is an extension method to <see
        /// cref="Keystroke"/>.</remarks>
        /// <param name="keystroke">The <see cref="Keystroke"/> to
        /// check.</param>
        /// <param name="key">The key to check for a match.</param>
        /// <param name="modifierKeys">The modifier keys to check for a
        /// match.</param>
        /// <returns><see langword="true"/> if the keystrokes match or <see
        /// langword="false"/> otherwise.</returns>
        public static bool Is(this Keystroke keystroke, Key key, ExtendedModifierKeys modifierKeys)
        {
            if (keystroke.Key != key)
            {
                return false;
            }

            // Check for no flags
            if (modifierKeys == ExtendedModifierKeys.None)
            {
                return keystroke.ModifierKeys == ModifierKeys.None;
            }

            // Check for inclusive flags
            foreach (ModifierKeys flag in Enum.GetValues(typeof(ModifierKeys)))
            {
                if ((keystroke.ModifierKeys & flag) != ModifierKeys.None)
                {
                    if (!modifierKeys.HasFlag((ExtendedModifierKeys)flag))
                    {
                        return false;
                    }
                }
            }

            // Build the difference between ExtendedModifierKeys and
            // ModifierKeys
            if (s_extendedModifiers is null)
            {
                ModifierKeys[] modifiers = (ModifierKeys[])Enum.GetValues(typeof(ModifierKeys));
                bool skip = false;
                s_extendedModifiers = [];

                foreach (ExtendedModifierKeys extendedModifier in Enum.GetValues(typeof(ExtendedModifierKeys)))
                {
                    foreach (ModifierKeys modifier in modifiers)
                    {
                        if ((byte)extendedModifier == (byte)modifier)
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (!skip)
                    {
                        s_extendedModifiers.Add(extendedModifier);
                    }

                    skip = false;
                }
            }

            // Check for exclusive flags
            foreach (ExtendedModifierKeys flag in s_extendedModifiers)
            {
                if ((modifierKeys & flag) != ExtendedModifierKeys.None)
                {
                    if ((keystroke.ModifierKeys & (ModifierKeys)flag) == 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
