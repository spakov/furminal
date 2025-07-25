using Microsoft.UI.Xaml;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// A settings item button, presented as a <see
    /// cref="Microsoft.UI.Xaml.Controls.Button"/>.
    /// </summary>
    public partial class ButtonSettingsItem : SettingsItem
    {
        /// <summary>
        /// Invoked when the button is clicked.
        /// </summary>
        public required RoutedEventHandler Click { get; set; }
    }
}
