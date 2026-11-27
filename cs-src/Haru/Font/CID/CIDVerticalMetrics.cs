/* The metrics of vertical writing of a CID font (/DW2, /W2) */
using System.Collections.Generic;
using Haru.Objects;

namespace Haru.Font.CID
{
    /// <summary>
    /// The vertical metrics of a glyph that differ from the default of its font (/DW2): how far the
    /// glyph moves the text position down, and where its vertical origin is.
    /// </summary>
    /// <param name="Cid">The CID of the glyph.</param>
    /// <param name="Advance">The distance the glyph moves the text position down, in 1000-unit glyph space.</param>
    /// <param name="OriginX">The horizontal position of the vertical origin, from the left edge of the glyph.</param>
    /// <param name="OriginY">The height of the vertical origin above the baseline.</param>
    internal readonly record struct CIDVerticalGlyphMetrics(ushort Cid, int Advance, float OriginX, int OriginY);

    /// <summary>
    /// The metrics a CID font of vertical writing has for its glyphs: where the origin of a glyph
    /// is, and how far a glyph moves the text position down. A font has one pair of values for all
    /// its glyphs (/DW2); the glyphs whose metrics differ from it are listed in /W2. A column is
    /// measured with the same values a reader lays it out with.
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

        /// <summary>
        /// Creates the /W2 array of a font: for every run of consecutive CIDs, the first CID and the
        /// (downward, so negative) advance, the horizontal and the vertical position of the origin of
        /// each glyph of the run.
        /// </summary>
        /// <param name="glyphs">The glyphs whose metrics differ from /DW2, sorted by CID.</param>
        public static HpdfArray CreateW2(IReadOnlyList<CIDVerticalGlyphMetrics> glyphs)
        {
            var w2 = new HpdfArray();
            HpdfArray? run = null;
            int previous = -1;
            foreach (CIDVerticalGlyphMetrics glyph in glyphs)
            {
                if (run is null || glyph.Cid != previous + 1)
                {
                    run = new HpdfArray();
                    w2.Add(new HpdfNumber(glyph.Cid));
                    w2.Add(run);
                }

                run.Add(new HpdfNumber(-glyph.Advance));
                run.Add(new HpdfReal(glyph.OriginX));
                run.Add(new HpdfNumber(glyph.OriginY));
                previous = glyph.Cid;
            }

            return w2;
        }
    }
}
