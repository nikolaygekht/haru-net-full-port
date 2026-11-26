using System;

namespace Haru.Font
{
    /// <summary>
    /// Extension methods for HpdfFont providing advanced text measurement capabilities.
    /// </summary>
    public static class HpdfFontExtensions
    {
        /// <summary>
        /// Measures how many characters of text fit within a specified width with word wrapping support.
        /// This is the full version compatible with libharu's HPDF_Font_MeasureText.
        /// </summary>
        /// <param name="font">The font to use for measurement.</param>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontSize">The font size in points.</param>
        /// <param name="width">The maximum width constraint in user space units.</param>
        /// <param name="charSpace">Character spacing in user space units.</param>
        /// <param name="wordSpace">Word spacing (added to each whitespace character) in user space units; ignored for CID fonts, to which viewers do not apply it.</param>
        /// <param name="wordWrap">If true, break only at word boundaries; if false, break anywhere.</param>
        /// <param name="realWidth">OUTPUT: The actual width of the text that fits.</param>
        /// <returns>The number of characters that fit within the specified width.</returns>
        public static int MeasureText(
            this HpdfFont font,
            string text,
            float fontSize,
            float width,
            float charSpace,
            float wordSpace,
            bool wordWrap,
            out float realWidth)
        {
            ArgumentNullException.ThrowIfNull(font);
            realWidth = 0;

            if (string.IsNullOrEmpty(text))
                return 0;

            float w = 0;
            int tmpLen = 0;

            // Viewers apply word spacing only to the single-byte code 32, never to CID fonts
            if (font.IsCIDFont)
                wordSpace = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                // A line feed, or a carriage return not followed by one, ends the line here
                if (c == 0x0A || (c == 0x0D && (i + 1 >= text.Length || text[i + 1] != 0x0A)))
                {
                    realWidth = w;
                    return i + 1;
                }

                // Handle whitespace
                if (IsWhiteSpace(c))
                {
                    tmpLen = i + 1;  // Can break AFTER whitespace
                    realWidth = w;
                    w += wordSpace;
                }
                else if (!wordWrap)
                {
                    // Non-wordwrap mode: can break anywhere
                    tmpLen = i;  // Can break BEFORE this character
                    realWidth = w;
                }

                // Add character width (get it in user space units from the font implementation)
                // Use single-character string to leverage font's proper scaling (handles different UnitsPerEm)
                w += font.MeasureText(c.ToString(), fontSize);

                // Check if we exceeded width
                if (w > width)
                    return tmpLen;

                // Add character spacing (except for first character)
                if (i > 0)
                    w += charSpace;
            }

            // All text fits within the specified width
            realWidth = w;
            return text.Length;
        }

        /// <summary>
        /// Checks if a character is whitespace.
        /// Whitespace includes: null, tab, line feed, form feed, carriage return, and space.
        /// The whole character is compared, not its low byte, so Unicode characters such as
        /// U+010A are not mistaken for whitespace.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character is whitespace, false otherwise.</returns>
        private static bool IsWhiteSpace(char c)
        {
            return c == 0x00 ||  // null
                   c == 0x09 ||  // tab
                   c == 0x0A ||  // line feed
                   c == 0x0C ||  // form feed
                   c == 0x0D ||  // carriage return
                   c == 0x20;    // space
        }
    }
}
