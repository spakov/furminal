using System;
using System.Diagnostics.CodeAnalysis;

namespace Spakov.Terminal
{
    /// <summary>
    /// A rectangle, used for <see cref="Cell"/> overfill tracking.
    /// </summary>
    internal struct RectF
    {
        public float Top;
        public float Left;
        public float Right;
        public float Bottom;

        /// <summary>
        /// Initializes a <see cref="RectF"/> with <paramref name="top"/>,
        /// <paramref name="left"/>, <paramref name="right"/>, and <paramref
        /// name="bottom"/>.
        /// </summary>
        /// <param name="top">The top.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="bottom">The bottom.</param>
        public RectF(float top, float left, float right, float bottom)
        {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }

        /// <inheritdoc cref="RectF(float, float, float, float)"/>
        public RectF(double top, double left, double right, double bottom)
        {
            Top = (float)top;
            Left = (float)left;
            Right = (float)right;
            Bottom = (float)bottom;
        }

        [SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Impacts readability")]
        public static bool operator ==(RectF a, RectF b)
        {
            if (a.Top != b.Top)
            {
                return false;
            }

            if (a.Left != b.Left)
            {
                return false;
            }

            if (a.Right != b.Right)
            {
                return false;
            }

            if (a.Bottom != b.Bottom)
            {
                return false;
            }

            return true;
        }

        public static bool operator !=(RectF a, RectF b) => !(a == b);

        public override readonly bool Equals([NotNullWhen(true)] object? obj) => obj is RectF other && this == other;

        public override readonly int GetHashCode() => HashCode.Combine(Top, Left, Right, Bottom);
    }
}
