using Spakov.Terminal.Settings;
using System.Diagnostics.CodeAnalysis;

namespace Spakov.Terminal
{
    /// <summary>
    /// Prevents trimming of classes that are never instantiated with new().
    /// </summary>
    internal static class TrimRoots
    {
        /// <summary>
        /// This method, which does nothing at runtime, must be invoked to
        /// ensure the classes marked with <see
        /// cref="DynamicDependencyAttribute"/> are preserved by the trimmer.
        /// </summary>
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SettingsGroup))]
        public static void PreserveTrimmableClasses() {
        }
    }
}