using System;
using Haru.Font;
using Haru.Types;
using Haru.Streams;

namespace Haru.Doc
{
    /// <summary>
    /// Text operations for HpdfPage
    /// </summary>
    public static class HpdfPageText
    {
        // Text Object Operations

        /// <summary>
        /// Begins a text object (BT operator).
        /// Must be paired with EndText().
        /// </summary>
        public static void BeginText(this HpdfPage page)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteString("BT\n");

            // Initialize text matrix to identity
            page.TextMatrix = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);
            page.TextLineMatrix = new HpdfTransMatrix(1, 0, 0, 1, 0, 0);
        }

        /// <summary>
        /// Ends a text object (ET operator).
        /// </summary>
        public static void EndText(this HpdfPage page)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteString("ET\n");
        }

        // Text State Operations

        /// <summary>
        /// Sets the character spacing (Tc operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="charSpace">Character spacing in user space units.</param>
        public static void SetCharSpace(this HpdfPage page, float charSpace)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(charSpace);
            stream.WriteString(" Tc\n");

            page.GraphicsState.CharSpace = charSpace;
        }

        /// <summary>
        /// Sets the word spacing (Tw operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="wordSpace">Word spacing in user space units.</param>
        public static void SetWordSpace(this HpdfPage page, float wordSpace)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(wordSpace);
            stream.WriteString(" Tw\n");

            page.GraphicsState.WordSpace = wordSpace;
        }

        /// <summary>
        /// Sets the horizontal scaling (Tz operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="scale">Horizontal scaling as a percentage (100 = normal).</param>
        public static void SetHorizontalScaling(this HpdfPage page, float scale)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (scale <= 0)
                throw new HpdfException(HpdfErrorCode.PageOutOfRange, "Horizontal scaling must be positive");

            var stream = page.Contents.Stream;
            stream.WriteReal(scale);
            stream.WriteString(" Tz\n");

            page.GraphicsState.HorizontalScaling = scale;
        }

        /// <summary>
        /// Sets the text leading (TL operator).
        /// Leading is the vertical distance between baselines.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="leading">Text leading in user space units.</param>
        public static void SetTextLeading(this HpdfPage page, float leading)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(leading);
            stream.WriteString(" TL\n");

            page.GraphicsState.TextLeading = leading;
        }

        /// <summary>
        /// Sets the font and size (Tf operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="font">The font to use.</param>
        /// <param name="size">The font size in points.</param>
        public static void SetFontAndSize(this HpdfPage page, HpdfFont font, float size)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (font is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Font cannot be null");
            if (size <= 0)
                throw new HpdfException(HpdfErrorCode.PageOutOfRange, "Font size must be positive");

            // Add font to page resources if not already added
            page.AddFontResource(font);

            var stream = page.Contents.Stream;
            stream.WriteEscapedName(font.LocalName);
            stream.WriteChar(' ');
            stream.WriteReal(size);
            stream.WriteString(" Tf\n");

            page.CurrentFont = font;
            page.GraphicsState.FontSize = size;
        }

        /// <summary>
        /// Sets the text rendering mode (Tr operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="mode">The rendering mode.</param>
        public static void SetTextRenderingMode(this HpdfPage page, HpdfTextRenderingMode mode)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (mode < HpdfTextRenderingMode.Fill || mode >= HpdfTextRenderingMode.RenderingModeEof)
                throw new HpdfException(HpdfErrorCode.PageOutOfRange, $"Invalid text rendering mode: {mode}");

            var stream = page.Contents.Stream;
            stream.WriteInt((int)mode);
            stream.WriteString(" Tr\n");

            page.GraphicsState.RenderingMode = (int)mode;
        }

        /// <summary>
        /// Sets the text rise (Ts operator).
        /// Rise is the vertical distance from the baseline.
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="rise">Text rise in user space units.</param>
        public static void SetTextRise(this HpdfPage page, float rise)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(rise);
            stream.WriteString(" Ts\n");

            page.GraphicsState.TextRise = rise;
        }

        // Text Positioning Operations

        /// <summary>
        /// Moves to the start of the next line (Td operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="tx">Horizontal translation.</param>
        /// <param name="ty">Vertical translation.</param>
        public static void MoveTextPos(this HpdfPage page, float tx, float ty)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(tx);
            stream.WriteChar(' ');
            stream.WriteReal(ty);
            stream.WriteString(" Td\n");

            TranslateTextLine(page, tx, ty);
        }

        /// <summary>
        /// Moves to the start of the next line and sets leading (TD operator).
        /// Equivalent to: SetTextLeading(-ty); MoveTextPos(tx, ty);
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="tx">Horizontal translation.</param>
        /// <param name="ty">Vertical translation.</param>
        public static void MoveTextPosAndSetLeading(this HpdfPage page, float tx, float ty)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(tx);
            stream.WriteChar(' ');
            stream.WriteReal(ty);
            stream.WriteString(" TD\n");

            page.GraphicsState.TextLeading = -ty;
            TranslateTextLine(page, tx, ty);
        }

        /// <summary>
        /// Sets the text matrix and text line matrix (Tm operator).
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="matrix">The transformation matrix.</param>
        public static void SetTextMatrix(this HpdfPage page, HpdfTransMatrix matrix)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteReal(matrix.A);
            stream.WriteChar(' ');
            stream.WriteReal(matrix.B);
            stream.WriteChar(' ');
            stream.WriteReal(matrix.C);
            stream.WriteChar(' ');
            stream.WriteReal(matrix.D);
            stream.WriteChar(' ');
            stream.WriteReal(matrix.X);
            stream.WriteChar(' ');
            stream.WriteReal(matrix.Y);
            stream.WriteString(" Tm\n");

            page.TextMatrix = matrix;
            page.TextLineMatrix = matrix;
        }

        /// <summary>
        /// Moves to the start of the next line (T* operator).
        /// Uses the text leading set by SetTextLeading().
        /// </summary>
        public static void MoveToNextLine(this HpdfPage page)
        {
            ArgumentNullException.ThrowIfNull(page);
            var stream = page.Contents.Stream;
            stream.WriteString("T*\n");

            TranslateTextLine(page, 0, -page.GraphicsState.TextLeading);
        }

        // Text Showing Operations

        /// <summary>
        /// Shows a text string (Tj operator).
        /// Text is encoded using the current font's encoding:
        /// - CID fonts (Type 0): Glyph IDs as hex string (for CIDToGIDMap=Identity)
        /// - TrueType fonts: Code page encoding (literal string)
        /// - Standard fonts: PDFDocEncoding
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="text">The text to show.</param>
        public static void ShowText(this HpdfPage page, string text)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (string.IsNullOrEmpty(text))
                return;

            WriteTextOperand(page, text);
            page.Contents.Stream.WriteString(" Tj\n");
        }

        /// <summary>
        /// Moves to the next line and shows text (combined operator).
        /// Equivalent to: MoveToNextLine(); ShowText(text);
        /// Text is encoded using the current font's encoding:
        /// - CID fonts (Type 0): Glyph IDs as hex string (for CIDToGIDMap=Identity)
        /// - TrueType fonts: Code page encoding (literal string)
        /// - Standard fonts: PDFDocEncoding
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="text">The text to show.</param>
        public static void ShowTextNextLine(this HpdfPage page, string text)
        {
            ArgumentNullException.ThrowIfNull(page);
            if (string.IsNullOrEmpty(text))
            {
                // Nothing to show, but the line still moves
                page.MoveToNextLine();
                return;
            }

            WriteTextOperand(page, text);
            page.Contents.Stream.WriteString(" '\n");

            TranslateTextLine(page, 0, -page.GraphicsState.TextLeading);
        }

        /// <summary>
        /// Sets word and character spacing, moves to next line, and shows text (" operator).
        /// Text is encoded using the current font's encoding:
        /// - CID fonts (Type 0): Glyph IDs as hex string (for CIDToGIDMap=Identity)
        /// - TrueType fonts: Code page encoding (literal string)
        /// - Standard fonts: PDFDocEncoding
        /// </summary>
        /// <param name="page">The page.</param>
        /// <param name="wordSpace">Word spacing.</param>
        /// <param name="charSpace">Character spacing.</param>
        /// <param name="text">The text to show.</param>
        public static void SetSpacingAndShowText(this HpdfPage page, float wordSpace, float charSpace, string text)
        {
            ArgumentNullException.ThrowIfNull(page);
            text ??= string.Empty;
            RequireFont(page);

            var stream = page.Contents.Stream;
            stream.WriteReal(wordSpace);
            stream.WriteChar(' ');
            stream.WriteReal(charSpace);
            stream.WriteChar(' ');
            // An empty string still sets the spacing and moves to the next line
            WriteTextOperand(page, text);
            stream.WriteString(" \"\n");

            // Update state
            page.GraphicsState.WordSpace = wordSpace;
            page.GraphicsState.CharSpace = charSpace;

            TranslateTextLine(page, 0, -page.GraphicsState.TextLeading);
        }

        /// <summary>
        /// Prints text inside a rectangle, wrapping it into lines (port of HPDF_Page_TextRect).
        /// Must be called between <see cref="BeginText"/> and <see cref="EndText"/>, after a font is set.
        /// </summary>
        /// <remarks>
        /// <para>Lines break at whitespace; a word that is wider than the rectangle is broken between
        /// characters. Line feeds in the text start a new line. The first baseline is placed so that
        /// the font's bounding box touches <paramref name="top"/>; lines are printed while the font's
        /// bounding box stays above <paramref name="bottom"/>.</para>
        /// <para>If the text leading is 0, it is set to the height of the font's bounding box.
        /// <see cref="HpdfTextAlignment.Justify"/> stretches every line except the last line of each
        /// paragraph by adjusting the character spacing; the original character spacing is restored
        /// afterwards. The text matrix is expected to have no rotation or scaling.</para>
        /// <para>With a font of vertical writing (<see cref="HpdfFont.WritingMode"/>) the text is laid
        /// out in columns: a column runs from <paramref name="top"/> down to <paramref name="bottom"/>,
        /// the first column is at the right edge and the next ones follow to the left, one text
        /// leading apart (the width of a column if the leading is 0). The alignment works along the
        /// column: <see cref="HpdfTextAlignment.Left"/> starts a column at the top,
        /// <see cref="HpdfTextAlignment.Right"/> ends it at the bottom, <see cref="HpdfTextAlignment.Center"/>
        /// centers it and <see cref="HpdfTextAlignment.Justify"/> stretches it over the height.
        /// Columns are printed while they fit between <paramref name="right"/> and
        /// <paramref name="left"/>.</para>
        /// </remarks>
        /// <param name="page">The page.</param>
        /// <param name="left">Left edge of the rectangle.</param>
        /// <param name="top">Top edge of the rectangle.</param>
        /// <param name="right">Right edge of the rectangle.</param>
        /// <param name="bottom">Bottom edge of the rectangle.</param>
        /// <param name="text">The text to print.</param>
        /// <param name="alignment">Horizontal alignment of the lines.</param>
        /// <returns>
        /// The number of characters of <paramref name="text"/> that were printed (including the
        /// whitespace consumed at line breaks). It is less than the text length if the rectangle
        /// is too small for the whole text.
        /// </returns>
        public static int TextRect(this HpdfPage page, float left, float top, float right, float bottom,
            string text, HpdfTextAlignment alignment)
        {
            ArgumentNullException.ThrowIfNull(page);
            HpdfFont font = page.CurrentFont
                ?? throw new HpdfException(HpdfErrorCode.PageFontNotFound, "No font has been set");

            if (string.IsNullOrEmpty(text))
                return 0;

            if (font.WritingMode == HpdfWritingMode.Vertical)
                return TextRectVertical(page, font, left, top, right, bottom, text, alignment);

            var state = page.GraphicsState;
            float fontSize = state.FontSize;
            HpdfBox bbox = font.GetBBox();

            if (state.TextLeading == 0)
                page.SetTextLeading((bbox.Top - bbox.Bottom) / 1000f * fontSize);

            float leading = state.TextLeading;
            float width = right - left;
            bool justify = alignment == HpdfTextAlignment.Justify;
            float savedCharSpace = state.CharSpace;
            float measureCharSpace = justify ? 0 : savedCharSpace;
            // Viewers apply word spacing only to the single-byte code 32, never to CID fonts
            float wordSpace = font.IsCIDFont ? 0 : state.WordSpace;
            // Horizontal scaling (Tz) scales glyph widths, character and word spacing alike:
            // lines are measured in unscaled text space against the correspondingly wider rectangle
            float scale = state.HorizontalScaling / 100f;
            float measureWidth = width / scale;
            float lowestBaseline = bottom - bbox.Bottom / 1000f * fontSize;

            // Each line is shown with the ' operator, which first moves down by the leading:
            // the line starts one leading above its baseline.
            float lineStartY = top - bbox.Top / 1000f * fontSize + leading;
            bool positioned = false;
            int position = 0;

            while (position < text.Length)
            {
                if (lineStartY - leading < lowestBaseline)
                    break;

                if (!TryTakeLine(font, text, ref position, fontSize, measureWidth, measureCharSpace, wordSpace,
                        out string line, out bool paragraphEnd))
                {
                    break;
                }

                float naturalWidth = MeasureLine(font, line, fontSize, measureCharSpace, wordSpace);
                float lineWidth = naturalWidth * scale;
                float x = alignment switch
                {
                    HpdfTextAlignment.Right => right - lineWidth,
                    HpdfTextAlignment.Center => left + (width - lineWidth) / 2,
                    _ => left
                };

                if (!positioned || alignment == HpdfTextAlignment.Right || alignment == HpdfTextAlignment.Center)
                {
                    MoveTextLineTo(page, x, lineStartY);
                    positioned = true;
                }

                if (justify)
                {
                    int glyphCount = CountCharacters(line);
                    float charSpace = paragraphEnd || position >= text.Length || glyphCount < 2
                        ? savedCharSpace
                        : (measureWidth - naturalWidth) / (glyphCount - 1);
                    if (charSpace != state.CharSpace)
                        page.SetCharSpace(charSpace);
                }

                if (line.Length > 0)
                    page.ShowTextNextLine(line);
                else
                    page.MoveToNextLine();

                lineStartY = page.TextLineMatrix.Y;
            }

            if (state.CharSpace != savedCharSpace)
                page.SetCharSpace(savedCharSpace);

            return position;
        }

        /// <summary>
        /// Prints text in a rectangle with a font of vertical writing: in columns that run down from
        /// the top and follow each other from the right edge to the left.
        /// </summary>
        private static int TextRectVertical(HpdfPage page, HpdfFont font, float left, float top, float right, float bottom,
            string text, HpdfTextAlignment alignment)
        {
            // The edges are compared with sums of column widths: a rectangle that is exactly some
            // columns wide must hold them all
            const float Tolerance = 0.001f;

            var state = page.GraphicsState;
            float fontSize = state.FontSize;

            // A glyph is one em wide and hangs centered under the text position. Horizontal scaling
            // (Tz) stretches the glyphs across the column; the advance along the column is not scaled
            float columnWidth = fontSize * state.HorizontalScaling / 100f;

            if (state.TextLeading == 0)
                page.SetTextLeading(columnWidth);

            float leading = state.TextLeading;
            float height = top - bottom;
            bool justify = alignment == HpdfTextAlignment.Justify;
            float savedCharSpace = state.CharSpace;
            float measureCharSpace = justify ? 0 : savedCharSpace;

            float columnX = right - columnWidth / 2;
            int position = 0;

            while (position < text.Length)
            {
                if (columnX - columnWidth / 2 < left - Tolerance)
                    break;

                // Word spacing is never applied to a CID font
                if (!TryTakeLine(font, text, ref position, fontSize, height, measureCharSpace, 0,
                        out string line, out bool paragraphEnd))
                {
                    break;
                }

                float naturalHeight = MeasureLine(font, line, fontSize, measureCharSpace, 0);
                float y = alignment switch
                {
                    HpdfTextAlignment.Right => bottom + naturalHeight,
                    HpdfTextAlignment.Center => top - (height - naturalHeight) / 2,
                    _ => top
                };

                if (justify)
                {
                    // The distance to add between the characters, as a character spacing
                    int glyphCount = CountCharacters(line);
                    float charSpace = paragraphEnd || position >= text.Length || glyphCount < 2
                        ? savedCharSpace
                        : font.GetSpacingAlongText((height - naturalHeight) / (glyphCount - 1));
                    if (charSpace != state.CharSpace)
                        page.SetCharSpace(charSpace);
                }

                if (line.Length > 0)
                {
                    MoveTextLineTo(page, columnX, y);
                    page.ShowText(line);
                }

                columnX -= leading;
            }

            if (state.CharSpace != savedCharSpace)
                page.SetCharSpace(savedCharSpace);

            return position;
        }

        /// <summary>
        /// Takes the next line of a text: the characters from a position that fit a length, broken at
        /// whitespace, or between characters if the first word is longer than the length.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="text">The whole text.</param>
        /// <param name="position">The start of the line; moved behind the line and the whitespace
        /// that ends it.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="length">The length the line may have along the writing direction, in text space.</param>
        /// <param name="charSpace">The character spacing.</param>
        /// <param name="wordSpace">The word spacing.</param>
        /// <param name="line">The characters to print: the line without its trailing whitespace.</param>
        /// <param name="paragraphEnd">True if a line feed ends the line.</param>
        /// <returns>False if not even one character fits.</returns>
        private static bool TryTakeLine(HpdfFont font, string text, ref int position, float fontSize, float length,
            float charSpace, float wordSpace, out string line, out bool paragraphEnd)
        {
            line = string.Empty;
            paragraphEnd = false;

            string rest = text.Substring(position);
            int lineLength = font.MeasureText(rest, fontSize, length, charSpace, wordSpace, true, out _);
            if (lineLength == 0)
                lineLength = font.MeasureText(rest, fontSize, length, charSpace, wordSpace, false, out _);
            if (lineLength == 0)
                return false;

            // Trailing whitespace is not printed; a line feed in it ends the paragraph
            int visibleLength = lineLength;
            while (visibleLength > 0 && IsWhiteSpace(rest[visibleLength - 1]))
            {
                visibleLength--;
                if (rest[visibleLength] == '\n' || rest[visibleLength] == '\r')
                    paragraphEnd = true;
            }

            line = rest.Substring(0, visibleLength);
            position += lineLength;

            // Whitespace after the last word belongs to the last line
            if (IsWhiteSpaceOnly(text, position))
                position = text.Length;

            return true;
        }

        /// <summary>
        /// Writes text as a string operand, encoded for the current font.
        /// </summary>
        private static void WriteTextOperand(HpdfPage page, string text)
        {
            RequireFont(page).WriteTextOperand(page.Contents.Stream, text);
        }

        /// <summary>
        /// Gets the current font; showing or measuring text without one is an error, as in the C library.
        /// </summary>
        private static HpdfFont RequireFont(HpdfPage page) =>
            page.CurrentFont
            ?? throw new HpdfException(HpdfErrorCode.PageFontNotFound, "No font has been set: call SetFontAndSize first");

        /// <summary>
        /// Moves the text line matrix by (tx, ty) in text space, as the Td operator does:
        /// Tlm = [1 0 0 1 tx ty] x Tlm. The text matrix follows the line matrix.
        /// </summary>
        private static void TranslateTextLine(HpdfPage page, float tx, float ty)
        {
            var tm = page.TextLineMatrix;
            page.TextLineMatrix = new HpdfTransMatrix(tm.A, tm.B, tm.C, tm.D,
                tx * tm.A + ty * tm.C + tm.X,
                tx * tm.B + ty * tm.D + tm.Y);
            page.TextMatrix = page.TextLineMatrix;
        }

        /// <summary>
        /// Moves the start of the text line to the given position with a Td operator.
        /// </summary>
        private static void MoveTextLineTo(HpdfPage page, float x, float y)
        {
            var tm = page.TextLineMatrix;
            float dx = x - tm.X;
            float dy = y - tm.Y;
            float determinant = tm.A * tm.D - tm.B * tm.C;

            if (determinant == 0)
            {
                page.MoveTextPos(dx, dy);
                return;
            }

            page.MoveTextPos((dx * tm.D - dy * tm.C) / determinant, (dy * tm.A - dx * tm.B) / determinant);
        }

        /// <summary>
        /// Measures a line the way <see cref="HpdfFontExtensions.MeasureText(HpdfFont, string, float, float, float, float, bool, out float)"/>
        /// does: glyph widths, the character spacing between characters and the word spacing of each whitespace,
        /// along the writing direction of the font.
        /// </summary>
        private static float MeasureLine(HpdfFont font, string line, float fontSize, float charSpace, float wordSpace)
        {
            if (line.Length == 0)
                return 0;

            int whitespaceCount = 0;
            foreach (char c in line)
            {
                if (IsWhiteSpace(c))
                    whitespaceCount++;
            }

            return font.MeasureText(line, fontSize)
                + font.GetSpacingAlongText(charSpace) * (CountCharacters(line) - 1)
                + wordSpace * whitespaceCount;
        }

        /// <summary>
        /// Counts the characters of a string, a surrogate pair counting as one.
        /// </summary>
        private static int CountCharacters(string text)
        {
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (!char.IsLowSurrogate(text[i]))
                    count++;
            }
            return count;
        }

        private static bool IsWhiteSpaceOnly(string text, int start)
        {
            for (int i = start; i < text.Length; i++)
            {
                if (!IsWhiteSpace(text[i]))
                    return false;
            }
            return true;
        }

        private static bool IsWhiteSpace(char c) =>
            c == '\0' || c == '\t' || c == '\n' || c == '\f' || c == '\r' || c == ' ';
    }
}
