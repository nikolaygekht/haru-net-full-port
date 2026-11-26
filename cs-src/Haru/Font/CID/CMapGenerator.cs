using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Haru.Font.CID
{
    /// <summary>
    /// Generates CMap streams for CID fonts.
    /// CMaps map character codes to CIDs (Character IDs) or Unicode values.
    /// </summary>
    internal static class CMapGenerator
    {
        /// <summary>
        /// Generates a ToUnicode CMap for CID fonts.
        /// This enables text extraction and searching in PDF viewers.
        ///
        /// The character codes are the 2-byte codes of the content stream (glyph IDs for Identity-H
        /// with an identity CIDToGIDMap); each maps to a Unicode code point, written as UTF-16BE.
        /// </summary>
        /// <param name="systemInfo">CID system info</param>
        /// <param name="codeToUnicode">Mapping from character code to Unicode code point; may be empty
        /// (a font no text was shown with)</param>
        /// <returns>ToUnicode CMap content as string</returns>
        public static string GenerateToUnicodeCMap(
            CIDSystemInfo systemInfo,
            IReadOnlyDictionary<ushort, int> codeToUnicode)
        {
            ArgumentNullException.ThrowIfNull(codeToUnicode);

            var sb = new StringBuilder();

            // CMap header
            sb.AppendLine("/CIDInit /ProcSet findresource begin");
            sb.AppendLine("12 dict begin");
            sb.AppendLine("begincmap");

            // CIDSystemInfo dictionary
            sb.AppendLine("/CIDSystemInfo");
            sb.AppendLine("<< /Registry (Adobe)");
            sb.AppendLine("/Ordering (UCS)");  // UCS for ToUnicode
            sb.AppendLine("/Supplement 0");
            sb.AppendLine(">> def");

            sb.AppendLine("/CMapName /Adobe-Identity-UCS def");
            sb.AppendLine("/CMapType 2 def");  // Type 2 for ToUnicode

            // Code space range (2-byte for CID fonts)
            sb.AppendLine("1 begincodespacerange");
            sb.AppendLine("<0000> <FFFF>");  // Full 2-byte range
            sb.AppendLine("endcodespacerange");

            // Generate bfrange entries
            var ranges = BuildBfRanges(codeToUnicode);
            if (ranges.Count > 0)
            {
                // Split into chunks of 100 (PDF spec recommendation)
                var chunks = ranges.Select((range, index) => new { range, index })
                                  .GroupBy(x => x.index / 100)
                                  .Select(g => g.Select(x => x.range).ToList())
                                  .ToList();

                foreach (var chunk in chunks)
                {
                    sb.AppendLine($"{chunk.Count} beginbfrange");
                    foreach (var range in chunk)
                    {
                        sb.AppendLine(range);
                    }
                    sb.AppendLine("endbfrange");
                }
            }

            // CMap footer
            sb.AppendLine("endcmap");
            sb.AppendLine("CMapName currentdict /CMap defineresource pop");
            sb.AppendLine("end");
            sb.AppendLine("end");

            return sb.ToString();
        }

        /// <summary>
        /// Builds bfrange entries for the ToUnicode CMap.
        /// Groups consecutive codes with consecutive Unicode values into ranges. A range stays within
        /// one high byte of the code and one high byte of the Unicode value: readers increment only
        /// the last byte of the destination string.
        /// </summary>
        /// <param name="codeToUnicode">Character code to Unicode mapping</param>
        /// <returns>List of bfrange entries</returns>
        private static List<string> BuildBfRanges(IReadOnlyDictionary<ushort, int> codeToUnicode)
        {
            var ranges = new List<string>();
            if (codeToUnicode.Count == 0)
                return ranges;

            var codes = new List<ushort>(codeToUnicode.Keys);
            codes.Sort();

            ushort rangeStart = codes[0];
            ushort previous = codes[0];
            for (int i = 1; i < codes.Count; i++)
            {
                ushort current = codes[i];
                int currentUnicode = codeToUnicode[current];
                int previousUnicode = codeToUnicode[previous];

                bool continuesRange = current == previous + 1 &&
                                      currentUnicode == previousUnicode + 1 &&
                                      (current >> 8) == (previous >> 8) &&
                                      (currentUnicode >> 8) == (previousUnicode >> 8);

                if (!continuesRange)
                {
                    ranges.Add(FormatBfRange(rangeStart, previous, codeToUnicode[rangeStart]));
                    rangeStart = current;
                }
                previous = current;
            }

            ranges.Add(FormatBfRange(rangeStart, previous, codeToUnicode[rangeStart]));

            return ranges;
        }

        /// <summary>
        /// Formats a bfrange entry: &lt;srcStart&gt; &lt;srcEnd&gt; &lt;dstStart&gt;, the destination
        /// as UTF-16BE (a surrogate pair for a code point above U+FFFF).
        /// </summary>
        private static string FormatBfRange(ushort startCode, ushort endCode, int startUnicode)
        {
            string unicode = startUnicode <= 0xFFFF
                ? startUnicode.ToString("X4", CultureInfo.InvariantCulture)
                : string.Concat(
                    (0xD800 + ((startUnicode - 0x10000) >> 10)).ToString("X4", CultureInfo.InvariantCulture),
                    (0xDC00 + ((startUnicode - 0x10000) & 0x3FF)).ToString("X4", CultureInfo.InvariantCulture));

            return string.Create(CultureInfo.InvariantCulture, $"<{startCode:X4}> <{endCode:X4}> <{unicode}>");
        }

        /// <summary>
        /// Generates an Identity-H CMap (if needed).
        /// For most cases, we can just use the predefined "/Identity-H" name in PDF.
        /// This method is provided for completeness but may not be used in the implementation.
        /// </summary>
        /// <param name="systemInfo">CID system info</param>
        /// <returns>Identity-H CMap content (usually not needed, use "/Identity-H" name instead)</returns>
        public static string GenerateIdentityHCMap(CIDSystemInfo systemInfo)
        {
            var sb = new StringBuilder();

            // CMap header
            sb.AppendLine("/CIDInit /ProcSet findresource begin");
            sb.AppendLine("12 dict begin");
            sb.AppendLine("begincmap");

            // CIDSystemInfo dictionary
            sb.AppendLine("/CIDSystemInfo");
            sb.AppendLine($"<< /Registry ({systemInfo.Registry})");
            sb.AppendLine($"/Ordering ({systemInfo.Ordering})");
            sb.AppendLine($"/Supplement {systemInfo.Supplement}");
            sb.AppendLine(">> def");

            sb.AppendLine("/CMapName /Identity-H def");
            sb.AppendLine("/CMapType 1 def");  // Type 1 for encoding CMap
            sb.AppendLine("/WMode 0 def");     // Horizontal writing

            // Code space range
            sb.AppendLine("1 begincodespacerange");
            sb.AppendLine("<0000> <FFFF>");
            sb.AppendLine("endcodespacerange");

            // Identity mapping: all codes map to themselves
            // For Identity-H, we typically use the predefined name instead
            sb.AppendLine("1 begincidrange");
            sb.AppendLine("<0000> <FFFF> 0");
            sb.AppendLine("endcidrange");

            // CMap footer
            sb.AppendLine("endcmap");
            sb.AppendLine("CMapName currentdict /CMap defineresource pop");
            sb.AppendLine("end");
            sb.AppendLine("end");

            return sb.ToString();
        }
    }
}
