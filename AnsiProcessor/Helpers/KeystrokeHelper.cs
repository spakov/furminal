using Spakov.AnsiProcessor.Ansi;
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

            KeystrokeType? keyType = null;

            if (keystroke.Key is Key.Home or Key.End)
            {
                keyType = KeystrokeType.ComplexKeystroke;
            }

            if (keystroke.Key is >= Key.F1 and <= Key.F12)
            {
                keyType = KeystrokeType.ComplexKeystroke;
            }

            if (
                keystroke.XTMODKEYS.ModifyKeypadKeysValue != ModifyKeypadKeysValue.Disabled
                && keystroke.Key is >= Key.NumPad0 and <= Key.NumPad9
            )
            {
                keyType = KeystrokeType.ComplexKeystroke;
            }

            if (keystroke.XTMODKEYS.ModifyOtherKeysValue != ModifyOtherKeysValue.Disabled)
            {
                keyType = keystroke.XTMODKEYS.ModifyOtherKeysValue == ModifyOtherKeysValue.All
                    ? KeystrokeType.ComplexKeystroke
                    : keystroke.ModifierKeys == ModifierKeys.None
                        ? null
                        : KeystrokeType.ComplexKeystroke;
            }

            keyType ??= keystroke.Key switch
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

            keyType ??=
                (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
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
                KeystrokeType.ComplexKeystroke => ComplexKeystrokeToAnsiEscapeSequence(keystroke),
                _ => throw new InvalidOperationException(),
            };
        }

        /// <summary>
        /// Converts <paramref name="keystroke"/> to an ANSI escape sequence.
        /// </summary>
        /// <remarks>
        /// <para>This method does not consider <see
        /// cref="Keystroke.XTMODKEYS"/> and is truly only suited for control
        /// characters.</para>
        /// <para>Assumes Alt, Control, and/or Meta are high in <paramref
        /// name="keystroke"/>'s <see cref="ModifierKeys"/>.</para>
        /// </remarks>
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
                escapeSequenceBuilder.Append(C0.ESC);
                escapeSequenceBuilder.Append((char)keystrokeToSend);

                return escapeSequenceBuilder.ToString();
            }

            // Special case: backspace
            if (keystrokeToSend == C0.BS && terminalCapabilities.Input.BackspaceIsDel)
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
        private static string ComplexKeystrokeToAnsiEscapeSequence(Keystroke keystroke)
        {
            StringBuilder escapeSequenceBuilder = new();

            // Special case: tab is different than the other keys
            if (keystroke.Key == Key.Tab)
            {
                if (keystroke.ToCSIModifier() == 1)
                {
                    return C0.HT.ToString();

                } // The only other supported case for Tab is Shift-Tab
                else
                {
                    escapeSequenceBuilder.Append(C0.ESC);
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.Fe.CSI);
                    escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.CBT);
                    return escapeSequenceBuilder.ToString();
                }
            }

            char escapeSequenceType = keystroke.Key
                is Key.Home
                or Key.End
                ? Ansi.EscapeSequences.Fe.CSI
                : keystroke.Key
                    is Key.Up
                    or Key.Down
                    or Key.Right
                    or Key.Left
                    or Key.F1
                    or Key.F2
                    or Key.F3
                    or Key.F4
                    ? keystroke.ApplicationCursorKeys
                        ? Ansi.EscapeSequences.Fe.SS3
                        : Ansi.EscapeSequences.Fe.CSI
                    : Ansi.EscapeSequences.Fe.CSI;

            bool useModifiers = keystroke.Key switch
            {
                Key.Up
                or Key.Down
                or Key.Right
                or Key.Left
                or Key.Home
                or Key.End
                or Key.PageUp
                or Key.PageDown =>
                    keystroke.XTMODKEYS.ModifyCursorKeysValue == ModifyCursorKeysValue.All
                    || keystroke.ModifierKeys != ModifierKeys.None,

                >= Key.NumPad0
                and <= Key.NumPad9 =>
                    keystroke.XTMODKEYS.ModifyKeypadKeysValue == ModifyKeypadKeysValue.All
                    || keystroke.ModifierKeys != ModifierKeys.None,

                >= Key.F1
                and <= Key.F12 =>
                    keystroke.XTMODKEYS.ModifyFunctionKeysValue == ModifyFunctionKeysValue.All
                    || keystroke.ModifierKeys != ModifierKeys.None,

                _ =>
                    keystroke.XTMODKEYS.ModifyOtherKeysValue == ModifyOtherKeysValue.All
                    || keystroke.ModifierKeys != ModifierKeys.None
            };

            object keycode = keystroke.Key switch
            {
                Key.Up => Keycodes.UP,
                Key.Down => Keycodes.DOWN,
                Key.Right => Keycodes.RIGHT,
                Key.Left => Keycodes.LEFT,

                Key.Home => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_HOME
                    : useModifiers
                        ? Keycodes.OTHER
                        : Keycodes.HOME,
                Key.Insert => Keycodes.INSERT,
                Key.Delete => Keycodes.DELETE,
                Key.End => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_END
                    : useModifiers
                        ? Keycodes.OTHER
                        : Keycodes.END,
                Key.PageUp => Keycodes.PAGE_UP,
                Key.PageDown => Keycodes.PAGE_DOWN,

                Key.F1 => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_F1
                    : Keycodes.F1,
                Key.F2 => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_F2
                    : Keycodes.F2,
                Key.F3 => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_F3
                    : Keycodes.F3,
                Key.F4 => keystroke.ApplicationCursorKeys
                    ? Keycodes.DECCKM_F4
                    : Keycodes.F4,

                Key.F5 => Keycodes.F5,
                Key.F6 => Keycodes.F6,
                Key.F7 => Keycodes.F7,
                Key.F8 => Keycodes.F8,
                Key.F9 => Keycodes.F9,
                Key.F10 => Keycodes.F10,
                Key.F11 => Keycodes.F11,
                Key.F12 => Keycodes.F12,

                _ => Keycodes.OTHER
            };

            escapeSequenceBuilder.Append(C0.ESC);
            escapeSequenceBuilder.Append(escapeSequenceType);
            escapeSequenceBuilder.Append(keycode);

            if (useModifiers)
            {
                byte modifier = keystroke.ToCSIModifier();

                escapeSequenceBuilder.Append(Keycodes.PARAMETER_SEPARATOR);
                escapeSequenceBuilder.Append(modifier);

                if (Equals(keycode, Keycodes.OTHER))
                {
                    escapeSequenceBuilder.Append(Keycodes.PARAMETER_SEPARATOR);

                    if (keystroke.Key == Key.Home)
                    {
                        escapeSequenceBuilder.Append(Keycodes.HOME);
                    }
                    else if (keystroke.Key == Key.End)
                    {
                        escapeSequenceBuilder.Append(Keycodes.END);
                    }
                    else
                    {
                        string? stringRepresentation = keystroke.ToStringRepresentation();

                        if (
                            stringRepresentation is not null
                            && stringRepresentation.Length == 1
                            && stringRepresentation[0] < 0x100
                        )
                        {
                            escapeSequenceBuilder.Append((int)stringRepresentation[0]);
                        }
                        else
                        {
                            escapeSequenceBuilder.Append((int)keystroke.Key);
                        }
                    }
                }
            }

            if (
                (
                    keystroke.Key
                    is Key.Insert
                    or Key.Delete
                    or Key.PageUp
                    or Key.PageDown
                )
                || Equals(keycode, Keycodes.OTHER)
            )
            {
                if (
                    keystroke.Key
                    is not Key.Home
                    and not Key.End
                )
                {
                    escapeSequenceBuilder.Append(Keycodes.TERMINATOR);
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
