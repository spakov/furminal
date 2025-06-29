using System;

namespace Terminal.Settings {
  /// <summary>
  /// A key binding settings item, presented as <see
  /// cref="Microsoft.UI.Xaml.Controls.TextBlock"/>s describing the key
  /// binding.
  /// </summary>
  /// <remarks>Note that these aren't actually configurable; this is merely
  /// presentation to the user.</remarks>
  public partial class KeyBindingSettingsItem : SettingsItem {
    /// <summary>
    /// Gets the key binding to be presented.
    /// </summary>
    /// <remarks>This is meant to be a representation of a keystroke, like
    /// <c>Shift-Page Up</c>.</remarks>
    public required Func<string> Getter { get; set; }

    /// <summary>
    /// The key binding to be presented.
    /// </summary>
    public string KeyBinding => Getter();
  }
}
