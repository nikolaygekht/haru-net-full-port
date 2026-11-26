/* Encoding names of the PDF library API (GetFont, RegisterFont) and their code pages */
using System;
using System.Collections.Generic;
using System.Globalization;

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

            // CMaps of the CJK encodings
            ["GB-EUC-H"] = 936,
            ["GB-EUC-V"] = 936,
            ["GBK-EUC-H"] = 936,
            ["GBK-EUC-V"] = 936,
            ["ETen-B5-H"] = 950,
            ["ETen-B5-V"] = 950,
            ["90ms-RKSJ-H"] = 932,
            ["90ms-RKSJ-V"] = 932,
            ["90msp-RKSJ-H"] = 932,
            ["EUC-H"] = 20932,
            ["EUC-V"] = 20932,
            ["KSCms-UHC-H"] = 949,
            ["KSCms-UHC-HW-H"] = 949,
            ["KSCms-UHC-HW-V"] = 949,
            ["KSC-EUC-H"] = 51949,
            ["KSC-EUC-V"] = 51949,
        };

        // ISO 8859 part number -> Windows code page (there is no part 12)
        private static readonly Dictionary<int, int> _iso8859CodePages = new Dictionary<int, int>
        {
            [1] = 28591, [2] = 28592, [3] = 28593, [4] = 28594, [5] = 28595, [6] = 28596, [7] = 28597,
            [8] = 28598, [9] = 28599, [10] = 28600, [11] = 28601, [13] = 28603, [14] = 28604, [15] = 28605,
            [16] = 28606,
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

        private static bool TryParseNumber(string text, out int number)
        {
            return int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0;
        }
    }
}
