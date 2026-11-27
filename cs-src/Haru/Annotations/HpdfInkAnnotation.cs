using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Ink annotation (PDF 1.3, not in the C library): freehand strokes, each a path of points (/InkList), drawn
    /// with the annotation color (red until changed) and the border width, with round ends and joins.
    /// Create it with <see cref="HpdfPage.CreateInkAnnotation"/>. The annotation rectangle follows from the
    /// points; the appearance stream is generated when the document is saved.
    /// </summary>
    public class HpdfInkAnnotation : HpdfMarkupAnnotation
    {
        private HpdfPoint[][] _strokes = Array.Empty<HpdfPoint[]>();

        /// <summary>
        /// Creates an ink annotation (use <see cref="HpdfPage.CreateInkAnnotation"/>).
        /// </summary>
        internal HpdfInkAnnotation(HpdfXref xref, HpdfPoint[][] strokes, string text)
            : base(xref, HpdfAnnotationType.Ink, default, HpdfVersion.Version13)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            SetInkList(strokes);
            SetRgbColor(1, 0, 0);
        }

        /// <summary>
        /// Gets the strokes.
        /// </summary>
        public HpdfPoint[][] Strokes => Array.ConvertAll(_strokes, stroke => (HpdfPoint[])stroke.Clone());

        /// <summary>
        /// Sets the strokes (/InkList): every stroke is a path of points in page coordinates; a stroke of one
        /// point is a dot.
        /// </summary>
        /// <param name="strokes">The strokes, at least one, each with at least one point.</param>
        public void SetInkList(params HpdfPoint[][] strokes)
        {
            ValidateStrokes(strokes);

            var list = new HpdfArray();
            foreach (var stroke in strokes)
            {
                var path = new HpdfArray();
                foreach (var point in stroke)
                {
                    path.Add(new HpdfReal(point.X));
                    path.Add(new HpdfReal(point.Y));
                }
                list.Add(path);
            }

            _strokes = Array.ConvertAll(strokes, stroke => (HpdfPoint[])stroke.Clone());
            _dict["InkList"] = list;
            SetRect(GetAppearanceRect());
        }

        /// <summary>
        /// Checks strokes: at least one, each with at least one finite point (before an annotation object is created).
        /// </summary>
        internal static void ValidateStrokes(HpdfPoint[][]? strokes)
        {
            if (strokes is null || strokes.Length == 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "At least one stroke is required");
            foreach (var stroke in strokes)
            {
                if (stroke is null || stroke.Length == 0)
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "A stroke needs at least one point");
                foreach (var point in stroke)
                {
                    if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                        throw new HpdfException(HpdfErrorCode.InvalidParameter, "Stroke points must be finite numbers");
                }
            }
        }

        private protected override HpdfRect GetAppearanceRect()
        {
            float left = float.MaxValue, bottom = float.MaxValue, right = float.MinValue, top = float.MinValue;
            foreach (var stroke in _strokes)
            {
                foreach (var point in stroke)
                {
                    left = Math.Min(left, point.X);
                    bottom = Math.Min(bottom, point.Y);
                    right = Math.Max(right, point.X);
                    top = Math.Max(top, point.Y);
                }
            }

            // Round ends and joins reach half the line width beyond the points
            float pad = BorderWidth / 2 + 1;
            return new HpdfRect(left - pad, bottom - pad, right + pad, top + pad);
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color || BorderWidth <= 0)
                return;

            canvas.GSave();
            color.SetStroke(canvas);
            canvas.SetLineWidth(BorderWidth);
            canvas.SetLineCap(HpdfLineCap.RoundEnd);
            canvas.SetLineJoin(HpdfLineJoin.RoundJoin);
            if (BorderStyle == HpdfBorderStyle.Dashed)
                canvas.SetDash(new[] { BorderDash.On, BorderDash.Off }, 0);

            foreach (var stroke in _strokes)
            {
                canvas.MoveTo(stroke[0].X, stroke[0].Y);
                if (stroke.Length == 1)
                    canvas.LineTo(stroke[0].X, stroke[0].Y);
                for (int i = 1; i < stroke.Length; i++)
                    canvas.LineTo(stroke[i].X, stroke[i].Y);
            }
            canvas.Stroke();
            canvas.GRestore();
        }
    }
}
