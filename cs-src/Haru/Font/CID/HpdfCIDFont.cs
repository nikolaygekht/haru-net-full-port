using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Haru.Objects;
using Haru.Xref;
using Haru.Types;
using Haru.Font.TrueType;
using Haru.Streams;
using Haru.Doc;

namespace Haru.Font.CID
{
    /// <summary>
    /// Represents a CID (Character Identifier) font for multi-byte character sets (CJK).
    /// Uses CIDFontType2 (TrueType-based) with Identity-H encoding.
    /// </summary>
    public class HpdfCIDFont : IHpdfFontImplementation, IHpdfBeforeWrite, IDisposable
    {
        private readonly HpdfDict _dict;               // Type 0 (composite) font dictionary
        private readonly HpdfDict _cidFontDict;        // CIDFontType2 descendant font dictionary
        private readonly HpdfDict _descriptor;         // Font descriptor
        private string _baseFont = null!;
        private readonly string _localName;
        private readonly HpdfXref _xref;
        private readonly int _codePage;

        // TrueType font data - initialized by LoadFromStream factory method
        private TrueTypeOffsetTable _offsetTable = null!;
        private TrueTypeHead _head = null!;
        private TrueTypeMaxp _maxp = null!;
        private TrueTypeHhea _hhea = null!;
        private TrueTypeLongHorMetric[] _hMetrics = null!;
        private TrueTypeNameTable? _nameTable;
        private TrueTypeTable? _nameTableRef;  // Reference to name table for extracting strings
        private TrueTypeCmapFormat4 _cmap = null!;
        private TrueTypeOS2? _os2;
        private TrueTypePost? _post;

        private byte[] _fontData = null!;
        private HpdfStreamObject _fontFileStream = null!;
        private HpdfStreamObject _toUnicodeStream = null!;
        private CIDSystemInfo _systemInfo = null!;
        private CIDWritingMode _writingMode;
        private bool _disposed = false;

        // Glyphs shown with this font: glyph ID -> the Unicode code point it was first shown for.
        // /ToUnicode and /W are written from it when the document is saved.
        private readonly Dictionary<ushort, int> _usedGlyphs = new Dictionary<ushort, int>();
        private int _writtenGlyphCount = -1;

        /// <summary>
        /// Gets the underlying Type 0 font dictionary.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the BaseFont name.
        /// </summary>
        public string BaseFont => _baseFont;

        /// <summary>
        /// Gets the local name used to reference this font in page resources.
        /// </summary>
        public string LocalName => _localName;

        /// <summary>
        /// Gets the code page used for text encoding.
        /// </summary>
        public int? CodePage => _codePage;

        /// <summary>
        /// Gets the font ascent in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public int Ascent
        {
            get
            {
                if (_hhea != null && _head != null)
                    return (int)Math.Round(_hhea.Ascender * 1000.0 / _head.UnitsPerEm);
                return 750;
            }
        }

        /// <summary>
        /// Gets the font descent in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public int Descent
        {
            get
            {
                if (_hhea != null && _head != null)
                    return (int)Math.Round(_hhea.Descender * 1000.0 / _head.UnitsPerEm);
                return -250;
            }
        }

        /// <summary>
        /// Gets the x-height in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public int XHeight
        {
            get
            {
                if (_os2 != null && _head != null)
                    return (int)Math.Round(_os2.STypoAscender * 1000.0 / _head.UnitsPerEm);
                if (_head != null)
                    return (int)Math.Round(_head.YMax * 1000.0 / _head.UnitsPerEm);
                return 500;
            }
        }

        /// <summary>
        /// Gets the font bounding box in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public HpdfBox FontBBox
        {
            get
            {
                if (_head != null)
                {
                    float scale = 1000.0f / _head.UnitsPerEm;
                    return new HpdfBox(
                        _head.XMin * scale,
                        _head.YMin * scale,
                        _head.XMax * scale,
                        _head.YMax * scale);
                }
                return new HpdfBox(0, -250, 1000, 750);
            }
        }

        /// <summary>
        /// Gets an HpdfFont wrapper for this CID font that can be used with page operations.
        /// </summary>
        public HpdfFont AsFont()
        {
            return new HpdfFont(this);
        }

