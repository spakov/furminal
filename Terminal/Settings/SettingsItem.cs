using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// A settings item.
    /// </summary>
    public abstract class SettingsItem : INotifyPropertyChanged
    {
        /// <summary>
        /// A unique identifier for the settings item, useful when taking
        /// advantage of <see cref="SettingsViewModel.Groups"/> customization.
        /// </summary>
        public required string Key { get; set; }

        /// <summary>
        /// The name of the settings item, as displayed in the settings window.
        /// </summary>
        public string? Name { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? callerMemberName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerMemberName));
    }
}
