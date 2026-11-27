using System;
using Haru.Font.TrueType;
using Haru.Font.Type1;

namespace Haru.Font
{
    /// <summary>
    /// Base class for parsed font data that can be shared across multiple document instances.
    /// This enables font pooling - loading font files once and reusing the parsed data.
    /// </summary>
    public abstract class ParsedFontData
    {
        /// <summary>
        /// Absolute file path where this font was loaded from (for cache key).
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// When this font data was loaded (for cache management).
        /// </summary>
        public DateTime LoadedAt { get; }

        protected ParsedFontData(string filePath)
            : this(filePath, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Creates font data that was loaded earlier (another view of cached data).
        /// </summary>
        protected ParsedFontData(string filePath, DateTime loadedAt)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            LoadedAt = loadedAt;
        }
    }

    /// <summary>
    /// Holds parsed TrueType font data that can be shared across multiple HpdfDocument instances.
    /// The font file is loaded into memory and parsed once, and cached for reuse.
    /// </summary>
    public sealed class ParsedTrueTypeData : ParsedFontData
    {
        // The parsed font file, shared by the data of every code page
        private readonly Lazy<TrueTypeFontFile> _fontFile;

        /// <summary>
        /// Raw font file data (needed for embedding in PDF and for parsing).
        /// This is the complete TTF/OTF file content loaded from disk; for a font of a font
        /// collection, the whole TTC file, which all the fonts of the collection share.
        /// </summary>
        public byte[] FontData { get; }

        /// <summary>
        /// Extracted font name (PostScript name or Family-Subfamily).
        /// Cached here to avoid re-extracting on every font instance creation.
        /// </summary>
        public string ExtractedFontName { get; }

        /// <summary>
        /// Gets the code page of the fonts that <see cref="HpdfTrueTypeFont.LoadFromSharedData(Haru.Xref.HpdfXref, string, ParsedTrueTypeData, bool)"/>
        /// creates from this data. The font file itself does not depend on a code page: the pool
        /// caches it once, and every code page asked for gets its own object over the same bytes.
        /// </summary>
        public int CodePage { get; }

        /// <summary>
        /// Gets the index of the font in its font collection (.ttc); null for a font file.
        /// </summary>
        public int? FontIndex { get; }

        public ParsedTrueTypeData(
            string filePath,
            byte[] fontData,
            string extractedFontName,
            int codePage)
            : base(filePath)
        {
            FontData = fontData ?? throw new ArgumentNullException(nameof(fontData));
            ExtractedFontName = extractedFontName ?? throw new ArgumentNullException(nameof(extractedFontName));
            CodePage = codePage;
            _fontFile = new Lazy<TrueTypeFontFile>(() => TrueTypeFontFile.Parse(FontData));
        }

        /// <summary>
        /// Creates the data of a font file that is already parsed.
        /// </summary>
        internal ParsedTrueTypeData(string filePath, TrueTypeFontFile fontFile, string extractedFontName, int codePage)
            : base(filePath)
        {
            FontData = fontFile.Data;
            FontIndex = fontFile.FontIndex;
            ExtractedFontName = extractedFontName;
            CodePage = codePage;
            _fontFile = new Lazy<TrueTypeFontFile>(() => fontFile);
        }

        private ParsedTrueTypeData(ParsedTrueTypeData source, int codePage)
            : base(source.FilePath, source.LoadedAt)
        {
            _fontFile = source._fontFile;
            FontData = source.FontData;
            FontIndex = source.FontIndex;
            ExtractedFontName = source.ExtractedFontName;
            CodePage = codePage;
        }

        /// <summary>
        /// Gets the parsed font file. It is parsed once and shared by every font created from this
        /// data, whatever its code page is.
        /// </summary>
        internal TrueTypeFontFile FontFile => _fontFile.Value;

        /// <summary>
        /// Gets the same font data (the bytes and the parsed font file are shared, not copied) for
        /// another code page.
        /// </summary>
        internal ParsedTrueTypeData WithCodePage(int codePage)
        {
            return codePage == CodePage ? this : new ParsedTrueTypeData(this, codePage);
        }
    }

    /// <summary>
    /// Holds parsed Type1 font data that can be shared across multiple HpdfDocument instances.
    /// Type1 fonts consist of two files: AFM (Adobe Font Metrics) and PFB (Printer Font Binary).
    /// </summary>
    public sealed class ParsedType1Data : ParsedFontData
    {
        /// <summary>
        /// Path to the AFM (Adobe Font Metrics) file.
        /// </summary>
        public string AfmPath { get; }

        /// <summary>
        /// Path to the PFB (Printer Font Binary) file.
        /// </summary>
        public string PfbPath { get; }

        /// <summary>
        /// Parsed AFM data (font metrics).
        /// </summary>
        public AfmData AfmData { get; }

        /// <summary>
        /// Raw PFB data (font program).
        /// </summary>
        public byte[] PfbData { get; }

        /// <summary>
        /// Font name extracted from AFM (usually from FontName field).
        /// </summary>
        public string ExtractedFontName { get; }

        public ParsedType1Data(
            string afmPath,
            string pfbPath,
            AfmData afmData,
            byte[] pfbData,
            string extractedFontName)
            : base(afmPath) // Use AFM path as primary key
        {
            AfmPath = afmPath ?? throw new ArgumentNullException(nameof(afmPath));
            PfbPath = pfbPath ?? throw new ArgumentNullException(nameof(pfbPath));
            AfmData = afmData ?? throw new ArgumentNullException(nameof(afmData));
            PfbData = pfbData ?? throw new ArgumentNullException(nameof(pfbData));
            ExtractedFontName = extractedFontName ?? throw new ArgumentNullException(nameof(extractedFontName));
        }
    }
}
