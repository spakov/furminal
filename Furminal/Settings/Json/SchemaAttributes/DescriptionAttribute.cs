using System;

namespace Spakov.Furminal.Settings.Json.SchemaAttributes
{
    /// <summary>
    /// Represents a JSON Schema <c>description</c> annotation.
    /// </summary>
    /// <param name="description"><inheritdoc cref="Description"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DescriptionAttribute(string description) : Attribute
    {
        /// <summary>
        /// The description.
        /// </summary>
        public virtual string Description { get; private init; } = description;
    }
}