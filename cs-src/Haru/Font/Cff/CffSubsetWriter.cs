/* Writes a CFF font program for a PDF document: some glyphs of a CFF font, CID-keyed or by name */
using System;
using System.Collections.Generic;

namespace Haru.Font.Cff
{
    /// <summary>
    /// Writes a CFF font program with some glyphs of a CFF font, for a font program of a PDF
    /// document (FontFile3). The glyphs are stored one after the other; what the content of a
    /// document selects them by stays the same:
    /// <list type="bullet">
    /// <item>a CID-keyed program (<see cref="WriteCidKeyed"/>, for a CIDFontType0 font) maps every
    /// glyph to the CID that is its glyph ID in the original font, whatever the font was keyed by:
    /// a document shows the glyph IDs of the font file as CIDs;</item>
    /// <item>a program with glyph names (<see cref="WriteNameKeyed"/>, for a Type1 font) keeps the
    /// names of the glyphs, which the encoding of the document selects them by.</item>
    /// </list>
    /// A subset gets the charstrings without subroutines (<see cref="CffCharStringFlattener"/>), so
    /// it has no subroutines; the whole font keeps its charstrings and subroutines as they are.
    /// </summary>
    internal static class CffSubsetWriter
    {
        private const int FixedIntSize = 5;

        // Top DICT entries that describe the layout of the original font program or identify the
        // original font; they are written anew or left out
        private static readonly HashSet<int> RewrittenTopEntries = new HashSet<int>
        {
            CffDict.Ros, CffDict.CidCount, CffDict.UidBase, CffDict.UniqueId, CffDict.Xuid,
            CffDict.Charset, CffDict.Encoding, CffDict.CharStrings, CffDict.Private,
            CffDict.FdArray, CffDict.FdSelect,
        };

        /// <summary>
        /// Writes a CID-keyed font program (for a CIDFontType0C font program) with some glyphs.
        /// </summary>
        /// <param name="font">The font.</param>
        /// <param name="glyphs">The glyphs to keep, in ascending order, glyph 0 first.</param>
        /// <param name="charStrings">The charstrings of the glyphs without subroutines
        /// (<see cref="CffCharStringFlattener"/>); null to keep the charstrings and all subroutines.</param>
        public static byte[] WriteCidKeyed(CffFont font, IReadOnlyList<int> glyphs, IReadOnlyDictionary<int, byte[]>? charStrings)
        {
            CheckGlyphs(font, glyphs);
            var writer = new Writer(font, glyphs, charStrings, cidKeyed: true);
            return writer.Write();
        }

        /// <summary>
        /// Writes a font program with glyph names (for a Type1C font program) with some glyphs.
        /// </summary>
        /// <param name="font">The font; it must have glyph names.</param>
        /// <param name="glyphs">The glyphs to keep, in ascending order, glyph 0 first.</param>
        /// <param name="charStrings">The charstrings of the glyphs without subroutines
        /// (<see cref="CffCharStringFlattener"/>); null to keep the charstrings and all subroutines.</param>
        public static byte[] WriteNameKeyed(CffFont font, IReadOnlyList<int> glyphs, IReadOnlyDictionary<int, byte[]>? charStrings)
        {
            if (font.IsCidKeyed)
                throw new InvalidOperationException("A CID-keyed font has no glyph names");

            CheckGlyphs(font, glyphs);
            var writer = new Writer(font, glyphs, charStrings, cidKeyed: false);
            return writer.Write();
        }

        private static void CheckGlyphs(CffFont font, IReadOnlyList<int> glyphs)
        {
            if (glyphs.Count == 0 || glyphs[0] != 0)
                throw new ArgumentException("The glyphs must start with glyph 0", nameof(glyphs));

            for (int i = 1; i < glyphs.Count; i++)
            {
                if (glyphs[i] <= glyphs[i - 1] || glyphs[i] >= font.GlyphCount)
                    throw new ArgumentException("The glyphs must be glyphs of the font, in ascending order", nameof(glyphs));
            }
        }

        /// <summary>
        /// Writes one font program. The offsets in the DICTs are written in five bytes whatever
        /// their values are, so every part has its size before anything is placed.
        /// </summary>
        private sealed class Writer
        {
            private readonly CffFont _font;
            private readonly IReadOnlyList<int> _glyphs;
            private readonly IReadOnlyDictionary<int, byte[]>? _charStrings;
            private readonly bool _cidKeyed;

