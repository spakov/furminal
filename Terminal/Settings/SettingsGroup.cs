using System.Collections.ObjectModel;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// A settings group.
  /// </summary>
  public class SettingsGroup {
    /// <summary>
    /// A unique identifier for the settings group, useful when taking
    /// advantage of <see cref="SettingsViewModel.Groups"/> customization.
    /// </summary>
    public required string Key { get; set; }

    /// <summary>
    /// The name of the settings group, as displayed in the settings window.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The <see cref="SettingsItem"/>s contained in this <see
    /// cref="SettingsGroup"/>.
    /// </summary>
    public ObservableCollection<SettingsItem>? Items { get; set; }
  }
}
