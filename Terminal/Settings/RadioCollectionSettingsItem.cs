using System;
using System.Collections.ObjectModel;

namespace Spakov.Terminal.Settings
{
    /// <summary>
    /// A settings item representing a collection of <see
    /// cref="RadioSettingsItem"/>s, presented as a <see
    /// cref="Microsoft.UI.Xaml.Controls.RadioButtons"/>.
    /// </summary>
    public partial class RadioCollectionSettingsItem : SettingsItem
    {
        /// <summary>
        /// <see cref="RadioSettingsItem"/>s to present.
        /// </summary>
        public ObservableCollection<RadioSettingsItem>? Items { get; set; }

        private int _boundValue;

        /// <summary>
        /// Gets the <see cref="RadioCollectionSettingsItem"/>'s value (as the
        /// selected index).
        /// </summary>
        public required Func<int> Getter { get; set; }

        /// <summary>
        /// Sets the <see cref="RadioCollectionSettingsItem"/>'s value (as the
        /// selected index).
        /// </summary>
        public required Action<int> Setter { get; set; }

        /// <summary>
        /// The value of the <see cref="RadioCollectionSettingsItem"/> (as the
        /// selected index).
        /// </summary>
        public int BoundValue
        {
            get => Getter();

            set
            {
                if (_boundValue != value)
                {
                    _boundValue = value;
                    Setter(value);
                    OnPropertyChanged();
                }
            }
        }
    }
}