            // The Private DICTs of the program: of the font dictionaries in use, or the one of a
            // font with glyph names; and for every glyph the index of its font dictionary
            private readonly List<CffPrivateDict> _privates = new List<CffPrivateDict>();
            private readonly List<CffFontDict?> _fontDicts = new List<CffFontDict?>();
            private readonly int[] _glyphFontDict;

            // The SIDs "Adobe" and "Identity" that a CID-keyed program adds to the strings
            private int _adobeSid;
            private int _identitySid;

            public Writer(CffFont font, IReadOnlyList<int> glyphs, IReadOnlyDictionary<int, byte[]>? charStrings, bool cidKeyed)
            {
                _font = font;
                _glyphs = glyphs;
                _charStrings = charStrings;
                _cidKeyed = cidKeyed;
                _glyphFontDict = new int[glyphs.Count];

                if (font.IsCidKeyed)
                {
                    var newIndex = new Dictionary<int, int>();
                    for (int i = 0; i < glyphs.Count; i++)
                    {
                        int fd = font.GetFontDictIndex(glyphs[i]);
                        if (!newIndex.TryGetValue(fd, out int index))
                        {
                            index = _fontDicts.Count;
                            newIndex.Add(fd, index);
                            _fontDicts.Add(font.FontDicts[fd]);
                            _privates.Add(font.FontDicts[fd].Private);
                        }
                        _glyphFontDict[i] = index;
                    }
                }
                else
                {
                    // A font with glyph names has one Private DICT; a CID-keyed program made of it
                    // has one font dictionary
                    _fontDicts.Add(null);
                    _privates.Add(font.Private!);
                }
            }

            public byte[] Write()
            {
                ReadOnlySpan<byte> data = _font.Data;

                // The parts whose content does not depend on where things are
                byte[] header = { 1, 0, 4, 4 };
                byte[] names = WriteIndex(new[] { System.Text.Encoding.ASCII.GetBytes(_font.Name) });
                byte[] strings = WriteStrings(data);
                byte[] globalSubrs = _charStrings is null ? WriteSubrs(data, _font.GlobalSubrs) : WriteIndex(Array.Empty<byte[]>());
                byte[] charset = WriteCharset();
                byte[] fdSelect = _cidKeyed ? WriteFdSelect() : Array.Empty<byte>();
                byte[] charStrings = WriteCharStrings(data);

                var privates = new List<byte[]>(_privates.Count);
                foreach (CffPrivateDict privateDict in _privates)
                    privates.Add(WritePrivate(data, privateDict));

                // The parts with offsets: written once to get their sizes, then with the offsets
                var layout = new Layout();
                byte[] topDict = WriteIndex(new[] { WriteTopDict(data, layout) });
                byte[] fdArray = _cidKeyed ? WriteIndex(WriteFontDicts(data, layout)) : Array.Empty<byte>();

                int position = header.Length + names.Length + topDict.Length + strings.Length + globalSubrs.Length;
                layout.Charset = position;
                position += charset.Length;
                layout.FdSelect = position;
                position += fdSelect.Length;
                layout.CharStrings = position;
                position += charStrings.Length;
                layout.FdArray = position;
                position += fdArray.Length;
                layout.Privates = new int[privates.Count];
                for (int i = 0; i < privates.Count; i++)
                {
                    layout.Privates[i] = position;
                    position += privates[i].Length;
                }

                byte[] finalTopDict = WriteIndex(new[] { WriteTopDict(data, layout) });
                byte[] finalFdArray = _cidKeyed ? WriteIndex(WriteFontDicts(data, layout)) : Array.Empty<byte>();
                if (finalTopDict.Length != topDict.Length || finalFdArray.Length != fdArray.Length)
                    throw new InvalidOperationException("The size of a DICT changed with its offsets");

                var output = new byte[position];
                int offset = 0;
                foreach (byte[] part in new[] { header, names, finalTopDict, strings, globalSubrs, charset, fdSelect, charStrings, finalFdArray })
                {
                    part.CopyTo(output, offset);
                    offset += part.Length;
                }
                foreach (byte[] part in privates)
                {
                    part.CopyTo(output, offset);
                    offset += part.Length;
                }
                return output;
            }

