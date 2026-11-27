/* The rectangle differences (/RD) of square, circle, free text and caret annotations. */
using Haru.Objects;
using Haru.Types;

namespace Haru.Annotations
{
    /// <summary>
    /// The distances from the edges of the annotation rectangle to the drawn shape (/RD). ISO 32000-1 lists
    /// them as left, top, right and bottom, in this order (the C library writes left, bottom, right, top).
    /// </summary>
    internal readonly record struct HpdfRectDifference(float Left, float Top, float Right, float Bottom)
    {
        /// <summary>
        /// No difference: the shape fills the rectangle.
        /// </summary>
        public static HpdfRectDifference None => default;

        /// <summary>
        /// The same distance on every side.
        /// </summary>
        public static HpdfRectDifference Uniform(float distance) => new(distance, distance, distance, distance);

        /// <summary>
        /// The differences between an outer rectangle and an inner one.
        /// </summary>
        public static HpdfRectDifference Between(HpdfRect outer, HpdfRect inner) =>
            new(inner.Left - outer.Left, outer.Top - inner.Top, outer.Right - inner.Right, inner.Bottom - outer.Bottom);

        /// <summary>
        /// Checks distances set by the user: non-negative, finite, leaving room inside the rectangle.
        /// </summary>
        public static HpdfRectDifference Create(HpdfRect rect, float left, float top, float right, float bottom)
        {
            if (!IsDistance(left) || !IsDistance(top) || !IsDistance(right) || !IsDistance(bottom))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Rectangle differences must be non-negative numbers");
            if (left + right >= rect.Right - rect.Left || top + bottom >= rect.Top - rect.Bottom)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The rectangle differences leave no room for the shape");

            return new HpdfRectDifference(left, top, right, bottom);
        }

        /// <summary>
        /// Gets the inner rectangle of an outer one.
        /// </summary>
        public HpdfRect Inside(HpdfRect rect) => new(rect.Left + Left, rect.Bottom + Bottom, rect.Right - Right, rect.Top - Top);

        /// <summary>
        /// Creates the /RD array.
        /// </summary>
        public HpdfArray ToArray()
        {
            var array = new HpdfArray();
            array.Add(new HpdfReal(Left));
            array.Add(new HpdfReal(Top));
            array.Add(new HpdfReal(Right));
            array.Add(new HpdfReal(Bottom));
            return array;
        }

        private static bool IsDistance(float value) => value >= 0 && float.IsFinite(value);
    }
}
