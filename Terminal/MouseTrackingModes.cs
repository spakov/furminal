using System;

namespace Terminal {
  /// <summary>
  /// Different mouse-tracking modes.
  /// </summary>
  [Flags]
  internal enum MouseTrackingModes {
    /// <summary>
    /// No mouse tracking.
    /// </summary>
    None = 0x00,

    /// <summary>
    /// X10 mouse tracking mode, as in DECSET 9.
    /// </summary>
    X10 = 0x01,

    /// <summary>
    /// X11 mouse tracking mode, as in DECSET 1000.
    /// </summary>
    X11 = 0x02,

    /// <summary>
    /// Cell motion mouse tracking mode, as in DECSET 1002.
    /// </summary>
    CellMotion = 0x04,

    /// <summary>
    /// All motion mouse tracking mode, as in DECSET 1003.
    /// </summary>
    AllMotion = 0x08,

    /// <summary>
    /// SGR mouse tracking mode, as in DECSET 1006.
    /// </summary>
    SGR = 0x10,

    /// <summary>
    /// Pixel mouse tracking mode, as in DECSET 1016.
    /// </summary>
    Pixel = 0x20
  }
}
