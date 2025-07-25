using System;

namespace Spakov.W6t.Settings.Json.SchemaAttributes
{
    /// <summary>
    /// Represents a JSON Schema <c>minimum</c> range validation keyword.
    /// </summary>
    /// <remarks>Handles <see langword="int"/>s.</remarks>
    /// <param name="minimum"><inheritdoc cref="Minimum"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class MinimumIntAttribute(int minimum) : Attribute
    {
        /// <summary>
        /// The minimum.
        /// </summary>
        public virtual int Minimum { get; private init; } = minimum;
    }

    /// <summary>
    /// Represents a JSON Schema <c>minimum</c> range validation keyword.
    /// </summary>
    /// <remarks>Handles <see langword="double"/>s.</remarks>
    /// <param name="minimum"><inheritdoc cref="Minimum"
    /// path="/summary"/></param>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    internal class MinimumDoubleAttribute(double minimum) : Attribute
    {
        /// <summary>
        /// The minimum.
        /// </summary>
        public virtual double Minimum { get; private init; } = minimum;
    }
}