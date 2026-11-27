/* Draws the line endings (/LE) of line annotations and free text callouts. */
using System;
using Haru.Doc;
using Haru.Graphics;

namespace Haru.Annotations
{
    /// <summary>
    /// Builds the path of a line ending at an endpoint. The size grows with the line width
    /// (9 points for a line of width 1); arrowheads have an angle of 30 degrees to the line.
    /// </summary>
    internal static class HpdfLineEndings
    {
        private static readonly double Cos30 = Math.Cos(Math.PI / 6);
        private const double Sin30 = 0.5;

        /// <summary>
        /// The PDF names of the styles, by <see cref="HpdfLineEndingStyle"/> value.
        /// </summary>
        private static readonly string[] Names =
        {
            "None", "Square", "Circle", "Diamond", "OpenArrow", "ClosedArrow", "Butt", "ROpenArrow", "RClosedArrow", "Slash"
        };

        /// <summary>
        /// Gets the PDF name of a style; an undefined value throws.
        /// </summary>
        public static string GetName(HpdfLineEndingStyle style) =>
            (int)style >= 0 && (int)style < Names.Length
                ? Names[(int)style]
                : throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid line ending style: {style}");

        /// <summary>
        /// Gets the size of the line endings of a line width: the length of an arrowhead and of a butt or slash;
        /// squares, circles and diamonds are two thirds of it across.
        /// </summary>
        public static double GetSize(float lineWidth) => 6 + 3 * lineWidth;

        /// <summary>
        /// Gets how far the line stops before the endpoint: at the edge of a square, circle or diamond and
        /// at the base of a closed arrowhead, so that the line does not cross an unfilled shape.
        /// </summary>
        public static double GetLineInset(HpdfLineEndingStyle style, double size) => style switch
        {
            HpdfLineEndingStyle.Square or HpdfLineEndingStyle.Circle or HpdfLineEndingStyle.Diamond => size / 3,
            HpdfLineEndingStyle.ClosedArrow => size * Cos30,
            _ => 0
        };

        /// <summary>
        /// Appends the path of a line ending.
        /// </summary>
        /// <param name="canvas">The canvas.</param>
        /// <param name="style">The style.</param>
        /// <param name="x">The endpoint, x.</param>
        /// <param name="y">The endpoint, y.</param>
        /// <param name="dirX">The unit direction pointing away from the line at the endpoint, x.</param>
        /// <param name="dirY">The unit direction pointing away from the line at the endpoint, y.</param>
        /// <param name="size">The size (<see cref="GetSize"/>).</param>
        /// <returns>Whether a closed shape was appended (to be filled), false for an open one;
        /// null when nothing was appended.</returns>
        public static bool? Append(IDrawable canvas, HpdfLineEndingStyle style, double x, double y,
            double dirX, double dirY, double size)
        {
            // Perpendicular to the direction, to the left
            double perpX = -dirY;
            double perpY = dirX;
            double half = size / 3;

            switch (style)
            {
                case HpdfLineEndingStyle.OpenArrow:
                case HpdfLineEndingStyle.ClosedArrow:
                case HpdfLineEndingStyle.ROpenArrow:
                case HpdfLineEndingStyle.RClosedArrow:
                {
                    // The wings go back along the line, or away from it for the reverse arrows
                    double back = style is HpdfLineEndingStyle.OpenArrow or HpdfLineEndingStyle.ClosedArrow ? -1 : 1;
                    double alongX = back * dirX * size * Cos30;
                    double alongY = back * dirY * size * Cos30;
                    double sideX = perpX * size * Sin30;
                    double sideY = perpY * size * Sin30;
                    MoveTo(canvas, x + alongX + sideX, y + alongY + sideY);
                    LineTo(canvas, x, y);
                    LineTo(canvas, x + alongX - sideX, y + alongY - sideY);
                    bool closed = style is HpdfLineEndingStyle.ClosedArrow or HpdfLineEndingStyle.RClosedArrow;
                    if (closed)
                        canvas.ClosePath();
                    return closed;
                }

                case HpdfLineEndingStyle.Butt:
                    MoveTo(canvas, x + perpX * size / 2, y + perpY * size / 2);
                    LineTo(canvas, x - perpX * size / 2, y - perpY * size / 2);
                    return false;

                case HpdfLineEndingStyle.Slash:
                {
                    // The perpendicular turned 30 degrees clockwise
                    double slashX = perpX * Cos30 + perpY * Sin30;
                    double slashY = -perpX * Sin30 + perpY * Cos30;
                    MoveTo(canvas, x + slashX * size / 2, y + slashY * size / 2);
                    LineTo(canvas, x - slashX * size / 2, y - slashY * size / 2);
                    return false;
                }

                case HpdfLineEndingStyle.Square:
                    MoveTo(canvas, x + (dirX + perpX) * half, y + (dirY + perpY) * half);
                    LineTo(canvas, x + (-dirX + perpX) * half, y + (-dirY + perpY) * half);
                    LineTo(canvas, x + (-dirX - perpX) * half, y + (-dirY - perpY) * half);
                    LineTo(canvas, x + (dirX - perpX) * half, y + (dirY - perpY) * half);
                    canvas.ClosePath();
                    return true;

                case HpdfLineEndingStyle.Diamond:
                    MoveTo(canvas, x + dirX * half, y + dirY * half);
                    LineTo(canvas, x + perpX * half, y + perpY * half);
                    LineTo(canvas, x - dirX * half, y - dirY * half);
                    LineTo(canvas, x - perpX * half, y - perpY * half);
                    canvas.ClosePath();
                    return true;

                case HpdfLineEndingStyle.Circle:
                    canvas.Circle((float)x, (float)y, (float)half);
                    canvas.ClosePath();
                    return true;

                default:
                    return null;
            }
        }

        private static void MoveTo(IDrawable canvas, double x, double y) => canvas.MoveTo((float)x, (float)y);

        private static void LineTo(IDrawable canvas, double x, double y) => canvas.LineTo((float)x, (float)y);
    }
}
