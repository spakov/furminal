using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Windows.UI;

namespace Spakov.W6t.Settings.Json
{
    /// <summary>
    /// A <c>System.Text.Json</c> converter for <see cref="Color"/>.
    /// </summary>
    internal class ColorJsonConverter : JsonConverter<Color>
    {
        public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString()!.ToColor();
        public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToHexCode());
    }
}
