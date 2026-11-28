using Haru.Objects;
using Haru.Types;

namespace Haru.Font
{
    /// <summary>
    /// Interface for font implementation providing metrics and text measurement capabilities.
    /// Implemented by Standard, TrueType, Type 1, and CID fonts.
    /// </summary>
    public interface IHpdfFontImplementation
    {
        /// <summary>
        /// Gets the font dictionary object.
        /// </summary>
        HpdfDict Dict { get; }

        /// <summary>
        /// Gets the BaseFont name (PostScript name).
        /// </summary>
        string BaseFont { get; }

        /// <summary>
        /// Gets the local name used to reference this font in page resources.
        /// </summary>
        string LocalName { get; }

        /// <summary>
        /// Gets the encoding code page for this font, or null for standard fonts.
        /// </summary>
        int? CodePage { get; }

        /// <summary>
        /// Gets the font ascent in 1000-unit glyph space.
        /// Ascent is the maximum height above the baseline.
        /// </summary>
        int Ascent { get; }

        /// <summary>
        /// Gets the font descent in 1000-unit glyph space.
        /// Descent is the maximum depth below the baseline (negative value).
        /// </summary>
        int Descent { get; }

        /// <summary>
        /// Gets the font x-height in 1000-unit glyph space.
        /// X-height is the height of lowercase 'x'.
        /// </summary>
        int XHeight { get; }

        /// <summary>
        /// Gets the font bounding box in 1000-unit glyph space.
        /// </summary>
        HpdfBox FontBBox { get; }

        /// <summary>
        /// Gets the writing mode of the font: horizontal for every font but a CID font with a
        /// vertical CMap. Text shown with a vertical font runs from top to bottom, and
        /// <see cref="MeasureText"/> gives its length in that direction.
        /// </summary>
        HpdfWritingMode WritingMode => HpdfWritingMode.Horizontal;

        /// <summary>
        /// Gets the name of the CMap the font is encoded with: the /Encoding of a composite font
        /// ("90ms-RKSJ-V", "Identity-H"); null for a simple font, which is encoded with a code page.
        /// </summary>
        internal string? CMapName => null;

        /// <summary>
        /// Gets the cap height (the top of the capital letters) in 1000-unit glyph space; 0 if the font does not tell it.
        /// </summary>
        int CapHeight => 0;

        /// <summary>
        /// The name of the font's encoding (HPDF_Font_GetEncodingName): the CMap of a composite font, else the name
        /// of its code page.
        /// </summary>
        internal string EncodingName => CMapName ?? Haru.Encoding.HpdfEncodingNames.GetEncodingName(CodePage);

        /// <summary>
        /// Gets the width of a character in 1000-unit glyph space.
        /// </summary>
        /// <param name="charCode">The character code.</param>
        /// <returns>The character width.</returns>
        float GetCharWidth(byte charCode);

        /// <summary>
        /// Gets the width of the glyph the font has for a Unicode character, in 1000-unit glyph space, whatever the
        /// font's encoding is (HPDF_Font_GetUnicodeWidth); 0 if the font has no glyph for it. The default measures the
        /// character as text, for implementations outside the library.
        /// </summary>
        /// <param name="character">The Unicode character.</param>
        /// <returns>The width, or 0.</returns>
        float GetUnicodeWidth(System.Text.Rune character) => MeasureText(character.ToString(), 1000);

        /// <summary>
        /// Calculates the width of text in user space units: the distance the text position moves
        /// when the text is shown. For a font with vertical writing this is the height of the text.
        /// </summary>
        /// <param name="text">The text to measure.</param>
        /// <param name="fontSize">The font size.</param>
        /// <returns>The text width in user space units.</returns>
        float MeasureText(string text, float fontSize);

        /// <summary>
        /// Converts text to bytes for use in PDF content stream.
        /// For CID fonts, this may return glyph IDs or encoded bytes depending on the encoding.
        /// For non-CID fonts, returns empty array (handled by page text encoding).
        /// </summary>
        /// <param name="text">The text to convert.</param>
        /// <returns>Byte array for PDF content stream, or empty if not applicable.</returns>
        byte[] ConvertTextToGlyphIDs(string text);

        /// <summary>
        /// Encodes text to bytes using the font's encoding.
        /// This is used for both measurement and output to ensure consistency.
        /// </summary>
        /// <param name="text">The text to encode.</param>
        /// <returns>Byte array encoded using the font's encoding.</returns>
        byte[] EncodeText(string text);
    }
}
