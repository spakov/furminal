using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;

namespace Spakov.Furminal.Settings.Json.SchemaAttributes
{
    /// <summary>
    /// Contains schema node transformation methods.
    /// </summary>
    internal static class SchemaAttributeTransformHelper
    {
        /// <summary>
        /// Transforms <paramref name="schema"/> by applying schema attributes
        /// to it in the form of corresponding JSON Schema annotations or
        /// validation keywords.
        /// </summary>
        /// <remarks>This is based on the example provided by Microsoft at <see
        /// href="https://devblogs.microsoft.com/dotnet/system-text-json-in-dotnet-9/#json-schema-exporter"
        /// />.</remarks>
        /// <param name="context">A <see
        /// cref="JsonSchemaExporterContext"/>.</param>
        /// <param name="schema">A <see cref="JsonNode"/>.</param>
        /// <returns>An updated <paramref name="schema"/>.</returns>
        internal static JsonNode TransformSchemaNodeSchemaAttributes(JsonSchemaExporterContext context, JsonNode schema)
        {
            // Determine if a type or property and extract the relevant
            // attribute provider
            ICustomAttributeProvider? attributeProvider = context.PropertyInfo is not null
                ? context.PropertyInfo.AttributeProvider
                : context.TypeInfo.Type;

            // These effectively get added in reverse order
            ApplyMaximumAnnotation(schema, attributeProvider);
            ApplyMinimumAnnotation(schema, attributeProvider);
            ApplyDefaultAnnotation(schema, attributeProvider);
            ApplyDescriptionAnnotation(schema, attributeProvider);

            return schema;
        }

        /// <summary>
        /// Applies <see cref="DefaultStringAttribute"/>s, <see
        /// cref="DefaultIntNumberAttribute"/>s, <see
        /// cref="DefaultDoubleNumberAttribute"/>s, <see
        /// cref="DefaultBooleanAttribute"/>s, and <see
        /// cref="DefaultNullAttribute"/>s as <c>default</c>
        /// annotations.
        /// </summary>
        /// <param name="schema"><inheritdoc
        /// cref="TransformSchemaNodeSchemaAttributes"
        /// path="/param[@name='schema']"/></param>
        /// <param name="attributeProvider">An <see
        /// cref="ICustomAttributeProvider"/>.</param>
        private static void ApplyDefaultAnnotation(JsonNode schema, ICustomAttributeProvider? attributeProvider)
        {
            // Look up any default string attributes
            DefaultStringAttribute? defaultStringAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DefaultStringAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply default attribute to the generated schema
            if (defaultStringAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "default", defaultStringAttribute.Default);
                }
            }

            // Look up any default int number attributes
            DefaultIntNumberAttribute? defaultIntNumberAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DefaultIntNumberAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply default attribute to the generated schema
            if (defaultIntNumberAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "default", defaultIntNumberAttribute.Default);
                }
            }

            // Look up any default double number attributes
            DefaultDoubleNumberAttribute? defaultDoubleNumberAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DefaultDoubleNumberAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply default attribute to the generated schema
            if (defaultDoubleNumberAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "default", defaultDoubleNumberAttribute.Default);
                }
            }

            // Look up any default boolean attributes
            DefaultBooleanAttribute? defaultBooleanAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DefaultBooleanAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply default attribute to the generated schema
            if (defaultBooleanAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "default", defaultBooleanAttribute.Default);
                }
            }

            // Look up any default null attributes
            DefaultNullAttribute? defaultNullAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DefaultNullAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply default attribute to the generated schema
            if (defaultNullAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "default", null);
                }
            }
        }

        /// <summary>
        /// Applies <see cref="DescriptionAttribute"/>s as <c>description</c>
        /// annotations.
        /// </summary>
        /// <param name="schema"><inheritdoc
        /// cref="TransformSchemaNodeSchemaAttributes"
        /// path="/param[@name='schema']"/></param>
        /// <param name="attributeProvider">An <see
        /// cref="ICustomAttributeProvider"/>.</param>
        private static void ApplyDescriptionAnnotation(JsonNode schema, ICustomAttributeProvider? attributeProvider)
        {
            // Look up any description attributes
            DescriptionAttribute? descriptionAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as DescriptionAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply description attribute to the generated schema
            if (descriptionAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "description", descriptionAttribute.Description);
                }
            }
        }

        /// <summary>
        /// Applies <see cref="MaximumIntAttribute"/>s and <see
        /// cref="MaximumDoubleAttribute"/>s as <c>maximum</c> range validation
        /// keywords.
        /// </summary>
        /// <param name="schema"><inheritdoc
        /// cref="TransformSchemaNodeSchemaAttributes"
        /// path="/param[@name='schema']"/></param>
        /// <param name="attributeProvider">An <see
        /// cref="ICustomAttributeProvider"/>.</param>
        private static void ApplyMaximumAnnotation(JsonNode schema, ICustomAttributeProvider? attributeProvider)
        {
            // Look up any int maximum attributes
            MaximumIntAttribute? maximumIntAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as MaximumIntAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply maximum attribute to the generated schema
            if (maximumIntAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "maximum", maximumIntAttribute.Maximum);
                }
            }

            // Look up any double maximum attributes
            MaximumDoubleAttribute? maximumDoubleAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as MaximumDoubleAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply maximum attribute to the generated schema
            if (maximumDoubleAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "maximum", maximumDoubleAttribute.Maximum);
                }
            }
        }

        /// <summary>
        /// Applies <see cref="MinimumIntAttribute"/>s and <see
        /// cref="MinimumDoubleAttribute"/>s as <c>minimum</c> range validation
        /// keywords.
        /// </summary>
        /// <param name="schema"><inheritdoc
        /// cref="TransformSchemaNodeSchemaAttributes"
        /// path="/param[@name='schema']"/></param>
        /// <param name="attributeProvider">An <see
        /// cref="ICustomAttributeProvider"/>.</param>
        private static void ApplyMinimumAnnotation(JsonNode schema, ICustomAttributeProvider? attributeProvider)
        {
            // Look up any int minimum attributes
            MinimumIntAttribute? minimumIntAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as MinimumIntAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply minimum attribute to the generated schema
            if (minimumIntAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "minimum", minimumIntAttribute.Minimum);
                }
            }

            // Look up any double minimum attributes
            MinimumDoubleAttribute? minimumDoubleAttribute = attributeProvider?
                .GetCustomAttributes(inherit: true)
                .Select(attr => attr as MinimumDoubleAttribute)
                .FirstOrDefault(attr => attr is not null);

            // Apply minimum attribute to the generated schema
            if (minimumDoubleAttribute != null)
            {
                if (schema is JsonObject jObj)
                {
                    jObj.Insert(0, "minimum", minimumDoubleAttribute.Minimum);
                }
            }
        }
    }
}