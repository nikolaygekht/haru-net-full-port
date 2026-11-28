/* Encoding names of the PDF library API (GetFont, RegisterFont) and their code pages */
using System;
using System.Collections.Generic;
using System.Globalization;
using Haru.Font;
using Haru.Font.CID;
using Haru.Types;

namespace Haru.Encoding
{
    /// <summary>
    /// Translates the encoding names of the library (the names of the C library: "WinAnsiEncoding",
    /// "CP1251", "ISO8859-2", "KOI8-R", "UTF-8", the CMap names of the CJK encodings ...) to code pages.
    /// This is the one place where an encoding name is interpreted, so that a font registered under
    /// a name is found again under the same name.
    /// </summary>
    internal static class HpdfEncodingNames
    {
        /// <summary>
        /// The code page of a font when no encoding is named: Windows-1252 (WinAnsiEncoding).
        /// </summary>
        public const int DefaultCodePage = 1252;

        private static readonly Dictionary<string, int> _codePages = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            // Encodings of the PDF format
            ["WinAnsiEncoding"] = 1252,
            ["WinAnsi"] = 1252,
            ["StandardEncoding"] = 1252,   // the standard fonts are written with WinAnsiEncoding
            ["FontSpecific"] = 1252,       // Symbol and ZapfDingbats ignore the encoding
            ["MacRomanEncoding"] = 10000,

            // Single-byte encodings without a regular "CPnnnn" / "ISO8859-n" name
            ["KOI8-R"] = 20866,

            // Unicode (CID fonts)
            ["UTF-8"] = 65001,
            ["UTF8"] = 65001,
            ["Identity-H"] = 65001,
            ["Identity-V"] = 65001,        // vertical writing (see _verticalNames)
        };

