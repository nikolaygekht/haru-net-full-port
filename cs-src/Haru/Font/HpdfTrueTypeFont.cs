using System;
using System.IO;
using System.Collections.Generic;
using Haru.Objects;
using Haru.Xref;
using Haru.Types;
using Haru.Font.TrueType;
using Haru.Streams;

namespace Haru.Font
{
    /// <summary>
    /// Represents a simple font (one byte per character, a single-byte code page) made from an
    /// OpenType font file that can be embedded in a PDF: a TrueType font for TrueType outlines, a
    /// Type1 font for CFF outlines with glyph names. A font with CID-keyed CFF outlines (CJK
    /// OpenType fonts) can only be a CID font (<see cref="CID.HpdfCIDFont"/>).
    /// </summary>
    public class HpdfTrueTypeFont : IHpdfFontImplementation, IHpdfBeforeWrite, IDisposable
    {
        private readonly HpdfDict _dict;
        private readonly string _baseFont;
        private readonly string _localName;
        private readonly HpdfXref _xref;

        // The font file: parsed once, possibly shared with other fonts (FontDataPool)
        private readonly TrueTypeFontFile _file;

        // The embedded font program (FontFile2 or FontFile3), written when the document is saved;
        // null for a font that is not embedded
        private readonly IEmbeddedFontProgram? _fontProgram;
        private List<ushort>? _codePageGlyphs;

        private HpdfDict _descriptor = null!;
        private HpdfStreamObject? _toUnicodeStream;
        private readonly int _codePage;
        private int[]? _codeWidths;
        private bool _disposed = false;

        /// <summary>
        /// Gets the underlying dictionary object for this font.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the BaseFont name (PostScript name).
        /// </summary>
        public string BaseFont => _baseFont;

        /// <summary>
        /// Gets the local name used to reference this font in page resources.
        /// </summary>
        public string LocalName => _localName;

        /// <summary>
        /// Gets the code page used for encoding this font.
        /// </summary>
        public int? CodePage => _codePage;

        /// <summary>
        /// Gets the font descriptor dictionary.
        /// </summary>
        public HpdfDict Descriptor => _descriptor;

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
        /// Gets or sets whether an embedded font is embedded as a subset: the font program in the
        /// document holds the outlines of the glyphs of the font's code page only, and the font name
        /// gets a subset tag ("ABCDEF+Name"). This is the default. Set it to false to embed the whole
        /// font (the font file, or for CFF outlines the whole CFF program). A font that forbids
        /// subsetting (fsType of its OS/2 table) is always embedded in full. The property has no
        /// effect on a font that is not embedded.
        /// </summary>
        public bool EmbedSubset { get; set; } = true;

        /// <summary>
        /// Gets an HpdfFont wrapper for this TrueType font that can be used with page operations.
        /// </summary>
        public HpdfFont AsFont()
        {
            return new HpdfFont(this);
        }

        private HpdfTrueTypeFont(HpdfXref xref, string localName, TrueTypeFontFile file, bool embedding, int codePage)
        {
            _xref = xref ?? throw new ArgumentNullException(nameof(xref));
            _localName = localName ?? throw new ArgumentNullException(nameof(localName));
            _file = file;
            _codePage = codePage;
            _dict = new HpdfDict();

            // The name of the font in the document; a synthetic one if the font file has none
            _baseFont = file.Names.PostScriptName ?? $"CustomTTFont-{localName}";

            // Glyphs of a simple font are selected by name with CFF outlines: a CID-keyed font has none
            if (file.Cff?.IsCidKeyed == true)
                throw CffFontProgram.CidKeyedSimpleFont(_baseFont);

            // The font must permit embedding
            if (embedding)
                _fontProgram = EmbeddedFontProgram.Create(file, _baseFont, EmbeddedFontKind.Simple);
        }

        /// <summary>
        /// Creates the font of a document from a parsed font file.
        /// </summary>
        private static HpdfTrueTypeFont Create(HpdfXref xref, string localName, TrueTypeFontFile file, bool embedding, int codePage)
        {
            var font = new HpdfTrueTypeFont(xref, localName, file, embedding, codePage);

            font.CreateFontDictionary();
            font.CreateFontDescriptor();

            // The embedded font program is written when the document is saved
            if (embedding)
                font._xref.RegisterBeforeWrite(font);

            return font;
        }

