using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// A font family picker settings item, presented as a <see
  /// cref="Microsoft.UI.Xaml.Controls.ComboBox"/> and associated <see
  /// cref="Microsoft.UI.Xaml.Controls.ToggleSwitch"/> to filter by fixed-width
  /// fonts.
  /// </summary>
  public partial class FontFamilyPickerSettingsItem : SettingsItem {
    private const float epsilon = 0.01f;

    private string? _boundValue = string.Empty;

    private bool _monospaceOnly = true;

    /// <summary>
    /// Gets the <see cref="FontFamilyPickerSettingsItem"/>'s value.
    /// </summary>
    public required Func<string> Getter { get; set; }

    /// <summary>
    /// Sets the <see cref="FontFamilyPickerSettingsItem"/>'s value.
    /// </summary>
    public required Action<string> Setter { get; set; }

    /// <summary>
    /// The value of the <see cref="FontFamilyPickerSettingsItem"/>.
    /// </summary>
    public string? BoundValue {
      get => Getter is not null ? Getter() : null;

      set {
        if (Setter is not null && _boundValue != value) {
          _boundValue = value;
          Setter(value!);
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// The text to apply to the fixed-width only toggle.
    /// </summary>
    public required string MonospaceOnlyName { get; set; }

    /// <summary>
    /// The default font family name.
    /// </summary>
    public required string DefaultFontFamily { get; set; }

    /// <summary>
    /// Whether only monospace fonts should be displayed.
    /// </summary>
    public bool MonospaceOnly {
      get => _monospaceOnly;

      set {
        if (_monospaceOnly != value) {
          _monospaceOnly = value;
          EnumerateFonts();
          OnPropertyChanged();
        }
      }
    }

    /// <summary>
    /// A list of available font families.
    /// </summary>
    public ObservableCollection<string> Items { get; } = [];

    /// <summary>
    /// Initializes a <see cref="FontFamilyPickerSettingsItem"/>.
    /// </summary>
    public FontFamilyPickerSettingsItem() {
      EnumerateFonts();
    }

    /// <summary>
    /// Updates <see cref="Items"/> to contain a list of available fonts.
    /// </summary>
    /// <remarks>Fixed-width fonts are determined by laying out text samples,
    /// which is a non-trivial operation. This is potentially
    /// GPU-bound.</remarks>
    private void EnumerateFonts() {
      string? selectedFont = BoundValue;

      Items.Clear();

      List<string> systemFontFamilies = [.. CanvasTextFormat.GetSystemFontFamilies()];

      if (MonospaceOnly) {
        List<string> monospaceFontFamilies = [];

        using (CanvasDrawingSession drawingSession = new CanvasRenderTarget(new CanvasDevice(), 1.0f, 1.0f, 96).CreateDrawingSession()) {
          foreach (string systemFontFamily in systemFontFamilies) {
            CanvasTextFormat canvasTextFormat = new() {
              FontFamily = systemFontFamily
            };

            CanvasTextLayout period = new(drawingSession, ".", canvasTextFormat, 0.0f, 0.0f);
            CanvasTextLayout em = new(drawingSession, "M", canvasTextFormat, 0.0f, 0.0f);

            if (Math.Abs(period.LayoutBounds.Width - em.LayoutBounds.Width) < epsilon) {
              monospaceFontFamilies.Add(systemFontFamily);
            }
          }
        }

        monospaceFontFamilies.Sort();

        foreach (string monospaceFontFamily in monospaceFontFamilies) {
          Items.Add(monospaceFontFamily);
        }
      } else {
        systemFontFamilies.Sort();

        foreach (string systemFontFamily in systemFontFamilies) {
          Items.Add(systemFontFamily);
        }
      }

      if (selectedFont is not null && Items.Contains(selectedFont)) {
        if (BoundValue != selectedFont) {
          BoundValue = selectedFont;
        }
      } else {
        BoundValue = DefaultFontFamily;
      }
    }
  }
}
