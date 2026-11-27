/* Breaks text into lines for generated appearance streams (form fields, free text annotations). */
using System.Collections.Generic;

namespace Haru.Font
{
    /// <summary>
    /// Lays text out in lines for appearance streams: the breaks come from <see cref="HpdfFontExtensions.MeasureText"/>,
    /// the line height from the font's bounding box.
    /// </summary>
    internal static class HpdfTextLines
    {
        /// <summary>
        /// Wraps text into lines that fit the width: at whitespace, inside a word only when the word
        /// alone is wider than the line, and at line feeds. Trailing whitespace is removed from each line.
        /// </summary>
        public static List<string> Wrap(HpdfFont font, string text, float fontSize, float width)
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

        /// <summary>
        /// Gets the line height for a 1-point font: the height of the font's bounding box.
        /// </summary>
        public static float LineHeightFactor(HpdfFont font)
        {
            var bbox = font.GetBBox();
            float factor = (bbox.Top - bbox.Bottom) / 1000f;
            return factor > 0 ? factor : 1f;
        }
    }
}
