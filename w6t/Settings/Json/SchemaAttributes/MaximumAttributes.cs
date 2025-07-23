using System;

namespace Spakov.W6t.Settings.Json.SchemaAttributes {
  /// <summary>
  /// Represents a JSON Schema <c>maximum</c> range validation keyword.
  /// </summary>
  /// <remarks>Handles <see langword="int"/>s.</remarks>
  /// <param name="maximum"><inheritdoc cref="Maximum"
  /// path="/summary"/></param>
  [AttributeUsage(AttributeTargets.All, Inherited = false)]
  internal class MaximumIntAttribute(int maximum) : Attribute {
    /// <summary>
    /// The maximum.
    /// </summary>
    public virtual int Maximum { get; private init; } = maximum;
  }

  /// <summary>
  /// Represents a JSON Schema <c>maximum</c> range validation keyword.
  /// </summary>
  /// <remarks>Handles <see langword="double"/>s.</remarks>
  /// <param name="maximum"><inheritdoc cref="Maximum"
  /// path="/summary"/></param>
  [AttributeUsage(AttributeTargets.All, Inherited = false)]
  internal class MaximumDoubleAttribute(double maximum) : Attribute {
    /// <summary>
    /// The maximum.
    /// </summary>
    public virtual double Maximum { get; private init; } = maximum;
  }
}