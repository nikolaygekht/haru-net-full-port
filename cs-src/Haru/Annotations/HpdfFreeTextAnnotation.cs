using System;
using System.Collections.Generic;
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
    /// Free text annotation (PDF 1.3): text shown directly on the page in a box, optionally with a callout
    /// line that points at an area of the page. Create it with <see cref="HpdfPage.CreateFreeTextAnnotation"/>.
    /// The text, the border and the callout line are drawn in the text color (as Acrobat does: the color of
    /// the default appearance string), the background in the annotation color (/C; none until set). The
    /// appearance stream is generated when the document is saved, with any font of the document.
    /// </summary>
    public class HpdfFreeTextAnnotation : HpdfMarkupAnnotation
    {
        /// <summary>
        /// Space between the border and the text.
        /// </summary>
        private const float Padding = 2f;

        /// <summary>
        /// Smallest line height, per font size.
        /// </summary>
        private const float MinLineHeight = 1.15f;

        private readonly string _text;
        private HpdfRect _textBox;
        private HpdfFont _font;
        private float _fontSize;
        private HpdfAnnotationColor _textColor = HpdfAnnotationColor.Gray(0);
        private HpdfTextAlignment _alignment = HpdfTextAlignment.Left;
        private HpdfFreeTextIntent _intent = HpdfFreeTextIntent.Plain;
        private HpdfPoint[]? _callout;
        private HpdfLineEndingStyle _calloutEnding;
        private int _cloudIntensity;

        /// <summary>
        /// Creates a free text annotation (use <see cref="HpdfPage.CreateFreeTextAnnotation"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The text box.</param>
        /// <param name="text">The text (/Contents); may be empty.</param>
        /// <param name="font">The font of the text.</param>
        /// <param name="fontSize">The font size.</param>
        internal HpdfFreeTextAnnotation(HpdfXref xref, HpdfRect rect, string text, HpdfFont font, float fontSize)
            : base(xref, HpdfAnnotationType.FreeText, rect, HpdfVersion.Version13)
        {
            ArgumentNullException.ThrowIfNull(text);
            ValidateFont(font, fontSize);

            _text = text;
            _textBox = Rect;
            _font = font;
            _fontSize = fontSize;
            _dict.Add("Contents", new HpdfString(text));
            WriteDefaultAppearance();
        }

        /// <summary>
        /// Checks the font arguments (before the annotation object is created).
        /// </summary>
        internal static void ValidateFont(HpdfFont font, float fontSize)
        {
            if (font is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Font cannot be null");
            if (font.WritingMode == HpdfWritingMode.Vertical)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Free text needs a font of horizontal writing");
            if (!(fontSize > 0) || !float.IsFinite(fontSize))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The font size must be a positive number");
        }

        /// <summary>
        /// Sets the font and the font size of the text.
        /// </summary>
        /// <param name="font">The font (any font of the document; not a vertical one).</param>
        /// <param name="fontSize">The font size.</param>
        public void SetFont(HpdfFont font, float fontSize)
        {
            ValidateFont(font, fontSize);
            _font = font;
            _fontSize = fontSize;
            WriteDefaultAppearance();
        }

        /// <summary>
        /// Sets an RGB color of the text, the border and the callout line (black by default).
        /// </summary>
        public void SetTextRgbColor(float r, float g, float b) => SetTextColor(HpdfAnnotationColor.Rgb(r, g, b));

        /// <summary>
        /// Sets a CMYK color of the text, the border and the callout line.
        /// </summary>
        public void SetTextCmykColor(float c, float m, float y, float k) => SetTextColor(HpdfAnnotationColor.Cmyk(c, m, y, k));

        /// <summary>
        /// Sets a gray color of the text, the border and the callout line.
        /// </summary>
        public void SetTextGrayColor(float gray) => SetTextColor(HpdfAnnotationColor.Gray(gray));

        private void SetTextColor(HpdfAnnotationColor color)
        {
            _textColor = color;
            WriteDefaultAppearance();
        }

        /// <summary>
        /// Sets the horizontal alignment of the text in the box (/Q, PDF 1.4): left, center or right.
        /// </summary>
        /// <param name="alignment">The alignment; justified text is not available in free text.</param>
        public void SetAlignment(HpdfTextAlignment alignment)
        {
            int quadding = alignment switch
            {
                HpdfTextAlignment.Left => 0,
                HpdfTextAlignment.Center => 1,
                HpdfTextAlignment.Right => 2,
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Free text cannot be aligned {alignment}")
            };

            _alignment = alignment;
            _dict["Q"] = new HpdfNumber(quadding);
            _xref.RequireVersion(HpdfVersion.Version14);
        }

        /// <summary>
        /// Sets the default style string (/DS, PDF 1.5): CSS text properties that viewers use when the user
        /// edits the text, such as <c>font: 12pt Helvetica; color: #FF0000</c>. The generated appearance
        /// does not use it.
        /// </summary>
        /// <param name="style">The style string.</param>
        public void SetDefaultStyle(string style)
        {
            if (style is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Style cannot be null");

            _dict["DS"] = new HpdfString(style);
            _xref.RequireVersion(HpdfVersion.Version15);
        }

        /// <summary>
        /// Sets a callout line of two points (/CL, PDF 1.6) and the intent FreeTextCallout. The annotation
        /// rectangle grows to enclose the line; the text box stays where it is (/RD).
        /// </summary>
        /// <param name="start">The point the callout points at (where the line ending is drawn).</param>
        /// <param name="end">The point at the text box.</param>
        public void SetCallout(HpdfPoint start, HpdfPoint end) => SetCallout(new[] { start, end });

        /// <summary>
        /// Sets a callout line of three points (/CL, PDF 1.6) and the intent FreeTextCallout. The annotation
        /// rectangle grows to enclose the line; the text box stays where it is (/RD).
        /// </summary>
        /// <param name="start">The point the callout points at (where the line ending is drawn).</param>
        /// <param name="knee">The point where the line bends.</param>
        /// <param name="end">The point at the text box.</param>
        public void SetCallout(HpdfPoint start, HpdfPoint knee, HpdfPoint end) => SetCallout(new[] { start, knee, end });

        private void SetCallout(HpdfPoint[] points)
        {
            var array = new HpdfArray();
            foreach (var point in points)
            {
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y))
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "Callout points must be finite numbers");
                array.Add(new HpdfReal(point.X));
                array.Add(new HpdfReal(point.Y));
            }

            _callout = points;
            _dict["CL"] = array;
            SetIntent(HpdfFreeTextIntent.Callout);
        }

        /// <summary>
        /// Removes the callout line (and its line ending); the intent becomes a plain text box.
        /// </summary>
        public void RemoveCallout()
        {
            if (_callout is null)
                return;

            // The plain intent removes the callout
            SetIntent(HpdfFreeTextIntent.Plain);
        }

        /// <summary>
        /// Sets the line ending at the start of the callout line (/LE, PDF 1.6); closed endings are filled
        /// with the text color.
        /// </summary>
        /// <param name="style">The line ending.</param>
        public void SetLineEndingStyle(HpdfLineEndingStyle style)
        {
            _dict["LE"] = new HpdfName(HpdfLineEndings.GetName(style));
            _calloutEnding = style;
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        /// <summary>
        /// Draws the border of the text box as a cloud (/BE, PDF 1.6). The bumps lie outside the text box:
        /// the annotation rectangle grows by them, and /RD keeps the text box.
        /// </summary>
        /// <param name="intensity">0 (no cloud), 1 or 2 (larger bumps).</param>
        public void SetCloudEffect(int intensity)
        {
            HpdfCloudyBorder.WriteEffect(_dict, intensity);
            _cloudIntensity = intensity;
            if (intensity > 0)
                _xref.RequireVersion(HpdfVersion.Version16);
            else if (_callout is null)
            {
                _dict.Remove("RD");
                SetRect(_textBox);
            }
        }

        /// <summary>
        /// Sets how the annotation is meant to be shown (/IT, PDF 1.6). A typewriter annotation has no border.
        /// An intent other than <see cref="HpdfFreeTextIntent.Callout"/> removes the callout line.
        /// </summary>
        /// <param name="intent">The intent.</param>
        public void SetIntent(HpdfFreeTextIntent intent)
        {
            string name = intent switch
            {
                HpdfFreeTextIntent.Plain => "FreeText",
                HpdfFreeTextIntent.Callout => "FreeTextCallout",
                HpdfFreeTextIntent.TypeWriter => "FreeTextTypeWriter",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Invalid free text intent: {intent}")
            };

            if (intent != HpdfFreeTextIntent.Callout && _callout != null)
            {
                _callout = null;
                _dict.Remove("CL");
                _dict.Remove("LE");
                if (_cloudIntensity == 0)
                {
                    _dict.Remove("RD");
                    SetRect(_textBox);
                }
            }

            _intent = intent;
            _dict["IT"] = new HpdfName(name);
            _xref.RequireVersion(HpdfVersion.Version16);
        }

        /// <summary>
        /// Writes the default appearance string (/DA): the font, the size and the text color.
        /// </summary>
        private void WriteDefaultAppearance()
        {
            string size = _fontSize.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
            _dict["DA"] = new HpdfString($"/{_font.LocalName} {size} Tf {_textColor.ToFillOperator()}");
        }

        internal override HpdfFont? DefaultAppearanceFont => _font;

        private bool HasBorder => BorderWidth > 0 && _intent != HpdfFreeTextIntent.TypeWriter;

        /// <summary>
        /// The room of the cloud's bumps outside the text box: their radius and half the border width.
        /// </summary>
        private float CloudMargin => _cloudIntensity == 0 ? 0 : HpdfCloudyBorder.GetRadius(_cloudIntensity) + BorderWidth / 2;

        private protected override HpdfRect GetAppearanceRect()
        {
            float margin = CloudMargin;
            double left = _textBox.Left - margin, bottom = _textBox.Bottom - margin;
            double right = _textBox.Right + margin, top = _textBox.Top + margin;
            if (_callout is null)
                return new HpdfRect((float)left, (float)bottom, (float)right, (float)top);

            double size = HpdfLineEndings.GetSize(CalloutWidth);
            double pad = CalloutWidth + 1;
            for (int i = 0; i < _callout.Length; i++)
            {
                // The ending at the start point reaches up to its size around the point
                double reach = i == 0 && _calloutEnding != HpdfLineEndingStyle.None ? size : 0;
                left = Math.Min(left, _callout[i].X - reach - pad);
                bottom = Math.Min(bottom, _callout[i].Y - reach - pad);
                right = Math.Max(right, _callout[i].X + reach + pad);
                top = Math.Max(top, _callout[i].Y + reach + pad);
            }

            return new HpdfRect((float)left, (float)bottom, (float)right, (float)top);
        }

        /// <summary>
        /// The width of the callout line: the border width, at least 1 (also without a border).
        /// </summary>
        private float CalloutWidth => Math.Max(1, BorderWidth);

        private protected override void DrawAppearance(HpdfAppearanceCanvas canvas, HpdfAnnotationWriteContext context)
        {
            if (_callout != null || _cloudIntensity > 0)
            {
                // ISO 32000-1, 12.5.6.6: left, top, right and bottom, from /Rect to the text box
                var difference = new HpdfArray();
                difference.Add(new HpdfReal(_textBox.Left - Rect.Left));
                difference.Add(new HpdfReal(Rect.Top - _textBox.Top));
                difference.Add(new HpdfReal(Rect.Right - _textBox.Right));
                difference.Add(new HpdfReal(_textBox.Bottom - Rect.Bottom));
                _dict["RD"] = difference;
            }

            canvas.GSave();

            if (_callout != null)
                DrawCallout(canvas);

            float width = _textBox.Right - _textBox.Left;
            float height = _textBox.Top - _textBox.Bottom;

            float inset = 0;
            if (_cloudIntensity > 0)
            {
                DrawCloud(canvas);
                inset = HasBorder ? BorderWidth : 0;
            }
            else
            {
                if (Color is { IsTransparent: false } background)
                {
                    background.SetFill(canvas);
                    canvas.Rectangle(_textBox.Left, _textBox.Bottom, width, height);
                    canvas.Fill();
                }
            }

            if (_cloudIntensity == 0 && HasBorder)
            {
                inset = BorderWidth;
                if (width > BorderWidth && height > BorderWidth)
                {
                    _textColor.SetStroke(canvas);
                    canvas.SetLineWidth(BorderWidth);
                    if (BorderStyle == HpdfBorderStyle.Dashed)
                        canvas.SetDash(new[] { BorderDash.On, BorderDash.Off }, 0);
                    canvas.Rectangle(_textBox.Left + BorderWidth / 2, _textBox.Bottom + BorderWidth / 2,
                        width - BorderWidth, height - BorderWidth);
                    canvas.Stroke();
                }
            }

            DrawText(canvas, inset + Padding);
            canvas.GRestore();
        }

        /// <summary>
        /// Draws the cloud around the text box: filled with the background color, stroked in the text color.
        /// </summary>
        private void DrawCloud(HpdfAppearanceCanvas canvas)
        {
            bool fill = Color is { IsTransparent: false };
            bool stroke = HasBorder;
            if (!fill && !stroke)
                return;

            canvas.GSave();
            if (fill)
                Color!.SetFill(canvas);
            if (stroke)
            {
                _textColor.SetStroke(canvas);
                canvas.SetLineWidth(BorderWidth);
                if (BorderStyle == HpdfBorderStyle.Dashed)
                    canvas.SetDash(new[] { BorderDash.On, BorderDash.Off }, 0);
            }

            if (HpdfCloudyBorder.AppendRectangle(canvas, _textBox.Left, _textBox.Bottom, _textBox.Right, _textBox.Top,
                    HpdfCloudyBorder.GetRadius(_cloudIntensity)))
            {
                if (fill && stroke)
                    canvas.FillStroke();
                else if (fill)
                    canvas.Fill();
                else
                    canvas.Stroke();
            }
            canvas.GRestore();
        }

        private void DrawCallout(HpdfAppearanceCanvas canvas)
        {
            var points = _callout!;
            _textColor.SetStroke(canvas);
            _textColor.SetFill(canvas);
            canvas.SetLineWidth(CalloutWidth);

            canvas.MoveTo(points[0].X, points[0].Y);
            for (int i = 1; i < points.Length; i++)
                canvas.LineTo(points[i].X, points[i].Y);
            canvas.Stroke();

            // The ending points away from the line: from the next point to the start point
            double dx = points[0].X - points[1].X;
            double dy = points[0].Y - points[1].Y;
            double length = Math.Sqrt(dx * dx + dy * dy);
            if (length == 0)
                return;

            bool? closed = HpdfLineEndings.Append(canvas, _calloutEnding, points[0].X, points[0].Y,
                dx / length, dy / length, HpdfLineEndings.GetSize(CalloutWidth));
            if (closed == true)
                canvas.FillStroke();
            else if (closed == false)
                canvas.Stroke();
        }

        private void DrawText(HpdfAppearanceCanvas canvas, float inset)
        {
            float lineWidth = _textBox.Right - _textBox.Left - 2 * inset;
            float boxHeight = _textBox.Top - _textBox.Bottom - 2 * inset;
            if (_text.Length == 0 || lineWidth <= 0 || boxHeight <= 0)
                return;

            UseAppearanceFont(_font);
            List<string> lines = HpdfTextLines.Wrap(_font, _text, _fontSize, lineWidth);
            // The typographic line height (not the bounding box, which is very tall in CJK fonts)
            float lineHeight = Math.Max((_font.GetAscent() - _font.GetDescent()) / 1000f, MinLineHeight) * _fontSize;
            float baseline = _textBox.Top - inset - _font.GetAscent() / 1000f * _fontSize;
            float left = _textBox.Left + inset;

            // Clipped to the inside of the border
            canvas.Rectangle(left, _textBox.Bottom + inset, lineWidth, boxHeight);
            canvas.Clip();
            canvas.EndPath();

            var stream = canvas.Stream;
            stream.WriteString("BT\n");
            _textColor.SetFill(canvas);
            stream.WriteEscapedName(_font.LocalName);
            stream.WriteChar(' ');
            stream.WriteReal(_fontSize);
            stream.WriteString(" Tf\n");

            foreach (string line in lines)
            {
                // Lines below the box are clipped anyway
                if (baseline < _textBox.Bottom - lineHeight)
                    break;

                if (line.Length > 0)
                {
                    float x = _alignment switch
                    {
                        HpdfTextAlignment.Center => left + (lineWidth - _font.MeasureText(line, _fontSize)) / 2,
                        HpdfTextAlignment.Right => left + lineWidth - _font.MeasureText(line, _fontSize),
                        _ => left
                    };

                    stream.WriteString("1 0 0 1 ");
                    stream.WriteReal(x);
                    stream.WriteChar(' ');
                    stream.WriteReal(baseline);
                    stream.WriteString(" Tm\n");
                    _font.WriteTextOperand(stream, line);
                    stream.WriteString(" Tj\n");
                }

                baseline -= lineHeight;
            }

            stream.WriteString("ET\n");
        }
    }
}
