using System;
using Windows.UI;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// A <see cref="Color"/> settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.Button"/> that reveals a <see
  /// cref="Microsoft.UI.Xaml.Controls.Flyout"/> containing a <see
  /// cref="Microsoft.UI.Xaml.Controls.ColorPicker"/>.
  /// </summary>
  public partial class ColorSettingsItem : SettingsItem {
    private Color _boundValue;

    /// <summary>
    /// Gets the <see cref="ColorSettingsItem"/>'s value.
    /// </summary>
    public required Func<Color> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="ColorSettingsItem"/>'s value.
    /// </summary>
    public required Action<Color> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="ColorSettingsItem"/>.
    /// </summary>
    public Color BoundValue {
      get => Getter();

      set {
        if (_boundValue != value) {
          _boundValue = value;
          Setter(value);
          OnPropertyChanged();
        }
      }
    }
  }
}
