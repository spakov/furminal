using Spakov.AnsiProcessor.Helpers;
using System;

namespace Spakov.AnsiProcessor.Input {
  /// <summary>
  /// Keyboard modifier keys, for a standard US QWERTY 104-key keyboard.
  /// </summary>
  [Flags]
  public enum ModifierKeys {
    None =          0x00,
    LeftShift =     0x01,
    RightShift =    0x02,
    LeftAlt =       0x04,
    RightAlt =      0x08,
    LeftControl =   0x10,
    RightControl =  0x20,
    LeftMeta =      0x40,
    RightMeta =     0x80
  }

  /// <summary>
  /// Extended keyboard modifier keys, for a standard US QWERTY 104-key
  /// keyboard.
  /// </summary>
  /// <remarks>Useful for <see cref="KeystrokeHelper.Is"/>.</remarks>
  [Flags]
  public enum ExtendedModifierKeys {
    None =          0x00,
    LeftShift =     0x01,
    RightShift =    0x02,
    Shift =         0x03,
    LeftAlt =       0x04,
    RightAlt =      0x08,
    Alt =           0x0c,
    LeftControl =   0x10,
    RightControl =  0x20,
    Control =       0x30,
    LeftMeta =      0x40,
    RightMeta =     0x80,
    Meta =          0xc0
  }
}
