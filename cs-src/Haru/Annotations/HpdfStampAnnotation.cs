using System;
using Haru.Doc;
using Haru.Font;
using Haru.Graphics;
using Haru.Objects;
using Haru.Streams;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Rubber stamp annotation (PDF 1.3): one of the standard stamps (Approved, Draft, Confidential, ...)
    /// shown in a box. Create it with <see cref="HpdfPage.CreateStampAnnotation"/>. The appearance stream is
    /// generated when the document is saved: a frame with rounded corners and the stamp's label in capitals,
    /// in the annotation color (green, red or blue by the meaning of the stamp until changed).
    /// </summary>
    public class HpdfStampAnnotation : HpdfMarkupAnnotation
    {
        private static readonly string[] Names =
        {
            "Approved", "Experimental", "NotApproved", "AsIs", "Expired", "NotForPublicRelease", "Confidential",
            "Final", "Sold", "Departmental", "ForComment", "TopSecret", "Draft", "ForPublicRelease"
        };

        private static readonly string[] Labels =
        {
            "APPROVED", "EXPERIMENTAL", "NOT APPROVED", "AS IS", "EXPIRED", "NOT FOR PUBLIC RELEASE", "CONFIDENTIAL",
            "FINAL", "SOLD", "DEPARTMENTAL", "FOR COMMENT", "TOP SECRET", "DRAFT", "FOR PUBLIC RELEASE"
        };

        private readonly HpdfStampName _name;
        private HpdfFont _font;

        /// <summary>
        /// Creates a stamp annotation (use <see cref="HpdfPage.CreateStampAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The box of the stamp.</param>
        /// <param name="name">The standard stamp.</param>
        /// <param name="text">The text of the annotation (/Contents), shown in its pop-up window; may be empty.</param>
        /// <param name="font">The font of the label.</param>
        internal HpdfStampAnnotation(HpdfXref xref, HpdfRect rect, HpdfStampName name, string text, HpdfFont font)
            : base(xref, HpdfAnnotationType.Stamp, rect, HpdfVersion.Version13)
        {
            ArgumentNullException.ThrowIfNull(text);
            ValidateArguments(name, font);

            _name = name;
            _font = font;
            _dict.Add("Name", new HpdfName(Names[(int)name]));
            _dict.Add("Contents", new HpdfString(text));

            switch (name)
            {
                case HpdfStampName.Approved:
                case HpdfStampName.Final:
                case HpdfStampName.Sold:
                case HpdfStampName.ForPublicRelease:
                    SetRgbColor(0.1f, 0.5f, 0.1f);
                    break;
                case HpdfStampName.NotApproved:
                case HpdfStampName.Expired:
                case HpdfStampName.NotForPublicRelease:
                case HpdfStampName.Confidential:
                case HpdfStampName.TopSecret:
                    SetRgbColor(0.75f, 0.1f, 0.1f);
                    break;
                default:
                    SetRgbColor(0.1f, 0.2f, 0.6f);
                    break;
            }
        }

        /// <summary>
        /// Checks the arguments (before the annotation object is created).
        /// </summary>
        internal static void ValidateArguments(HpdfStampName name, HpdfFont font)
        {
            if ((int)name < 0 || (int)name >= Names.Length)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid stamp name: {name}");
            ValidateFont(font);
        }

        private static void ValidateFont(HpdfFont font)
        {
            if (font is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Font cannot be null");
            if (font.WritingMode == HpdfWritingMode.Vertical)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The stamp needs a font of horizontal writing");
        }

        /// <summary>
        /// Gets the standard stamp.
        /// </summary>
        public HpdfStampName Name => _name;

        /// <summary>
        /// Sets the font of the label.
        /// </summary>
        /// <param name="font">The font (any font of the document; not a vertical one).</param>
        public void SetFont(HpdfFont font)
        {
            ValidateFont(font);
            _font = font;
        }

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (Color is not { IsTransparent: false } color)
                return;

            float width = Rect.Right - Rect.Left;
            float height = Rect.Top - Rect.Bottom;
            if (width <= 0 || height <= 0)
                return;

            // A frame of a twelfth of the height (at least one point), rounded with a quarter of the height
            float lineWidth = Math.Max(1, Math.Min(width, height) / 12);
            float left = Rect.Left + lineWidth / 2;
            float bottom = Rect.Bottom + lineWidth / 2;
            float right = Rect.Right - lineWidth / 2;
            float top = Rect.Top - lineWidth / 2;
            float radius = Math.Min((top - bottom) / 4, (right - left) / 2);

            canvas.GSave();
            color.SetStroke(canvas);
            color.SetFill(canvas);
            canvas.SetLineWidth(lineWidth);
            AppendRoundedRectangle(canvas, left, bottom, right, top, radius);
            canvas.Stroke();

            // The label: as large as fits into the frame, centered
            string label = Labels[(int)_name];
            float innerWidth = right - left - 2 * (lineWidth + radius / 2);
            float innerHeight = top - bottom - 2 * lineWidth;
            float ascent = _font.GetAscent() / 1000f;
            float descent = _font.GetDescent() / 1000f;
            float labelWidth = _font.MeasureText(label, 1);
            if (innerWidth > 0 && innerHeight > 0 && labelWidth > 0 && ascent - descent > 0)
            {
                // The capitals fill up to 60 percent of the height
                float fontSize = Math.Min(innerWidth / labelWidth, innerHeight * 0.6f / ascent);
                float x = Rect.Left + (width - labelWidth * fontSize) / 2;
                float y = Rect.Bottom + (height - ascent * fontSize) / 2;

                UseAppearanceFont(_font);
                var stream = canvas.Stream;
                stream.WriteString("BT\n");
                stream.WriteEscapedName(_font.LocalName);
                stream.WriteChar(' ');
                stream.WriteReal(fontSize);
                stream.WriteString(" Tf\n1 0 0 1 ");
                stream.WriteReal(x);
                stream.WriteChar(' ');
                stream.WriteReal(y);
                stream.WriteString(" Tm\n");
                _font.WriteTextOperand(stream, label);
                stream.WriteString(" Tj\nET\n");
            }

            canvas.GRestore();
        }

        private static void AppendRoundedRectangle(IDrawable canvas, float left, float bottom, float right, float top, float radius)
        {
            // Bezier approximation of a quarter circle
            const float Kappa = 0.5523f;
            float k = radius * Kappa;

            canvas.MoveTo(left + radius, bottom);
            canvas.LineTo(right - radius, bottom);
            canvas.CurveTo(right - radius + k, bottom, right, bottom + radius - k, right, bottom + radius);
            canvas.LineTo(right, top - radius);
            canvas.CurveTo(right, top - radius + k, right - radius + k, top, right - radius, top);
            canvas.LineTo(left + radius, top);
            canvas.CurveTo(left + radius - k, top, left, top - radius + k, left, top - radius);
            canvas.LineTo(left, bottom + radius);
            canvas.CurveTo(left, bottom + radius - k, left + radius - k, bottom, left + radius, bottom);
            canvas.ClosePath();
        }
    }
}
