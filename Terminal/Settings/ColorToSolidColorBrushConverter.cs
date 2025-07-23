using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Spakov.Terminal.Settings {
  /// <summary>
  /// Conversion to/from <see cref="Color"/> and <see cref="SolidColorBrush"/>.
  /// </summary>
  public partial class ColorToSolidColorBrushConverter : IValueConverter {
    public object Convert(object value, Type targetType, object parameter, string language) => value is Color color ? new SolidColorBrush(color) : DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, string language) => value is SolidColorBrush brush ? brush.Color : DependencyProperty.UnsetValue;
  }
}
