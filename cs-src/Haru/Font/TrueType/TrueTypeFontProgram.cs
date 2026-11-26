/* The font program of a TrueType font file embedded in a document (FontFile2) */
using System;
using System.Collections.Generic;
using Haru.Objects;
using Haru.Streams;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// The embedded font program of a font made from a TrueType font file: the /FontFile2 stream
    /// and the name the font has with it. The program is written when the document is saved, as a
    /// subset with the outlines of the glyphs in use ("ABCDEF+Name") or as the whole font file.
    /// Glyph IDs are the same in both, so nothing else in the document depends on the choice.
    /// A subset is always made from the original font file.
    /// </summary>
    internal sealed class TrueTypeFontProgram : IDisposable
    {
        private readonly TrueTypeFontFile _file;
        private readonly string _baseFont;
        private readonly IReadOnlyCollection<string> _tables;

        private bool _subsettable;
        private bool _written;
        private bool _writtenSubset;
        private int _writtenGlyphCount;

        /// <summary>
        /// Creates the font program of a font; nothing is written yet.
        /// </summary>
        /// <param name="file">The font file.</param>
        /// <param name="baseFont">The name of the font in the document.</param>
        /// <param name="tables">The tables a subset keeps (<see cref="TrueTypeSubsetter.SimpleFontTables"/>
        /// or <see cref="TrueTypeSubsetter.CidFontTables"/>).</param>
        /// <exception cref="HpdfException">The font does not permit embedding
        /// (<see cref="HpdfErrorCode.TtfCannotEmbeddingFont"/>).</exception>
        public TrueTypeFontProgram(TrueTypeFontFile file, string baseFont, IReadOnlyCollection<string> tables)
        {
            _file = file ?? throw new ArgumentNullException(nameof(file));
            _baseFont = baseFont ?? throw new ArgumentNullException(nameof(baseFont));
            _tables = tables ?? throw new ArgumentNullException(nameof(tables));

            TrueTypeEmbeddingRights.EnsureEmbeddable(file.FsType, baseFont);
            _subsettable = TrueTypeEmbeddingRights.AllowsSubsetting(file.FsType);

            FontName = baseFont;
            Stream = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
        }

        /// <summary>
        /// Gets the /FontFile2 stream.
        /// </summary>
        public HpdfStreamObject Stream { get; }

        /// <summary>
        /// Gets the name of the font as it is embedded: with a subset tag if the program written
        /// last is a subset.
        /// </summary>
        public string FontName { get; private set; }

        /// <summary>
        /// Brings the font program up to date. A font that forbids subsetting or has no TrueType
        /// outlines is written in full whatever is asked for.
        /// </summary>
        /// <param name="embedSubset">True to write a subset, false to write the whole font file.</param>
        /// <param name="usedGlyphs">The glyphs a subset holds, in any order. Glyphs are only ever
        /// added to the glyphs in use of a font, never removed.</param>
        /// <returns>True if the program was written and <see cref="FontName"/> may have changed;
        /// false if what is written is still right.</returns>
        public bool Update(bool embedSubset, IReadOnlyCollection<ushort> usedGlyphs)
        {
            ArgumentNullException.ThrowIfNull(usedGlyphs);

            bool subset = embedSubset && _subsettable;
            if (_written && subset == _writtenSubset && (!subset || _writtenGlyphCount == usedGlyphs.Count))
                return false;

            byte[]? fontProgram = null;
            string fontName = _baseFont;
            if (subset)
            {
                // Sorted, so that the same glyphs always give the same subset tag
                var glyphs = new List<ushort>(usedGlyphs);
                glyphs.Sort();

                fontProgram = TrueTypeSubsetter.CreateSubset(_file.Data, glyphs, _tables);
                if (fontProgram is null)
                {
                    // No TrueType outlines to select from: the font is embedded as it is
                    _subsettable = false;
                    subset = false;
                }
                else
                {
                    fontName = $"{TrueTypeSubsetter.CreateSubsetTag(_baseFont, glyphs)}+{_baseFont}";
                }
            }
            fontProgram ??= _file.Data;

            Stream.ClearStream();
            Stream.WriteToStream(fontProgram);
            Stream["Length1"] = new HpdfNumber(fontProgram.Length);

            FontName = fontName;
            _written = true;
            _writtenSubset = subset;
            _writtenGlyphCount = usedGlyphs.Count;
            return true;
        }

        /// <summary>
        /// Releases the stream of the font program.
        /// </summary>
        public void Dispose() => Stream.Dispose();
    }
}
