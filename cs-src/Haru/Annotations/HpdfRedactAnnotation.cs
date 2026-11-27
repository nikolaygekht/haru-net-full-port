using System;
using System.Globalization;
using Haru.Doc;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Redaction annotation (PDF 1.7, not in the C library): marks content to be removed. The library does not
    /// remove anything; a viewer that applies redactions (such as Acrobat) removes the content under the
    /// annotation and fills the area with the interior color, optionally with an overlay text. Create it with
    /// <see cref="HpdfPage.CreateRedactAnnotation"/>. The appearance stream, generated when the document is saved,
    /// shows the marked area as an outline in the annotation color (red until changed). PDF/A-1 does not allow it.
    /// </summary>
    public class HpdfRedactAnnotation : HpdfMarkupAnnotation
    {
        private HpdfQuad[]? _quads;
        private HpdfFont? _overlayFont;
        private float _overlayFontSize;
        private HpdfAnnotationColor _overlayTextColor = HpdfAnnotationColor.Rgb(1, 0, 0);

        /// <summary>
        /// Creates a redaction annotation (use <see cref="HpdfPage.CreateRedactAnnotation"/>).
        /// </summary>
        internal HpdfRedactAnnotation(HpdfXref xref, HpdfRect rect, string text)
            : base(xref, HpdfAnnotationType.Redact, rect, HpdfVersion.Version17)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            SetRgbColor(1, 0, 0);
        }

        /// <summary>
        /// Gets the color that fills the redacted area (/IC), or null.
        /// </summary>
        internal HpdfAnnotationColor? InteriorColor { get; private set; }

        /// <summary>
        /// Sets the areas to redact as rectangles, for example one per line of text (/QuadPoints); without them
        /// the annotation rectangle is redacted.
        /// </summary>
        /// <param name="areas">The rectangles.</param>
        public void SetQuadPoints(params HpdfRect[] areas)
        {
            _quads = HpdfQuad.FromRects(areas);
            _dict["QuadPoints"] = HpdfQuad.ToArray(_quads);
        }

        /// <summary>
        /// Sets the area to redact as one quadrilateral (/QuadPoints), for an area that is not horizontal.
        /// </summary>
        public void SetQuadPoints(HpdfPoint leftBottom, HpdfPoint rightBottom, HpdfPoint leftTop, HpdfPoint rightTop)
        {
            _quads = new[] { HpdfQuad.FromPoints(leftBottom, rightBottom, leftTop, rightTop) };
            _dict["QuadPoints"] = HpdfQuad.ToArray(_quads);
        }

        /// <summary>
        /// Sets an RGB color that fills the area once it is redacted (/IC).
        /// </summary>
        public void SetInteriorRgbColor(float r, float g, float b) => SetInteriorColor(HpdfAnnotationColor.Rgb(r, g, b));

        /// <summary>
        /// Sets a CMYK color that fills the area once it is redacted (/IC).
        /// </summary>
        public void SetInteriorCmykColor(float c, float m, float y, float k) => SetInteriorColor(HpdfAnnotationColor.Cmyk(c, m, y, k));

        /// <summary>
        /// Sets a gray color that fills the area once it is redacted (/IC).
        /// </summary>
        public void SetInteriorGrayColor(float gray) => SetInteriorColor(HpdfAnnotationColor.Gray(gray));

        /// <summary>
        /// Leaves the redacted area unfilled (an empty /IC array).
        /// </summary>
        public void SetInteriorTransparent() => SetInteriorColor(HpdfAnnotationColor.Transparent);

        private void SetInteriorColor(HpdfAnnotationColor color)
        {
            InteriorColor = color;
            _dict["IC"] = color.ToArray();
        }

        /// <summary>
        /// Sets the text shown in the redacted area once it is redacted (/OverlayText, /DA, /Q, /Repeat).
        /// </summary>
        /// <param name="text">The overlay text.</param>
        /// <param name="font">The font of the text (any font of the document; not a vertical one).</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="alignment">Left, center or right.</param>
        /// <param name="repeat">Whether the text is repeated to fill the area.</param>
        public void SetOverlayText(string text, HpdfFont font, float fontSize,
            HpdfTextAlignment alignment = HpdfTextAlignment.Left, bool repeat = false)
        {
            if (text is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Text cannot be null");
            HpdfFreeTextAnnotation.ValidateFont(font, fontSize);
            int quadding = alignment switch
            {
                HpdfTextAlignment.Left => 0,
                HpdfTextAlignment.Center => 1,
                HpdfTextAlignment.Right => 2,
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Overlay text cannot be aligned {alignment}")
            };

            _overlayFont = font;
            _overlayFontSize = fontSize;
            _dict["OverlayText"] = new HpdfString(text);
            _dict["Q"] = new HpdfNumber(quadding);
            if (repeat)
                _dict["Repeat"] = new HpdfBoolean(true);
            else
                _dict.Remove("Repeat");
            WriteDefaultAppearance();
        }

        /// <summary>
        /// Sets the color of the overlay text (red by default).
        /// </summary>
        public void SetOverlayTextRgbColor(float r, float g, float b)
        {
            _overlayTextColor = HpdfAnnotationColor.Rgb(r, g, b);
            WriteDefaultAppearance();
        }

        /// <summary>
        /// Removes the overlay text.
        /// </summary>
        public void RemoveOverlayText()
        {
            _overlayFont = null;
            _dict.Remove("OverlayText");
            _dict.Remove("DA");
            _dict.Remove("Q");
            _dict.Remove("Repeat");
        }

        internal override HpdfFont? DefaultAppearanceFont => _overlayFont;

        private void WriteDefaultAppearance()
        {
            if (_overlayFont is null)
                return;

            string size = _overlayFontSize.ToString("0.####", CultureInfo.InvariantCulture);
            _dict["DA"] = new HpdfString($"/{_overlayFont.LocalName} {size} Tf {_overlayTextColor.ToFillOperator()}");
        }

        private protected override HpdfRect GetAppearanceRect() =>
            _quads is null ? Rect : HpdfQuad.Bounds(_quads, 1);

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color)
                return;

            canvas.GSave();
            color.SetStroke(canvas);
            canvas.SetLineWidth(1);

            if (_quads is null)
            {
                float width = Rect.Right - Rect.Left - 1;
                float height = Rect.Top - Rect.Bottom - 1;
                if (width > 0 && height > 0)
                {
                    canvas.Rectangle(Rect.Left + 0.5f, Rect.Bottom + 0.5f, width, height);
                    canvas.Stroke();
                }
            }
            else
            {
                foreach (var quad in _quads)
                    quad.AppendPath(canvas);
                canvas.Stroke();
            }

            canvas.GRestore();
        }
    }
}