        /// <summary>
        /// Loads a TrueType font from a file with default code page (1252 - Windows ANSI/WinAnsiEncoding).
        /// This matches libharu's default behavior and PDF standard encoding for Western European text.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="filePath">Path to the TTF file.</param>
        /// <param name="embedding">Whether to embed the font data.</param>
        /// <returns>The loaded TrueType font.</returns>
        public static HpdfTrueTypeFont LoadFromFile(HpdfXref xref, string localName, string filePath, bool embedding)
        {
            return LoadFromFile(xref, localName, filePath, embedding, 1252);
        }

        /// <summary>
        /// Loads a TrueType font from a file with specified code page.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="filePath">Path to the TTF file.</param>
        /// <param name="embedding">Whether to embed the font data.</param>
        /// <param name="codePage">The code page to use for encoding (e.g., 437 for DOS, 1251 for Cyrillic).</param>
        /// <returns>The loaded TrueType font.</returns>
        public static HpdfTrueTypeFont LoadFromFile(HpdfXref xref, string localName, string filePath, bool embedding, int codePage)
        {
            if (!File.Exists(filePath))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"Font file not found: {filePath}");

            byte[] fontData = File.ReadAllBytes(filePath);

            using (var stream = new MemoryStream(fontData))
            {
                return LoadFromStream(xref, localName, stream, fontData, embedding, codePage);
            }
        }

        /// <summary>
        /// Loads a font of a TrueType font collection (.ttc), as <c>HPDF_LoadTTFontFromFile2</c> does.
        /// An embedded font is taken out of the collection: the document holds the one font (or its
        /// subset), not the collection.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="filePath">Path to the TTC file.</param>
        /// <param name="fontIndex">The index of the font in the collection, from 0.</param>
        /// <param name="embedding">Whether to embed the font data.</param>
        /// <param name="codePage">The code page to use for encoding (e.g., 1252 for Windows ANSI, 1251 for Cyrillic).</param>
        /// <returns>The loaded TrueType font.</returns>
        /// <exception cref="HpdfException">The file does not exist (<see cref="HpdfErrorCode.FileNotFound"/>),
        /// is not a font collection (<see cref="HpdfErrorCode.InvalidTtcFile"/>), has no font with the
        /// index (<see cref="HpdfErrorCode.InvalidTtcIndex"/>), or the font cannot be used.</exception>
        public static HpdfTrueTypeFont LoadFromCollection(HpdfXref xref, string localName, string filePath, int fontIndex, bool embedding, int codePage = 1252)
        {
            if (!File.Exists(filePath))
                throw new HpdfException(HpdfErrorCode.FileNotFound, $"Font file not found: {filePath}");

            return Create(xref, localName, TrueTypeFontFile.Parse(File.ReadAllBytes(filePath), fontIndex), embedding, codePage);
        }

        /// <summary>
        /// Loads a font of a TrueType font collection (.ttc) from a stream, for example an embedded
        /// resource. The stream is read from its current position to its end and is not closed.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="stream">The TTC data.</param>
        /// <param name="fontIndex">The index of the font in the collection, from 0.</param>
        /// <param name="embedding">Whether to embed the font data.</param>
        /// <param name="codePage">The code page to use for encoding (e.g., 1252 for Windows ANSI, 1251 for Cyrillic).</param>
        /// <returns>The loaded TrueType font.</returns>
        /// <exception cref="HpdfException">The data is not a font collection (<see cref="HpdfErrorCode.InvalidTtcFile"/>),
        /// has no font with the index (<see cref="HpdfErrorCode.InvalidTtcIndex"/>), or the font cannot be used.</exception>
        public static HpdfTrueTypeFont LoadFromCollection(HpdfXref xref, string localName, Stream stream, int fontIndex, bool embedding, int codePage = 1252)
        {
            return Create(xref, localName, TrueTypeFontFile.Parse(stream, fontIndex), embedding, codePage);
        }

        /// <summary>
        /// Loads a TrueType font from a stream.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="stream">Stream containing TTF data.</param>
        /// <param name="fontData">Font data bytes (for embedding).</param>
        /// <param name="embedding">Whether to embed the font data.</param>
        /// <param name="codePage">The code page to use for encoding (e.g., 1252 for Windows ANSI, 1251 for Cyrillic).</param>
        /// <returns>The loaded TrueType font.</returns>
        public static HpdfTrueTypeFont LoadFromStream(HpdfXref xref, string localName, Stream stream, byte[] fontData, bool embedding, int codePage = 1252)
        {
            return Create(xref, localName, TrueTypeFontFile.Parse(stream, fontData), embedding, codePage);
        }

