using System.Collections.ObjectModel;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// A group of <see cref="SettingsItem"/>s, presented as a WinUI 3-style
    /// Card.
    /// </summary>
    public partial class GroupSettingsItem : SettingsItem
    {
        /// <summary>
        /// <see cref="SettingsItem"/>s to present.
        /// </summary>
        public ObservableCollection<SettingsItem>? Items { get; set; }
    }
}
