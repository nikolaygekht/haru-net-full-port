using System;
using System.Collections.Concurrent;
using System.IO;
using Haru.Font.TrueType;
using Haru.Font.Type1;

namespace Haru.Font
{
    /// <summary>
    /// Thread-safe global pool for sharing parsed font data across all HpdfDocument instances.
    /// This eliminates redundant font file parsing and reduces memory usage when multiple documents
    /// use the same fonts.
    ///
    /// Design principles:
    /// - Thread-safe: Uses ConcurrentDictionary for lock-free reads
    /// - Key by absolute path: Ensures same file = same cached data. The cached font file does not
    ///   depend on a code page; a code page only selects how a font created from the data encodes text
    /// - Explicit cleanup: Call Clear() to free memory (e.g., between test runs or after batch processing)
    /// - Immutable data: ParsedFontData instances are read-only after creation
    /// </summary>
    public static class FontDataPool
    {
        /// <summary>
        /// The code page of a TrueType font when none is given: Windows-1252 (WinAnsiEncoding), as for
        /// <see cref="HpdfTrueTypeFont.LoadFromFile(Haru.Xref.HpdfXref, string, string, bool)"/>.
        /// </summary>
        public const int DefaultCodePage = 1252;

        // Cache TrueType fonts by absolute file path
        private static readonly ConcurrentDictionary<string, ParsedTrueTypeData> _trueTypeFonts =
            new ConcurrentDictionary<string, ParsedTrueTypeData>(StringComparer.OrdinalIgnoreCase);

        // Cache Type1 fonts by AFM path (AFM+PFB are treated as a pair)
        private static readonly ConcurrentDictionary<string, ParsedType1Data> _type1Fonts =
            new ConcurrentDictionary<string, ParsedType1Data>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or loads TrueType font data from the pool.
        /// If the font is already cached, returns the cached data.
        /// Otherwise, loads and parses the font file, caches it, and returns the data.
        /// </summary>
        /// <param name="filePath">Path to the TrueType font file (.ttf or .otf).</param>
        /// <param name="codePage">Code page of the fonts created from the returned data (default: 1252,
        /// WinAnsiEncoding). The font file is cached once whatever code pages are asked for: each code
        /// page gets data that shares the cached bytes.</param>
        /// <returns>Parsed TrueType font data that can be shared across documents.</returns>
        /// <exception cref="ArgumentNullException">If filePath is null.</exception>
        /// <exception cref="FileNotFoundException">If the font file does not exist.</exception>
        /// <exception cref="InvalidOperationException">If the font file is invalid or cannot be parsed.</exception>
        public static ParsedTrueTypeData GetOrLoadTrueType(string filePath, int codePage = DefaultCodePage)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentNullException(nameof(filePath));

            // Normalize to absolute path for consistent cache key
            string absolutePath = Path.GetFullPath(filePath);

            if (!File.Exists(absolutePath))
                throw new FileNotFoundException($"TrueType font file not found: {absolutePath}", absolutePath);

            // GetOrAdd reads from the cache without locking; a font that is not cached yet is loaded
            var cachedData = _trueTypeFonts.GetOrAdd(absolutePath, LoadTrueTypeFont);

            return cachedData.WithCodePage(codePage);
        }

