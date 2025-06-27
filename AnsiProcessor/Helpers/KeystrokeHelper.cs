using AnsiProcessor.Input;
using AnsiProcessor.TermCap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace AnsiProcessor.Helpers {
  /// <summary>
  /// Methods for converting keystrokes to ANSI.
  /// </summary>
  public static class KeystrokeHelper {
    /// <summary>
    /// Types of keystrokes.
    /// </summary>
    private enum KeystrokeTypes {
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
    /// Converts <paramref name="keystroke"/> to its corresponding ANSI string.
    /// </summary>
    /// <param name="keystroke">A keystroke.</param>
    /// <param name="terminalCapabilities">A <see cref="TerminalCapabilities"/>
    /// configuration.</param>
    /// <returns>The ANSI string representation of <paramref
    /// name="keystroke"/>, or <see langword="null"/> if one could not be
    /// determined.</returns>
    internal static string? KeystrokeToAnsi(Keystroke keystroke, TerminalCapabilities terminalCapabilities) {
      // Ignore repeated key presses if DECARM is not in effect
      if (!keystroke.AutoRepeatKeys && keystroke.IsRepeat) return null;

      KeystrokeTypes? keyType = keystroke.Key switch {
        Keys.Up => KeystrokeTypes.ComplexKeystroke,
        Keys.Down => KeystrokeTypes.ComplexKeystroke,
        Keys.Right => KeystrokeTypes.ComplexKeystroke,
        Keys.Left => KeystrokeTypes.ComplexKeystroke,
        Keys.Home => KeystrokeTypes.ComplexKeystroke,
        Keys.Insert => KeystrokeTypes.ComplexKeystroke,
        Keys.Delete => KeystrokeTypes.ComplexKeystroke,
        Keys.End => KeystrokeTypes.ComplexKeystroke,
        Keys.PageUp => KeystrokeTypes.ComplexKeystroke,
        Keys.PageDown => KeystrokeTypes.ComplexKeystroke,
        Keys.F1 => KeystrokeTypes.ComplexKeystroke,
        Keys.F2 => KeystrokeTypes.ComplexKeystroke,
        Keys.F3 => KeystrokeTypes.ComplexKeystroke,
        Keys.F4 => KeystrokeTypes.ComplexKeystroke,
        Keys.F5 => KeystrokeTypes.ComplexKeystroke,
        Keys.F6 => KeystrokeTypes.ComplexKeystroke,
        Keys.F7 => KeystrokeTypes.ComplexKeystroke,
        Keys.F8 => KeystrokeTypes.ComplexKeystroke,
        Keys.F9 => KeystrokeTypes.ComplexKeystroke,
        Keys.F10 => KeystrokeTypes.ComplexKeystroke,
        Keys.F11 => KeystrokeTypes.ComplexKeystroke,
        Keys.F12 => KeystrokeTypes.ComplexKeystroke,
        Keys.Tab => KeystrokeTypes.ComplexKeystroke,
        _ => null
      };

      keyType ??= (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
        || (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
        || (keystroke.ModifierKeys & ModifierKeys.LeftMeta) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightMeta) != 0
        ? KeystrokeTypes.ControlCharacter
        : KeystrokeTypes.Text;

      if (keyType == KeystrokeTypes.Text && (int) keystroke.Key < 0x20) {
        keyType = KeystrokeTypes.ControlCharacter;
      }

      return keyType switch {
        KeystrokeTypes.Text => keystroke.ToStringRepresentation(),
        KeystrokeTypes.ControlCharacter => ControlCharacterKeystrokeToAnsi(keystroke, terminalCapabilities),
        KeystrokeTypes.ComplexKeystroke => ComplexKeystrokeToAnsiEscapeSequence(keystroke, terminalCapabilities),
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
    /// <returns>The generated ANSI escape sequence, or <see langword="null"/>
    /// if one could not be determined.</returns>
    private static string? ControlCharacterKeystrokeToAnsi(Keystroke keystroke, TerminalCapabilities terminalCapabilities) {
      StringBuilder escapeSequenceBuilder = new();

      byte keystrokeToSend = (byte) keystroke.Key;

      // Control subtracts 0x40 to convert to C0
      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
      ) {
        if (keystroke.Key is >= Keys.A and <= Keys.GraveAccent) {
          keystrokeToSend -= 0x40;
        }
      }

      // Alt says we use an ESC encoding
      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
      ) {
        escapeSequenceBuilder = new();
        escapeSequenceBuilder.Append(Ansi.C0.ESC);
        escapeSequenceBuilder.Append((char) keystrokeToSend);

        return escapeSequenceBuilder.ToString();
      }

      // Special case: backspace
      if (keystrokeToSend == Ansi.C0.BS && terminalCapabilities.Input.BackspaceIsDel) {
        keystrokeToSend = 0x7f; // DEL
      }

      escapeSequenceBuilder.Append((char) keystrokeToSend);
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
    /// <exception cref="ArgumentException"><paramref name="keystroke"/> is not
    /// representable as an escape sequence.</exception>
    private static string ComplexKeystrokeToAnsiEscapeSequence(Keystroke keystroke, TerminalCapabilities terminalCapabilities) {
      StringBuilder escapeSequenceBuilder = new();

      escapeSequenceBuilder.Append(Ansi.C0.ESC);

      char escapeSequenceType = keystroke.Key switch {
        Keys.Up => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,
        Keys.Down => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,
        Keys.Right => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,
        Keys.Left => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,

        Keys.Home => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,
        Keys.Insert => Ansi.EscapeSequences.Fe.CSI,
        Keys.Delete => Ansi.EscapeSequences.Fe.CSI,
        Keys.End => keystroke.ApplicationCursorKeys
          ? Ansi.EscapeSequences.Fe.SS3
          : Ansi.EscapeSequences.Fe.CSI,
        Keys.PageUp => Ansi.EscapeSequences.Fe.CSI,
        Keys.PageDown => Ansi.EscapeSequences.Fe.CSI,

        // See https://invisible-island.net/xterm/ctlseqs/ctlseqs.html#:~:text=the%20SS3%20%20sent%20before%20F1%20through%20F4%20is%20altered%20to%20CSI
        Keys.F1 => keystroke.ModifierKeys != 0
          ? Ansi.EscapeSequences.Fe.CSI
          : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
        Keys.F2 => keystroke.ModifierKeys != 0
          ? Ansi.EscapeSequences.Fe.CSI
          : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
        Keys.F3 => keystroke.ModifierKeys != 0
          ? Ansi.EscapeSequences.Fe.CSI
          : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,
        Keys.F4 => keystroke.ModifierKeys != 0
          ? Ansi.EscapeSequences.Fe.CSI
          : terminalCapabilities.Input.F1ThroughF4KeysEscapeSequence,

        Keys.F5 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F6 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F7 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F8 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F9 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F10 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F11 => Ansi.EscapeSequences.Fe.CSI,
        Keys.F12 => Ansi.EscapeSequences.Fe.CSI,

        Keys.Tab => Ansi.C0.HT,

        _ => throw new ArgumentException("Keystroke not representable as an escape sequence.", nameof(keystroke))
      };

      if (escapeSequenceType == Ansi.EscapeSequences.Fe.CSI) {
        if (keystroke.ModifierKeys != 0) {
          byte modifier = keystroke.ToCSIModifier();

          escapeSequenceBuilder.Append(modifier);
          escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.KEYCODE_PARAMETER_SEPARATOR);
        }
      }

      // Special case: tab is different than the other keys
      if (escapeSequenceType == Ansi.C0.HT) {
        if (keystroke.ToCSIModifier() == 1) {
          return escapeSequenceType.ToString();
          // The only other supported case for Tab is Shift-Tab
        } else {
          escapeSequenceBuilder.Clear();
          escapeSequenceBuilder.Append(Ansi.C0.ESC);
          escapeSequenceBuilder.Append(Ansi.EscapeSequences.Fe.CSI);
          escapeSequenceBuilder.Append(Ansi.EscapeSequences.CSI.CBT);
          return escapeSequenceBuilder.ToString();
        }
      }

      escapeSequenceBuilder.Append(escapeSequenceType);

      escapeSequenceBuilder.Append(keystroke.Key switch {
        Keys.Up => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.CUU
          : Ansi.EscapeSequences.SS3.CUU,
        Keys.Down => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.CUD
          : Ansi.EscapeSequences.SS3.CUD,
        Keys.Right => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.CUF
          : Ansi.EscapeSequences.SS3.CUF,
        Keys.Left => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.CUB
          : Ansi.EscapeSequences.SS3.CUB,

        Keys.Home => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_HOME
          : Ansi.EscapeSequences.SS3.KEYCODE_HOME,
        Keys.Insert => Ansi.EscapeSequences.CSI.KEYCODE_INSERT,
        Keys.Delete => Ansi.EscapeSequences.CSI.KEYCODE_DELETE,
        Keys.End => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_END
          : Ansi.EscapeSequences.SS3.KEYCODE_END,
        Keys.PageUp => Ansi.EscapeSequences.CSI.KEYCODE_PAGE_UP,
        Keys.PageDown => Ansi.EscapeSequences.CSI.KEYCODE_PAGE_DOWN,

        Keys.F1 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_F1
          : Ansi.EscapeSequences.SS3.KEYCODE_F1,
        Keys.F2 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_F2
          : Ansi.EscapeSequences.SS3.KEYCODE_F2,
        Keys.F3 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_F3
          : Ansi.EscapeSequences.SS3.KEYCODE_F3,
        Keys.F4 => escapeSequenceType == Ansi.EscapeSequences.Fe.CSI
          ? Ansi.EscapeSequences.CSI.KEYCODE_F4
          : Ansi.EscapeSequences.SS3.KEYCODE_F4,

        Keys.F5 => Ansi.EscapeSequences.CSI.KEYCODE_F5,
        Keys.F6 => Ansi.EscapeSequences.CSI.KEYCODE_F6,
        Keys.F7 => Ansi.EscapeSequences.CSI.KEYCODE_F7,
        Keys.F8 => Ansi.EscapeSequences.CSI.KEYCODE_F8,
        Keys.F9 => Ansi.EscapeSequences.CSI.KEYCODE_F9,
        Keys.F10 => Ansi.EscapeSequences.CSI.KEYCODE_F10,
        Keys.F11 => Ansi.EscapeSequences.CSI.KEYCODE_F11,
        Keys.F12 => Ansi.EscapeSequences.CSI.KEYCODE_F12,

        _ => throw new ArgumentException("Keystroke not representable as an escape sequence.", nameof(keystroke))
      });

      if (escapeSequenceType == Ansi.EscapeSequences.Fe.CSI) {
        if (
          keystroke.Key is not Keys.Up
          and not Keys.Down
          and not Keys.Right
          and not Keys.Left
        ) {
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
    /// <param name="keystroke">The <see cref="Keystroke"/> to convert.</param>
    /// <returns>A <see cref="char"/>, or <see langword="null"/> if the
    /// conversion failed.</returns>
    private static string? ToStringRepresentation(this Keystroke keystroke) {
      char[] representation = new char[8];
      byte[] keyboardState = new byte[256];

      if ((keystroke.ModifierKeys & ModifierKeys.LeftShift) != 0) {
        keyboardState[(int) VIRTUAL_KEY.VK_SHIFT] = 0x80;
        keyboardState[(int) VIRTUAL_KEY.VK_LSHIFT] = 0x80;
      }

      if ((keystroke.ModifierKeys & ModifierKeys.RightShift) != 0) {
        keyboardState[(int) VIRTUAL_KEY.VK_SHIFT] = 0x80;
        keyboardState[(int) VIRTUAL_KEY.VK_RSHIFT] = 0x80;
      }

      if (keystroke.CapsLock) {
        keyboardState[(int) VIRTUAL_KEY.VK_CAPITAL] = 0x01;
      }

      uint virtualKey = (uint) keystroke.Key;
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
    /// Converts a <see cref="Keystroke"/> to its CSI escape sequence modifier
    /// parameter.
    /// </summary>
    /// <remarks>This is an extension method to <see
    /// cref="Keystroke"/>.</remarks>
    /// <param name="keystroke">The <see cref="Keystroke"/> to convert.</param>
    /// <returns>A <see cref="byte"/> containing the modifier
    /// parameter.</returns>
    private static byte ToCSIModifier(this Keystroke keystroke) {
      byte modifier = 1;

      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftShift) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightShift) != 0
      ) {
        modifier += 1;
      }

      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftAlt) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightAlt) != 0
      ) {
        modifier += 2;
      }

      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftControl) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightControl) != 0
      ) {
        modifier += 4;
      }

      if (
        (keystroke.ModifierKeys & ModifierKeys.LeftMeta) != 0
        || (keystroke.ModifierKeys & ModifierKeys.RightMeta) != 0
      ) {
        modifier += 8;
      }

      return modifier;
    }

    private static List<ExtendedModifierKeys>? extendedModifiers;

    /// <summary>
    /// Determines whether a <see cref="Keystroke"/> is equivalent to <paramref
    /// name="key"/> with <paramref name="simpleModifiers"/>.
    /// </summary>
    /// <remarks>This is an extension method to <see
    /// cref="Keystroke"/>.</remarks>
    /// <param name="keystroke">The <see cref="Keystroke"/> to check.</param>
    /// <param name="key">The key to check for a match.</param>
    /// <param name="modifierKeys">The modifier keys to check for a
    /// match.</param>
    /// <returns><see langword="true"/> if the keystrokes match or <see
    /// langword="false"/> otherwise.</returns>
    public static bool Is(this Keystroke keystroke, Keys key, ExtendedModifierKeys modifierKeys) {
      if (keystroke.Key != key) return false;

      // Check for no flags
      if (modifierKeys == ExtendedModifierKeys.None) {
        return keystroke.ModifierKeys == ModifierKeys.None;
      }

      // Check for inclusive flags
      foreach (ModifierKeys flag in Enum.GetValues(typeof(ModifierKeys))) {
        if ((keystroke.ModifierKeys & flag) != ModifierKeys.None) {
          if (!modifierKeys.HasFlag((ExtendedModifierKeys) flag)) return false;
        }
      }

      // Build the difference between ExtendedModifierKeys and ModifierKeys
      if (extendedModifiers is null) {
        ModifierKeys[] modifiers = (ModifierKeys[]) Enum.GetValues(typeof(ModifierKeys));
        bool skip = false;
        extendedModifiers = [];

        foreach (ExtendedModifierKeys extendedModifier in Enum.GetValues(typeof(ExtendedModifierKeys))) {
          foreach (ModifierKeys modifier in modifiers) {
            if ((byte) extendedModifier == (byte) modifier) {
              skip = true;
              break;
            }
          }

          if (!skip) {
            extendedModifiers.Add(extendedModifier);
          }

          skip = false;
        }
      }

      // Check for exclusive flags
      foreach (ExtendedModifierKeys flag in extendedModifiers) {
        if ((modifierKeys & flag) != ExtendedModifierKeys.None) {
          if ((keystroke.ModifierKeys & (ModifierKeys) flag) == 0) return false;
        }
      }

      return true;
    }
  }
}