        /// <summary>
        /// Creates a TrueType font instance from shared parsed data (from FontDataPool).
        /// This method reuses the cached font file: it is read from disk and parsed once, whatever
        /// number of fonts and documents use it.
        /// </summary>
        /// <param name="xref">The cross-reference table for this document.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="parsedData">Shared parsed font data from FontDataPool.</param>
        /// <param name="embedding">Whether to embed the font data in this document.</param>
        /// <returns>The TrueType font instance ready to use in this document.</returns>
        public static HpdfTrueTypeFont LoadFromSharedData(HpdfXref xref, string localName, ParsedTrueTypeData parsedData, bool embedding)
        {
            if (parsedData == null)
                throw new ArgumentNullException(nameof(parsedData));

            return LoadFromSharedData(xref, localName, parsedData, embedding, parsedData.CodePage);
        }

        /// <summary>
        /// Creates a TrueType font instance from shared parsed data (from FontDataPool) with the
        /// given code page. The font file data is shared; the code page belongs to the font.
        /// </summary>
        /// <param name="xref">The cross-reference table for this document.</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="parsedData">Shared parsed font data from FontDataPool.</param>
        /// <param name="embedding">Whether to embed the font data in this document.</param>
        /// <param name="codePage">The code page to use for encoding (e.g., 1252 for Windows ANSI, 1251 for Cyrillic).</param>
        /// <returns>The TrueType font instance ready to use in this document.</returns>
        public static HpdfTrueTypeFont LoadFromSharedData(HpdfXref xref, string localName, ParsedTrueTypeData parsedData, bool embedding, int codePage)
        {
            if (parsedData == null)
                throw new ArgumentNullException(nameof(parsedData));

            return Create(xref, localName, parsedData.FontFile, embedding, codePage);
        }

        private void CreateFontDictionary()
        {
            _dict.Add("Type", new HpdfName("Font"));
            // A font with CFF outlines is a Type1 font (its program is a Type1C font program)
            _dict.Add("Subtype", new HpdfName(_file.Cff != null ? "Type1" : "TrueType"));
            _dict.Add("BaseFont", new HpdfName(_baseFont));

            // Add FirstChar and LastChar
            byte firstChar = 32;
            byte lastChar = 255;

            TrueTypeOS2? os2 = _file.OS2;
            if (os2 != null)
            {
                firstChar = (byte)Math.Max(32, (int)os2.FirstCharIndex);
                lastChar = (byte)Math.Min(255, (int)os2.LastCharIndex);
            }

            _dict.Add("FirstChar", new HpdfNumber(firstChar));
            _dict.Add("LastChar", new HpdfNumber(lastChar));

            // Register code page provider if needed
            try
            {
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            }
            catch
            {
                // Already registered
            }

            var encoding = System.Text.Encoding.GetEncoding(_codePage);

            // Create custom Encoding dictionary with Differences array
            // This tells the PDF reader which glyph to render for each byte value
            CreateEncodingDictionary(encoding, firstChar, lastChar);

            // Add Widths array (1000-unit em square): the same widths the text is measured with
            var codeWidths = CodeWidths;
            var widths = new HpdfArray();
            for (int i = firstChar; i <= lastChar; i++)
            {
                widths.Add(new HpdfNumber(codeWidths[i]));
            }
            _dict.Add("Widths", widths);

            // Add to xref
            _xref.Add(_dict);
        }

