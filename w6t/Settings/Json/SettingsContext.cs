using System.Text.Json.Serialization;

namespace Spakov.W6t.Settings.Json {
  [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
  [JsonSerializable(typeof(Settings))]
  internal partial class SettingsContext : JsonSerializerContext { }
}
