using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Terminal.Settings {
  /// <summary>
  /// Selects the appropriate XAML template based on a type of <see
  /// cref="SettingsItem"/>.
  /// </summary>
  public partial class SettingsItemTemplateSelector : DataTemplateSelector {
    public DataTemplate? BooleanSettingsItemTemplate { get; set; }
    public DataTemplate? ButtonSettingsItemTemplate { get; set; }
    public DataTemplate? CaptionSettingsItemTemplate { get; set; }
    public DataTemplate? ColorSettingsItemTemplate { get; set; }
    public DataTemplate? FontFamilyPickerItemTemplate { get; set; }
    public DataTemplate? GroupSettingsItemTemplate { get; set; }
    public DataTemplate? IntegerSettingsItemTemplate { get; set; }
    public DataTemplate? KeyBindingSettingsItemTemplate { get; set; }
    public DataTemplate? NumberSettingsItemTemplate { get; set; }
    public DataTemplate? RadioCollectionSettingsItemTemplate { get; set; }
    public DataTemplate? RadioSettingsItemTemplate { get; set; }
    public DataTemplate? TextSettingsItemTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) {
      return item switch {
        BooleanSettingsItem => BooleanSettingsItemTemplate!,
        ButtonSettingsItem => ButtonSettingsItemTemplate!,
        CaptionSettingsItem => CaptionSettingsItemTemplate!,
        ColorSettingsItem => ColorSettingsItemTemplate!,
        FontFamilyPickerSettingsItem => FontFamilyPickerItemTemplate!,
        GroupSettingsItem => GroupSettingsItemTemplate!,
        IntegerSettingsItem => IntegerSettingsItemTemplate!,
        KeyBindingSettingsItem => KeyBindingSettingsItemTemplate!,
        NumberSettingsItem => NumberSettingsItemTemplate!,
        RadioCollectionSettingsItem => RadioCollectionSettingsItemTemplate!,
        RadioSettingsItem => RadioSettingsItemTemplate!,
        TextSettingsItem => TextSettingsItemTemplate!,
        _ => base.SelectTemplateCore(item)
      };
    }
  }
}
