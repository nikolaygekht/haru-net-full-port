/* The predefined CJK CMaps the library has encoders for, as in libharu (hpdf_encoder_cns.c,
   hpdf_encoder_cnt.c, hpdf_encoder_jp.c, hpdf_encoder_kr.c) */
using System;
using System.Collections.Generic;

namespace Haru.Font.CID
{
    /// <summary>
    /// The bytes that make a two-byte character code of a CJK encoding.
    /// </summary>
    internal sealed class CMapByteRule
    {
        public static readonly CMapByteRule Gbk = new CMapByteRule(new[] { (0x81, 0xFE) }, new[] { (0x40, 0xFE) });
        public static readonly CMapByteRule GbEuc = new CMapByteRule(new[] { (0xA1, 0xFE) }, new[] { (0xA1, 0xFE) });
        public static readonly CMapByteRule Big5 = Gbk;  // the same bytes
        public static readonly CMapByteRule ShiftJis = new CMapByteRule(new[] { (0x81, 0x9F), (0xE0, 0xFC) }, new[] { (0x40, 0x7E), (0x80, 0xFC) });
        public static readonly CMapByteRule EucJp = new CMapByteRule(new[] { (0x8E, 0x8E), (0xA1, 0xFE) }, new[] { (0xA0, 0xFE) });
        public static readonly CMapByteRule Uhc = new CMapByteRule(new[] { (0x81, 0xFE) }, new[] { (0x41, 0xFE) });
        public static readonly CMapByteRule KscEuc = new CMapByteRule(new[] { (0xA1, 0xFE) }, new[] { (0xA0, 0xFE) });

        private readonly bool[] _lead = new bool[256];
        private readonly bool[] _trail = new bool[256];

        private CMapByteRule((int First, int Last)[] lead, (int First, int Last)[] trail)
        {
            Fill(_lead, lead);
            Fill(_trail, trail);
        }

        /// <summary>
        /// Gets whether a byte is the first byte of a two-byte character code.
        /// </summary>
        public bool IsLeadByte(byte value) => _lead[value];

        /// <summary>
        /// Gets whether a byte can be the second byte of a two-byte character code.
        /// </summary>
        public bool IsTrailByte(byte value) => _trail[value];

        private static void Fill(bool[] set, (int First, int Last)[] ranges)
        {
            foreach ((int first, int last) in ranges)
            {
                for (int value = first; value <= last; value++)
                    set[value] = true;
            }
        }
    }

    /// <summary>
    /// A predefined CMap: its name in a PDF, the character collection it maps to, the .NET code page
    /// that encodes text to its character codes, and its code to CID mapping. This is the one list of
    /// the CMaps the library knows; the encoders and the encoding names are made from it.
    /// </summary>
    internal sealed class CMapDefinition
    {
        // Simplified Chinese (Adobe-GB1). GB-EUC covers GB 2312 only: its code page is the strict one,
        // so that a character outside the CMap becomes '?' and not a code the CMap cannot read
        public static readonly CMapDefinition GbkEucH = new CMapDefinition("GBK-EUC-H", "GB1", 2, 936, CMapByteRule.Gbk, CIDCMapData.GbkEucH);
        public static readonly CMapDefinition GbEucH = new CMapDefinition("GB-EUC-H", "GB1", 0, 20936, CMapByteRule.GbEuc, CIDCMapData.GbEucH);

        // Traditional Chinese (Adobe-CNS1)
        public static readonly CMapDefinition ETenB5H = new CMapDefinition("ETen-B5-H", "CNS1", 0, 950, CMapByteRule.Big5, CIDCMapData.ETenB5H);

        // Japanese (Adobe-Japan1); 90msp is 90ms with proportional Latin characters
        public static readonly CMapDefinition Ms90RksjH = new CMapDefinition("90ms-RKSJ-H", "Japan1", 2, 932, CMapByteRule.ShiftJis, CIDCMapData.Ms90RksjH);
        public static readonly CMapDefinition Ms90pRksjH = new CMapDefinition("90msp-RKSJ-H", "Japan1", 2, 932, CMapByteRule.ShiftJis, CIDCMapData.Ms90pRksjH);
        public static readonly CMapDefinition EucH = new CMapDefinition("EUC-H", "Japan1", 1, 20932, CMapByteRule.EucJp, CIDCMapData.EucH);

        // Korean (Adobe-Korea1); HW is UHC with half-width Latin characters, KSC-EUC covers KS X 1001 only
        public static readonly CMapDefinition KscMsUhcH = new CMapDefinition("KSCms-UHC-H", "Korea1", 1, 949, CMapByteRule.Uhc, CIDCMapData.KscMsUhcH);
        public static readonly CMapDefinition KscMsUhcHwH = new CMapDefinition("KSCms-UHC-HW-H", "Korea1", 1, 949, CMapByteRule.Uhc, CIDCMapData.KscMsUhcHwH);
        public static readonly CMapDefinition KscEucH = new CMapDefinition("KSC-EUC-H", "Korea1", 0, 51949, CMapByteRule.KscEuc, CIDCMapData.KscEucH);

        private static readonly Dictionary<string, CMapDefinition> _byName = CreateIndex(
            GbkEucH, GbEucH, ETenB5H, Ms90RksjH, Ms90pRksjH, EucH, KscMsUhcH, KscMsUhcHwH, KscEucH);

        private CMapDefinition(string name, string ordering, int supplement, int codePage, CMapByteRule bytes, ushort[] cidRanges)
        {
            Name = name;
            Ordering = ordering;
            Supplement = supplement;
            CodePage = codePage;
            Bytes = bytes;
            CidRanges = cidRanges;
        }

        /// <summary>
        /// Gets the name of the CMap, as it is written to /Encoding of a font.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ordering of the Adobe character collection the CMap maps to ("GB1", "Japan1" ...).
        /// </summary>
        public string Ordering { get; }

        /// <summary>
        /// Gets the supplement of the character collection.
        /// </summary>
        public int Supplement { get; }

        /// <summary>
        /// Gets the .NET code page that encodes text to the character codes of the CMap.
        /// </summary>
        public int CodePage { get; }

        /// <summary>
        /// Gets the bytes that make a two-byte character code.
        /// </summary>
        public CMapByteRule Bytes { get; }

        /// <summary>
        /// Gets the code to CID mapping: sorted ranges that do not overlap, each as three numbers -
        /// the first code, the last code and the CID of the first code.
        /// </summary>
        public ushort[] CidRanges { get; }

        /// <summary>
        /// Gets the names of the CMaps the library has encoders for.
        /// </summary>
        public static IReadOnlyCollection<string> Names => _byName.Keys;

        /// <summary>
        /// Finds a CMap by its name, in any letter case.
        /// </summary>
        /// <returns>The CMap; null if the library does not have it.</returns>
        public static CMapDefinition? Find(string? name)
        {
            return name != null && _byName.TryGetValue(name, out CMapDefinition? definition) ? definition : null;
        }

        private static Dictionary<string, CMapDefinition> CreateIndex(params CMapDefinition[] definitions)
        {
            var index = new Dictionary<string, CMapDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (CMapDefinition definition in definitions)
                index.Add(definition.Name, definition);
            return index;
        }
    }
}
