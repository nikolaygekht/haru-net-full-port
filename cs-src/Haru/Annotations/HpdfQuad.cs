/* Quadrilaterals (/QuadPoints) of text markup and redaction annotations. */
using System;
using System.Collections.Generic;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;

namespace Haru.Annotations
{
    /// <summary>
    /// A quadrilateral around a line of text or an area, as /QuadPoints lists them.
    /// </summary>
    internal readonly record struct HpdfQuad(HpdfPoint LeftTop, HpdfPoint RightTop, HpdfPoint LeftBottom, HpdfPoint RightBottom)
    {
        /// <summary>
        /// The quadrilateral of a rectangle (normalized).
        /// </summary>
        public static HpdfQuad FromRect(HpdfRect rect)
        {
            if (rect.Top < rect.Bottom)
                (rect.Top, rect.Bottom) = (rect.Bottom, rect.Top);
            if (rect.Right < rect.Left)
                (rect.Right, rect.Left) = (rect.Left, rect.Right);

            return new HpdfQuad(
                new HpdfPoint(rect.Left, rect.Top), new HpdfPoint(rect.Right, rect.Top),
                new HpdfPoint(rect.Left, rect.Bottom), new HpdfPoint(rect.Right, rect.Bottom));
        }

        /// <summary>
        /// Checks rectangles and makes quadrilaterals of them.
        /// </summary>
        public static HpdfQuad[] FromRects(HpdfRect[]? rects)
        {
            if (rects is null || rects.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "At least one rectangle is required");

            var quads = new HpdfQuad[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                if (!float.IsFinite(rect.Left) || !float.IsFinite(rect.Bottom) || !float.IsFinite(rect.Right) || !float.IsFinite(rect.Top))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Rectangle coordinates must be finite numbers");
                quads[i] = FromRect(rect);
            }
            return quads;
        }

        /// <summary>
        /// Checks the corners of a quadrilateral and makes it.
        /// </summary>
        public static HpdfQuad FromPoints(HpdfPoint leftBottom, HpdfPoint rightBottom, HpdfPoint leftTop, HpdfPoint rightTop)
        {
            foreach (var point in new[] { leftBottom, rightBottom, leftTop, rightTop })
            {
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Quadrilateral points must be finite numbers");
            }
            return new HpdfQuad(leftTop, rightTop, leftBottom, rightBottom);
        }

        /// <summary>
        /// Creates /QuadPoints in the order viewers read it (Acrobat, pdf.js, PDFium): upper left, upper right,
        /// lower left, lower right - not the counterclockwise order of the specification's text.
        /// </summary>
        public static HpdfArray ToArray(IEnumerable<HpdfQuad> quads)
        {
            var array = new HpdfArray();
            foreach (var quad in quads)
            {
                foreach (var point in quad.Corners)
                {
                    array.Add(new HpdfReal(point.X));
                    array.Add(new HpdfReal(point.Y));
                }
            }
            return array;
        }

        /// <summary>
        /// Gets the bounding box of quadrilaterals grown by a margin.
        /// </summary>
        public static HpdfRect Bounds(IEnumerable<HpdfQuad> quads, double margin)
        {
            double left = double.MaxValue, bottom = double.MaxValue, right = double.MinValue, top = double.MinValue;
            foreach (var quad in quads)
            {
                foreach (var point in quad.Corners)
                {
                    left = Math.Min(left, point.X);
                    bottom = Math.Min(bottom, point.Y);
                    right = Math.Max(right, point.X);
                    top = Math.Max(top, point.Y);
                }
            }
            return new HpdfRect((float)(left - margin), (float)(bottom - margin), (float)(right + margin), (float)(top + margin));
        }

        /// <summary>
        /// The corners in the order of /QuadPoints.
        /// </summary>
        public HpdfPoint[] Corners => new[] { LeftTop, RightTop, LeftBottom, RightBottom };

        /// <summary>
        /// Appends the outline as a closed path.
        /// </summary>
        public void AppendPath(IDrawable canvas)
        {
            canvas.MoveTo(LeftBottom.X, LeftBottom.Y);
            canvas.LineTo(RightBottom.X, RightBottom.Y);
            canvas.LineTo(RightTop.X, RightTop.Y);
            canvas.LineTo(LeftTop.X, LeftTop.Y);
            canvas.ClosePath();
        }

        /// <summary>
        /// The distance from the bottom edge to the top edge (between their centers).
        /// </summary>
        public double Height
        {
            get
            {
                double dx = (LeftTop.X + RightTop.X - LeftBottom.X - RightBottom.X) / 2;
                double dy = (LeftTop.Y + RightTop.Y - LeftBottom.Y - RightBottom.Y) / 2;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }

        /// <summary>
        /// The unit direction perpendicular to the bottom edge, towards the top edge.
        /// </summary>
        public (double X, double Y) Up
        {
            get
            {
                double dx = RightBottom.X - LeftBottom.X;
                double dy = RightBottom.Y - LeftBottom.Y;
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length == 0)
                    return (0, 1);

                // Left of the bottom edge's direction; flipped if the top edge is on the other side
                double upX = -dy / length, upY = dx / length;
                double toTopX = LeftTop.X - LeftBottom.X, toTopY = LeftTop.Y - LeftBottom.Y;
                return upX * toTopX + upY * toTopY < 0 ? (-upX, -upY) : (upX, upY);
            }
        }
    }
}
