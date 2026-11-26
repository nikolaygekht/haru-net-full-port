using System;
using System.Text;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// Represents a TrueType font table entry.
    /// </summary>
    internal class TrueTypeTable
    {
        public string Tag { get; set; } = string.Empty;
        public uint CheckSum { get; set; }
        public uint Offset { get; set; }
        public uint Length { get; set; }
    }

    /// <summary>
    /// Represents the TrueType offset table (table directory).
    /// </summary>
    internal class TrueTypeOffsetTable
    {
        public uint SfntVersion { get; set; }
        public ushort NumTables { get; set; }
        public ushort SearchRange { get; set; }
        public ushort EntrySelector { get; set; }
        public ushort RangeShift { get; set; }
        public TrueTypeTable[] Tables { get; set; } = Array.Empty<TrueTypeTable>();
    }

    /// <summary>
    /// Represents the 'head' table (font header).
    /// </summary>
    internal class TrueTypeHead
    {
        public byte[] VersionNumber { get; set; } = new byte[4];  // 4 bytes
        public uint FontRevision { get; set; }
        public uint CheckSumAdjustment { get; set; }
        public uint MagicNumber { get; set; }
        public ushort Flags { get; set; }
        public ushort UnitsPerEm { get; set; }
        public byte[] Created { get; set; } = new byte[8];  // 8 bytes
        public byte[] Modified { get; set; } = new byte[8];  // 8 bytes
        public short XMin { get; set; }
        public short YMin { get; set; }
        public short XMax { get; set; }
        public short YMax { get; set; }
        public ushort MacStyle { get; set; }
        public ushort LowestRecPPEM { get; set; }
        public short FontDirectionHint { get; set; }
        public short IndexToLocFormat { get; set; }
        public short GlyphDataFormat { get; set; }
    }

    /// <summary>
    /// Represents the 'maxp' table (maximum profile).
    /// </summary>
    internal class TrueTypeMaxp
    {
        public uint Version { get; set; }
        public ushort NumGlyphs { get; set; }
        public ushort MaxPoints { get; set; }
        public ushort MaxContours { get; set; }
        public ushort MaxCompositePoints { get; set; }
        public ushort MaxCompositeContours { get; set; }
        public ushort MaxZones { get; set; }
        public ushort MaxTwilightPoints { get; set; }
        public ushort MaxStorage { get; set; }
        public ushort MaxFunctionDefs { get; set; }
        public ushort MaxInstructionDefs { get; set; }
        public ushort MaxStackElements { get; set; }
        public ushort MaxSizeOfInstructions { get; set; }
        public ushort MaxComponentElements { get; set; }
        public ushort MaxComponentDepth { get; set; }
    }

    /// <summary>
    /// Represents the 'hhea' table (horizontal header).
    /// </summary>
    internal class TrueTypeHhea
    {
        public uint Version { get; set; }
        public short Ascender { get; set; }
        public short Descender { get; set; }
        public short LineGap { get; set; }
        public ushort AdvanceWidthMax { get; set; }
        public short MinLeftSideBearing { get; set; }
        public short MinRightSideBearing { get; set; }
        public short XMaxExtent { get; set; }
        public short CaretSlopeRise { get; set; }
        public short CaretSlopeRun { get; set; }
        public short CaretOffset { get; set; }
        public short Reserved1 { get; set; }
        public short Reserved2 { get; set; }
        public short Reserved3 { get; set; }
        public short Reserved4 { get; set; }
        public short MetricDataFormat { get; set; }
        public ushort NumberOfHMetrics { get; set; }
    }

    /// <summary>
    /// Represents a horizontal metric entry from 'hmtx' table.
    /// </summary>
    internal class TrueTypeLongHorMetric
    {
        public ushort AdvanceWidth { get; set; }
        public short LeftSideBearing { get; set; }
    }

    /// <summary>
    /// Represents the 'cmap' format 4 subtable.
    /// </summary>
    internal class TrueTypeCmapFormat4
    {
        public ushort Format { get; set; }
        public ushort Length { get; set; }
        public ushort Language { get; set; }
        public ushort SegCountX2 { get; set; }
        public ushort SearchRange { get; set; }
        public ushort EntrySelector { get; set; }
        public ushort RangeShift { get; set; }
        public ushort[] EndCount { get; set; } = Array.Empty<ushort>();
        public ushort ReservedPad { get; set; }
        public ushort[] StartCount { get; set; } = Array.Empty<ushort>();
        public short[] IdDelta { get; set; } = Array.Empty<short>();
        public ushort[] IdRangeOffset { get; set; } = Array.Empty<ushort>();
        public ushort[] GlyphIdArray { get; set; } = Array.Empty<ushort>();

        /// <summary>
        /// Gets the glyph ID of a character of the Basic Multilingual Plane, 0 (.notdef) if the font
        /// has no glyph for it.
        /// </summary>
        public ushort GetGlyphId(ushort unicode)
        {
            int segCount = SegCountX2 / 2;
            if (segCount == 0)
                return 0;

            // The segments are sorted by their end code: find the first one that ends at or
            // after the character
            int low = 0;
            int high = segCount - 1;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (EndCount[middle] < unicode)
                    low = middle + 1;
                else
                    high = middle;
            }

            int i = low;
            if (unicode > EndCount[i] || unicode < StartCount[i])
                return 0;

            if (IdRangeOffset[i] == 0)
                return (ushort)((unicode + IdDelta[i]) & 0xFFFF);

            int offset = IdRangeOffset[i] / 2 + (unicode - StartCount[i]) - (segCount - i);
            if (offset >= 0 && offset < GlyphIdArray.Length)
            {
                ushort glyphId = GlyphIdArray[offset];
                if (glyphId != 0)
                    return (ushort)((glyphId + IdDelta[i]) & 0xFFFF);
            }

            return 0;
        }
    }

    /// <summary>
    /// Represents the 'cmap' format 12 subtable (segmented coverage): the mapping of the whole
    /// Unicode range, including the characters above U+FFFF, as groups of consecutive characters
    /// with consecutive glyph IDs.
    /// </summary>
    internal class TrueTypeCmapFormat12
    {
        public uint[] StartCharCode { get; set; } = Array.Empty<uint>();
        public uint[] EndCharCode { get; set; } = Array.Empty<uint>();
        public uint[] StartGlyphId { get; set; } = Array.Empty<uint>();

        /// <summary>
        /// Gets the glyph ID of a Unicode code point, 0 (.notdef) if the font has no glyph for it.
        /// </summary>
        public ushort GetGlyphId(int codePoint)
        {
            // The groups are sorted by character code: find the first one that ends at or after it
            int low = 0;
            int high = EndCharCode.Length - 1;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (EndCharCode[middle] < codePoint)
                    low = middle + 1;
                else
                    high = middle;
            }

            if (high < 0 || codePoint < StartCharCode[low] || codePoint > EndCharCode[low])
                return 0;

            uint glyphId = StartGlyphId[low] + ((uint)codePoint - StartCharCode[low]);
            return glyphId <= ushort.MaxValue ? (ushort)glyphId : (ushort)0;
        }
    }

    /// <summary>
    /// The Unicode character to glyph mapping of a font ('cmap' table): the subtable of the Basic
    /// Multilingual Plane (format 4) and the subtable of all of Unicode (format 12); a font has
    /// at least one of them.
    /// </summary>
    internal sealed class TrueTypeCmap
    {
        private readonly TrueTypeCmapFormat4? _bmp;
        private readonly TrueTypeCmapFormat12? _full;

        public TrueTypeCmap(TrueTypeCmapFormat4? bmp, TrueTypeCmapFormat12? full)
        {
            _bmp = bmp;
            _full = full;
        }

        /// <summary>
        /// Gets the glyph ID of a character of the Basic Multilingual Plane, 0 (.notdef) if the font
        /// has no glyph for it.
        /// </summary>
        public ushort GetGlyphId(ushort unicode)
        {
            // A font may have only the subtable for all of Unicode
            return _bmp != null ? _bmp.GetGlyphId(unicode) : _full?.GetGlyphId(unicode) ?? 0;
        }

        /// <summary>
        /// Gets the glyph ID of a Unicode character, 0 (.notdef) if the font has no glyph for it.
        /// A character above U+FFFF needs the subtable for all of Unicode.
        /// </summary>
        public ushort GetGlyphId(Rune character)
        {
            return character.IsBmp
                ? GetGlyphId((ushort)character.Value)
                : _full?.GetGlyphId(character.Value) ?? 0;
        }
    }

    /// <summary>
    /// Represents a name record from the 'name' table.
    /// </summary>
    internal class TrueTypeNameRecord
    {
        public ushort PlatformId { get; set; }
        public ushort EncodingId { get; set; }
        public ushort LanguageId { get; set; }
        public ushort NameId { get; set; }
        public ushort Length { get; set; }
        public ushort Offset { get; set; }
    }

    /// <summary>
    /// Represents the 'name' table.
    /// </summary>
    internal class TrueTypeNameTable
    {
        public ushort Format { get; set; }
        public ushort Count { get; set; }
        public ushort StringOffset { get; set; }
        public TrueTypeNameRecord[] NameRecords { get; set; } = Array.Empty<TrueTypeNameRecord>();
    }

    /// <summary>
    /// Represents the 'OS/2' table.
    /// </summary>
    internal class TrueTypeOS2
    {
        public ushort Version { get; set; }
        public short XAvgCharWidth { get; set; }
        public ushort WeightClass { get; set; }
        public ushort WidthClass { get; set; }
        public ushort FsType { get; set; }
        public short YSubscriptXSize { get; set; }
        public short YSubscriptYSize { get; set; }
        public short YSubscriptXOffset { get; set; }
        public short YSubscriptYOffset { get; set; }
        public short YSuperscriptXSize { get; set; }
        public short YSuperscriptYSize { get; set; }
        public short YSuperscriptXOffset { get; set; }
        public short YSuperscriptYOffset { get; set; }
        public short YStrikeoutSize { get; set; }
        public short YStrikeoutPosition { get; set; }
        public short SFamilyClass { get; set; }
        public byte[] Panose { get; set; } = new byte[10];  // 10 bytes
        public uint UnicodeRange1 { get; set; }
        public uint UnicodeRange2 { get; set; }
        public uint UnicodeRange3 { get; set; }
        public uint UnicodeRange4 { get; set; }
        public byte[] AchVendID { get; set; } = new byte[4];  // 4 bytes
        public ushort FsSelection { get; set; }
        public ushort FirstCharIndex { get; set; }
        public ushort LastCharIndex { get; set; }
        public short STypoAscender { get; set; }
        public short STypoDescender { get; set; }
        public short STypoLineGap { get; set; }
        public ushort UsWinAscent { get; set; }
        public ushort UsWinDescent { get; set; }
        public uint CodePageRange1 { get; set; }
        public uint CodePageRange2 { get; set; }

        // Version 2 and later; 0 in an older table
        public short SxHeight { get; set; }
        public short SCapHeight { get; set; }
    }

    /// <summary>
    /// Represents the 'post' table (PostScript information).
    /// </summary>
    internal class TrueTypePost
    {
        public uint Version { get; set; }
        public int ItalicAngle { get; set; }  // Fixed-point 16.16
        public short UnderlinePosition { get; set; }
        public short UnderlineThickness { get; set; }
        public uint IsFixedPitch { get; set; }
        public uint MinMemType42 { get; set; }
        public uint MaxMemType42 { get; set; }
        public uint MinMemType1 { get; set; }
        public uint MaxMemType1 { get; set; }
    }
}
