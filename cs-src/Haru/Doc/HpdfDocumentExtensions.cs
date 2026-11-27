/*
 * Extension methods for HpdfDocument for API compatibility
 */

using System;
using Haru.Encoding;
using Haru.Font;
using Haru.Types;

namespace Haru.Doc
{
    /// <summary>
    /// Extension methods for HpdfDocument providing convenience methods for API compatibility.
    /// </summary>
    public static class HpdfDocumentExtensions
    {
        /// <summary>
        /// Gets a font by name: a font registered with <see cref="HpdfDocument.RegisterFont"/>, or one of
        /// the 14 standard fonts, which is created on the first call.
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="fontName">The name of a standard font (e.g., "Helvetica", "Courier", "Times-Roman") or the
        /// BaseFont name of a registered font.</param>
        /// <param name="encodingName">The encoding name (e.g., "WinAnsiEncoding", "CP1251", "ISO8859-5", "UTF-8",
        /// "90ms-RKSJ-V"), in any letter case. Without it, a standard font gets WinAnsiEncoding, and a registered
        /// font is found whatever its encoding is. A code page name ("CP932") also finds a registered font of
        /// that code page that writes horizontally, whatever CMap it has.</param>
        /// <returns>The font.</returns>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> for an encoding name the
        /// library does not know; <see cref="HpdfErrorCode.InvalidParameter"/> for a font name that is neither a
        /// standard font nor registered.</exception>
        public static HpdfFont GetFont(this HpdfDocument document, string fontName, string? encodingName = null)
        {
            if (document is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Document cannot be null");
            if (string.IsNullOrEmpty(fontName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Font name cannot be null or empty");

            string cacheKey = HpdfDocument.GetFontRegistryKey(fontName, HpdfEncodingNames.GetRegistryKey(encodingName));

            if (document.FontRegistry.TryGetValue(cacheKey, out var cachedFont))
            {
                return cachedFont;
            }

            if (!TryMapFontName(fontName, out var standardFont))
            {
                if (TryFindRegisteredFont(document, fontName, encodingName, out var registeredFont))
                    return registeredFont;

                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"Unknown font name: {fontName}");
            }

            var localName = $"{HpdfDocument.GeneratedNamePrefix}F{document.Xref.Entries.Count}";
            var font = new HpdfFont(document.Xref, standardFont, localName, HpdfEncodingNames.GetCodePage(encodingName));
            document.FontRegistry[cacheKey] = font;

            return font;
        }

        /// <summary>
        /// Finds a registered font that is not registered under the encoding asked for, but matches it:
        /// asked by its name alone, the first font of this name, whatever its encoding is (it has one
        /// encoding, the one it was loaded with); asked with a code page name, the first font of this name
        /// with this code page that writes horizontally (a predefined CJK font registered under its CMap).
        /// </summary>
        private static bool TryFindRegisteredFont(HpdfDocument document, string fontName, string? encodingName, out HpdfFont font)
        {
            bool anyEncoding = string.IsNullOrEmpty(encodingName);
            if (!anyEncoding && !HpdfEncodingNames.IsCodePageName(encodingName!))
            {
                font = null!;
                return false;
            }

            int codePage = anyEncoding ? 0 : HpdfEncodingNames.GetCodePage(encodingName);
            string prefix = fontName + "#";
            foreach (var entry in document.FontRegistry)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;

                HpdfFont candidate = entry.Value;
                if (anyEncoding ||
                    (candidate.EncodingCodePage == codePage && candidate.WritingMode == HpdfWritingMode.Horizontal))
                {
                    font = candidate;
                    return true;
                }
            }

            font = null!;
            return false;
        }

        /// <summary>
        /// Maps a font name string to a HpdfStandardFont enum value.
        /// </summary>
        private static bool TryMapFontName(string fontName, out HpdfStandardFont standardFont)
        {
            HpdfStandardFont? found = fontName switch
            {
                "Courier" => HpdfStandardFont.Courier,
                "Courier-Bold" => HpdfStandardFont.CourierBold,
                "Courier-Oblique" => HpdfStandardFont.CourierOblique,
                "Courier-BoldOblique" => HpdfStandardFont.CourierBoldOblique,
                "Helvetica" => HpdfStandardFont.Helvetica,
                "Helvetica-Bold" => HpdfStandardFont.HelveticaBold,
                "Helvetica-Oblique" => HpdfStandardFont.HelveticaOblique,
                "Helvetica-BoldOblique" => HpdfStandardFont.HelveticaBoldOblique,
                "Times-Roman" => HpdfStandardFont.TimesRoman,
                "Times-Bold" => HpdfStandardFont.TimesBold,
                "Times-Italic" => HpdfStandardFont.TimesItalic,
                "Times-BoldItalic" => HpdfStandardFont.TimesBoldItalic,
                "Symbol" => HpdfStandardFont.Symbol,
                "ZapfDingbats" => HpdfStandardFont.ZapfDingbats,
                _ => null
            };

            standardFont = found ?? default;
            return found.HasValue;
        }

        /// <summary>
        /// Sets the compression mode for the document.
        /// This controls which types of content will be compressed in the PDF.
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="mode">The compression mode flags.</param>
        public static void SetCompressionMode(this HpdfDocument document, HpdfCompressionMode mode)
        {
            if (document is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Document cannot be null");

            document.CompressionMode = mode;
        }

        /// <summary>
        /// Sets the compression mode for the document using uint flags (P/Invoke compatibility).
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="mode">The compression mode flags as uint.</param>
        public static void SetCompressionMode(this HpdfDocument document, uint mode)
        {
            SetCompressionMode(document, (HpdfCompressionMode)mode);
        }
    }
}
