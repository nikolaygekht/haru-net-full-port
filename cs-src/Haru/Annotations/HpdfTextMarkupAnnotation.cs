using System;
using System.Collections.Generic;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Text markup annotation: highlight, underline, squiggly underline or strikeout (PDF 1.3; squiggly
    /// PDF 1.4) of text on the page, given as quadrilaterals (/QuadPoints), one per line of text.
    /// Create it with <see cref="HpdfPage.CreateHighlightAnnotation"/>, <see cref="HpdfPage.CreateUnderlineAnnotation"/>,
    /// <see cref="HpdfPage.CreateSquigglyAnnotation"/> or <see cref="HpdfPage.CreateStrikeOutAnnotation"/>.
    /// The annotation rectangle encloses the quadrilaterals; the appearance stream is generated when the
    /// document is saved (a highlight is painted with the Multiply blend mode, so the text stays readable;
    /// in PDF/A, which allows no blend modes, it is drawn as an outline).
    /// </summary>
    public class HpdfTextMarkupAnnotation : HpdfMarkupAnnotation
    {
        /// <summary>
        /// Line width of underlines and strikeouts, and amplitude of squiggles, per height of the quadrilateral.
        /// </summary>
        private const double ThicknessPerHeight = 1.0 / 14;

        private Quad[] _quads;

        /// <summary>
        /// Creates a text markup annotation (use the <c>Create...Annotation</c> methods of <see cref="HpdfPage"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type">Highlight, Underline, Squiggly or StrikeOut.</param>
        /// <param name="rect">The rectangle of the marked text: the first quadrilateral until
        /// <see cref="SetQuadPoints(HpdfRect[])"/> is called.</param>
        /// <param name="text">The text of the annotation (/Contents), shown in its pop-up window; may be empty.</param>
        internal HpdfTextMarkupAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect, string text)
            : base(xref, ValidateType(type), rect, type == HpdfAnnotationType.Squiggly ? HpdfVersion.Version14 : HpdfVersion.Version13)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            _quads = new[] { Quad.FromRect(Rect) };
            WriteQuadPoints();

            if (type == HpdfAnnotationType.Highlight)
                SetRgbColor(1, 1, 0);
            else
                SetRgbColor(1, 0, 0);
        }

        private static HpdfAnnotationType ValidateType(HpdfAnnotationType type) =>
            type is HpdfAnnotationType.Highlight or HpdfAnnotationType.Underline
                or HpdfAnnotationType.Squiggly or HpdfAnnotationType.StrikeOut
                ? type
                : throw new HpdfException(HpdfErrorCode.InvalidParameter, "The type must be Highlight, Underline, Squiggly or StrikeOut");

        /// <summary>
        /// Sets the marked text as rectangles, one per line of text (/QuadPoints).
        /// </summary>
        /// <param name="lines">The rectangles of the lines: from the bottom of the descenders to the top
        /// of the ascenders.</param>
        public void SetQuadPoints(params HpdfRect[] lines)
        {
            if (lines is null || lines.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "At least one rectangle is required");

            var quads = new Quad[lines.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                var rect = lines[i];
                if (!float.IsFinite(rect.Left) || !float.IsFinite(rect.Bottom) || !float.IsFinite(rect.Right) || !float.IsFinite(rect.Top))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Rectangle coordinates must be finite numbers");
                quads[i] = Quad.FromRect(Normalize(rect));
            }

            _quads = quads;
            WriteQuadPoints();
        }

        /// <summary>
        /// Sets the marked text as one quadrilateral (/QuadPoints), for text that is not horizontal: the
        /// bottom edge runs from <paramref name="leftBottom"/> to <paramref name="rightBottom"/> along the
        /// text, the top edge from <paramref name="leftTop"/> to <paramref name="rightTop"/>.
        /// </summary>
        public void SetQuadPoints(HpdfPoint leftBottom, HpdfPoint rightBottom, HpdfPoint leftTop, HpdfPoint rightTop)
        {
            foreach (var point in new[] { leftBottom, rightBottom, leftTop, rightTop })
            {
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Quadrilateral points must be finite numbers");
            }

            _quads = new[] { new Quad(leftTop, rightTop, leftBottom, rightBottom) };
            WriteQuadPoints();
        }

        private static HpdfRect Normalize(HpdfRect rect)
        {
            if (rect.Top < rect.Bottom)
                (rect.Top, rect.Bottom) = (rect.Bottom, rect.Top);
            if (rect.Right < rect.Left)
                (rect.Right, rect.Left) = (rect.Left, rect.Right);
            return rect;
        }

        /// <summary>
        /// Writes /QuadPoints in the order viewers read it (Acrobat, pdf.js, PDFium): upper left, upper right,
        /// lower left, lower right - not the counterclockwise order of the specification's text.
        /// </summary>
        private void WriteQuadPoints()
        {
            var array = new HpdfArray();
            foreach (var quad in _quads)
            {
                foreach (var point in new[] { quad.LeftTop, quad.RightTop, quad.LeftBottom, quad.RightBottom })
                {
                    array.Add(new HpdfReal(point.X));
                    array.Add(new HpdfReal(point.Y));
                }
            }
            _dict["QuadPoints"] = array;
        }

        private protected override bool UsesMultiply(HpdfAnnotationWriteContext context) =>
            Type == HpdfAnnotationType.Highlight && !context.PdfA;

        private protected override HpdfRect GetAppearanceRect()
        {
            double left = double.MaxValue, bottom = double.MaxValue, right = double.MinValue, top = double.MinValue;
            double pad = 0;
            foreach (var quad in _quads)
            {
                foreach (var point in new[] { quad.LeftTop, quad.RightTop, quad.LeftBottom, quad.RightBottom })
                {
                    left = Math.Min(left, point.X);
                    bottom = Math.Min(bottom, point.Y);
                    right = Math.Max(right, point.X);
                    top = Math.Max(top, point.Y);
                }
                pad = Math.Max(pad, quad.Height * ThicknessPerHeight);
            }

            // Room for the outline of a PDF/A highlight and the squiggles
            pad += 1;
            return new HpdfRect((float)(left - pad), (float)(bottom - pad), (float)(right + pad), (float)(top + pad));
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color)
                return;

            canvas.GSave();
            if (Type == HpdfAnnotationType.Highlight && !context.PdfA)
                color.SetFill(canvas);
            else
                color.SetStroke(canvas);

            foreach (var quad in _quads)
            {
                switch (Type)
                {
                    case HpdfAnnotationType.Highlight:
                        DrawHighlight(canvas, quad, context.PdfA);
                        break;
                    case HpdfAnnotationType.Underline:
                        DrawLine(canvas, quad, 1.0 / 7);
                        break;
                    case HpdfAnnotationType.StrikeOut:
                        DrawLine(canvas, quad, 0.5);
                        break;
                    case HpdfAnnotationType.Squiggly:
                        DrawSquiggle(canvas, quad);
                        break;
                }
            }

            canvas.GRestore();
        }

        private static void DrawHighlight(HpdfAppearanceCanvas canvas, Quad quad, bool outline)
        {
            if (outline)
                canvas.SetLineWidth(1);

            MoveTo(canvas, quad.LeftBottom);
            LineTo(canvas, quad.RightBottom);
            LineTo(canvas, quad.RightTop);
            LineTo(canvas, quad.LeftTop);
            canvas.ClosePath();

            if (outline)
                canvas.Stroke();
            else
                canvas.Fill();
        }

        /// <summary>
        /// Draws a line parallel to the bottom edge at a part of the height (underline, strikeout).
        /// </summary>
        private static void DrawLine(HpdfAppearanceCanvas canvas, Quad quad, double heightPart)
        {
            double thickness = Math.Max(0.5, quad.Height * ThicknessPerHeight);
            var (upX, upY) = quad.Up;
            double offset = quad.Height * heightPart;

            canvas.SetLineWidth((float)thickness);
            canvas.MoveTo((float)(quad.LeftBottom.X + upX * offset), (float)(quad.LeftBottom.Y + upY * offset));
            canvas.LineTo((float)(quad.RightBottom.X + upX * offset), (float)(quad.RightBottom.Y + upY * offset));
            canvas.Stroke();
        }

        /// <summary>
        /// Draws a zigzag along the bottom edge (squiggly underline).
        /// </summary>
        private static void DrawSquiggle(HpdfAppearanceCanvas canvas, Quad quad)
        {
            double amplitude = Math.Max(1, quad.Height * ThicknessPerHeight);
            double step = amplitude * 2;
            var (upX, upY) = quad.Up;
            double dx = quad.RightBottom.X - quad.LeftBottom.X;
            double dy = quad.RightBottom.Y - quad.LeftBottom.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0)
                return;

            double dirX = dx / length, dirY = dy / length;
            canvas.SetLineWidth((float)Math.Max(0.5, amplitude / 2));
            int count = Math.Max(1, (int)Math.Round(length / step));
            double actualStep = length / count;
            for (int i = 0; i <= count; i++)
            {
                double along = i * actualStep;
                double across = i % 2 == 0 ? amplitude / 2 : amplitude * 2;
                float x = (float)(quad.LeftBottom.X + dirX * along + upX * across);
                float y = (float)(quad.LeftBottom.Y + dirY * along + upY * across);
                if (i == 0)
                    canvas.MoveTo(x, y);
                else
                    canvas.LineTo(x, y);
            }
            canvas.Stroke();
        }

        private static void MoveTo(IDrawable canvas, HpdfPoint point) => canvas.MoveTo(point.X, point.Y);

        private static void LineTo(IDrawable canvas, HpdfPoint point) => canvas.LineTo(point.X, point.Y);

        /// <summary>
        /// A quadrilateral around a line of text.
        /// </summary>
        private readonly record struct Quad(HpdfPoint LeftTop, HpdfPoint RightTop, HpdfPoint LeftBottom, HpdfPoint RightBottom)
        {
            public static Quad FromRect(HpdfRect rect) => new Quad(
                new HpdfPoint(rect.Left, rect.Top), new HpdfPoint(rect.Right, rect.Top),
                new HpdfPoint(rect.Left, rect.Bottom), new HpdfPoint(rect.Right, rect.Bottom));

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
}
