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
        private readonly string _baseFont;
        private readonly string _localName;
        private readonly HpdfXref _xref;
        private readonly int _codePage;

        // The font file, parsed once
        private readonly TrueTypeFontFile _file;

        // The embedded font program (FontFile2), written when the document is saved
        private readonly TrueTypeFontProgram _fontProgram;

        private HpdfStreamObject _toUnicodeStream = null!;
        private CIDSystemInfo _systemInfo = null!;
        private HpdfWritingMode _writingMode;
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
        public int Ascent => _file.Ascent;

        /// <summary>
        /// Gets the font descent in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public int Descent => _file.Descent;

        /// <summary>
        /// Gets the x-height in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public int XHeight => _file.XHeight;

        /// <summary>
        /// Gets the font bounding box in 1000-unit glyph space (scaled from font units).
        /// </summary>
        public HpdfBox FontBBox => _file.FontBBox;

        /// <summary>
        /// Gets or sets whether the font is embedded as a subset: the font program in the document
        /// holds the outlines of the glyphs in use only, and the font name gets a subset tag
        /// ("ABCDEF+Name"). This is the default. Set it to false to embed the whole font file.
        /// A font that forbids subsetting (fsType of its OS/2 table) or has no TrueType outlines
        /// is always embedded in full.
        /// </summary>
        public bool EmbedSubset { get; set; } = true;

        /// <summary>
        /// Gets an HpdfFont wrapper for this CID font that can be used with page operations.
        /// </summary>
        public HpdfFont AsFont()
        {
            return new HpdfFont(this);
        }

        private HpdfCIDFont(HpdfXref xref, string localName, TrueTypeFontFile file, int codePage)
        {
            _xref = xref ?? throw new ArgumentNullException(nameof(xref));
            _localName = localName ?? throw new ArgumentNullException(nameof(localName));
            _file = file;
            _codePage = codePage;

            // The name of the font in the document; a synthetic one if the font file has none
            _baseFont = file.Names.PostScriptName ?? $"CIDFont-{localName}";

            // A CID font is always embedded: the font must permit it
            _fontProgram = new TrueTypeFontProgram(file, _baseFont, TrueTypeSubsetter.CidFontTables);
            _systemInfo = CIDSystemInfo.CreateIdentity();
            _writingMode = HpdfWritingMode.Horizontal;

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

            return Create(document.Xref, localName, TrueTypeFontFile.Parse(File.ReadAllBytes(filePath)), codePage);
        }

        /// <summary>
        /// Creates the font of a document from a parsed font file.
        /// </summary>
        private static HpdfCIDFont Create(HpdfXref xref, string localName, TrueTypeFontFile file, int codePage)
        {
            var font = new HpdfCIDFont(xref, localName, file, codePage);

            // CRITICAL: Follow Adobe-compatible build order
            // 1. FontFile2 (font program stream): added to xref BEFORE it is referenced. The font
            //    program is written when the document is saved: a subset depends on the glyphs in use
            xref.Add(font._fontProgram.Stream);

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
        /// Writes the embedded font program and the font names that go with it: a subset with the
        /// outlines of the glyphs in use, named "ABCDEF+Name", or the whole font file.
        /// </summary>
        private void UpdateFontProgram()
        {
            if (_fontProgram.Update(EmbedSubset, _usedGlyphs.Keys))
            {
                _descriptor["FontName"] = new HpdfName(_fontProgram.FontName);
                _cidFontDict["BaseFont"] = new HpdfName(_fontProgram.FontName);
                _dict["BaseFont"] = new HpdfName(_fontProgram.FontName);
            }
        }

        /// <summary>
        /// Step 2: Create FontDescriptor.
        /// Must be done after FontFile2 and before CIDFontType2.
        /// </summary>
        private void CreateFontDescriptor()
        {
            TrueTypeFontDescriptor.AddFontMetrics(_descriptor, _file, _baseFont);

            // Link to embedded font file
            _descriptor.Add("FontFile2", _fontProgram.Stream);

            // Adobe compatibility: Add optional but recommended fields
            // Extract font family name for FontFamily field
            string? fontFamily = _file.Names.FamilyName;
            if (!string.IsNullOrEmpty(fontFamily))
            {
                _descriptor.Add("FontFamily", new HpdfString(fontFamily));
            }

            // Add FontStretch (assume Normal for CJK fonts)
            _descriptor.Add("FontStretch", new HpdfName("Normal"));

            // Add FontWeight from OS/2 table
            int fontWeight = _file.OS2?.WeightClass ?? 400;
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

        void IHpdfBeforeWrite.BeforeWrite()
        {
            UpdateUsedGlyphObjects();
            UpdateFontProgram();
        }

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
            string encoding = _writingMode == HpdfWritingMode.Horizontal ? "Identity-H" : "Identity-V";
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
            // The average width of the first glyphs of the font
            int count = Math.Min(100, _file.GlyphCount);
            if (count == 0)
                return 1000; // Default 1em width

            int sum = 0;
            for (ushort glyph = 0; glyph < count; glyph++)
            {
                sum += _file.GetGlyphWidth(glyph);
            }

            // Scale from font units to 1000-unit em square
            return _file.Scale(sum / count);
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
        public ushort GetGlyphId(ushort unicode) => _file.GetGlyphId(unicode);

        /// <summary>
        /// Gets the glyph that draws a character, 0 (.notdef) if the font has no glyph for it.
        /// This is the one mapping from text to glyphs: text is drawn (<see cref="ConvertTextToGlyphIDs"/>)
        /// and measured (<see cref="MeasureText"/>) with it, whatever the font's code page is.
        /// A character above U+FFFF (a surrogate pair in a string) needs a font with a cmap subtable
        /// for all of Unicode (format 12).
        /// </summary>
        /// <param name="character">The Unicode character.</param>
        public ushort GetGlyphId(Rune character) => _file.GetGlyphId(character);

        /// <summary>
        /// Gets the width of a glyph in font units.
        /// </summary>
        public int GetGlyphWidth(ushort glyphId) => _file.GetGlyphWidth(glyphId);

        /// <summary>
        /// Gets the width of a glyph in 1000-unit glyph space, rounded as it is written to /W:
        /// text is measured with the advances a viewer uses.
        /// </summary>
        private int GetScaledGlyphWidth(ushort glyphId) => _file.GetScaledGlyphWidth(glyphId);

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
                    _fontProgram.Dispose();
                    _toUnicodeStream?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
