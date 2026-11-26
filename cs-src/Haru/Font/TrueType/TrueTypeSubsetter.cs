/* Subsetting of TrueType font programs for embedding, as in libharu (hpdf_fontdef_tt.c, RecreateGLYF):
   glyph IDs stay as they are, only the outlines of the unused glyphs are dropped. */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// Creates a subset of a TrueType font program that contains the outlines of the glyphs in use only.
    /// The glyphs keep their IDs: an unused glyph becomes an empty glyph, so the content streams,
    /// the widths, a CIDToGIDMap and the font's cmap stay valid. Tables a PDF viewer does not need
    /// (layout, vertical metrics, bitmaps ...) are dropped.
    /// </summary>
    internal static class TrueTypeSubsetter
    {
        /// <summary>
        /// Tables of a font program embedded for a CIDFontType2: the glyphs are selected by glyph ID,
        /// so only the outlines, their metrics and the hinting programs are needed.
        /// </summary>
        public static readonly string[] CidFontTables =
        {
            "cvt ", "fpgm", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "prep",
        };

        /// <summary>
        /// Tables of a font program embedded for a simple TrueType font (the tables libharu keeps):
        /// the glyphs are found through the font's cmap and glyph names.
        /// </summary>
        public static readonly string[] SimpleFontTables =
        {
            "OS/2", "cmap", "cvt ", "fpgm", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "name", "post", "prep",
        };

        private const uint TrueTypeVersion = 0x00010000;
        private const uint AppleTrueTypeVersion = 0x74727565; // 'true'
        private const uint ChecksumMagic = 0xB1B0AFBA;

        private const int HeadChecksumAdjustmentOffset = 8;
        private const int HeadIndexToLocFormatOffset = 50;
        private const int HeadLength = 54;
        private const int MaxpNumGlyphsOffset = 4;

        // Flags of a component of a composite glyph
        private const int Arg1And2AreWords = 0x0001;
        private const int WeHaveAScale = 0x0008;
        private const int MoreComponents = 0x0020;
        private const int WeHaveAnXAndYScale = 0x0040;
        private const int WeHaveATwoByTwo = 0x0080;

        /// <summary>
        /// Creates the subset font program.
        /// </summary>
        /// <param name="fontData">The whole font file.</param>
        /// <param name="glyphs">The glyphs in use. Glyph 0 (.notdef) and the components of composite
        /// glyphs are added automatically.</param>
        /// <param name="tables">The tags of the tables to keep, in any order.</param>
        /// <returns>The subset font program, or null if the font cannot be subset: it has no TrueType
        /// outlines (an OpenType font with CFF outlines, a font collection) or its tables are damaged.
        /// Such a font is embedded as it is.</returns>
        public static byte[]? CreateSubset(byte[] fontData, IEnumerable<ushort> glyphs, IReadOnlyCollection<string> tables)
        {
            ArgumentNullException.ThrowIfNull(fontData);
            ArgumentNullException.ThrowIfNull(glyphs);
            ArgumentNullException.ThrowIfNull(tables);

            ReadOnlySpan<byte> font = fontData;

            var directory = ReadDirectory(font);
            if (directory is null ||
                !directory.TryGetValue("head", out var head) || head.Length < HeadLength ||
                !directory.TryGetValue("maxp", out var maxp) || maxp.Length < MaxpNumGlyphsOffset + 2 ||
                !directory.TryGetValue("loca", out var loca) ||
                !directory.TryGetValue("glyf", out var glyf))
            {
                return null;
            }

            int numGlyphs = BinaryPrimitives.ReadUInt16BigEndian(font.Slice(maxp.Offset + MaxpNumGlyphsOffset));
            bool longLoca = BinaryPrimitives.ReadInt16BigEndian(font.Slice(head.Offset + HeadIndexToLocFormatOffset)) != 0;

            int[]? offsets = ReadLoca(font.Slice(loca.Offset, loca.Length), numGlyphs, longLoca, glyf.Length);
            if (offsets is null)
                return null;

            ReadOnlySpan<byte> glyfData = font.Slice(glyf.Offset, glyf.Length);
            bool[] used = FindUsedGlyphs(glyfData, offsets, glyphs);

            byte[] newGlyf = BuildGlyf(glyfData, offsets, used, longLoca, out int[] newOffsets);
            byte[] newLoca = BuildLoca(newOffsets, longLoca);

            return BuildFont(font, directory, tables, newGlyf, newLoca);
        }

        /// <summary>
        /// Creates the tag that marks a font name as a subset ("ABCDEF+Name"): six uppercase letters
        /// derived from the font name and the glyphs, so the same subset always gets the same tag
        /// and different subsets of one font get different names.
        /// </summary>
        /// <param name="fontName">The name of the font.</param>
        /// <param name="glyphs">The glyphs in use, in a defined (sorted) order.</param>
        public static string CreateSubsetTag(string fontName, IEnumerable<ushort> glyphs)
        {
            ArgumentNullException.ThrowIfNull(fontName);
            ArgumentNullException.ThrowIfNull(glyphs);

            // FNV-1a, 64 bit
            ulong hash = 14695981039346656037UL;
            foreach (char c in fontName)
            {
                hash = (hash ^ c) * 1099511628211UL;
            }
            foreach (ushort glyph in glyphs)
            {
                hash = (hash ^ (byte)(glyph >> 8)) * 1099511628211UL;
                hash = (hash ^ (byte)glyph) * 1099511628211UL;
            }

            Span<char> tag = stackalloc char[6];
            for (int i = 0; i < tag.Length; i++)
            {
                tag[i] = (char)('A' + (int)(hash % 26));
                hash /= 26;
            }
            return new string(tag);
        }

        private static Dictionary<string, (int Offset, int Length)>? ReadDirectory(ReadOnlySpan<byte> font)
        {
            if (font.Length < 12)
                return null;

            uint version = BinaryPrimitives.ReadUInt32BigEndian(font);
            if (version != TrueTypeVersion && version != AppleTrueTypeVersion)
                return null;

            int numTables = BinaryPrimitives.ReadUInt16BigEndian(font.Slice(4));
            if (font.Length < 12 + numTables * 16)
                return null;

            var directory = new Dictionary<string, (int Offset, int Length)>(numTables, StringComparer.Ordinal);
            for (int i = 0; i < numTables; i++)
            {
                ReadOnlySpan<byte> record = font.Slice(12 + i * 16, 16);
                string tag = System.Text.Encoding.Latin1.GetString(record.Slice(0, 4));
                uint offset = BinaryPrimitives.ReadUInt32BigEndian(record.Slice(8));
                uint length = BinaryPrimitives.ReadUInt32BigEndian(record.Slice(12));
                if ((ulong)offset + length > (ulong)font.Length)
                    return null;

                directory[tag] = ((int)offset, (int)length);
            }
            return directory;
        }

        /// <summary>
        /// Reads the offsets of the glyphs in the glyf table (numGlyphs + 1 entries).
        /// </summary>
        private static int[]? ReadLoca(ReadOnlySpan<byte> loca, int numGlyphs, bool longLoca, int glyfLength)
        {
            if (loca.Length < (numGlyphs + 1) * (longLoca ? 4 : 2))
                return null;

            var offsets = new int[numGlyphs + 1];
            long previous = 0;
            for (int i = 0; i <= numGlyphs; i++)
            {
                long offset = longLoca
                    ? BinaryPrimitives.ReadUInt32BigEndian(loca.Slice(i * 4))
                    : BinaryPrimitives.ReadUInt16BigEndian(loca.Slice(i * 2)) * 2L;
                if (offset < previous || offset > glyfLength)
                    return null;

                offsets[i] = (int)offset;
                previous = offset;
            }
            return offsets;
        }

        /// <summary>
        /// Marks the glyphs to keep: glyph 0, the glyphs in use and, recursively, the glyphs they
        /// are composed of.
        /// </summary>
        private static bool[] FindUsedGlyphs(ReadOnlySpan<byte> glyf, int[] offsets, IEnumerable<ushort> glyphs)
        {
            int numGlyphs = offsets.Length - 1;
            var used = new bool[numGlyphs];
            var pending = new Stack<int>();

            void Use(int glyph)
            {
                if (glyph < numGlyphs && !used[glyph])
                {
                    used[glyph] = true;
                    pending.Push(glyph);
                }
            }

            Use(0);
            foreach (ushort glyph in glyphs)
                Use(glyph);

            while (pending.Count > 0)
            {
                int glyph = pending.Pop();
                ReadOnlySpan<byte> data = glyf.Slice(offsets[glyph], offsets[glyph + 1] - offsets[glyph]);

                // A composite glyph has a negative number of contours, followed by its components
                if (data.Length < 10 || BinaryPrimitives.ReadInt16BigEndian(data) >= 0)
                    continue;

                int position = 10;
                int flags;
                do
                {
                    if (position + 4 > data.Length)
                        break;

                    flags = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(position));
                    Use(BinaryPrimitives.ReadUInt16BigEndian(data.Slice(position + 2)));
                    position += 4;

                    position += (flags & Arg1And2AreWords) != 0 ? 4 : 2;
                    if ((flags & WeHaveATwoByTwo) != 0)
                        position += 8;
                    else if ((flags & WeHaveAnXAndYScale) != 0)
                        position += 4;
                    else if ((flags & WeHaveAScale) != 0)
                        position += 2;
                }
                while ((flags & MoreComponents) != 0);
            }

            return used;
        }

        private static byte[] BuildGlyf(ReadOnlySpan<byte> glyf, int[] offsets, bool[] used, bool longLoca, out int[] newOffsets)
        {
            // The short loca format stores offset / 2, so glyphs start at even offsets;
            // the long format aligns to 4 bytes as recommended
            int alignment = longLoca ? 4 : 2;

            newOffsets = new int[offsets.Length];
            int size = 0;
            for (int glyph = 0; glyph < used.Length; glyph++)
            {
                newOffsets[glyph] = size;
                if (used[glyph])
                    size += Align(offsets[glyph + 1] - offsets[glyph], alignment);
            }
            newOffsets[used.Length] = size;

            var newGlyf = new byte[size];
            for (int glyph = 0; glyph < used.Length; glyph++)
            {
                if (used[glyph])
                    glyf.Slice(offsets[glyph], offsets[glyph + 1] - offsets[glyph]).CopyTo(newGlyf.AsSpan(newOffsets[glyph]));
            }
            return newGlyf;
        }

        private static byte[] BuildLoca(int[] offsets, bool longLoca)
        {
            var loca = new byte[offsets.Length * (longLoca ? 4 : 2)];
            for (int i = 0; i < offsets.Length; i++)
            {
                if (longLoca)
                    BinaryPrimitives.WriteUInt32BigEndian(loca.AsSpan(i * 4), (uint)offsets[i]);
                else
                    BinaryPrimitives.WriteUInt16BigEndian(loca.AsSpan(i * 2), (ushort)(offsets[i] / 2));
            }
            return loca;
        }

        /// <summary>
        /// Writes the font file: the table directory, sorted by tag, and the tables, each aligned to
        /// four bytes, with the checksums of the tables and of the whole file.
        /// </summary>
        private static byte[] BuildFont(
            ReadOnlySpan<byte> font,
            Dictionary<string, (int Offset, int Length)> directory,
            IReadOnlyCollection<string> tables,
            byte[] newGlyf,
            byte[] newLoca)
        {
            var tags = new List<string>(tables.Count);
            foreach (string tag in tables)
            {
                if (directory.ContainsKey(tag) && !tags.Contains(tag))
                    tags.Add(tag);
            }
            tags.Sort(StringComparer.Ordinal);

            int size = 12 + tags.Count * 16;
            foreach (string tag in tags)
                size += Align(TableLength(tag), 4);

            var output = new byte[size];
            Span<byte> result = output;

            int entrySelector = 0;
            while ((2 << entrySelector) <= tags.Count)
                entrySelector++;
            int searchRange = (1 << entrySelector) * 16;

            BinaryPrimitives.WriteUInt32BigEndian(result, BinaryPrimitives.ReadUInt32BigEndian(font));
            BinaryPrimitives.WriteUInt16BigEndian(result.Slice(4), (ushort)tags.Count);
            BinaryPrimitives.WriteUInt16BigEndian(result.Slice(6), (ushort)searchRange);
            BinaryPrimitives.WriteUInt16BigEndian(result.Slice(8), (ushort)entrySelector);
            BinaryPrimitives.WriteUInt16BigEndian(result.Slice(10), (ushort)(tags.Count * 16 - searchRange));

            int offset = 12 + tags.Count * 16;
            int headOffset = -1;
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                int length = TableLength(tag);
                Span<byte> table = result.Slice(offset, length);

                if (tag == "glyf")
                    newGlyf.CopyTo(table);
                else if (tag == "loca")
                    newLoca.CopyTo(table);
                else
                    font.Slice(directory[tag].Offset, length).CopyTo(table);

                if (tag == "head")
                {
                    // The checksum of the head table is computed with a zero file checksum
                    headOffset = offset;
                    table.Slice(HeadChecksumAdjustmentOffset, 4).Clear();
                }

                Span<byte> record = result.Slice(12 + i * 16, 16);
                System.Text.Encoding.Latin1.GetBytes(tag, record);
                BinaryPrimitives.WriteUInt32BigEndian(record.Slice(4), Checksum(result.Slice(offset, Align(length, 4))));
                BinaryPrimitives.WriteUInt32BigEndian(record.Slice(8), (uint)offset);
                BinaryPrimitives.WriteUInt32BigEndian(record.Slice(12), (uint)length);

                offset += Align(length, 4);
            }

            if (headOffset >= 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(
                    result.Slice(headOffset + HeadChecksumAdjustmentOffset),
                    unchecked(ChecksumMagic - Checksum(result)));
            }

            return output;

            int TableLength(string tag) => tag switch
            {
                "glyf" => newGlyf.Length,
                "loca" => newLoca.Length,
                _ => directory[tag].Length,
            };
        }

        private static int Align(int length, int alignment) => (length + alignment - 1) / alignment * alignment;

        /// <summary>
        /// Sum of the big-endian 32-bit words of data whose length is a multiple of four.
        /// </summary>
        private static uint Checksum(ReadOnlySpan<byte> data)
        {
            uint sum = 0;
            for (int i = 0; i + 4 <= data.Length; i += 4)
            {
                sum = unchecked(sum + BinaryPrimitives.ReadUInt32BigEndian(data.Slice(i)));
            }
            return sum;
        }
    }
}
