using System.Text.Json.Serialization;

namespace Spakov.Furminal.Settings.Json
{
    /// <summary>
    /// A <see cref="JsonSerializerContext"/> for reading and writing JSON
    /// settings via generated code.
    /// </summary>
    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(Settings))]
    internal partial class SettingsContext : JsonSerializerContext {
    }
}