/* A CFF font program (the 'CFF ' table of an OpenType font), parsed once */
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace Haru.Font.Cff
{
    /// <summary>
    /// The Private DICT of a CFF font, or of a font dictionary of a CID-keyed font, with its local
    /// subroutines.
    /// </summary>
    internal sealed class CffPrivateDict
    {
        public CffPrivateDict(IReadOnlyList<CffDictEntry> entries, CffIndex? localSubrs)
        {
            Entries = entries;
            LocalSubrs = localSubrs;
        }

        /// <summary>The entries of the DICT.</summary>
        public IReadOnlyList<CffDictEntry> Entries { get; }

        /// <summary>The local subroutines; null if the DICT has none.</summary>
        public CffIndex? LocalSubrs { get; }
    }

    /// <summary>
    /// A font dictionary of a CID-keyed CFF font (an item of its FDArray) with its Private DICT.
    /// </summary>
    internal sealed class CffFontDict
    {
        public CffFontDict(IReadOnlyList<CffDictEntry> entries, CffPrivateDict privateDict)
        {
            Entries = entries;
            Private = privateDict;
        }

        /// <summary>The entries of the font dictionary.</summary>
        public IReadOnlyList<CffDictEntry> Entries { get; }

        /// <summary>The Private DICT of the font dictionary.</summary>
        public CffPrivateDict Private { get; }
    }

    /// <summary>
    /// A CFF font program: the outlines of an OpenType font with CFF outlines (its 'CFF ' table).
    /// What a document needs is read once - the glyph names or CIDs (charset), the font
    /// dictionary of every glyph (FDSelect), where the charstrings and the subroutines are; the
    /// charstrings themselves stay in the bytes of the file. An object does not change after it
    /// is created, so it can be shared by any number of fonts and threads.
    /// </summary>
    internal sealed class CffFont
    {
        private const int DefaultCidCount = 8720;

        private readonly ushort[] _charset;       // glyph -> SID (glyphs by name) or CID
        private readonly byte[]? _fdSelect;       // glyph -> font dictionary (CID-keyed)
        private readonly Lazy<Dictionary<string, int>>? _glyphsByName;

        private CffFont(byte[] data, int offset, int length)
        {
            Data = data;
            Offset = offset;
            Length = length;

            ReadOnlySpan<byte> file = data;
            int limit = offset + length;
            if (length < 4)
                throw Damaged("the table is too short");

            int major = file[offset];
            if (major != 1)
                throw new HpdfException(HpdfErrorCode.UnsupportedFontType, $"CFF version {major} is not supported");

            int headerSize = file[offset + 2];
            HeaderSize = headerSize;

            CffIndex names = CffIndex.Read(file, offset + headerSize, limit);
            CffIndex topDicts = CffIndex.Read(file, names.End, limit);
            Strings = CffIndex.Read(file, topDicts.End, limit);
            GlobalSubrs = CffIndex.Read(file, Strings.End, limit);

            if (names.Count < 1 || topDicts.Count < 1)
                throw Damaged("the table has no font");

            Name = System.Text.Encoding.ASCII.GetString(names.GetItemData(file, 0));
            (int topOffset, int topLength) = topDicts.GetItem(file, 0);
            TopDict = CffDict.Parse(file, topOffset, topLength);

            CffDictEntry? charstringType = Find(TopDict, CffDict.CharstringType);
            if (charstringType != null && charstringType.GetInt(0) != 2)
                throw new HpdfException(HpdfErrorCode.UnsupportedFontType, $"CFF charstrings of type {charstringType.GetInt(0)} are not supported");

            CffDictEntry charStrings = Find(TopDict, CffDict.CharStrings)
                ?? throw Damaged("the font has no charstrings");
            CharStrings = CffIndex.Read(file, ToFile(charStrings.GetInt(0)), limit);
            if (CharStrings.Count < 1)
                throw Damaged("the font has no glyphs");

            IsCidKeyed = Find(TopDict, CffDict.Ros) != null;
            _charset = ReadCharset(file, Find(TopDict, CffDict.Charset)?.GetInt(0) ?? 0, GlyphCount, IsCidKeyed, limit);

            if (IsCidKeyed)
            {
                CidCount = Find(TopDict, CffDict.CidCount)?.GetInt(0) ?? DefaultCidCount;

                CffDictEntry fdArray = Find(TopDict, CffDict.FdArray) ?? throw Damaged("a CID-keyed font has no FDArray");
                CffIndex fontDicts = CffIndex.Read(file, ToFile(fdArray.GetInt(0)), limit);
                var dicts = new CffFontDict[fontDicts.Count];
                for (int i = 0; i < dicts.Length; i++)
                {
                    (int dictOffset, int dictLength) = fontDicts.GetItem(file, i);
                    List<CffDictEntry> entries = CffDict.Parse(file, dictOffset, dictLength);
                    dicts[i] = new CffFontDict(entries, ReadPrivate(file, entries, limit));
                }
                FontDicts = dicts;

                CffDictEntry fdSelect = Find(TopDict, CffDict.FdSelect) ?? throw Damaged("a CID-keyed font has no FDSelect");
                _fdSelect = ReadFdSelect(file, ToFile(fdSelect.GetInt(0)), GlyphCount, dicts.Length, limit);
            }
            else
            {
                FontDicts = Array.Empty<CffFontDict>();
                Private = ReadPrivate(file, TopDict, limit);
                _glyphsByName = new Lazy<Dictionary<string, int>>(BuildGlyphsByName);
            }
        }

        /// <summary>
        /// Parses the CFF table of a font file.
        /// </summary>
        /// <param name="data">The whole file. The array is kept, not copied.</param>
        /// <param name="offset">Where the CFF table starts.</param>
        /// <param name="length">The length of the CFF table.</param>
        /// <exception cref="HpdfException">The table is damaged or of a kind that is not supported
        /// (<see cref="HpdfErrorCode.TtInvalidFormat"/>).</exception>
        public static CffFont Parse(byte[] data, int offset, int length)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (offset < 0 || length < 0 || (long)offset + length > data.Length)
                throw Damaged("the table is outside the file");

            try
            {
                return new CffFont(data, offset, length);
            }
            catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException)
            {
                throw new HpdfException(HpdfErrorCode.TtInvalidFormat, 0, "The CFF table is damaged", ex);
            }
        }

        /// <summary>The bytes of the whole file.</summary>
        public byte[] Data { get; }

        /// <summary>Where the CFF table starts in <see cref="Data"/>.</summary>
        public int Offset { get; }

        /// <summary>The length of the CFF table.</summary>
        public int Length { get; }

        /// <summary>The size of the CFF header.</summary>
        public int HeaderSize { get; }

        /// <summary>The name of the font (Name INDEX).</summary>
        public string Name { get; }

        /// <summary>The entries of the Top DICT.</summary>
        public IReadOnlyList<CffDictEntry> TopDict { get; }

        /// <summary>The strings of the font (String INDEX).</summary>
        public CffIndex Strings { get; }

        /// <summary>The global subroutines.</summary>
        public CffIndex GlobalSubrs { get; }

        /// <summary>The charstrings, one per glyph.</summary>
        public CffIndex CharStrings { get; }

        /// <summary>Whether the font is CID-keyed (glyphs by CID, with font dictionaries) or has
        /// its glyphs by name.</summary>
        public bool IsCidKeyed { get; }

        /// <summary>The number of CIDs of a CID-keyed font (CIDCount); 0 for a font with glyph names.</summary>
        public int CidCount { get; }

        /// <summary>The font dictionaries of a CID-keyed font; empty for a font with glyph names.</summary>
        public IReadOnlyList<CffFontDict> FontDicts { get; }

        /// <summary>The Private DICT of a font with glyph names; null for a CID-keyed font.</summary>
        public CffPrivateDict? Private { get; }

        /// <summary>The number of glyphs.</summary>
        public int GlyphCount => CharStrings.Count;

        /// <summary>
        /// Gets the SID of the name of a glyph (a font with glyph names) or its CID (CID-keyed).
        /// </summary>
        public int GetCharsetEntry(int glyphId) => _charset[glyphId];

        /// <summary>
        /// Gets the font dictionary of a glyph of a CID-keyed font.
        /// </summary>
        public int GetFontDictIndex(int glyphId) => _fdSelect![glyphId];

        /// <summary>
        /// Gets the Private DICT that a glyph is drawn with: the one of its font dictionary in a
        /// CID-keyed font.
        /// </summary>
        public CffPrivateDict GetPrivate(int glyphId)
        {
            return IsCidKeyed ? FontDicts[_fdSelect![glyphId]].Private : Private!;
        }

        /// <summary>
        /// Gets the name of a glyph of a font with glyph names.
        /// </summary>
        public string GetGlyphName(int glyphId)
        {
            if (IsCidKeyed)
                throw new InvalidOperationException("A CID-keyed font has no glyph names");

            return GetString(_charset[glyphId]);
        }

        /// <summary>
        /// Finds a glyph of a font with glyph names by its name.
        /// </summary>
        /// <returns>The glyph; -1 if the font has no glyph with the name.</returns>
        public int FindGlyph(string name)
        {
            if (_glyphsByName is null)
                return -1;

            return _glyphsByName.Value.TryGetValue(name, out int glyph) ? glyph : -1;
        }

        /// <summary>
        /// Gets a string by its SID: a standard string or one of the font.
        /// </summary>
        public string GetString(int sid)
        {
            if (sid < CffStandardData.StandardStringCount)
                return CffStandardData.StandardStrings[sid];

            int index = sid - CffStandardData.StandardStringCount;
            if (index >= Strings.Count)
                throw Damaged($"the string {sid} is not in the font");

            return System.Text.Encoding.ASCII.GetString(Strings.GetItemData(Data, index));
        }

        /// <summary>
        /// Finds the entry of an operator in a DICT.
        /// </summary>
        public static CffDictEntry? Find(IReadOnlyList<CffDictEntry> dict, int op)
        {
            foreach (CffDictEntry entry in dict)
            {
                if (entry.Operator == op)
                    return entry;
            }
            return null;
        }

        internal static HpdfException Damaged(string what)
        {
            return new HpdfException(HpdfErrorCode.TtInvalidFormat, $"The CFF table is damaged: {what}");
        }

        private int ToFile(int tableOffset)
        {
            if (tableOffset < 0 || tableOffset >= Length)
                throw Damaged("an offset is outside the table");
            return Offset + tableOffset;
        }

        private CffPrivateDict ReadPrivate(ReadOnlySpan<byte> file, IReadOnlyList<CffDictEntry> dict, int limit)
        {
            CffDictEntry? privateEntry = Find(dict, CffDict.Private);
            if (privateEntry is null)
                return new CffPrivateDict(Array.Empty<CffDictEntry>(), null);

            int size = privateEntry.GetInt(0);
            int start = ToFile(privateEntry.GetInt(1));
            if (size < 0 || start + size > limit)
                throw Damaged("a Private DICT is outside the table");

            List<CffDictEntry> entries = CffDict.Parse(file, start, size);
            CffDictEntry? subrs = Find(entries, CffDict.Subrs);

            // The offset of the local subroutines is from the start of the Private DICT
            CffIndex? localSubrs = subrs is null ? null : CffIndex.Read(file, start + subrs.GetInt(0), limit);
            return new CffPrivateDict(entries, localSubrs);
        }

        private ushort[] ReadCharset(ReadOnlySpan<byte> file, int charsetOffset, int glyphCount, bool cidKeyed, int limit)
        {
            var charset = new ushort[glyphCount];

            // The predefined charsets of fonts with glyph names; ISOAdobe has the SIDs 0-228 in order
            if (!cidKeyed && charsetOffset <= 2)
            {
                if (charsetOffset != 0)
                    throw new HpdfException(HpdfErrorCode.UnsupportedFontType, "The CFF font has an Expert charset, which is not supported");

                for (int glyph = 0; glyph < glyphCount; glyph++)
                    charset[glyph] = (ushort)(glyph <= 228 ? glyph : 0);
                return charset;
            }

            int position = ToFile(charsetOffset);
            int format = file[position++];
            int next = 1; // glyph 0 is .notdef / CID 0 and is not in the charset
            switch (format)
            {
                case 0:
                    if (position + 2 * (glyphCount - 1) > limit)
                        throw Damaged("the charset is outside the table");
                    for (; next < glyphCount; next++, position += 2)
                        charset[next] = BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position));
                    break;

                case 1:
                case 2:
                    int countSize = format == 1 ? 1 : 2;
                    while (next < glyphCount)
                    {
                        if (position + 2 + countSize > limit)
                            throw Damaged("the charset is outside the table");

                        int first = BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position));
                        int left = format == 1 ? file[position + 2] : BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position + 2));
                        position += 2 + countSize;
                        for (int i = 0; i <= left && next < glyphCount; i++)
                            charset[next++] = (ushort)(first + i);
                    }
                    break;

                default:
                    throw Damaged($"the charset has the unknown format {format}");
            }

            return charset;
        }

        private static byte[] ReadFdSelect(ReadOnlySpan<byte> file, int position, int glyphCount, int fontDictCount, int limit)
        {
            var fdSelect = new byte[glyphCount];
            if (position >= limit)
                throw Damaged("the FDSelect is outside the table");

            int format = file[position++];
            switch (format)
            {
                case 0:
                    if (position + glyphCount > limit)
                        throw Damaged("the FDSelect is outside the table");
                    file.Slice(position, glyphCount).CopyTo(fdSelect);
                    break;

                case 3:
                    if (position + 2 > limit)
                        throw Damaged("the FDSelect is outside the table");
                    int ranges = BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position));
                    position += 2;
                    if (position + ranges * 3 + 2 > limit)
                        throw Damaged("the FDSelect is outside the table");

                    for (int i = 0; i < ranges; i++)
                    {
                        int first = BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position + i * 3));
                        int fd = file[position + i * 3 + 2];
                        int end = BinaryPrimitives.ReadUInt16BigEndian(file.Slice(position + (i + 1) * 3));
                        for (int glyph = first; glyph < end && glyph < glyphCount; glyph++)
                            fdSelect[glyph] = (byte)fd;
                    }
                    break;

                default:
                    throw Damaged($"the FDSelect has the unknown format {format}");
            }

            foreach (byte fd in fdSelect)
            {
                if (fd >= fontDictCount)
                    throw Damaged("the FDSelect names a font dictionary the font does not have");
            }

            return fdSelect;
        }

        private Dictionary<string, int> BuildGlyphsByName()
        {
            var glyphs = new Dictionary<string, int>(GlyphCount, StringComparer.Ordinal);
            for (int glyph = 0; glyph < GlyphCount; glyph++)
                glyphs.TryAdd(GetGlyphName(glyph), glyph);
            return glyphs;
        }
    }
}
