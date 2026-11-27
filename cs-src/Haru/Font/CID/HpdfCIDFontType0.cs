using System;
using System.Collections.Generic;
using Haru.Objects;
using Haru.Xref;
using Haru.Types;
using Haru.Streams;
using Haru.Doc;

namespace Haru.Font.CID
{
    /// <summary>
    /// Represents a CIDFontType0 (predefined CJK font with built-in metrics).
    /// Does NOT embed font data - references system fonts by name.
    /// Requires the font to be installed on the viewer's system.
    /// </summary>
    public class HpdfCIDFontType0 : IHpdfFontImplementation, IDisposable
    {
        private readonly HpdfDict _dict;               // Type 0 (composite) font dictionary
        private readonly HpdfDict _cidFontDict;        // CIDFontType0 descendant font dictionary
        private readonly HpdfDict _descriptor;         // Font descriptor
        private readonly string _baseFont;
        private readonly string _localName;
        private readonly HpdfXref _xref;
        private readonly string _encodingName;
        private readonly CIDEncoder _encoder;          // Encodes text, maps character codes to CIDs

        // Font definition (contains metrics and widths)
        private readonly PredefinedFontDefinition _fontDef;
        private bool _disposed = false;

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
        /// Gets the code page used for text encoding: the code page of the font's encoding (CMap).
        /// </summary>
        public int? CodePage => _encoder.CodePage;

        /// <summary>
        /// Gets the writing mode of the font: the one of its encoding (CMap).
        /// </summary>
        public HpdfWritingMode WritingMode => _encoder.WritingMode;

        /// <inheritdoc />
        string? IHpdfFontImplementation.CMapName => _encodingName;

        /// <summary>
        /// Gets the font ascent in 1000-unit glyph space.
        /// </summary>
        public int Ascent => _fontDef.Metrics.Ascent;

        /// <summary>
        /// Gets the font descent in 1000-unit glyph space.
        /// </summary>
        public int Descent => _fontDef.Metrics.Descent;

        /// <summary>
        /// Gets the x-height in 1000-unit glyph space.
        /// </summary>
        public int XHeight => _fontDef.Metrics.CapHeight; // Use cap height as approximation

        /// <summary>
        /// Gets the font bounding box in 1000-unit glyph space.
        /// </summary>
        public HpdfBox FontBBox => _fontDef.Metrics.FontBBox;

        /// <summary>
        /// Gets an HpdfFont wrapper for this CID font that can be used with page operations.
        /// </summary>
        public HpdfFont AsFont()
        {
            return new HpdfFont(this);
        }

        private HpdfCIDFontType0(
            HpdfXref xref,
            string localName,
            PredefinedFontDefinition fontDef,
            string encodingName,
            CIDEncoder? encoder = null)
        {
            _xref = xref ?? throw new ArgumentNullException(nameof(xref));
            _localName = localName ?? throw new ArgumentNullException(nameof(localName));
            _fontDef = fontDef ?? throw new ArgumentNullException(nameof(fontDef));
            _baseFont = fontDef.FontName;
            ArgumentNullException.ThrowIfNull(encodingName);

            // The widths of the font are given by CID: the encoder of the CMap is always needed.
            // A CMap found by its name is written as the CMap spells it, whatever the letter case
            // of the name given is; with an encoder of the caller the name is written as it is
            _encoder = encoder ?? CIDEncoder.Create(encodingName);
            _encodingName = encoder is null ? _encoder.Name : encodingName;

            _dict = new HpdfDict();
            _cidFontDict = new HpdfDict();
            _descriptor = new HpdfDict();
        }

        /// <summary>
        /// Creates a CIDFontType0 font from a predefined font definition.
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="fontName">The font name (e.g., "SimSun", "MingLiU").</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="codePage">Not used: the text is encoded with the code page of the encoding
        /// (<see cref="CIDEncoder.CodePage"/>), which is also the <see cref="CodePage"/> of the font.
        /// Use the overload without this parameter.</param>
        /// <param name="encodingName">The encoding name (e.g., "GBK-EUC-H", "ETen-B5-H").</param>
        /// <param name="encoder">Optional CIDEncoder instance. If not provided, the encoder of the CMap named by
        /// <paramref name="encodingName"/> is used (see <see cref="CIDEncoder.Create"/>).</param>
        /// <returns>A new CIDFontType0 instance.</returns>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> if no encoder is
        /// given and the library has none for the encoding name.</exception>
        public static HpdfCIDFontType0 Create(
            HpdfDocument document,
            string fontName,
            string localName,
            int codePage,
            string encodingName,
            CIDEncoder? encoder = null)
        {
            return Create(document, fontName, localName, encodingName, encoder);
        }

