using System;

namespace Terminal.Settings {
  /// <summary>
  /// An integer settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.NumberBox"/> with a spinner.
  /// </summary>
  public partial class IntegerSettingsItem : SettingsItem {
    private int _boundValue;

    /// <summary>
    /// Gets the <see cref="IntegerSettingsItem"/>'s value.
    /// </summary>
    public required Func<int> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="IntegerSettingsItem"/>'s value.
    /// </summary>
    public required Action<int> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="IntegerSettingsItem"/>.
    /// </summary>
    public int BoundValue {
      get => Getter();

      set {
        if (_boundValue != value) {
          _boundValue = value;
          Setter(value);
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// The small change delta, which is effective by using the spinner.
    /// </summary>
    public int SmallChange { get; set; } = 1;

    /// <summary>
    /// The large change delta, which is effective by using Page Up and Page
    /// Down.
    /// </summary>
    public int LargeChange { get; set; } = 5;
  }
}
