using Windows.UI;
using Windows.UI.Composition;
using WinUIEx;

namespace Spakov.W6t.Settings
{
    /// <summary>
    /// Supported window backdrops.
    /// </summary>
    public enum WindowBackdrop
    {
        DefaultBackgroundColor,
        Mica,
        Acrylic,
        Blurred,
        Transparent,
        SolidColor
    }

    /// <summary>
    /// A solid color backdrop.
    /// </summary>
    internal partial class SolidColorBackdrop : CompositionBrushBackdrop
    {
        private Color _color;

        internal SolidColorBackdrop(Color color) : base()
        {
            _color = color;
        }

        protected override CompositionBrush CreateBrush(Compositor compositor) => compositor.CreateColorBrush(_color);
    }

    /// <summary>
    /// The blurred backdrop.
    /// </summary>
    /// <remarks>Pulled from the WinUIEx sample.</remarks>
    internal partial class BlurredBackdrop : CompositionBrushBackdrop
    {
        protected override CompositionBrush CreateBrush(Compositor compositor) => compositor.CreateHostBackdropBrush();
    }
}
