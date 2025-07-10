using System;
using Windows.Globalization.NumberFormatting;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// A floating-point settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.NumberBox"/> with a spinner.
  /// </summary>
  public partial class NumberSettingsItem : SettingsItem {
    private double _boundValue;

    /// <summary>
    /// Gets the <see cref="NumberSettingsItem"/>'s value.
    /// </summary>
    public required Func<double> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="NumberSettingsItem"/>'s value.
    /// </summary>
    public required Action<double> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="NumberSettingsItem"/>.
    /// </summary>
    public double BoundValue {
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
    public double SmallChange { get; set; } = 0.1;

    /// <summary>
    /// The large change delta, which is effective by using Page Up and Page
    /// Down.
    /// </summary>
    public double LargeChange { get; set; } = 1.0;

    /// <summary>
    /// A <see cref="DecimalFormatter"/> used to format the value.
    /// </summary>
    public DecimalFormatter Formatter { get; set; } = new() {
      FractionDigits = 1
    };
  }
}