        // The names of encodings with a code page that are written vertically. The vertical CMaps
        // of the CJK encodings are not here: a CMap is an encoding of its own
        private static readonly HashSet<string> _verticalNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Identity-V",
        };

        // ISO 8859 part number -> Windows code page (there is no part 12)
        private static readonly Dictionary<int, int> _iso8859CodePages = new Dictionary<int, int>
        {
            [1] = 28591, [2] = 28592, [3] = 28593, [4] = 28594, [5] = 28595, [6] = 28596, [7] = 28597,
            [8] = 28598, [9] = 28599, [10] = 28600, [11] = 28601, [13] = 28603, [14] = 28604, [15] = 28605,
            [16] = 28606,
        };

        /// <summary>
        /// Gets the name of a code page as an encoding name: WinAnsiEncoding, MacRomanEncoding, KOI8-R, ISO8859-n,
        /// UTF-8 or CPnnnn.
        /// </summary>
        public static string GetEncodingName(int? codePage) => codePage switch
        {
            null or DefaultCodePage => "WinAnsiEncoding",
            10000 => "MacRomanEncoding",
            20866 => "KOI8-R",
            65001 => "UTF-8",
            int page when _iso8859CodePages.ContainsValue(page) =>
                "ISO8859-" + System.Linq.Enumerable.First(_iso8859CodePages, entry => entry.Value == page).Key,
            int page => "CP" + page
        };

        /// <summary>
        /// Gets the code page of an encoding name.
        /// </summary>
        /// <param name="encodingName">The encoding name, in any letter case: a name of the PDF format
        /// ("WinAnsiEncoding"), "CPnnnn" or the number alone, "ISO8859-n" (also "ISO-8859-n"), "KOI8-R",
        /// "UTF-8", a CMap name of a CJK encoding ("GBK-EUC-H"), or a name .NET knows ("windows-1251").</param>
        /// <param name="defaultCodePage">The code page for a null or empty name.</param>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> for a name
        /// that is none of these.</exception>
        public static int GetCodePage(string? encodingName, int defaultCodePage = DefaultCodePage)
        {
            if (string.IsNullOrEmpty(encodingName))
                return defaultCodePage;

            if (_codePages.TryGetValue(encodingName, out int codePage))
                return codePage;

            // A predefined CMap of a CJK encoding
            CMapDefinition? cmap = CMapDefinition.Find(encodingName);
            if (cmap != null)
                return cmap.CodePage;

            if (TryParseNumber(encodingName, out codePage))
                return codePage;

            if (encodingName.StartsWith("CP", StringComparison.OrdinalIgnoreCase) &&
                TryParseNumber(encodingName.Substring(2), out codePage))
            {
                return codePage;
            }

            if (encodingName.StartsWith("ISO8859-", StringComparison.OrdinalIgnoreCase) ||
                encodingName.StartsWith("ISO-8859-", StringComparison.OrdinalIgnoreCase))
            {
                string part = encodingName.Substring(encodingName.LastIndexOf('-') + 1);
                if (TryParseNumber(part, out int partNumber) && _iso8859CodePages.TryGetValue(partNumber, out codePage))
                    return codePage;
            }
            else
            {
                // Other names of the .NET encodings ("windows-1251", "shift_jis" ...)
                try
                {
                    return System.Text.Encoding.GetEncoding(encodingName).CodePage;
                }
                catch (ArgumentException)
                {
                    // Not a name .NET knows either
                }
            }

            throw new HpdfException(HpdfErrorCode.InvalidEncodingName, $"Unknown encoding name: {encodingName}");
        }

        /// <summary>
        /// Gets the key of an encoding in the font registry (<see cref="Doc.HpdfDocument.FontRegistry"/>):
        /// the name of a predefined CMap as the CMap spells it ("90ms-RKSJ-V"), else the code page,
        /// followed by "-V" for vertical writing ("1251", "65001", "65001-V"). The names of one encoding
        /// ("CP1251", "windows-1251") have one key; different encodings of one code page
        /// ("90ms-RKSJ-H", "90msp-RKSJ-H", "90ms-RKSJ-V", "UTF-8" and "Identity-V") have different keys.
        /// </summary>
        /// <param name="encodingName">The encoding name, as for <see cref="GetCodePage"/>; null or empty
        /// for WinAnsiEncoding.</param>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> for a name
        /// the library does not know.</exception>
        public static string GetRegistryKey(string? encodingName)
        {
            CMapDefinition? cmap = CMapDefinition.Find(encodingName);
            if (cmap != null)
                return cmap.Name;

            HpdfWritingMode writingMode = encodingName != null && _verticalNames.Contains(encodingName)
                ? HpdfWritingMode.Vertical
                : HpdfWritingMode.Horizontal;
            return GetRegistryKey(GetCodePage(encodingName), writingMode);
        }

        /// <summary>
        /// Gets the key of the encoding of a font in the font registry: the one of its CMap (a
        /// predefined CJK CMap, Identity-H or Identity-V; the name itself for the CMap of an encoder of
        /// the caller), else the one of its code page (see <see cref="GetRegistryKey(string)"/>).
        /// </summary>
        public static string GetRegistryKey(HpdfFont font)
        {
            string? cmapName = font.CMapName;
            if (cmapName == null)
                return GetRegistryKey(font.EncodingCodePage ?? DefaultCodePage, font.WritingMode);

            CMapDefinition? cmap = CMapDefinition.Find(cmapName);
            if (cmap != null)
                return cmap.Name;

            return _codePages.ContainsKey(cmapName) ? GetRegistryKey(cmapName) : cmapName;
        }

        /// <summary>
        /// Tells whether an encoding name names a code page and nothing more: not a predefined CMap
        /// and not an encoding of vertical writing. Such a name stands for every font of its code page
        /// that writes horizontally, whatever CMap it has.
        /// </summary>
        public static bool IsCodePageName(string encodingName)
        {
            return CMapDefinition.Find(encodingName) == null && !_verticalNames.Contains(encodingName);
        }

        private static string GetRegistryKey(int codePage, HpdfWritingMode writingMode)
        {
            string key = codePage.ToString(CultureInfo.InvariantCulture);
            return writingMode == HpdfWritingMode.Vertical ? key + "-V" : key;
        }

        private static bool TryParseNumber(string text, out int number)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
        }
    }
}