        /// <summary>
        /// Creates a custom Encoding dictionary with Differences array for the specified code page.
        /// This maps byte codes to Unicode characters for proper glyph rendering.
        /// </summary>
        private void CreateEncodingDictionary(System.Text.Encoding encoding, byte firstChar, byte lastChar)
        {
            // CFF outlines are selected by the names of the glyphs in the font, whatever the code page
            if (_file.Cff != null)
            {
                CreateGlyphNameEncoding(encoding, firstChar, lastChar);
                return;
            }

            // For standard encodings, just use the built-in name
            if (_codePage == 1252)
            {
                _dict.Add("Encoding", new HpdfName("WinAnsiEncoding"));
                return;
            }
            else if (_codePage == 10000)
            {
                _dict.Add("Encoding", new HpdfName("MacRomanEncoding"));
                return;
            }

            // For custom code pages, create an Encoding dictionary with Differences array
            var encodingDict = new HpdfDict();
            encodingDict.Add("Type", new HpdfName("Encoding"));

            // Use WinAnsiEncoding as base for Western characters (0x20-0x7E are the same)
            encodingDict.Add("BaseEncoding", new HpdfName("WinAnsiEncoding"));

            // Build Differences array
            // Format: [code1 /glyphname1 /glyphname2 ... code2 /glyphname3 ...]
            var differences = new HpdfArray();

            // Track if we need to add a range
            int rangeStart = -1;
            var rangeGlyphs = new List<string>();

            for (int i = firstChar; i <= lastChar; i++)
            {
                // Convert byte value to Unicode character using the code page
                byte[] byteArray = new byte[] { (byte)i };
                string str = encoding.GetString(byteArray);
                ushort unicode = (str.Length > 0) ? (ushort)str[0] : (ushort)0;

                // Skip if it's ASCII range (0x20-0x7E) and matches WinAnsi
                // WinAnsi and most code pages share ASCII characters
                if (i >= 0x20 && i <= 0x7E)
                {
                    // Flush current range if any
                    if (rangeStart >= 0)
                    {
                        differences.Add(new HpdfNumber(rangeStart));
                        foreach (var glyph in rangeGlyphs)
                        {
                            differences.Add(new HpdfName(glyph));
                        }
                        rangeStart = -1;
                        rangeGlyphs.Clear();
                    }
                    continue;
                }

                // For non-ASCII, we need to specify the PostScript glyph name
                // Get the proper glyph name from the mapping table (e.g., afii10017 for Cyrillic А)
                // Fall back to uni[XXXX] format if no standard name exists
                string? glyphName = HpdfGlyphNames.GetGlyphName(unicode);
                if (glyphName == null)
                {
                    // Use uni[XXXX] format for unmapped characters
                    glyphName = $"uni{unicode:X4}";
                }

                // Start a new range or continue current one
                if (rangeStart < 0)
                {
                    rangeStart = i;
                }
                rangeGlyphs.Add(glyphName);
            }

            // Flush final range if any
            if (rangeStart >= 0)
            {
                differences.Add(new HpdfNumber(rangeStart));
                foreach (var glyph in rangeGlyphs)
                {
                    differences.Add(new HpdfName(glyph));
                }
            }

            // Only add Differences if we have custom mappings
            if (differences.Count > 0)
            {
                encodingDict.Add("Differences", differences);
            }

            // Add encoding dictionary to xref and link to font
            _xref.Add(encodingDict);
            _dict.Add("Encoding", encodingDict);
        }

        /// <summary>
        /// Creates the encoding of a font with CFF outlines: every code of the font gets the name
        /// its glyph has in the font (the character of the code page, through the font's cmap), so
        /// a reader finds the glyph by its name whatever names the font uses. A code without a
        /// glyph shows .notdef.
        /// </summary>
        private void CreateGlyphNameEncoding(System.Text.Encoding encoding, byte firstChar, byte lastChar)
        {
            Cff.CffFont cff = _file.Cff!;
            var differences = new HpdfArray();
            differences.Add(new HpdfNumber(firstChar));

            var code = new byte[1];
            for (int i = firstChar; i <= lastChar; i++)
            {
                code[0] = (byte)i;
                string decoded = encoding.GetString(code);
                ushort glyph = decoded.Length > 0 ? GetGlyphId(decoded[0]) : (ushort)0;
                differences.Add(new HpdfName(glyph != 0 && glyph < cff.GlyphCount ? cff.GetGlyphName(glyph) : ".notdef"));
            }

            var encodingDict = new HpdfDict();
            encodingDict.Add("Type", new HpdfName("Encoding"));
            encodingDict.Add("Differences", differences);

            _xref.Add(encodingDict);
            _dict.Add("Encoding", encodingDict);
        }

        private void CreateFontDescriptor()
        {
            _descriptor = new HpdfDict();
            TrueTypeFontDescriptor.AddFontMetrics(_descriptor, _file, _baseFont);

            // The stream of the font program (FontFile2 or FontFile3); it is filled when the document is saved
            if (_fontProgram != null)
            {
                _descriptor.Add(_fontProgram.DescriptorKey, _fontProgram.Stream);
                _xref.Add(_fontProgram.Stream);
            }

            // Add ToUnicode CMap for text extraction
            CreateToUnicodeCMap();

            // Link descriptor to font
            _dict.Add("FontDescriptor", _descriptor);

            // Add to xref
            _xref.Add(_descriptor);
        }

        /// <summary>
        /// Writes the embedded font program and the font names that go with it: a subset with the
        /// outlines of the glyphs of the font's code page, named "ABCDEF+Name", or the whole font
        /// file. Glyph IDs are the same in both, so the font's cmap stays valid.
        /// </summary>
        private void UpdateFontProgram()
        {
            if (_fontProgram is null)
                return;

            _codePageGlyphs ??= GetCodePageGlyphs();
            if (_fontProgram.Update(EmbedSubset, _codePageGlyphs))
            {
                _descriptor["FontName"] = new HpdfName(_fontProgram.FontName);
                _dict["BaseFont"] = new HpdfName(_fontProgram.FontName);
            }
        }

