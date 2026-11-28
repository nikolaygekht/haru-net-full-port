/* A widget that covers a rectangle with white on paper only (HPDF_Page_CreateWidgetAnnot_WhiteOnlyWhilePrint). */

using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// The widget of HPDF_Page_CreateWidgetAnnot_WhiteOnlyWhilePrint: a button widget named "Blind" with a white
    /// appearance and white background (/MK /BG), flagged Print and NoView, so that viewers show nothing on screen and
    /// printing covers the rectangle with white. Created by <c>HpdfDocument.CreateWhiteOnlyWhilePrintAnnotation</c>.
    /// </summary>
    internal sealed class HpdfPrintBlindAnnotation : HpdfAnnotation
    {
        internal HpdfPrintBlindAnnotation(HpdfXref xref, HpdfRect rect, HpdfStreamObject whiteAppearance)
            : base(xref, HpdfAnnotationType.Widget, rect)
        {
            var appearance = new HpdfDict();
            appearance.Add("N", whiteAppearance);
            _dict.Add("AP", appearance);

            var characteristics = new HpdfDict();
            characteristics.Add("BG", new HpdfArray { new HpdfReal(1), new HpdfReal(1), new HpdfReal(1) });
            _dict.Add("MK", characteristics);

            _dict.Add("FT", new HpdfName("Btn"));
            _dict.Add("T", new HpdfString("Blind"));
            Flags = HpdfAnnotationFlags.Print | HpdfAnnotationFlags.NoView;
        }

        /// <inheritdoc/>
        internal override void PrepareForWriting(HpdfAnnotationWriteContext context)
        {
            // ISO 19005-1, 6.5.3: the NoView flag shall be clear; without it the white would cover the page on screen
            if (context.PdfA)
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "PDF/A-1 does not allow annotations hidden on screen (NoView), as the white-on-print widget is");
        }
    }
}
