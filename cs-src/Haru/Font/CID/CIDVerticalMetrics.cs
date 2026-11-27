/* The metrics of vertical writing of a CID font (/DW2) */
using Haru.Objects;

namespace Haru.Font.CID
{
    /// <summary>
    /// The metrics a CID font of vertical writing has for every glyph: where the origin of a glyph
    /// is, and how far a glyph moves the text position down. The library writes one pair of values
    /// for the whole font (/DW2), so a column is measured as a reader lays it out.
    /// </summary>
    internal static class CIDVerticalMetrics
    {
        /// <summary>
        /// The height of the origin of a glyph above its baseline when a font does not tell it, in
        /// 1000-unit glyph space: the top of the ideographic em box of a CJK font. This is the value
        /// of the predefined fonts of libharu (hpdf_fontdef_cid.c) and the default of a PDF reader.
        /// </summary>
        public const int DefaultOriginY = 880;

        /// <summary>
        /// The distance a glyph moves the text position down, in 1000-unit glyph space: one em.
        /// </summary>
        public const int Advance = 1000;

        /// <summary>
        /// Creates the /DW2 array of a font: the origin of a glyph and its (downward, so negative)
        /// advance.
        /// </summary>
        /// <param name="originY">The height of the glyph origin above the baseline.</param>
        public static HpdfArray CreateDW2(int originY)
        {
            var dw2 = new HpdfArray();
            dw2.Add(new HpdfNumber(originY));
            dw2.Add(new HpdfNumber(-Advance));
            return dw2;
        }
    }
}
