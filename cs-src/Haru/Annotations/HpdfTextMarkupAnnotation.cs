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

        private HpdfQuad[] _quads;

        /// <summary>
        /// Creates a text markup annotation (use the <c>Create...Annotation</c> methods of <see cref="HpdfPage"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type">Highlight, Underline, Squiggly or StrikeOut.</param>
        /// <param name="rect">The rectangle of the marked text: the first quadrilateral until
        /// <see cref="SetQuadPoints(HpdfRect[])"/> is called.</param>
        /// <param name="text">The text of the annotation (/Contents), shown in its pop-up window; may be empty.</param>
        internal HpdfTextMarkupAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect, string text)
            : base(xref, ValidateType(type), rect, MinimumVersion(type))
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            _quads = new[] { HpdfQuad.FromRect(Rect) };
            WriteQuadPoints();

            if (type == HpdfAnnotationType.Highlight)
                SetRgbColor(1, 1, 0);
            else
                SetRgbColor(1, 0, 0);
        }

        /// <summary>
        /// Squiggly underlines came with PDF 1.4; highlights need it for the blend mode of their appearance.
        /// </summary>
        private static HpdfVersion MinimumVersion(HpdfAnnotationType type) =>
            type is HpdfAnnotationType.Squiggly or HpdfAnnotationType.Highlight ? HpdfVersion.Version14 : HpdfVersion.Version13;

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
            _quads = HpdfQuad.FromRects(lines);
            WriteQuadPoints();
        }

        /// <summary>
        /// Sets the marked text as one quadrilateral (/QuadPoints), for text that is not horizontal: the
        /// bottom edge runs from <paramref name="leftBottom"/> to <paramref name="rightBottom"/> along the
        /// text, the top edge from <paramref name="leftTop"/> to <paramref name="rightTop"/>.
        /// </summary>
        public void SetQuadPoints(HpdfPoint leftBottom, HpdfPoint rightBottom, HpdfPoint leftTop, HpdfPoint rightTop)
        {
            _quads = new[] { HpdfQuad.FromPoints(leftBottom, rightBottom, leftTop, rightTop) };
            WriteQuadPoints();
        }

        private void WriteQuadPoints() => _dict["QuadPoints"] = HpdfQuad.ToArray(_quads);

        private protected override bool UsesMultiply(HpdfAnnotationWriteContext context) =>
            Type == HpdfAnnotationType.Highlight && !context.PdfA;

        private protected override HpdfRect GetAppearanceRect()
        {
            double pad = 0;
            foreach (var quad in _quads)
                pad = Math.Max(pad, quad.Height * ThicknessPerHeight);

            // Room for the outline of a PDF/A highlight and the squiggles
            return HpdfQuad.Bounds(_quads, pad + 1);
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

        private static void DrawHighlight(HpdfAppearanceCanvas canvas, HpdfQuad quad, bool outline)
        {
            if (outline)
                canvas.SetLineWidth(1);

            quad.AppendPath(canvas);

            if (outline)
                canvas.Stroke();
            else
                canvas.Fill();
        }

        /// <summary>
        /// Draws a line parallel to the bottom edge at a part of the height (underline, strikeout).
        /// </summary>
        private static void DrawLine(HpdfAppearanceCanvas canvas, HpdfQuad quad, double heightPart)
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
        private static void DrawSquiggle(HpdfAppearanceCanvas canvas, HpdfQuad quad)
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

    }
}