        void IHpdfBeforeWrite.BeforeWrite() => UpdateFontProgram();

        /// <summary>
        /// Gets the glyphs of the characters of the font's code page: every glyph a text in this
        /// font can show.
        /// </summary>
        private List<ushort> GetCodePageGlyphs()
        {
            var encoding = System.Text.Encoding.GetEncoding(_codePage);
            var glyphs = new HashSet<ushort>();
            var code = new byte[1];
            for (int i = 32; i < 256; i++)
            {
                code[0] = (byte)i;
                string decoded = encoding.GetString(code);
                if (decoded.Length == 0)
                    continue;

                ushort glyph = GetGlyphId(decoded[0]);
                if (glyph != 0)
                    glyphs.Add(glyph);
            }
            return new List<ushort>(glyphs);
        }

        /// <summary>
        /// Creates the ToUnicode CMap for text extraction.
        /// </summary>
        private void CreateToUnicodeCMap()
        {
            // Create ToUnicode CMap for the specified code page
            var cmap = ToUnicodeCMap.CreateFromCodePage(_codePage);
            string cmapContent = cmap.Generate();

            // Create stream object
            _toUnicodeStream = new HpdfStreamObject();
            byte[] cmapBytes = System.Text.Encoding.UTF8.GetBytes(cmapContent);
            _toUnicodeStream.WriteToStream(cmapBytes);
            _toUnicodeStream.Filter = HpdfStreamFilter.FlateDecode;

            // Link to font dictionary
            _dict.Add("ToUnicode", _toUnicodeStream);

            // Add to xref
            _xref.Add(_toUnicodeStream);
        }

        /// <summary>
        /// Gets the glyph ID for a Unicode character.
        /// </summary>
        public ushort GetGlyphId(ushort unicode) => _file.GetGlyphId(unicode);

        /// <summary>
        /// Gets the width of a glyph in font units.
        /// </summary>
        public int GetGlyphWidth(ushort glyphId) => _file.GetGlyphWidth(glyphId);

        /// <summary>
        /// Gets the width of a byte of the font's code page, in 1000-unit glyph space.
        /// </summary>
        public float GetCharWidth(byte charCode)
        {
            return CodeWidths[charCode];
        }

        /// <summary>
        /// Widths, in 1000-unit glyph space, of the 256 bytes of the font's code page: each byte is
        /// decoded to Unicode and measured with the glyph the cmap gives for it. The values are rounded
        /// to integers and written to /Widths as they are, so text is measured with the advances a
        /// viewer uses.
        /// </summary>
        private int[] CodeWidths => _codeWidths ??= BuildCodeWidths();

        private int[] BuildCodeWidths()
        {
            var encoding = System.Text.Encoding.GetEncoding(_codePage);
            var widths = new int[256];
            var code = new byte[1];
            for (int i = 0; i < 256; i++)
            {
                code[0] = (byte)i;
                string decoded = encoding.GetString(code);
                ushort unicode = decoded.Length > 0 ? decoded[0] : (ushort)0;
                widths[i] = unicode < 32
                    ? 0
                    : _file.GetScaledGlyphWidth(GetGlyphId(unicode));
            }
            return widths;
        }

        /// <summary>
        /// Measures text width in user space units.
        /// </summary>
        public float MeasureText(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            // Measured as written: the text is encoded with the font's code page first
            var widths = CodeWidths;
            float totalWidth = 0;
            foreach (byte b in EncodeText(text))
            {
                totalWidth += widths[b];
            }

            // Convert from 1000-unit glyph space to user space
            return totalWidth * fontSize / 1000f;
        }

        /// <summary>
        /// Converts text to glyph IDs. Not applicable for TrueType fonts.
        /// </summary>
        public byte[] ConvertTextToGlyphIDs(string text)
        {
            throw new InvalidOperationException("The method is available for CID fonts only");
        }

        /// <summary>
        /// Encodes text to bytes using the font's code page encoding.
        /// </summary>
        public byte[] EncodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            var encoding = System.Text.Encoding.GetEncoding(_codePage);
            return encoding.GetBytes(text);
        }

        /// <summary>
        /// Releases all resources used by this TrueType font.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this TrueType font and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _fontProgram?.Dispose();
                    _toUnicodeStream?.Dispose();
                }

                _disposed = true;
            }
        }
    }
}
