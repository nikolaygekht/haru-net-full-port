/* Draws the contents of text and choice field appearance streams (variable text). */
using System;
using System.Collections.Generic;
using Haru.Doc;
using Haru.Font;
using Haru.Graphics;
using Haru.Streams;

namespace Haru.Forms.Appearance
{
    /// <summary>
    /// Draws the variable text of text fields, combo boxes and list boxes into appearance streams,
    /// the way viewers lay it out: the text is inset by 2 points, clipped to the field and marked
    /// as /Tx content; the font, size and color come from the default appearance string.
    /// </summary>
    internal static class HpdfVariableTextAppearance
    {
        private const float Padding = 2f;
        private const float DefaultAutoSize = 12f;
        private const float MinAutoSize = 4f;
        private const float AutoSizeStep = 0.5f;
        private static readonly float[] SelectionColor = { 0.6f, 0.757f, 0.855f };

        /// <summary>
        /// Draws one line of text (single-line text field or combo box), centered vertically.
        /// An auto size (0) fits the text into the height and the width of the field.
        /// </summary>
        public static void DrawSingleLine(IDrawable canvas, float width, float height,
            HpdfDefaultAppearance appearance, HpdfFont font, string text, HpdfFieldAlignment alignment)
        {
            if (text.Length == 0)
            {
                WriteEmpty(canvas);
                return;
            }

            float fontSize = appearance.FontSize;
            if (fontSize == 0)
            {
                fontSize = Math.Max(MinAutoSize, (height - 2 * Padding) / LineHeightFactor(font));
                float textWidth = font.MeasureText(text, fontSize);
                if (textWidth > width - 2 * Padding && textWidth > 0)
                    fontSize = Math.Max(MinAutoSize, fontSize * (width - 2 * Padding) / textWidth);
            }

            float ascent = font.GetAscent() / 1000f * fontSize;
            float descent = font.GetDescent() / 1000f * fontSize;
            float baseline = (height - (ascent - descent)) / 2 - descent;

            BeginText(canvas, width, height, appearance, fontSize);
            ShowLine(canvas.Stream, font, text, LineX(font, text, fontSize, width, alignment), baseline);
            EndText(canvas);
        }

        /// <summary>
        /// Draws text wrapped into lines from the top of the field (multiline text field).
        /// An auto size (0) is the largest size from 12 points down that fits all lines into the field.
        /// </summary>
        public static void DrawMultiline(IDrawable canvas, float width, float height,
            HpdfDefaultAppearance appearance, HpdfFont font, string text, HpdfFieldAlignment alignment)
        {
            if (text.Length == 0)
            {
                WriteEmpty(canvas);
                return;
            }

            float lineWidth = width - 2 * Padding;
            float fontSize = appearance.FontSize;
            List<string> lines;

            if (fontSize == 0)
            {
                fontSize = DefaultAutoSize;
                lines = WrapLines(font, text, fontSize, lineWidth);
                while (fontSize > MinAutoSize && lines.Count * LineHeightFactor(font) * fontSize > height - 2 * Padding)
                {
                    fontSize -= AutoSizeStep;
                    lines = WrapLines(font, text, fontSize, lineWidth);
                }
            }
            else
            {
                lines = WrapLines(font, text, fontSize, lineWidth);
            }

            float lineHeight = LineHeightFactor(font) * fontSize;
            float baseline = height - Padding - font.GetAscent() / 1000f * fontSize;

            BeginText(canvas, width, height, appearance, fontSize);
            foreach (string line in lines)
            {
                // Lines below the field are clipped anyway
                if (baseline < -lineHeight)
                    break;

                if (line.Length > 0)
                    ShowLine(canvas.Stream, font, line, LineX(font, line, fontSize, width, alignment), baseline);

                baseline -= lineHeight;
            }
            EndText(canvas);
        }