        /// <summary>
        /// Gets or loads Type1 font data from the pool.
        /// Type1 fonts consist of two files: AFM (metrics) and PFB (font program).
        /// </summary>
        /// <param name="afmPath">Path to the AFM (Adobe Font Metrics) file.</param>
        /// <param name="pfbPath">Path to the PFB (Printer Font Binary) file.</param>
        /// <returns>Parsed Type1 font data that can be shared across documents.</returns>
        /// <exception cref="ArgumentNullException">If afmPath or pfbPath is null.</exception>
        /// <exception cref="FileNotFoundException">If either file does not exist.</exception>
        /// <exception cref="InvalidOperationException">If the font files are invalid or cannot be parsed.</exception>
        public static ParsedType1Data GetOrLoadType1(string afmPath, string pfbPath)
        {
            if (string.IsNullOrWhiteSpace(afmPath))
                throw new ArgumentNullException(nameof(afmPath));
            if (string.IsNullOrWhiteSpace(pfbPath))
                throw new ArgumentNullException(nameof(pfbPath));

            // Normalize to absolute paths
            string absoluteAfmPath = Path.GetFullPath(afmPath);
            string absolutePfbPath = Path.GetFullPath(pfbPath);

            if (!File.Exists(absoluteAfmPath))
                throw new FileNotFoundException($"AFM file not found: {absoluteAfmPath}", absoluteAfmPath);
            if (!File.Exists(absolutePfbPath))
                throw new FileNotFoundException($"PFB file not found: {absolutePfbPath}", absolutePfbPath);

            // Cache key is the AFM path (AFM+PFB are treated as a pair)
            if (_type1Fonts.TryGetValue(absoluteAfmPath, out var cachedData))
            {
                return cachedData;
            }

            // Not in cache - load and parse the font
            var parsedData = _type1Fonts.GetOrAdd(absoluteAfmPath, path => LoadType1Font(absoluteAfmPath, absolutePfbPath));

            return parsedData;
        }

        /// <summary>
        /// Clears all cached font data. Call this to free memory when fonts are no longer needed
        /// (e.g., after batch PDF generation, between test runs, or on application shutdown).
        /// </summary>
        public static void Clear()
        {
            _trueTypeFonts.Clear();
            _type1Fonts.Clear();
        }

        /// <summary>
        /// Gets the current cache statistics for monitoring and diagnostics.
        /// </summary>
        public static FontDataPoolStats GetStats()
        {
            return new FontDataPoolStats
            {
                TrueTypeFontsCount = _trueTypeFonts.Count,
                Type1FontsCount = _type1Fonts.Count
            };
        }

        // Private helper: Loads and parses a TrueType font file
        private static ParsedTrueTypeData LoadTrueTypeFont(string absolutePath)
        {
            try
            {
                // Read entire font file into memory (this is the only disk I/O)
                byte[] fontData = File.ReadAllBytes(absolutePath);

                // The name fonts created from this data get; a synthetic one if the font file has none
                string extractedFontName = TrueTypeFontNames.Read(fontData).PostScriptName
                    ?? $"CustomTTFont-{Path.GetFileNameWithoutExtension(absolutePath)}";

                return new ParsedTrueTypeData(
                    filePath: absolutePath,
                    fontData: fontData,
                    extractedFontName: extractedFontName,
                    codePage: DefaultCodePage
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load TrueType font from {absolutePath}: {ex.Message}", ex);
            }
        }

        // Private helper: Loads and parses a Type1 font
        private static ParsedType1Data LoadType1Font(string absoluteAfmPath, string absolutePfbPath)
        {
            try
            {
                // Parse AFM file
                AfmData afmData = AfmParser.ParseFile(absoluteAfmPath);

                // Read PFB data
                byte[] pfbData = File.ReadAllBytes(absolutePfbPath);

                // Extract font name from AFM
                string extractedFontName = afmData.FontName ?? Path.GetFileNameWithoutExtension(absoluteAfmPath);

                return new ParsedType1Data(
                    afmPath: absoluteAfmPath,
                    pfbPath: absolutePfbPath,
                    afmData: afmData,
                    pfbData: pfbData,
                    extractedFontName: extractedFontName
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load Type1 font from {absoluteAfmPath}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Statistics about the font data pool for monitoring and diagnostics.
    /// </summary>
    public class FontDataPoolStats
    {
        /// <summary>
        /// Number of TrueType fonts currently cached.
        /// </summary>
        public int TrueTypeFontsCount { get; init; }

        /// <summary>
        /// Number of Type1 fonts currently cached.
        /// </summary>
        public int Type1FontsCount { get; init; }

        /// <summary>
        /// Total number of fonts cached.
        /// </summary>
        public int TotalFontsCount => TrueTypeFontsCount + Type1FontsCount;
    }
}