        private HpdfCIDFont(HpdfXref xref, string localName, int codePage)
        {
            _xref = xref ?? throw new ArgumentNullException(nameof(xref));
            _localName = localName ?? throw new ArgumentNullException(nameof(localName));
            _codePage = codePage;
            _systemInfo = CIDSystemInfo.CreateIdentity();
            _writingMode = CIDWritingMode.Horizontal;

            _dict = new HpdfDict();
            _cidFontDict = new HpdfDict();
            _descriptor = new HpdfDict();
        }

        /// <summary>
        /// Loads a TrueType font as a CID font with specified code page.
        /// Supports CJK code pages: 932 (Japanese), 936 (Chinese Simplified),
        /// 949 (Korean), 950 (Chinese Traditional).
        /// Automatically upgrades PDF version to 1.4 if needed (required for Adobe Acrobat CID font support).
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="localName">Local resource name (e.g., "CJK1").</param>
        /// <param name="filePath">Path to the TTF file.</param>
        /// <param name="codePage">Code page for encoding (e.g., 932 for Japanese).</param>
        /// <returns>The loaded CID font.</returns>
        public static HpdfCIDFont LoadFromTrueTypeFile(
            HpdfDocument document,
            string localName,
            string filePath,
            int codePage)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            // Validate code page first (before file existence check for better error reporting)
            ValidateCodePage(codePage);

            if (!File.Exists(filePath))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"Font file not found: {filePath}");

            // CID fonts require PDF 1.4 or later for Adobe Acrobat compatibility
            document.Xref.RequireVersion(HpdfVersion.Version14);

            byte[] fontData = File.ReadAllBytes(filePath);

