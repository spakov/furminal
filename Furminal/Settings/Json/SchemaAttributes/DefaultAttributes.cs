using System;

namespace Spakov.Furminal.Settings.Json.SchemaAttributes
{
    /// <summary>
    /// Represents a JSON Schema <c>default</c> annotation for a string (<see
    /// cref="System.Text.Json.JsonValueKind.String"/>).
    /// </summary>
    /// <param name="default"><inheritdoc cref="Default"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DefaultStringAttribute(string @default) : Attribute
    {
        /// <summary>
        /// The default.
        /// </summary>
        public virtual string Default { get; private init; } = @default;
    }

    /// <summary>
    /// Represents a JSON Schema <c>default</c> annotation for a number (<see
    /// cref="System.Text.Json.JsonValueKind.Number"/>).
    /// </summary>
    /// <remarks>Handles <see langword="int"/>s.</remarks>
    /// <param name="default"><inheritdoc cref="Default"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DefaultIntNumberAttribute(int @default) : Attribute
    {
        /// <summary>
        /// The default.
        /// </summary>
        public virtual int Default { get; private init; } = @default;
    }

    /// <summary>
    /// Represents a JSON Schema <c>default</c> annotation for a number (<see
    /// cref="System.Text.Json.JsonValueKind.Number"/>).
    /// </summary>
    /// <remarks>Handles <see langword="double"/>s.</remarks>
    /// <param name="default"><inheritdoc cref="Default"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DefaultDoubleNumberAttribute(double @default) : Attribute
    {
        /// <summary>
        /// The default.
        /// </summary>
        public virtual double Default { get; private init; } = @default;
    }

    /// <summary>
    /// Represents a JSON Schema <c>default</c> annotation for a Boolean (<see
    /// cref="System.Text.Json.JsonValueKind.True"/> or <see
    /// cref="System.Text.Json.JsonValueKind.False"/>).
    /// </summary>
    /// <param name="default"><inheritdoc cref="Default"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DefaultBooleanAttribute(bool @default) : Attribute
    {
        /// <summary>
        /// The default.
        /// </summary>
        public virtual bool Default { get; private init; } = @default;
    }

    /// <summary>
    /// Represents a JSON Schema <c>default</c> annotation of null (<see
    /// cref="System.Text.Json.JsonValueKind.Null"/>).
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class DefaultNullAttribute() : Attribute {
    }
}