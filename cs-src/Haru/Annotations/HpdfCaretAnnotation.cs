using System;
using Haru.Doc;
using Haru.Graphics;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Caret annotation (PDF 1.5, not in the C library): a caret that marks where text is to be inserted, the text
    /// to insert being the text of the annotation. Create it with <see cref="HpdfPage.CreateCaretAnnotation"/>.
    /// The appearance stream is generated when the document is saved: a caret with curved sides filled with the
    /// annotation color (blue until changed), and a pilcrow beside it for a new paragraph.
    /// </summary>
    public class HpdfCaretAnnotation : HpdfMarkupAnnotation
    {
        private HpdfCaretSymbol _symbol;
        private HpdfRectDifference? _rectDifference;

        /// <summary>
        /// Creates a caret annotation (use <see cref="HpdfPage.CreateCaretAnnotation"/>).
        /// </summary>
        internal HpdfCaretAnnotation(HpdfXref xref, HpdfRect rect, string text)
            : base(xref, HpdfAnnotationType.Caret, rect, HpdfVersion.Version15)
        {
            ArgumentNullException.ThrowIfNull(text);
            _dict.Add("Contents", new HpdfString(text));
            SetRgbColor(0, 0, 1);
        }

        /// <summary>
        /// Gets the symbol associated with the caret.
        /// </summary>
        public HpdfCaretSymbol Symbol => _symbol;

        /// <summary>
        /// Sets the symbol associated with the caret (/Sy): none, or a pilcrow for a new paragraph.
        /// </summary>
        /// <param name="symbol">The symbol.</param>
        public void SetSymbol(HpdfCaretSymbol symbol)
        {
            string name = symbol switch
            {
                HpdfCaretSymbol.None => "None",
                HpdfCaretSymbol.Paragraph => "P",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid caret symbol: {symbol}")
            };

            _symbol = symbol;
            _dict["Sy"] = new HpdfName(name);
        }

        /// <summary>
        /// Sets the distances from the edges of the annotation rectangle to the caret (/RD).
        /// </summary>
        /// <param name="left">Distance from the left edge.</param>
        /// <param name="top">Distance from the top edge.</param>
        /// <param name="right">Distance from the right edge.</param>
        /// <param name="bottom">Distance from the bottom edge.</param>
        public void SetRectDifference(float left, float top, float right, float bottom)
        {
            var difference = HpdfRectDifference.Create(Rect, left, top, right, bottom);
            _rectDifference = difference;
            _dict["RD"] = difference.ToArray();
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color)
                return;

            var box = (_rectDifference ?? HpdfRectDifference.None).Inside(Rect);
            float width = box.Right - box.Left;
            float height = box.Top - box.Bottom;
            if (width <= 0 || height <= 0)
                return;

            canvas.GSave();
            color.SetFill(canvas);

            // With a pilcrow, the caret takes the left part of the box
            float caretWidth = _symbol == HpdfCaretSymbol.Paragraph ? width * 0.55f : width;
            AppendCaret(canvas, box.Left, box.Bottom, caretWidth, height);
            canvas.Fill();

            if (_symbol == HpdfCaretSymbol.Paragraph)
            {
                AppendPilcrow(canvas, box.Left + caretWidth, box.Bottom, width - caretWidth, height);
                canvas.Fill();
            }

            canvas.GRestore();
        }

        /// <summary>
        /// Appends a caret: a point at the top center, sides curving in to the bottom corners.
        /// </summary>
        private static void AppendCaret(IDrawable canvas, float left, float bottom, float width, float height)
        {
            float right = left + width;
            float top = bottom + height;
            float center = left + width / 2;

            canvas.MoveTo(left, bottom);
            canvas.CurveTo(left + width * 0.3f, bottom + height * 0.25f, center - width * 0.12f, top - height * 0.35f, center, top);
            canvas.CurveTo(center + width * 0.12f, top - height * 0.35f, right - width * 0.3f, bottom + height * 0.25f, right, bottom);
            canvas.CurveTo(right - width * 0.25f, bottom + height * 0.08f, left + width * 0.25f, bottom + height * 0.08f, left, bottom);
            canvas.ClosePath();
        }

        /// <summary>
        /// Appends a pilcrow (paragraph sign): a bowl at the top left of two stems.
        /// </summary>
        private static void AppendPilcrow(IDrawable canvas, float left, float bottom, float width, float height)
        {
            float pad = width * 0.1f;
            float x0 = left + pad;
            float usable = width - 2 * pad;
            float stem = Math.Max(usable * 0.12f, 0.3f);
            float stem1 = x0 + usable * 0.5f;
            float stem2 = x0 + usable - stem;
            float top = bottom + height;
            float bowlBottom = top - height * 0.5f;
            float radius = (top - bowlBottom) / 2;

            // The two stems
            canvas.Rectangle(stem1, bottom, stem, height);
            canvas.Rectangle(stem2, bottom, stem, height);

            // The bowl: a half disk left of the first stem, as wide as the space allows
            const float Kappa = 0.5523f;
            float bowlWidth = Math.Min(radius, stem1 - x0);
            float centerY = bowlBottom + radius;
            canvas.MoveTo(stem1, top);
            canvas.CurveTo(stem1 - bowlWidth * Kappa, top, stem1 - bowlWidth, centerY + radius * Kappa, stem1 - bowlWidth, centerY);
            canvas.CurveTo(stem1 - bowlWidth, centerY - radius * Kappa, stem1 - bowlWidth * Kappa, bowlBottom, stem1, bowlBottom);
            canvas.ClosePath();
        }
    }
}
