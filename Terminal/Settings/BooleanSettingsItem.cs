using System;

namespace Terminal.Settings {
  /// <summary>
  /// A Boolean settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.ToggleSwitch"/>.
  /// </summary>
  public partial class BooleanSettingsItem : SettingsItem {
    private bool _boundValue;

    /// <summary>
    /// Gets the <see cref="BooleanSettingsItem"/>'s value.
    /// </summary>
    public required Func<bool> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="BooleanSettingsItem"/>'s value.
    /// </summary>
    public required Action<bool> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="BooleanSettingsItem"/>.
    /// </summary>
    public bool BoundValue {
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