            private byte[] WriteStrings(ReadOnlySpan<byte> data)
            {
                var items = new List<byte[]>(_font.Strings.Count + 2);
                for (int i = 0; i < _font.Strings.Count; i++)
                    items.Add(_font.Strings.GetItemData(data, i).ToArray());

                if (_cidKeyed)
                {
                    _adobeSid = CffStandardData.StandardStringCount + items.Count;
                    items.Add(System.Text.Encoding.ASCII.GetBytes("Adobe"));
                    _identitySid = CffStandardData.StandardStringCount + items.Count;
                    items.Add(System.Text.Encoding.ASCII.GetBytes("Identity"));
                }
                return WriteIndex(items);
            }

            private byte[] WriteTopDict(ReadOnlySpan<byte> data, Layout layout)
            {
                var dict = new List<byte>();
                if (_cidKeyed)
                {
                    // ROS must be the first entry of the Top DICT of a CID-keyed font
                    CffDict.WriteInt(dict, _adobeSid);
                    CffDict.WriteInt(dict, _identitySid);
                    CffDict.WriteInt(dict, 0);
                    CffDict.WriteOperator(dict, CffDict.Ros);
                }

                foreach (CffDictEntry entry in _font.TopDict)
                {
                    if (!RewrittenTopEntries.Contains(entry.Operator))
                        dict.AddRange(data.Slice(entry.Start, entry.Length).ToArray());
                }

                CffDict.WriteFixedInt(dict, layout.Charset);
                CffDict.WriteOperator(dict, CffDict.Charset);
                CffDict.WriteFixedInt(dict, layout.CharStrings);
                CffDict.WriteOperator(dict, CffDict.CharStrings);

                if (_cidKeyed)
                {
                    // The CIDs are the glyph IDs of the original font
                    CffDict.WriteInt(dict, _font.GlyphCount);
                    CffDict.WriteOperator(dict, CffDict.CidCount);
                    CffDict.WriteFixedInt(dict, layout.FdArray);
                    CffDict.WriteOperator(dict, CffDict.FdArray);
                    CffDict.WriteFixedInt(dict, layout.FdSelect);
                    CffDict.WriteOperator(dict, CffDict.FdSelect);
                }
                else
                {
                    WritePrivateEntry(dict, layout, 0);
                }
                return dict.ToArray();
            }

            private List<byte[]> WriteFontDicts(ReadOnlySpan<byte> data, Layout layout)
            {
                var dicts = new List<byte[]>(_fontDicts.Count);
                for (int i = 0; i < _fontDicts.Count; i++)
                {
                    var dict = new List<byte>();
                    CffFontDict? fontDict = _fontDicts[i];
                    if (fontDict != null)
                    {
                        foreach (CffDictEntry entry in fontDict.Entries)
                        {
                            if (entry.Operator != CffDict.Private)
                                dict.AddRange(data.Slice(entry.Start, entry.Length).ToArray());
                        }
                    }
                    WritePrivateEntry(dict, layout, i);
                    dicts.Add(dict.ToArray());
                }
                return dicts;
            }

            private void WritePrivateEntry(List<byte> dict, Layout layout, int index)
            {
                // The Private DICT is written with its local subroutines after it: its size is the
                // size of the DICT alone
                int size = _privateDictSizes[index];
                CffDict.WriteFixedInt(dict, size);
                CffDict.WriteFixedInt(dict, layout.Privates?[index] ?? 0);
                CffDict.WriteOperator(dict, CffDict.Private);
            }

            private readonly List<int> _privateDictSizes = new List<int>();

            private byte[] WritePrivate(ReadOnlySpan<byte> data, CffPrivateDict privateDict)
            {
                var dict = new List<byte>();
                foreach (CffDictEntry entry in privateDict.Entries)
                {
                    if (entry.Operator != CffDict.Subrs)
                        dict.AddRange(data.Slice(entry.Start, entry.Length).ToArray());
                }

                byte[]? localSubrs = null;
                // Charstrings without subroutines need no local subroutines
                if (privateDict.LocalSubrs is CffIndex subrs && _charStrings is null)
                {
                    // The local subroutines follow the DICT; their offset is from its start
                    int dictSize = dict.Count + FixedIntSize + 1;
                    CffDict.WriteFixedInt(dict, dictSize);
                    CffDict.WriteOperator(dict, CffDict.Subrs);
                    localSubrs = WriteSubrs(data, subrs);
                }

                _privateDictSizes.Add(dict.Count);
                if (localSubrs != null)
                    dict.AddRange(localSubrs);
                return dict.ToArray();
            }

