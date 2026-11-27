using Haru.Doc;
using Haru.Graphics;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Base class of the geometric markup annotations: lines, squares and circles. They are drawn with
    /// the annotation color (/C) and the border width and dash of <see cref="HpdfAnnotation.SetBorderStyle"/>,
    /// and may have an interior color (/IC) that fills the shape or the closed line endings.
    /// </summary>
    public abstract class HpdfGeometricAnnotation : HpdfMarkupAnnotation
    {
        private protected HpdfGeometricAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect)
            : base(xref, type, rect, HpdfVersion.Version13)
        {
        }

        /// <summary>
        /// Gets the interior color (/IC), or null when the interior is not filled.
        /// </summary>
        internal HpdfAnnotationColor? InteriorColor { get; private set; }

        /// <summary>
        /// Sets an RGB interior color (/IC, PDF 1.4).
        /// </summary>
        /// <param name="r">Red component (0.0 - 1.0).</param>
        /// <param name="g">Green component (0.0 - 1.0).</param>
        /// <param name="b">Blue component (0.0 - 1.0).</param>
        public void SetInteriorRgbColor(float r, float g, float b) => SetInteriorColor(HpdfAnnotationColor.Rgb(r, g, b));

        /// <summary>
        /// Sets a CMYK interior color (/IC, PDF 1.4).
        /// </summary>
        /// <param name="c">Cyan component (0.0 - 1.0).</param>
        /// <param name="m">Magenta component (0.0 - 1.0).</param>
        /// <param name="y">Yellow component (0.0 - 1.0).</param>
        /// <param name="k">Black component (0.0 - 1.0).</param>
        public void SetInteriorCmykColor(float c, float m, float y, float k) =>
            SetInteriorColor(HpdfAnnotationColor.Cmyk(c, m, y, k));

        /// <summary>
        /// Sets a gray interior color (/IC, PDF 1.4).
        /// </summary>
        /// <param name="gray">Gray level (0.0 = black, 1.0 = white).</param>
        public void SetInteriorGrayColor(float gray) => SetInteriorColor(HpdfAnnotationColor.Gray(gray));

        /// <summary>
        /// Leaves the interior unfilled (an empty /IC array).
        /// </summary>
        public void SetInteriorTransparent() => SetInteriorColor(HpdfAnnotationColor.Transparent);

        private void SetInteriorColor(HpdfAnnotationColor color)
        {
            InteriorColor = color;
            _dict["IC"] = color.ToArray();
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Whether the outline is stroked: the annotation has a color and a border width.
        /// </summary>
        private protected bool HasStroke => Color is { IsTransparent: false } && BorderWidth > 0;

        /// <summary>
        /// Whether the interior is filled.
        /// </summary>
        private protected bool HasFill => InteriorColor is { IsTransparent: false };

        /// <summary>
        /// Sets the stroke color, line width and dash of the border on the canvas (when it is stroked)
        /// and the fill color (when the interior is filled).
        /// </summary>
        private protected void SetPaint(IDrawable canvas)
        {
            if (HasStroke)
            {
                Color!.SetStroke(canvas);
                canvas.SetLineWidth(BorderWidth);
                if (BorderStyle == HpdfBorderStyle.Dashed)
                    canvas.SetDash(new[] { BorderDash.On, BorderDash.Off }, 0);
            }

            if (HasFill)
                InteriorColor!.SetFill(canvas);
        }

        /// <summary>
        /// Paints the current path: filled and stroked, filled or stroked, as the annotation is.
        /// </summary>
        private protected void PaintPath(IDrawable canvas, bool fill = true)
        {
            bool doFill = fill && HasFill;
            if (doFill && HasStroke)
                canvas.FillStroke();
            else if (doFill)
                canvas.Fill();
            else if (HasStroke)
                canvas.Stroke();
            else
                canvas.EndPath();
        }
    }
}