            using (var stream = new MemoryStream(fontData))
            {
                return LoadFromStream(document.Xref, localName, stream, fontData, codePage);
            }
        }

        /// <summary>
        /// Loads a TrueType font as a CID font from a stream.
        /// </summary>
        private static HpdfCIDFont LoadFromStream(
            HpdfXref xref,
            string localName,
            Stream stream,
            byte[] fontData,
            int codePage)
        {
            var font = new HpdfCIDFont(xref, localName, codePage);
            font._fontData = fontData;

            using (var parser = new TrueTypeParser(stream))
            {
                // Parse required tables
                font._offsetTable = parser.ParseOffsetTable();

                var headTable = parser.FindTable(font._offsetTable, "head");
                font._head = parser.ParseHead(headTable!);

                var maxpTable = parser.FindTable(font._offsetTable, "maxp");
                font._maxp = parser.ParseMaxp(maxpTable!);

                var hheaTable = parser.FindTable(font._offsetTable, "hhea");
                font._hhea = parser.ParseHhea(hheaTable!);

                var hmtxTable = parser.FindTable(font._offsetTable, "hmtx");
                font._hMetrics = parser.ParseHmtx(hmtxTable!, font._hhea.NumberOfHMetrics, font._maxp.NumGlyphs);

                var nameTable = parser.FindTable(font._offsetTable, "name");
                if (nameTable != null)
                {
                    font._nameTableRef = nameTable;  // Store table reference for later string extraction
                    font._nameTable = parser.ParseName(nameTable);
                }

                // Parse cmap table
                font.ParseCmapTable(parser);

                // Parse OS/2 table if present
                var os2Table = parser.FindTable(font._offsetTable, "OS/2");
                if (os2Table != null)
                {
                    font.ParseOS2Table(parser, os2Table);
                }

                // Parse post table if present
                var postTable = parser.FindTable(font._offsetTable, "post");
                if (postTable != null)
                {
                    font._post = parser.ParsePost(postTable);
                }
            }

            // Extract font name
            font._baseFont = font.ExtractFontName();

            // CRITICAL: Follow Adobe-compatible build order
            // 1. FontFile2 (font program stream)
            font.EmbedFontData();

            // 2. FontDescriptor
            font.CreateFontDescriptor();

            // 3. CIDFontType2 (descendant font)
            font.CreateCIDFontDictionary();

            // 4. ToUnicode CMap
            font.CreateToUnicodeCMap();

            // 5. Type 0 (composite) font
            font.CreateType0FontDictionary();

            // /ToUnicode and /W depend on the text shown with the font: they are valid from the
            // start (no text yet) and brought up to date every time the document is written
            font.UpdateUsedGlyphObjects();
            xref.RegisterBeforeWrite(font);

            return font;
        }

        private static void ValidateCodePage(int codePage)
        {
            // Supported code pages:
            //   932/936/949/950 — CJK variants (Shift-JIS / GBK / EUC-KR / Big5)
            //   65001           — UTF-8 / arbitrary Unicode (Identity-H via TTF cmap)
            if (codePage != 932 &&
                codePage != 936 &&
                codePage != 949 &&
                codePage != 950 &&
                codePage != 65001)
            {
                throw new HpdfException(
                    HpdfErrorCode.InvalidParameter,
                    $"Unsupported code page for CID font: {codePage}. Supported: 932 (JP), 936 (CN-S), 949 (KR), 950 (CN-T), 65001 (UTF-8/Unicode)");
            }

            // Register code page provider (needed for non-UTF-8 code pages on .NET Core/Linux)
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch
            {
                // Already registered
            }
        }

        private void ParseCmapTable(TrueTypeParser parser)
        {
            var cmapTable = parser.FindTable(_offsetTable, "cmap");
            if (cmapTable is null)
                throw new HpdfException(HpdfErrorCode.TtInvalidCmapTable, "cmap table not found");

            parser.Seek(cmapTable.Offset);
            ushort version = parser.ReadUInt16();
            ushort numTables = parser.ReadUInt16();

            // Find Unicode cmap (platform 3, encoding 1 or 10)
            uint cmapOffset = 0;
            for (int i = 0; i < numTables; i++)
            {
                ushort platformId = parser.ReadUInt16();
                ushort encodingId = parser.ReadUInt16();
                uint offset = parser.ReadUInt32();

                // Platform 3 (Microsoft), encoding 1 (Unicode BMP) or 10 (Unicode full)
                if (platformId == 3 && (encodingId == 1 || encodingId == 10))
                {
                    cmapOffset = offset;
                    break;
                }
                // Platform 0 (Unicode) as fallback
                else if (platformId == 0)
                {
                    cmapOffset = offset;
                }
            }

            if (cmapOffset == 0)
                throw new HpdfException(HpdfErrorCode.TtInvalidCmapTable, "No suitable cmap subtable found");

            // Parse cmap format 4
            parser.Seek(cmapTable.Offset + cmapOffset);
            ushort format = parser.ReadUInt16();

            if (format == 4)
            {
                ParseCmapFormat4(parser);
            }
            else
            {
                throw new HpdfException(HpdfErrorCode.UnsupportedFunc, $"Unsupported cmap format: {format}");
            }
        }

        private void ParseCmapFormat4(TrueTypeParser parser)
        {
            _cmap = new TrueTypeCmapFormat4
            {
                Format = 4,
                Length = parser.ReadUInt16(),
                Language = parser.ReadUInt16(),
                SegCountX2 = parser.ReadUInt16()
            };

            int segCount = _cmap.SegCountX2 / 2;

            _cmap.SearchRange = parser.ReadUInt16();
            _cmap.EntrySelector = parser.ReadUInt16();
            _cmap.RangeShift = parser.ReadUInt16();

            // Read endCount array
            _cmap.EndCount = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                _cmap.EndCount[i] = parser.ReadUInt16();

            _cmap.ReservedPad = parser.ReadUInt16();

            // Read startCount array
            _cmap.StartCount = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                _cmap.StartCount[i] = parser.ReadUInt16();

            // Read idDelta array
            _cmap.IdDelta = new short[segCount];
            for (int i = 0; i < segCount; i++)
                _cmap.IdDelta[i] = parser.ReadInt16();

            // Read idRangeOffset array
            _cmap.IdRangeOffset = new ushort[segCount];
            for (int i = 0; i < segCount; i++)
                _cmap.IdRangeOffset[i] = parser.ReadUInt16();

            // Read glyphIdArray
            int glyphIdArraySize = (_cmap.Length - (16 + segCount * 8)) / 2;
            _cmap.GlyphIdArray = new ushort[glyphIdArraySize];
            for (int i = 0; i < glyphIdArraySize; i++)
                _cmap.GlyphIdArray[i] = parser.ReadUInt16();
        }

        private void ParseOS2Table(TrueTypeParser parser, TrueTypeTable table)
        {
            parser.Seek(table.Offset);

            _os2 = new TrueTypeOS2
            {
                Version = parser.ReadUInt16(),
                XAvgCharWidth = parser.ReadInt16(),
                WeightClass = parser.ReadUInt16(),
                WidthClass = parser.ReadUInt16(),
                FsType = parser.ReadUInt16(),
                YSubscriptXSize = parser.ReadInt16(),
                YSubscriptYSize = parser.ReadInt16(),
                YSubscriptXOffset = parser.ReadInt16(),
                YSubscriptYOffset = parser.ReadInt16(),
                YSuperscriptXSize = parser.ReadInt16(),
                YSuperscriptYSize = parser.ReadInt16(),
                YSuperscriptXOffset = parser.ReadInt16(),
                YSuperscriptYOffset = parser.ReadInt16(),
                YStrikeoutSize = parser.ReadInt16(),
                YStrikeoutPosition = parser.ReadInt16(),
                SFamilyClass = parser.ReadInt16(),
                Panose = parser.ReadBytes(10)
            };

            _os2.UnicodeRange1 = parser.ReadUInt32();
            _os2.UnicodeRange2 = parser.ReadUInt32();
            _os2.UnicodeRange3 = parser.ReadUInt32();
            _os2.UnicodeRange4 = parser.ReadUInt32();
            _os2.AchVendID = parser.ReadBytes(4);
            _os2.FsSelection = parser.ReadUInt16();
            _os2.FirstCharIndex = parser.ReadUInt16();
            _os2.LastCharIndex = parser.ReadUInt16();
            _os2.STypoAscender = parser.ReadInt16();
            _os2.STypoDescender = parser.ReadInt16();
            _os2.STypoLineGap = parser.ReadInt16();
            _os2.UsWinAscent = parser.ReadUInt16();
            _os2.UsWinDescent = parser.ReadUInt16();

            if (_os2.Version >= 1)
            {
                _os2.CodePageRange1 = parser.ReadUInt32();
                _os2.CodePageRange2 = parser.ReadUInt32();
            }
        }

        /// <summary>
        /// Extracts the PostScript name from the TrueType font's name table.
        /// PostScript name is stored as name ID 6.
        /// </summary>
        private string ExtractFontName()
        {
            if (_nameTable is null || _nameTableRef is null || _nameTable.NameRecords is null)
            {
                // Fallback to synthetic name if name table not available
                return $"CIDFont-{_localName}";
            }

            // Create parser from font data to read name strings
            using (var stream = new MemoryStream(_fontData))
            using (var parser = new TrueTypeParser(stream))
            {
                // Priority 1: PostScript name (name ID 6) - Platform 3 (Microsoft), Encoding 1 (Unicode), Language 0x0409 (English US)
                TrueTypeNameRecord? postScriptName = FindNameRecord(_nameTable.NameRecords, 6, 3, 1, 0x0409);

                // Priority 2: PostScript name - Platform 3, any encoding, any language
                if (postScriptName is null)
                    postScriptName = FindNameRecord(_nameTable.NameRecords, 6, 3);

                // Priority 3: PostScript name - Platform 1 (Macintosh)
                if (postScriptName is null)
                    postScriptName = FindNameRecord(_nameTable.NameRecords, 6, 1);

                // Priority 4: PostScript name - Any platform
                if (postScriptName is null)
                    postScriptName = FindNameRecord(_nameTable.NameRecords, 6);

                if (postScriptName != null)
                {
                    string? name = parser.ReadNameString(_nameTable, _nameTableRef!, postScriptName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        // Clean up the name - remove invalid characters for PDF names
                        // PDF names cannot contain spaces, so some fonts use hyphen instead
                        return CleanFontName(name);
                    }
                }

                // Fallback: Try family name (ID 1) + subfamily name (ID 2)
                TrueTypeNameRecord? familyName = FindNameRecord(_nameTable.NameRecords, 1, 3, 1, 0x0409);
                if (familyName is null)
                    familyName = FindNameRecord(_nameTable.NameRecords, 1, 3);
                if (familyName is null)
                    familyName = FindNameRecord(_nameTable.NameRecords, 1);

                if (familyName != null)
                {
                    string? name = parser.ReadNameString(_nameTable, _nameTableRef!, familyName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return CleanFontName(name);
                    }
                }

                // Last resort: use synthetic name
                return $"CIDFont-{_localName}";
            }
        }

        /// <summary>
        /// Finds a name record by name ID, platform ID, encoding ID, and language ID.
        /// </summary>
        private TrueTypeNameRecord? FindNameRecord(TrueTypeNameRecord[] records, ushort nameId,
            ushort? platformId = null, ushort? encodingId = null, ushort? languageId = null)
        {
            foreach (var record in records)
            {
                if (record.NameId == nameId &&
                    (!platformId.HasValue || record.PlatformId == platformId.Value) &&
                    (!encodingId.HasValue || record.EncodingId == encodingId.Value) &&
                    (!languageId.HasValue || record.LanguageId == languageId.Value))
                {
                    return record;
                }
            }
            return null;
        }

        /// <summary>
        /// Cleans font name for use in PDF.
        /// Removes or replaces characters that are invalid in PDF names.
        /// </summary>
        private string CleanFontName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return $"CIDFont-{_localName}";

            // Remove spaces and invalid characters
            // PDF names should not contain: space, #, /, (, ), <, >, [, ], {, }, %, null
            var cleaned = new System.Text.StringBuilder();
            foreach (char c in name)
            {
                if (c > 32 && c < 127 &&  // Printable ASCII
                    c != '#' && c != '/' && c != '(' && c != ')' &&
                    c != '<' && c != '>' && c != '[' && c != ']' &&
                    c != '{' && c != '}' && c != '%' && c != ' ')
                {
                    cleaned.Append(c);
                }
                else if (c == ' ')
                {
                    // Replace spaces with hyphen (common in font names)
                    cleaned.Append('-');
                }
                // Skip other invalid characters
            }

            string result = cleaned.ToString();

            // Ensure name is not empty
            return string.IsNullOrWhiteSpace(result) ? $"CIDFont-{_localName}" : result;
        }

        /// <summary>
        /// Extracts the font family name from the name table (name ID 1).
        /// </summary>
        private string? ExtractFontFamily()
        {
            if (_nameTable is null || _nameTableRef is null || _nameTable.NameRecords is null)
                return null;

            using (var stream = new MemoryStream(_fontData))
            using (var parser = new TrueTypeParser(stream))
            {
                // Find family name (name ID 1)
                TrueTypeNameRecord? familyName = FindNameRecord(_nameTable.NameRecords, 1, 3, 1, 0x0409);
                if (familyName is null)
                    familyName = FindNameRecord(_nameTable.NameRecords, 1, 3);
                if (familyName is null)
                    familyName = FindNameRecord(_nameTable.NameRecords, 1);

                if (familyName != null)
                {
                    string? name = parser.ReadNameString(_nameTable, _nameTableRef!, familyName);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name.Trim();
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the language tag based on code page.
        /// </summary>
        private string? GetLanguageTag()
        {
            switch (_codePage)
            {
                case 932: // Japanese
                    return "ja";
                case 936: // Simplified Chinese
                    return "zh-CN";
                case 949: // Korean
                    return "ko";
                case 950: // Traditional Chinese
                    return "zh-TW";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Step 1: Embed font data (FontFile2 stream).
        /// Must be done first so it can be referenced by FontDescriptor.
        /// </summary>
        private void EmbedFontData()
        {
            _fontFileStream = new HpdfStreamObject();
            _fontFileStream.Add("Length1", new HpdfNumber(_fontData.Length));
            _fontFileStream.WriteToStream(_fontData);
            _fontFileStream.Filter = HpdfStreamFilter.FlateDecode;

            // Add to xref BEFORE referencing
            _xref.Add(_fontFileStream);
        }

        /// <summary>
        /// Step 2: Create FontDescriptor.
        /// Must be done after FontFile2 and before CIDFontType2.
        /// </summary>
        private void CreateFontDescriptor()
        {
            _descriptor.Add("Type", new HpdfName("FontDescriptor"));
            _descriptor.Add("FontName", new HpdfName(_baseFont));

            // Calculate flags
            int flags = CalculateFlags();
            _descriptor.Add("Flags", new HpdfNumber(flags));

            // Add font bounding box
            var bbox = new HpdfArray();
            bbox.Add(new HpdfNumber(_head.XMin));
            bbox.Add(new HpdfNumber(_head.YMin));
            bbox.Add(new HpdfNumber(_head.XMax));
            bbox.Add(new HpdfNumber(_head.YMax));
            _descriptor.Add("FontBBox", bbox);

            // Calculate and add italic angle
            double italicAngle = CalculateItalicAngle();
            _descriptor.Add("ItalicAngle", new HpdfReal((float)italicAngle));

            // Add metrics
            _descriptor.Add("Ascent", new HpdfNumber(_hhea.Ascender));
            _descriptor.Add("Descent", new HpdfNumber(_hhea.Descender));

            // Calculate CapHeight
            int capHeight = _os2 != null ? _os2.STypoAscender : _head.YMax;
            _descriptor.Add("CapHeight", new HpdfNumber(capHeight));

            // Calculate StemV
            int stemV = CalculateStemV();
            _descriptor.Add("StemV", new HpdfNumber(stemV));

            // Link to embedded font file
            _descriptor.Add("FontFile2", _fontFileStream);

            // Adobe compatibility: Add optional but recommended fields
            // Extract font family name for FontFamily field
            string? fontFamily = ExtractFontFamily();
            if (!string.IsNullOrEmpty(fontFamily))
            {
                _descriptor.Add("FontFamily", new HpdfString(fontFamily));
            }

            // Add FontStretch (assume Normal for CJK fonts)
            _descriptor.Add("FontStretch", new HpdfName("Normal"));

            // Add FontWeight from OS/2 table
            int fontWeight = _os2 != null ? _os2.WeightClass : 400;
            _descriptor.Add("FontWeight", new HpdfNumber(fontWeight));

            // Add Lang (language tag) based on code page
            string? langTag = GetLanguageTag();
            if (!string.IsNullOrEmpty(langTag))
            {
                _descriptor.Add("Lang", new HpdfName(langTag));
            }

            // Add to xref BEFORE referencing
            _xref.Add(_descriptor);
        }

        /// <summary>
        /// Step 3: Create CIDFontType2 dictionary (descendant font).
        /// Must be done after FontDescriptor and before Type 0 font.
        /// </summary>
        private void CreateCIDFontDictionary()
        {
            _cidFontDict.Add("Type", new HpdfName("Font"));
            _cidFontDict.Add("Subtype", new HpdfName("CIDFontType2"));
            _cidFontDict.Add("BaseFont", new HpdfName(_baseFont));

            // Add CIDSystemInfo
            _cidFontDict.Add("CIDSystemInfo", _systemInfo.ToDict());

            // Link to FontDescriptor
            _cidFontDict.Add("FontDescriptor", _descriptor);

            // Add default width
            int defaultWidth = CalculateDefaultWidth();
            _cidFontDict.Add("DW", new HpdfNumber(defaultWidth));

            // The widths array (W) is written by UpdateUsedGlyphObjects, for the glyphs in use

            // Add CIDToGIDMap - use Identity for TrueType fonts
            _cidFontDict.Add("CIDToGIDMap", new HpdfName("Identity"));

            // Add to xref BEFORE referencing
            _xref.Add(_cidFontDict);
        }

        /// <summary>
        /// Step 4: Create the ToUnicode CMap stream.
        /// Must be done after CIDFontType2 and before Type 0 font. The content is written by
        /// UpdateUsedGlyphObjects, from the glyphs the text uses.
        /// </summary>
        private void CreateToUnicodeCMap()
        {
            _toUnicodeStream = new HpdfStreamObject();
            _toUnicodeStream.Filter = HpdfStreamFilter.FlateDecode;

            // Add to xref BEFORE referencing
            _xref.Add(_toUnicodeStream);
        }

        /// <summary>
        /// Writes the objects that depend on the text shown with the font: the /ToUnicode CMap
        /// (glyph ID -> the character it was shown for) and the widths (/W) of the glyphs in use.
        /// The content stream holds glyph IDs (Identity-H, CIDToGIDMap Identity), so only the font
        /// knows which character a glyph stands for; a glyph shared by several characters maps to
        /// the one it was first shown for.
        /// </summary>
        private void UpdateUsedGlyphObjects()
        {
            // Glyphs are only added, so the count tells whether anything changed since the last call
            if (_writtenGlyphCount == _usedGlyphs.Count)
                return;

            string cmapContent = CMapGenerator.GenerateToUnicodeCMap(_systemInfo, _usedGlyphs);
            _toUnicodeStream.ClearStream();
            _toUnicodeStream.WriteToStream(System.Text.Encoding.ASCII.GetBytes(cmapContent));

            _cidFontDict["W"] = CreateCIDWidthsArray();

            _writtenGlyphCount = _usedGlyphs.Count;
        }

        void IHpdfBeforeWrite.BeforeWrite() => UpdateUsedGlyphObjects();

        /// <summary>
        /// Step 5: Create Type 0 (composite) font dictionary.
        /// This is the main font object that references everything else.
        /// Must be done last after all other objects are created and added to xref.
        /// </summary>
        private void CreateType0FontDictionary()
        {
            _dict.Add("Type", new HpdfName("Font"));
            _dict.Add("Subtype", new HpdfName("Type0"));
            _dict.Add("BaseFont", new HpdfName(_baseFont));

            // Encoding: Identity-H for horizontal writing
            string encoding = _writingMode == CIDWritingMode.Horizontal ? "Identity-H" : "Identity-V";
            _dict.Add("Encoding", new HpdfName(encoding));

            // DescendantFonts array (contains the CIDFont)
            var descendantFonts = new HpdfArray();
            descendantFonts.Add(_cidFontDict);
            _dict.Add("DescendantFonts", descendantFonts);

            // Link ToUnicode CMap
            _dict.Add("ToUnicode", _toUnicodeStream);

            // Add to xref
            _xref.Add(_dict);
        }

        /// <summary>
        /// Creates the W (widths) array for CIDFontType2: the widths of the glyphs in use and of
        /// glyph 0 (.notdef), which is drawn for every character the font has no glyph for.
        /// For Identity-H with CIDToGIDMap=Identity, CID = Glyph ID.
        /// Format: [c [w1 w2 ...]] - the first CID of a run followed by the widths of the run
        /// </summary>
        private HpdfArray CreateCIDWidthsArray()
        {
            var glyphs = new List<ushort>(_usedGlyphs.Count + 1) { 0 };
            glyphs.AddRange(_usedGlyphs.Keys); // glyph 0 is never among the keys
            glyphs.Sort();

            var widths = new HpdfArray();
            HpdfArray? run = null;
            int previous = -1;
            foreach (ushort glyph in glyphs)
            {
                if (run is null || glyph != previous + 1)
                {
                    run = new HpdfArray();
                    widths.Add(new HpdfNumber(glyph));
                    widths.Add(run);
                }

                run.Add(new HpdfNumber(GetScaledGlyphWidth(glyph)));
                previous = glyph;
            }

            return widths;
        }

        /// <summary>
        /// Calculates default width for CID font.
        /// </summary>
        private int CalculateDefaultWidth()
        {
            // Use average width of typical CJK character
            if (_hMetrics != null && _hMetrics.Length > 0)
            {
                // Calculate average from first few glyphs
                int sum = 0;
                int count = Math.Min(100, _hMetrics.Length);
                for (int i = 0; i < count; i++)
                {
                    sum += _hMetrics[i].AdvanceWidth;
                }
                int avgWidth = sum / count;

                // Scale from font units to 1000-unit em square
                return (int)Math.Round(avgWidth * 1000.0 / _head.UnitsPerEm);
            }

            return 1000; // Default 1em width
        }

        /// <summary>
        /// Calculates font descriptor flags.
        /// </summary>
        private int CalculateFlags()
        {
            int flags = 0;

            if (_os2 != null)
            {
                // Bit 1: FixedPitch
                if (_os2.Panose.Length > 3 && _os2.Panose[3] == 9)
                    flags |= 1;

                // Bit 2: Serif
                if (_os2.Panose.Length > 0 && _os2.Panose[0] == 2)
                    flags |= 2;

                // Bit 4: Script
                if (_os2.Panose.Length > 0 && _os2.Panose[0] == 3)
                    flags |= 8;

                // Bit 6: Italic
                if ((_os2.FsSelection & 0x01) != 0 || (_head.MacStyle & 0x02) != 0)
                    flags |= 64;
            }

            // Bit 6: Nonsymbolic (always set for text fonts)
            flags |= 32;

            return flags;
        }

        /// <summary>
        /// Calculates italic angle.
        /// </summary>
        private double CalculateItalicAngle()
        {
            if (_post != null)
            {
                return _post.ItalicAngle / 65536.0;
            }

            if (_os2 != null && (_os2.FsSelection & 0x01) != 0)
                return -12.0;

            if ((_head.MacStyle & 0x02) != 0)
                return -12.0;

            return 0.0;
        }

        /// <summary>
        /// Calculates StemV based on font weight.
        /// </summary>
        private int CalculateStemV()
        {
            if (_os2 != null)
            {
                int weight = _os2.WeightClass;
                return Math.Max(50, 50 + (weight - 400) / 5);
            }

            return 80;
        }

        /// <summary>
        /// Converts text to glyph IDs for use in content stream.
        /// For Identity-H with CIDToGIDMap=Identity, content stream must contain glyph IDs.
        /// The glyphs are remembered as used: the /ToUnicode CMap and the widths of the font are
        /// written for them when the document is saved.
        /// </summary>
        /// <param name="text">Text to convert</param>
        /// <returns>Array of glyph IDs (2 bytes each, big-endian), one per character</returns>
        public byte[] ConvertTextToGlyphIDs(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            var bytes = new byte[text.Length * 2];
            int length = 0;

            foreach (Rune character in text.EnumerateRunes())
            {
                ushort glyphId = GetGlyphId(character);
                if (glyphId != 0)
                    _usedGlyphs.TryAdd(glyphId, character.Value);

                bytes[length++] = (byte)(glyphId >> 8);
                bytes[length++] = (byte)(glyphId & 0xFF);
            }

            // A surrogate pair is one character (one glyph), so the text can be shorter than its length
            return length == bytes.Length ? bytes : bytes.AsSpan(0, length).ToArray();
        }

        /// <summary>
        /// Encodes text to glyph IDs for CID fonts.
        /// </summary>
        public byte[] EncodeText(string text)
        {
            // For CID fonts, encoding means converting to glyph IDs
            return ConvertTextToGlyphIDs(text);
        }

        /// <summary>
        /// Gets the glyph ID for a Unicode character.
        /// </summary>
        public ushort GetGlyphId(ushort unicode)
        {
            if (_cmap is null)
                return 0;

            int segCount = _cmap.SegCountX2 / 2;
            if (segCount == 0)
                return 0;

            // The segments are sorted by their end code: find the first one that ends at or
            // after the character
            int low = 0;
            int high = segCount - 1;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (_cmap.EndCount[middle] < unicode)
                    low = middle + 1;
                else
                    high = middle;
            }

            int i = low;
            if (unicode > _cmap.EndCount[i] || unicode < _cmap.StartCount[i])
                return 0; // .notdef

            if (_cmap.IdRangeOffset[i] == 0)
                return (ushort)((unicode + _cmap.IdDelta[i]) & 0xFFFF);

            int offset = _cmap.IdRangeOffset[i] / 2 + (unicode - _cmap.StartCount[i]) - (segCount - i);
            if (offset >= 0 && offset < _cmap.GlyphIdArray.Length)
            {
                ushort glyphId = _cmap.GlyphIdArray[offset];
                if (glyphId != 0)
                    return (ushort)((glyphId + _cmap.IdDelta[i]) & 0xFFFF);
            }

            return 0; // .notdef
        }

        /// <summary>
        /// Gets the glyph that draws a character. This is the one mapping from text to glyphs:
        /// text is drawn (<see cref="ConvertTextToGlyphIDs"/>) and measured (<see cref="MeasureText"/>)
        /// with it, whatever the font's code page is. The font's cmap covers the Basic Multilingual
        /// Plane only; other characters have no glyph.
        /// </summary>
        private ushort GetGlyphId(Rune character)
        {
            return character.IsBmp ? GetGlyphId((ushort)character.Value) : (ushort)0;
        }

        /// <summary>
        /// Gets the width of a glyph in font units.
        /// </summary>
        public int GetGlyphWidth(ushort glyphId)
        {
            if (_hMetrics == null || glyphId >= _hMetrics.Length)
                return 0;

            return _hMetrics[glyphId].AdvanceWidth;
        }

        /// <summary>
        /// Gets the width of a glyph in 1000-unit glyph space, rounded as it is written to /W:
        /// text is measured with the advances a viewer uses.
        /// </summary>
        private int GetScaledGlyphWidth(ushort glyphId)
        {
            return (int)Math.Round(GetGlyphWidth(glyphId) * 1000.0 / _head.UnitsPerEm);
        }

        /// <summary>
        /// Gets the width of a character in 1000-unit glyph space.
        /// For CID fonts, this converts the character to a glyph ID and returns its width.
        /// </summary>
        /// <param name="charCode">The character code (treated as Unicode).</param>
        /// <returns>The character width in 1000-unit glyph space.</returns>
        public float GetCharWidth(byte charCode)
        {
            return GetScaledGlyphWidth(GetGlyphId((ushort)charCode));
        }

        /// <summary>
        /// Measures text width in user space units: the sum of the widths of the glyphs the text
        /// is drawn with.
        /// </summary>
        public float MeasureText(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int totalWidth = 0;
            foreach (Rune character in text.EnumerateRunes())
            {
                totalWidth += GetScaledGlyphWidth(GetGlyphId(character));
            }

            // Convert from 1000-unit glyph space to user space
            return totalWidth * fontSize / 1000f;
        }

        /// <summary>
        /// Releases all resources used by this CID font.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this CID font and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _fontFileStream?.Dispose();
                    _toUnicodeStream?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
