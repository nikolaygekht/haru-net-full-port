using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Square or circle annotation (PDF 1.3): a rectangle or an ellipse inscribed in the annotation
    /// rectangle, stroked with the annotation color and the border width, filled with the interior color.
    /// Create it with <see cref="HpdfPage.CreateSquareAnnotation"/> or <see cref="HpdfPage.CreateCircleAnnotation"/>.
    /// The appearance stream is generated when the document is saved.
    /// </summary>
    public class HpdfShapeAnnotation : HpdfGeometricAnnotation
    {
        private int _cloudIntensity;
        private HpdfRectDifference? _rectDifference;

        /// <summary>
        /// Creates a square or circle annotation (use <see cref="HpdfPage.CreateSquareAnnotation"/> or
        /// <see cref="HpdfPage.CreateCircleAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="type"><see cref="HpdfAnnotationType.Square"/> or <see cref="HpdfAnnotationType.Circle"/>.</param>
        /// <param name="rect">The annotation rectangle; the shape is drawn inside it.</param>
        /// <param name="text">The text of the annotation (/Contents), shown in its pop-up window; may be empty.</param>
        internal HpdfShapeAnnotation(HpdfXref xref, HpdfAnnotationType type, HpdfRect rect, string text)
            : base(xref, ValidateType(type), rect)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            SetRgbColor(1, 0, 0);
        }

        private static HpdfAnnotationType ValidateType(HpdfAnnotationType type) =>
            type is HpdfAnnotationType.Square or HpdfAnnotationType.Circle
                ? type
                : throw new HpdfException(HpdfErrorCode.InvalidParameter, "The type must be Square or Circle");

        /// <summary>
        /// Draws the border as a cloud (/BE, PDF 1.5). The bumps lie inside the annotation rectangle,
        /// which becomes the shape enlarged by the bumps (/RD is set to match unless
        /// <see cref="SetRectDifference"/> was called).
        /// </summary>
        /// <param name="intensity">0 (no cloud), 1 or 2 (larger bumps).</param>
        public void SetCloudEffect(int intensity)
        {
            HpdfCloudyBorder.WriteEffect(_dict, intensity);
            _cloudIntensity = intensity;
            if (intensity > 0)
                _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Sets the distances from the edges of the annotation rectangle to the shape (/RD, PDF 1.5).
        /// Without it the shape fills the rectangle (with a cloudy border: the rectangle less the bumps).
        /// </summary>
        /// <param name="left">Distance from the left edge.</param>
        /// <param name="top">Distance from the top edge.</param>
        /// <param name="right">Distance from the right edge.</param>
        /// <param name="bottom">Distance from the bottom edge.</param>
        public void SetRectDifference(float left, float top, float right, float bottom)
        {
            _rectDifference = HpdfRectDifference.Create(Rect, left, top, right, bottom);
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            var difference = GetRectDifference();
            var (left, top, right, bottom) = difference;
            if (_rectDifference is null && _cloudIntensity == 0)
                _dict.Remove("RD");
            else
                _dict["RD"] = difference.ToArray();

            if (!HasStroke && !HasFill)
                return;

            // The shape: the rectangle less /RD; a plain border is stroked inside it
            double shapeLeft = Rect.Left + left;
            double shapeBottom = Rect.Bottom + bottom;
            double shapeRight = Rect.Right - right;
            double shapeTop = Rect.Top - top;

            canvas.GSave();
            SetPaint(canvas);

            if (_cloudIntensity > 0)
            {
                double radius = HpdfCloudyBorder.GetRadius(_cloudIntensity);
                bool drawn = Type == HpdfAnnotationType.Square
                    ? HpdfCloudyBorder.AppendRectangle(canvas, shapeLeft, shapeBottom, shapeRight, shapeTop, radius)
                    : HpdfCloudyBorder.AppendEllipse(canvas, (shapeLeft + shapeRight) / 2, (shapeBottom + shapeTop) / 2,
                        (shapeRight - shapeLeft) / 2, (shapeTop - shapeBottom) / 2, radius);
                if (drawn)
                    PaintPath(canvas);
            }
            else
            {
                double inset = HasStroke ? BorderWidth / 2 : 0;
                double width = shapeRight - shapeLeft - 2 * inset;
                double height = shapeTop - shapeBottom - 2 * inset;
                if (width > 0 && height > 0)
                {
                    if (Type == HpdfAnnotationType.Square)
                        canvas.Rectangle((float)(shapeLeft + inset), (float)(shapeBottom + inset), (float)width, (float)height);
                    else
                        canvas.Ellipse((float)((shapeLeft + shapeRight) / 2), (float)((shapeBottom + shapeTop) / 2),
                            (float)(width / 2), (float)(height / 2));
                    PaintPath(canvas);
                }
            }

            canvas.GRestore();
        }

        /// <summary>
        /// Gets the distances from the edges of the rectangle to the shape: the ones set, or with a cloudy
        /// border the room of the bumps (their radius and half the line width).
        /// </summary>
        private HpdfRectDifference GetRectDifference()
        {
            if (_rectDifference is { } difference)
                return difference;
            if (_cloudIntensity == 0)
                return HpdfRectDifference.None;

            return HpdfRectDifference.Uniform(HpdfCloudyBorder.GetRadius(_cloudIntensity) + (HasStroke ? BorderWidth / 2 : 0));
        }
    }
}
