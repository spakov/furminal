using System;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// A caption settings item, presented as a caption-styled <see
    /// cref="Microsoft.UI.Xaml.Controls.TextBlock"/>.
    /// </summary>
    /// <remarks>The <see cref="CaptionSettingsItem"/> does not use the <see
    /// cref="SettingsItem.Name"/> property.</remarks>
    public partial class CaptionSettingsItem : SettingsItem
    {
        /// <summary>
        /// Gets the text to be presented.
        /// </summary>
        public required Func<string> Getter { get; set; }

        /// <summary>
        /// The text to be presented.
        /// </summary>
        public string Caption => Getter();
    }
}
