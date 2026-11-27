/* A TrueType font file, parsed once */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Haru.Types;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// A TrueType font file: its bytes and what a PDF font needs from its tables - the names, the
    /// character to glyph mapping, the glyph widths, the metrics and the vertical metrics. This is the one place where a
    /// font file is read: the simple TrueType font, the CID font and the font data pool all work
    /// with it. An object does not change after it is created, so it can be shared by any number of
    /// fonts, documents and threads.
    /// </summary>
    internal sealed class TrueTypeFontFile
    {
        private readonly TrueTypeLongHorMetric[] _hMetrics;
        private readonly TrueTypeCmap _cmap;
        private readonly Dictionary<ushort, ushort> _verticalGlyphs;
        private readonly TrueTypeVerticalMetric[]? _vMetrics;

        // Where the outlines are ('loca' and 'glyf'); null in a font without TrueType outlines
        private readonly TrueTypeTable? _loca;
        private readonly TrueTypeTable? _glyf;
        private readonly bool _longLocaOffsets;

        private TrueTypeFontFile(byte[] data, TrueTypeParser parser)
        {
            Data = data;

            TrueTypeOffsetTable offsetTable = parser.ParseOffsetTable();

            Head = parser.ParseHead(parser.FindTable(offsetTable, "head")!);
            TrueTypeMaxp maxp = parser.ParseMaxp(parser.FindTable(offsetTable, "maxp")!);
            Hhea = parser.ParseHhea(parser.FindTable(offsetTable, "hhea")!);
            _hMetrics = parser.ParseHmtx(parser.FindTable(offsetTable, "hmtx")!, Hhea.NumberOfHMetrics, maxp.NumGlyphs);

            Names = TrueTypeFontNames.Read(parser, offsetTable);
            _cmap = parser.ParseCmap(parser.FindTable(offsetTable, "cmap"));

            TrueTypeTable? os2Table = parser.FindTable(offsetTable, "OS/2");
            if (os2Table != null)
                OS2 = parser.ParseOS2(os2Table);

            TrueTypeTable? postTable = parser.FindTable(offsetTable, "post");
            if (postTable != null)
                Post = parser.ParsePost(postTable);

            _loca = parser.FindTable(offsetTable, "loca");
            _glyf = parser.FindTable(offsetTable, "glyf");
            _longLocaOffsets = Head.IndexToLocFormat != 0;

            // The heights of the capital and of the small letters, as libharu takes them: from the
            // outlines of 'H' and 'x'; a font without TrueType outlines may give them in the OS/2 table
            short? capHeight = GetLetterTop('H') ?? GetOS2Height(OS2?.SCapHeight);
            short? xHeight = GetLetterTop('x') ?? GetOS2Height(OS2?.SxHeight);

            CapHeight = Scale(capHeight ?? Hhea.Ascender);
            XHeight = Scale(xHeight ?? 0);

            _verticalGlyphs = parser.ParseVerticalSubstitutions(parser.FindTable(offsetTable, "GSUB"));
            _vMetrics = parser.ParseVerticalMetrics(
                parser.FindTable(offsetTable, "vhea"), parser.FindTable(offsetTable, "vmtx"), maxp.NumGlyphs);
        }

        private short? GetLetterTop(char letter)
        {
            ushort glyphId = _cmap.GetGlyphId(letter);
            return glyphId != 0 ? ReadGlyphTop(glyphId) : null;
        }

        /// <summary>
        /// Reads the top of the bounding box of a glyph (yMax of its header in the 'glyf' table),
        /// in font units, from the bytes of the file.
        /// </summary>
        /// <returns>The top of the glyph; null if the font has no TrueType outlines, the glyph has
        /// no outline or the tables do not hold it.</returns>
        private short? ReadGlyphTop(ushort glyphId)
        {
            const int GlyphHeaderLength = 10;  // numberOfContours, xMin, yMin, xMax, yMax
            const int YMaxOffset = 8;

            if (_loca is null || _glyf is null)
                return null;

            // The glyph takes the bytes from its offset to the offset of the next glyph
            int entryLength = _longLocaOffsets ? 4 : 2;
            long entry = _loca.Offset + (long)glyphId * entryLength;
            if ((glyphId + 2L) * entryLength > _loca.Length || entry + 2L * entryLength > Data.Length)
                return null;

            ReadOnlySpan<byte> offsets = Data.AsSpan((int)entry, 2 * entryLength);
            long start = _longLocaOffsets
                ? BinaryPrimitives.ReadUInt32BigEndian(offsets)
                : BinaryPrimitives.ReadUInt16BigEndian(offsets) * 2L;
            long end = _longLocaOffsets
                ? BinaryPrimitives.ReadUInt32BigEndian(offsets.Slice(4))
                : BinaryPrimitives.ReadUInt16BigEndian(offsets.Slice(2)) * 2L;
            long header = _glyf.Offset + start;
            if (end - start < GlyphHeaderLength || end > _glyf.Length || header + GlyphHeaderLength > Data.Length)
                return null;

            return BinaryPrimitives.ReadInt16BigEndian(Data.AsSpan((int)(header + YMaxOffset), 2));
        }

        private static short? GetOS2Height(short? height) => height > 0 ? height : null;

        /// <summary>
        /// Parses a font file.
        /// </summary>
        /// <param name="fontData">The whole font file. The array is kept, not copied.</param>
        /// <exception cref="HpdfException">The file is not a TrueType font a document can use.</exception>
        public static TrueTypeFontFile Parse(byte[] fontData)
        {
            ArgumentNullException.ThrowIfNull(fontData);

            using (var stream = new MemoryStream(fontData, writable: false))
            {
                return Parse(stream, fontData);
            }
        }

        /// <summary>
        /// Parses a font file that is given both as a stream to read the tables from and as bytes.
        /// </summary>
        /// <param name="stream">A seekable stream with the font file.</param>
        /// <param name="fontData">The whole font file. The array is kept, not copied.</param>
        /// <exception cref="HpdfException">The file is not a TrueType font a document can use.</exception>
        public static TrueTypeFontFile Parse(Stream stream, byte[] fontData)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(fontData);

            using (var parser = new TrueTypeParser(stream))
            {
                return new TrueTypeFontFile(fontData, parser);
            }
        }

        /// <summary>
        /// Gets the bytes of the font file.
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Gets the names of the font ('name' table).
        /// </summary>
        public TrueTypeFontNames Names { get; }

        /// <summary>
        /// Gets the font header ('head' table).
        /// </summary>
        public TrueTypeHead Head { get; }

        /// <summary>
        /// Gets the horizontal header ('hhea' table).
        /// </summary>
        public TrueTypeHhea Hhea { get; }

        /// <summary>
        /// Gets the 'OS/2' table; null if the font has none.
        /// </summary>
        public TrueTypeOS2? OS2 { get; }

        /// <summary>
        /// Gets the 'post' table; null if the font has none.
        /// </summary>
        public TrueTypePost? Post { get; }

        /// <summary>
        /// Gets the number of glyphs in the font.
        /// </summary>
        public int GlyphCount => _hMetrics.Length;

        /// <summary>
        /// Gets the embedding permissions of the font (fsType of the OS/2 table); 0 if the font
        /// has no OS/2 table.
        /// </summary>
        public ushort FsType => OS2?.FsType ?? 0;

        /// <summary>
        /// Gets the ascent in 1000-unit glyph space.
        /// </summary>
        public int Ascent => Scale(Hhea.Ascender);

        /// <summary>
        /// Gets the descent in 1000-unit glyph space.
        /// </summary>
        public int Descent => Scale(Hhea.Descender);

        /// <summary>
        /// Gets the height of the capital letters in 1000-unit glyph space: the top of 'H'. A font
        /// that does not tell it (no 'H', no outlines and no value in the OS/2 table) gets its ascent.
        /// </summary>
        public int CapHeight { get; }

        /// <summary>
        /// Gets the height of the small letters in 1000-unit glyph space: the top of 'x'; 0 if the
        /// font does not tell it.
        /// </summary>
        public int XHeight { get; }

        /// <summary>
        /// Gets the font bounding box in 1000-unit glyph space.
        /// </summary>
        public HpdfBox FontBBox
        {
            get
            {
                float scale = 1000.0f / Head.UnitsPerEm;
                return new HpdfBox(Head.XMin * scale, Head.YMin * scale, Head.XMax * scale, Head.YMax * scale);
            }
        }

        /// <summary>
        /// Gets the glyph of a character of the Basic Multilingual Plane, 0 (.notdef) if the font
        /// has no glyph for it.
        /// </summary>
        public ushort GetGlyphId(ushort unicode) => _cmap.GetGlyphId(unicode);

        /// <summary>
        /// Gets the glyph of a Unicode character, 0 (.notdef) if the font has no glyph for it.
        /// A character above U+FFFF needs a font with a cmap subtable for all of Unicode (format 12).
        /// </summary>
        public ushort GetGlyphId(Rune character) => _cmap.GetGlyphId(character);

        /// <summary>
        /// Gets whether the font has glyphs for vertical writing (the 'vert' and 'vrt2' features of
        /// its 'GSUB' table).
        /// </summary>
        public bool HasVerticalGlyphs => _verticalGlyphs.Count > 0;

        /// <summary>
        /// Gets the form a glyph has in vertical writing: a rotated bracket or dash, punctuation
        /// moved to the corner of its box, a Latin letter turned on its side if the font has such
        /// glyphs ('vrt2'). Most glyphs are the same in both writing modes.
        /// </summary>
        /// <returns>The glyph for vertical writing; the glyph itself if it has no vertical form.</returns>
        public ushort GetVerticalGlyphId(ushort glyphId)
        {
            return _verticalGlyphs.TryGetValue(glyphId, out ushort vertical) ? vertical : glyphId;
        }

        /// <summary>
        /// Gets the height of the origin of a glyph of vertical writing above its baseline, in
        /// 1000-unit glyph space: the top of the em box. The typographic ascender and descender of a
        /// CJK font are the edges of that box; null in a font where they span something else than
        /// one em, which does not tell where the box is.
        /// </summary>
        public int? VerticalOriginY
        {
            get
            {
                bool typoMetricsAreTheEmBox = OS2 != null && OS2.STypoAscender - OS2.STypoDescender == Head.UnitsPerEm;
                return typoMetricsAreTheEmBox ? Scale(OS2!.STypoAscender) : null;
            }
        }

        /// <summary>
        /// Gets whether the font tells the vertical metrics of its glyphs (the 'vhea' and 'vmtx'
        /// tables).
        /// </summary>
        public bool HasVerticalMetrics => _vMetrics != null;

        /// <summary>
        /// Gets how far a glyph moves the text position down in vertical writing, in 1000-unit
        /// glyph space, rounded as it is written to a font: the advance height of 'vmtx'.
        /// </summary>
        /// <returns>The advance height; null if the font has no vertical metrics or not the glyph.</returns>
        public int? GetScaledGlyphHeight(ushort glyphId)
        {
            return _vMetrics != null && glyphId < _vMetrics.Length ? Scale(_vMetrics[glyphId].AdvanceHeight) : null;
        }

        /// <summary>
        /// Gets the height of the vertical origin of a glyph above its baseline, in 1000-unit glyph
        /// space: the top of its outline plus its top side bearing of 'vmtx', as layout engines take
        /// it from a font with TrueType outlines.
        /// </summary>
        /// <returns>The origin; null if the font has no vertical metrics or the glyph has no outline
        /// (a space), which has no vertical origin of its own.</returns>
        public int? GetVerticalOriginY(ushort glyphId)
        {
            if (_vMetrics is null || glyphId >= _vMetrics.Length)
                return null;

            short? top = ReadGlyphTop(glyphId);
            return top.HasValue ? Scale(top.Value + _vMetrics[glyphId].TopSideBearing) : null;
        }

        /// <summary>
        /// Gets the advance width of a glyph in font units; 0 for a glyph the font does not have.
        /// </summary>
        public int GetGlyphWidth(ushort glyphId)
        {
            return glyphId < _hMetrics.Length ? _hMetrics[glyphId].AdvanceWidth : 0;
        }

        /// <summary>
        /// Gets the advance width of a glyph in 1000-unit glyph space, rounded as it is written to
        /// the widths of a font (/Widths, /W): text is measured with the advances a viewer uses.
        /// </summary>
        public int GetScaledGlyphWidth(ushort glyphId) => Scale(GetGlyphWidth(glyphId));

        /// <summary>
        /// Converts a value in font units to 1000-unit glyph space.
        /// </summary>
        public int Scale(int fontUnits) => (int)Math.Round(fontUnits * 1000.0 / Head.UnitsPerEm);
    }
}