        /// <summary>
        /// Creates a CIDFontType0 font from a predefined font definition.
        /// </summary>
        /// <param name="document">The PDF document.</param>
        /// <param name="fontName">The font name (e.g., "SimSun", "MingLiU").</param>
        /// <param name="localName">Local resource name (e.g., "F1").</param>
        /// <param name="encodingName">The encoding name (e.g., "GBK-EUC-H", "ETen-B5-H").</param>
        /// <param name="encoder">Optional CIDEncoder instance. If not provided, the encoder of the CMap named by
        /// <paramref name="encodingName"/> is used (see <see cref="CIDEncoder.Create"/>).</param>
        /// <returns>A new CIDFontType0 instance.</returns>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> if no encoder is
        /// given and the library has none for the encoding name.</exception>
        public static HpdfCIDFontType0 Create(
            HpdfDocument document,
            string fontName,
            string localName,
            string encodingName,
            CIDEncoder? encoder = null)
        {
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            // Load font definition (from registry or embedded resource)
            var fontDef = PredefinedFontRegistry.GetDefinition(fontName);

            // CIDFontType0 requires PDF 1.4+
            document.Xref.RequireVersion(HpdfVersion.Version14);

            var font = new HpdfCIDFontType0(
                document.Xref,
                localName,
                fontDef,
                encodingName,
                encoder);

            // Build PDF objects
            font.CreateFontDescriptor();
            font.CreateCIDFontDictionary();
            font.CreateType0FontDictionary();

            return font;
        }

        /// <summary>
        /// Creates a SimSun CIDFontType0 font (Simplified Chinese, GBK-EUC-H).
        /// </summary>
        public static HpdfCIDFontType0 CreateSimSun(HpdfDocument document, string localName)
        {
            return Create(document, "SimSun", localName, "GBK-EUC-H");
        }

        /// <summary>
        /// Step 1: Create FontDescriptor (but no FontFile2 - predefined font).
        /// </summary>
        private void CreateFontDescriptor()
        {
            _descriptor.Add("Type", new HpdfName("FontDescriptor"));
            _descriptor.Add("FontName", new HpdfName(_baseFont));
            _descriptor.Add("Flags", new HpdfNumber(_fontDef.Metrics.Flags));

            // Add font bounding box
            var bbox = new HpdfArray();
            bbox.Add(new HpdfNumber((int)_fontDef.Metrics.FontBBox.Left));
            bbox.Add(new HpdfNumber((int)_fontDef.Metrics.FontBBox.Bottom));
            bbox.Add(new HpdfNumber((int)_fontDef.Metrics.FontBBox.Right));
            bbox.Add(new HpdfNumber((int)_fontDef.Metrics.FontBBox.Top));
            _descriptor.Add("FontBBox", bbox);

            _descriptor.Add("ItalicAngle", new HpdfNumber(_fontDef.Metrics.ItalicAngle));
            _descriptor.Add("Ascent", new HpdfNumber(_fontDef.Metrics.Ascent));
            _descriptor.Add("Descent", new HpdfNumber(_fontDef.Metrics.Descent));
            _descriptor.Add("CapHeight", new HpdfNumber(_fontDef.Metrics.CapHeight));
            _descriptor.Add("StemV", new HpdfNumber(_fontDef.Metrics.StemV));

            // NOTE: No FontFile2 entry - this is a predefined font

            // Add to xref
            _xref.Add(_descriptor);
        }

        /// <summary>
        /// Step 2: Create CIDFontType0 dictionary (descendant font).
        /// </summary>
        private void CreateCIDFontDictionary()
        {
            _cidFontDict.Add("Type", new HpdfName("Font"));
            _cidFontDict.Add("Subtype", new HpdfName("CIDFontType0"));
            _cidFontDict.Add("BaseFont", new HpdfName(_baseFont));

            // Add CIDSystemInfo
            _cidFontDict.Add("CIDSystemInfo", _fontDef.SystemInfo.ToDict());

            // Link to FontDescriptor
            _cidFontDict.Add("FontDescriptor", _descriptor);

            // Add default width
            _cidFontDict.Add("DW", new HpdfNumber(_fontDef.Metrics.DefaultWidth));

            // The metrics of vertical writing, the same for every predefined font
            if (WritingMode == HpdfWritingMode.Vertical)
                _cidFontDict.Add("DW2", CIDVerticalMetrics.CreateDW2(CIDVerticalMetrics.DefaultOriginY));

            // Add widths array (W)
            if (_fontDef.Widths != null && _fontDef.Widths.Length > 0)
            {
                CreateCIDWidthsArray();
            }

            // Add to xref
            _xref.Add(_cidFontDict);
        }