            private static byte[] WriteSubrs(ReadOnlySpan<byte> data, CffIndex subrs)
            {
                var items = new List<byte[]>(subrs.Count);
                for (int i = 0; i < subrs.Count; i++)
                    items.Add(subrs.GetItemData(data, i).ToArray());
                return WriteIndex(items);
            }

            private byte[] WriteCharStrings(ReadOnlySpan<byte> data)
            {
                var items = new List<byte[]>(_glyphs.Count);
                foreach (int glyph in _glyphs)
                    items.Add(_charStrings?[glyph] ?? _font.CharStrings.GetItemData(data, glyph).ToArray());
                return WriteIndex(items);
            }

            /// <summary>
            /// Writes the charset: for every glyph but glyph 0, its CID (the glyph ID in the original
            /// font) or the SID of its name. Runs of consecutive values are written as ranges
            /// (format 2) when that is shorter than a list of all values (format 0).
            /// </summary>
            private byte[] WriteCharset()
            {
                int count = _glyphs.Count - 1;
                var values = new int[count];
                for (int i = 0; i < count; i++)
                {
                    int glyph = _glyphs[i + 1];
                    values[i] = _cidKeyed ? glyph : _font.GetCharsetEntry(glyph);
                }

                var ranges = new List<(int First, int Left)>();
                for (int i = 0; i < count;)
                {
                    int start = i;
                    while (i + 1 < count && values[i + 1] == values[i] + 1 && i - start < ushort.MaxValue)
                        i++;
                    ranges.Add((values[start], i - start));
                    i++;
                }

                var output = new List<byte>();
                if (ranges.Count * 4 < count * 2)
                {
                    output.Add(2);
                    foreach ((int first, int left) in ranges)
                    {
                        AddUInt16(output, first);
                        AddUInt16(output, left);
                    }
                }
                else
                {
                    output.Add(0);
                    foreach (int value in values)
                        AddUInt16(output, value);
                }
                return output.ToArray();
            }

            /// <summary>
            /// Writes the FDSelect (format 3): the font dictionary of every glyph, as ranges.
            /// </summary>
            private byte[] WriteFdSelect()
            {
                var output = new List<byte> { 3, 0, 0 };
                int ranges = 0;
                for (int i = 0; i < _glyphFontDict.Length; i++)
                {
                    if (i == 0 || _glyphFontDict[i] != _glyphFontDict[i - 1])
                    {
                        AddUInt16(output, i);
                        output.Add((byte)_glyphFontDict[i]);
                        ranges++;
                    }
                }
                AddUInt16(output, _glyphFontDict.Length); // sentinel
                output[1] = (byte)(ranges >> 8);
                output[2] = (byte)ranges;
                return output.ToArray();
            }

            private static void AddUInt16(List<byte> output, int value)
            {
                output.Add((byte)(value >> 8));
                output.Add((byte)value);
            }

            /// <summary>
            /// Where the parts of the program are; zero while the sizes are being found.
            /// </summary>
            private sealed class Layout
            {
                public int Charset;
                public int FdSelect;
                public int CharStrings;
                public int FdArray;
                public int[]? Privates;
            }
        }

        /// <summary>
        /// Writes an INDEX with the smallest offset size that fits.
        /// </summary>
        public static byte[] WriteIndex(IReadOnlyList<byte[]> items)
        {
            if (items.Count == 0)
                return new byte[] { 0, 0 };

            long dataSize = 0;
            foreach (byte[] item in items)
                dataSize += item.Length;

            long lastOffset = dataSize + 1;
            int offSize = lastOffset <= 0xFF ? 1 : lastOffset <= 0xFFFF ? 2 : lastOffset <= 0xFFFFFF ? 3 : 4;

            var output = new byte[checked(3 + (items.Count + 1) * offSize + (int)dataSize)];
            output[0] = (byte)(items.Count >> 8);
            output[1] = (byte)items.Count;
            output[2] = (byte)offSize;

            int position = 3;
            long offset = 1;
            for (int i = 0; i <= items.Count; i++)
            {
                for (int b = offSize - 1; b >= 0; b--)
                    output[position++] = (byte)(offset >> (8 * b));
                if (i < items.Count)
                    offset += items[i].Length;
            }

            foreach (byte[] item in items)
            {
                item.CopyTo(output, position);
                position += item.Length;
            }
            return output;
        }
    }
}