        /// <summary>
        /// Draws the options of a list box from the top, with the selected options highlighted.
        /// An auto size (0) is 12 points.
        /// </summary>
        public static void DrawList(IDrawable canvas, float width, float height,
            HpdfDefaultAppearance appearance, HpdfFont font, IReadOnlyList<string> options, IReadOnlyCollection<string> selected)
        {
            if (options.Count == 0)
            {
                WriteEmpty(canvas);
                return;
            }

            float fontSize = appearance.FontSize == 0 ? DefaultAutoSize : appearance.FontSize;
            float lineHeight = LineHeightFactor(font) * fontSize;
            float topToBaseline = font.GetBBox().Top / 1000f * fontSize;
            int visibleCount = Math.Max(1, (int)Math.Ceiling((height - Padding) / lineHeight));

            var stream = canvas.Stream;
            stream.WriteString("/Tx BMC\n");
            canvas.GSave();
            ClipToField(canvas, width, height);

            // Highlight the selected options behind their text
            for (int i = 0; i < Math.Min(options.Count, visibleCount); i++)
            {
                if (!Contains(selected, options[i]))
                    continue;

                float top = height - Padding - i * lineHeight;
                canvas.SetRgbFill(SelectionColor[0], SelectionColor[1], SelectionColor[2]);
                canvas.Rectangle(1, top - lineHeight, width - 2, lineHeight);
                canvas.Fill();
            }

            stream.WriteString("BT\n");
            appearance.Write(stream, fontSize);
            for (int i = 0; i < Math.Min(options.Count, visibleCount); i++)
            {
                float top = height - Padding - i * lineHeight;
                ShowLine(stream, font, options[i], Padding, top - topToBaseline);
            }
            EndText(canvas);
        }

        /// <summary>
        /// Wraps text into lines that fit the width: at whitespace, inside a word only when the word
        /// alone is wider than the line, and at line feeds. Trailing whitespace is removed from each line.
        /// </summary>
        private static List<string> WrapLines(HpdfFont font, string text, float fontSize, float width)
        {
            var lines = new List<string>();
            int position = 0;

            while (position < text.Length)
            {
                string rest = text.Substring(position);
                int length = font.MeasureText(rest, fontSize, width, 0, 0, true, out _);
                if (length == 0)
                    length = font.MeasureText(rest, fontSize, width, 0, 0, false, out _);
                if (length == 0)
                    length = 1;  // A character wider than the field: show it clipped

                int visible = length;
                while (visible > 0 && char.IsWhiteSpace(rest[visible - 1]))
                    visible--;

                lines.Add(rest.Substring(0, visible));
                position += length;
            }

            return lines;
        }

        private static float LineX(HpdfFont font, string line, float fontSize, float width, HpdfFieldAlignment alignment)
        {
            return alignment switch
            {
                HpdfFieldAlignment.Center => (width - font.MeasureText(line, fontSize)) / 2,
                HpdfFieldAlignment.Right => width - Padding - font.MeasureText(line, fontSize),
                _ => Padding
            };
        }

        /// <summary>
        /// Gets the line height for a 1-point font: the height of the font's bounding box.
        /// </summary>
        private static float LineHeightFactor(HpdfFont font)
        {
            var bbox = font.GetBBox();
            float factor = (bbox.Top - bbox.Bottom) / 1000f;
            return factor > 0 ? factor : 1f;
        }

        private static bool Contains(IReadOnlyCollection<string> values, string value)
        {
            foreach (string item in values)
            {
                if (item == value)
                    return true;
            }
            return false;
        }

        private static void WriteEmpty(IDrawable canvas)
        {
            canvas.Stream.WriteString("/Tx BMC\nEMC\n");
        }

        private static void BeginText(IDrawable canvas, float width, float height, HpdfDefaultAppearance appearance, float fontSize)
        {
            var stream = canvas.Stream;
            stream.WriteString("/Tx BMC\n");
            canvas.GSave();
            ClipToField(canvas, width, height);
            stream.WriteString("BT\n");
            appearance.Write(stream, fontSize);
        }

        private static void EndText(IDrawable canvas)
        {
            canvas.Stream.WriteString("ET\n");
            canvas.GRestore();
            canvas.Stream.WriteString("EMC\n");
        }

        private static void ClipToField(IDrawable canvas, float width, float height)
        {
            canvas.Rectangle(1, 1, Math.Max(0, width - 2), Math.Max(0, height - 2));
            canvas.Clip();
            canvas.EndPath();
        }

        /// <summary>
        /// Shows a line at an absolute position: the text matrix is set for every line.
        /// </summary>
        private static void ShowLine(HpdfStream stream, HpdfFont font, string text, float x, float y)
        {
            stream.WriteString("1 0 0 1 ");
            stream.WriteReal(x);
            stream.WriteChar(' ');
            stream.WriteReal(y);
            stream.WriteString(" Tm\n");
            font.WriteTextOperand(stream, text);
            stream.WriteString(" Tj\n");
        }
    }
}
