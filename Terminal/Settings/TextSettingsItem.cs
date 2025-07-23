using System;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// A text settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.TextBox"/>.
  /// </summary>
  public partial class TextSettingsItem : SettingsItem {
    private string _boundValue = string.Empty;

    /// <summary>
    /// Gets the <see cref="TextSettingsItem"/>'s value.
    /// </summary>
    public required Func<string> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="TextSettingsItem"/>'s value.
    /// </summary>
    public required Action<string> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="TextSettingsItem"/>.
    /// </summary>
    public string BoundValue {
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
