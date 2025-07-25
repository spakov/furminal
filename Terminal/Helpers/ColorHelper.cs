namespace Spakov.Terminal.Helpers
{
    /// <summary>
    /// Extension methods to <see cref="System.Drawing.Color"/>.
    /// </summary>
    public static class ColorHelper
    {
        /// <summary>
        /// Returns a <see cref="Windows.UI.Color"/> representing this color.
        /// </summary>
        /// <remarks>This is an extension method to <see
        /// cref="System.Drawing.Color"/>.</remarks>
        /// <param name="systemColor">The <see cref="System.Drawing.Color"/> on
        /// which this is an extension method.</param>
        /// <returns>A <see cref="Windows.UI.Color"/> representing this
        /// color.</returns>
        public static Windows.UI.Color ToWindowsUIColor(this System.Drawing.Color systemColor)
        {
            return Windows.UI.Color.FromArgb(
                systemColor.A,
                systemColor.R,
                systemColor.G,
                systemColor.B
            );
        }
    }
}