        /// <summary>
        /// Creates the W (widths) array for CIDFontType0.
        /// Format: [c1 c2 w] - CIDs from c1 to c2 have width w
        /// Or: [c [w1 w2 ...]] - CID c and following have widths w1, w2, ...
        /// </summary>
        private void CreateCIDWidthsArray()
        {
            var widths = new HpdfArray();

            // _fontDef.Widths is already sorted by CID (from PredefinedFontDefinition.Load)
            // Group consecutive CIDs with same width into ranges
            int i = 0;
            while (i < _fontDef.Widths.Length)
            {
                ushort startCID = _fontDef.Widths[i].CID;
                short width = _fontDef.Widths[i].Width;
                ushort endCID = startCID;

                // Find consecutive CIDs with same width
                while (i + 1 < _fontDef.Widths.Length &&
                       _fontDef.Widths[i + 1].CID == endCID + 1 &&
                       _fontDef.Widths[i + 1].Width == width)
                {
                    i++;
                    endCID = _fontDef.Widths[i].CID;
                }

                // Add range: [startCID endCID width]
                widths.Add(new HpdfNumber(startCID));
                widths.Add(new HpdfNumber(endCID));
                widths.Add(new HpdfNumber(width));

                i++;
            }

            _cidFontDict.Add("W", widths);
        }

        /// <summary>
        /// Step 3: Create Type 0 (composite) font dictionary.
        /// The font has no /ToUnicode: the encoding is a predefined CMap of an Adobe character
        /// collection, for which a reader has the mapping to Unicode itself.
        /// </summary>
        private void CreateType0FontDictionary()
        {
            _dict.Add("Type", new HpdfName("Font"));
            _dict.Add("Subtype", new HpdfName("Type0"));
            _dict.Add("BaseFont", new HpdfName(_baseFont));

            // Encoding: e.g., "GBK-EUC-H" for horizontal writing
            _dict.Add("Encoding", new HpdfName(_encodingName));

            // DescendantFonts array (contains the CIDFont)
            var descendantFonts = new HpdfArray();
            descendantFonts.Add(_cidFontDict);
            _dict.Add("DescendantFonts", descendantFonts);

            // Add to xref
            _xref.Add(_dict);
        }

        /// <summary>
        /// Gets the width of a CID in 1000-unit glyph space.
        /// </summary>
        public short GetCIDWidth(ushort cid)
        {
            return _fontDef.GetCIDWidth(cid);
        }

        /// <summary>
        /// Gets the width of a single-byte character code in 1000-unit glyph space: the code is
        /// mapped to its CID by the CMap, and the width is the width of that CID. With a vertical
        /// encoding it is the height the character takes in a column, 1000.
        /// </summary>
        public float GetCharWidth(byte charCode)
        {
            return GetAdvance(_encoder.GetCID(charCode));
        }

        /// <summary>
        /// Gets the distance the text position moves by a glyph, in 1000-unit glyph space: the width
        /// of the glyph in horizontal writing, the same height for every glyph in vertical writing.
        /// </summary>
        private int GetAdvance(ushort cid)
        {
            return WritingMode == HpdfWritingMode.Vertical ? CIDVerticalMetrics.Advance : GetCIDWidth(cid);
        }

        /// <summary>
        /// Measures text width in user space units. The text is encoded as it is written to the
        /// content stream; every character code (one or two bytes) is mapped to its CID by the CMap,
        /// as a viewer does, and the widths of the CIDs are summed. With a vertical encoding the
        /// result is the height of the text: every character moves the text position down by the
        /// font size.
        /// </summary>
        public float MeasureText(string text, float fontSize)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            ReadOnlySpan<byte> encoded = ConvertTextToBytes(text);

            int totalWidth = 0;
            int position = 0;
            while (position < encoded.Length)
            {
                totalWidth += GetAdvance(_encoder.ReadCID(encoded, ref position));
            }

            return totalWidth * fontSize / 1000f;
        }

        /// <summary>
        /// Converts text to the character codes of the font's CMap for use in content stream
        /// (for GBK-EUC-H, the text encoded as GBK bytes).
        /// </summary>
        public byte[] ConvertTextToBytes(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            return _encoder.EncodeText(text);
        }

        /// <summary>
        /// Converts text to glyph IDs for use in PDF content stream.
        /// For CIDFontType0 with predefined encodings like GBK-EUC-H,
        /// this returns the GBK-encoded bytes (NOT Unicode glyph IDs).
        /// </summary>
        public byte[] ConvertTextToGlyphIDs(string text)
        {
            // For CIDFontType0, we use the encoding's byte representation
            // This is different from CIDFontType2 which uses Unicode→GlyphID mapping
            return ConvertTextToBytes(text);
        }

        /// <summary>
        /// Encodes text to bytes using the font's encoding.
        /// </summary>
        public byte[] EncodeText(string text)
        {
            // For CIDFontType0, encoding means converting to the encoding's byte representation
            return ConvertTextToBytes(text);
        }

        /// <summary>
        /// Releases all resources used by this CIDFontType0 font.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this CIDFontType0 font and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // The font has no streams of its own; the method is kept for compatibility

                _disposed = true;
            }
        }
    }
}
