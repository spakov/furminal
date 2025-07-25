using Spakov.W6t.Settings.Json;
using Spakov.W6t.Settings.Json.SchemaAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Windows.Win32;

namespace Spakov.W6t.Settings
{
    /// <summary>
    /// Settings helper methods.
    /// </summary>
    internal static partial class SettingsHelper
    {
        /// <summary>
        /// The name of the settings file.
        /// </summary>
        private const string SettingsFile = "settings.json";

        /// <summary>
        /// The JSON Schema metaschema to reference in the settings file.
        /// </summary>
        private const string Metaschema = "https://json-schema.org/draft/2020-12/schema";

        private static readonly JsonSerializerOptions s_jsonSerializerOptions = new(SettingsContext.Default.Options);

        private static readonly List<string> s_issue105769WorkaroundProperties =
        [
            "SolidColorWindowBackdropColor",
            "CursorColor",
            "DefaultBackgroundColor",
            "DefaultForegroundColor",
            "DefaultUnderlineColor",
            "Black",
            "Red",
            "Green",
            "Yellow",
            "Blue",
            "Magenta",
            "Cyan",
            "White",
            "BrightBlack",
            "BrightRed",
            "BrightGreen",
            "BrightYellow",
            "BrightBlue",
            "BrightMagenta",
            "BrightCyan",
            "BrightWhite"
        ];

        /// <summary>
        /// The path to the settings file.
        /// </summary>
        internal static string SettingsPath => $@"{Windows.Storage.ApplicationData.Current.LocalFolder.Path}\{SettingsFile}";

        /// <summary>
        /// The <see cref="System.Text.Json.JsonSerializerOptions"/>.
        /// </summary>
        internal static JsonSerializerOptions JsonSerializerOptions => s_jsonSerializerOptions;

        /// <summary>
        /// Generates the TermBar schema and writes it to the current working
        /// directory.
        /// </summary>
        internal static void GenerateSchema()
        {
            JsonNode schema = JsonSerializerOptions.GetJsonSchemaAsNode(
              typeof(Json.Settings),
              new()
              {
                  TreatNullObliviousAsNonNullable = true,
                  TransformSchemaNode = SchemaAttributeTransformHelper.TransformSchemaNodeSchemaAttributes
              }
            );

            EnhanceSchema(schema);

            string filename = $"w6t-{Assembly.GetExecutingAssembly().GetName().Version!.Major}.{Assembly.GetExecutingAssembly().GetName().Version!.Minor}-schema.json";
            File.WriteAllText(filename, schema.ToJsonString(JsonSerializerOptions));

            PInvoke.MessageBox(
                Windows.Win32.Foundation.HWND.Null,
                string.Format(App.ResourceLoader.GetString("SchemaHasBeenGenerated"), Path.Combine(Directory.GetCurrentDirectory(), filename)),
                App.ResourceLoader.GetString("W6tSchemaGenerated"),
                Windows.Win32.UI.WindowsAndMessaging.MESSAGEBOX_STYLE.MB_OK
            );
        }

        /// <summary>
        /// Enhances the schema by making several tweaks.
        /// </summary>
        /// <remarks>
        /// <para>Performs the following operations, intended to make the
        /// json-schema-for-humans-generated documentation prettier:</para>
        /// <list type="bullet">
        /// <item>Sets title.</item>
        /// <item>References the metaschema.</item>
        /// <item>Applies a workaround for Issue 105769.</item>
        /// </list>
        /// </remarks>
        /// <param name="jsonNode">The <see cref="JsonNode"/> at which to begin
        /// replacing. Invoke with the schema root.</param>
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Primitive types only")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        private static void EnhanceSchema(JsonNode? jsonNode)
        {
            // Check objects
            if (jsonNode is JsonObject jsonObject)
            {
                if (jsonObject.Parent is null)
                {
                    // Add title and metaschema to root object
                    jsonObject.Insert(0, "title", SettingsFile);
                    jsonObject.Insert(0, "$schema", Metaschema);
                }
                else
                {
                    foreach (string property in s_issue105769WorkaroundProperties)
                    {
                        if (jsonObject.ContainsKey(property))
                        {
                            jsonObject[property] = ApplyIssue105769Workaround(property);
                        }
                    }
                }

                // Recursively visit object children
                foreach (KeyValuePair<string, JsonNode?> property in jsonObject)
                {
                    EnhanceSchema(property.Value);
                }
                // Check arrays
            }
            else if (jsonNode is JsonArray jsonArray)
            {
                // Recursively visit array children
                foreach (JsonNode? property in jsonArray)
                {
                    EnhanceSchema(property);
                }
            }
        }

        /// <summary>
        /// Applies a workaround to <see
        /// href="https://github.com/dotnet/runtime/issues/105769"/>, building
        /// out the color subschemas "by hand".
        /// </summary>
        /// <remarks>See <see
        /// href="https://github.com/dotnet/runtime/discussions/115196#discussioncomment-12996967"
        /// /> for a discussion about this. Probably slated to be implemented
        /// in .NET 11.</remarks>
        /// <param name="property">The property for which to apply the
        /// workaround.</param>
        private static JsonObject ApplyIssue105769Workaround(string property)
        {
            Type? attributeType = property switch
            {
                "SolidColorWindowBackdropColor" => typeof(Appearance),
                "CursorColor" => typeof(Cursor),
                "DefaultBackgroundColor" => typeof(DefaultColors),
                "DefaultForegroundColor" => typeof(DefaultColors),
                "DefaultUnderlineColor" => typeof(DefaultColors),
                "Black" => typeof(StandardColors),
                "Red" => typeof(StandardColors),
                "Green" => typeof(StandardColors),
                "Yellow" => typeof(StandardColors),
                "Blue" => typeof(StandardColors),
                "Magenta" => typeof(StandardColors),
                "Cyan" => typeof(StandardColors),
                "White" => typeof(StandardColors),
                "BrightBlack" => typeof(BrightColors),
                "BrightRed" => typeof(BrightColors),
                "BrightGreen" => typeof(BrightColors),
                "BrightYellow" => typeof(BrightColors),
                "BrightBlue" => typeof(BrightColors),
                "BrightMagenta" => typeof(BrightColors),
                "BrightCyan" => typeof(BrightColors),
                "BrightWhite" => typeof(BrightColors),
                _ => null
            };

            MemberInfo[]? attributeMembers = attributeType?.GetMember(property, BindingFlags.Public | BindingFlags.Instance);

            DescriptionAttribute? description = attributeMembers?[0].GetCustomAttribute<DescriptionAttribute>();
            DefaultStringAttribute? @default = attributeMembers?[0].GetCustomAttribute<DefaultStringAttribute>();

            return new()
            {
                ["description"] = description?.Description,
                ["default"] = @default?.Default,
                ["type"] = new JsonArray()
                {
                    "string",
                    "null"
                }
            };
        }
    }
}