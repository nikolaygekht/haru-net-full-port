/* The font program of an OpenType font with CFF outlines embedded in a document (FontFile3) */
using System;
using System.Collections.Generic;
using Haru.Font.Cff;
using Haru.Objects;
using Haru.Streams;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// The embedded font program of a font made from an OpenType font file with CFF outlines: a
    /// /FontFile3 stream with the CFF program alone, never the OpenType file. A CID font gets a
    /// CID-keyed program (/Subtype /CIDFontType0C) whose CIDs are the glyph IDs of the font file,
    /// so the content of a document selects the glyphs by the same numbers as with TrueType
    /// outlines; a simple font gets a program with glyph names (/Subtype /Type1C). A subset holds
    /// the glyphs in use, with the subroutines they call put in place of the calls.
    /// </summary>
    internal sealed class CffFontProgram : IEmbeddedFontProgram
    {
        private readonly CffFont _cff;
        private readonly string _baseFont;
        private readonly EmbeddedFontKind _kind;
        private bool _subsettable;

        private bool _written;
        private bool _writtenSubset;
        private int _writtenGlyphCount;

        /// <summary>
        /// Creates the font program of a font; nothing is written yet.
        /// </summary>
        /// <param name="cff">The CFF outlines of the font.</param>
        /// <param name="fsType">The embedding permissions of the font (OS/2 fsType).</param>
        /// <param name="baseFont">The name of the font in the document.</param>
        /// <param name="kind">The kind of PDF font the program is embedded for.</param>
        /// <exception cref="HpdfException">The font does not permit embedding
        /// (<see cref="HpdfErrorCode.TtfCannotEmbeddingFont"/>), or a simple font is made of a
        /// CID-keyed font (<see cref="HpdfErrorCode.UnsupportedFontType"/>).</exception>
        public CffFontProgram(CffFont cff, ushort fsType, string baseFont, EmbeddedFontKind kind)
        {
            _cff = cff ?? throw new ArgumentNullException(nameof(cff));
            _baseFont = baseFont ?? throw new ArgumentNullException(nameof(baseFont));
            _kind = kind;

            if (kind == EmbeddedFontKind.Simple && cff.IsCidKeyed)
                throw CidKeyedSimpleFont(baseFont);

            TrueTypeEmbeddingRights.EnsureEmbeddable(fsType, baseFont);
            _subsettable = TrueTypeEmbeddingRights.AllowsSubsetting(fsType);

            FontName = baseFont;
            Stream = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
            Stream["Subtype"] = new HpdfName(kind == EmbeddedFontKind.Cid ? "CIDFontType0C" : "Type1C");
        }

        /// <inheritdoc />
        public HpdfStreamObject Stream { get; }

        /// <inheritdoc />
        public string DescriptorKey => "FontFile3";

        /// <inheritdoc />
        public string FontName { get; private set; }

        /// <summary>
        /// The error of a simple font made of a font with CID-keyed CFF outlines: its glyphs have no
        /// names a simple font could select them by.
        /// </summary>
        public static HpdfException CidKeyedSimpleFont(string fontName)
        {
            return new HpdfException(HpdfErrorCode.UnsupportedFontType,
                $"The font {fontName} has CID-keyed CFF outlines (a CJK OpenType font) and cannot be a simple font: load it as a CID font (HpdfCIDFont)");
        }

        /// <inheritdoc />
        public bool Update(bool embedSubset, IReadOnlyCollection<ushort> usedGlyphs)
        {
            ArgumentNullException.ThrowIfNull(usedGlyphs);

            bool subset = embedSubset && _subsettable;
            if (_written && subset == _writtenSubset && (!subset || _writtenGlyphCount == usedGlyphs.Count))
                return false;

            byte[]? program = null;
            string fontName = _baseFont;
            if (subset)
            {
                // Sorted, so that the same glyphs always give the same subset tag
                var sortedGlyphs = new List<ushort>(usedGlyphs);
                sortedGlyphs.Sort();

                var glyphs = new SortedSet<int> { 0 };
                foreach (ushort glyph in sortedGlyphs)
                {
                    if (glyph < _cff.GlyphCount)
                        glyphs.Add(glyph);
                }

                // Adds the glyphs accented glyphs are made of
                Dictionary<int, byte[]>? charStrings = CffCharStringFlattener.Flatten(_cff, glyphs);
                if (charStrings is null)
                {
                    // A charstring that cannot be followed: the font is embedded in full
                    _subsettable = false;
                    subset = false;
                }
                else
                {
                    program = Write(new List<int>(glyphs), charStrings);
                    fontName = $"{TrueTypeSubsetter.CreateSubsetTag(_baseFont, sortedGlyphs)}+{_baseFont}";
                }
            }

            if (program is null)
            {
                var glyphs = new List<int>(_cff.GlyphCount);
                for (int glyph = 0; glyph < _cff.GlyphCount; glyph++)
                    glyphs.Add(glyph);

                program = Write(glyphs, null);
            }

            Stream.ClearStream();
            Stream.WriteToStream(program);

            FontName = fontName;
            _written = true;
            _writtenSubset = subset;
            _writtenGlyphCount = usedGlyphs.Count;
            return true;
        }

        private byte[] Write(List<int> glyphs, IReadOnlyDictionary<int, byte[]>? charStrings)
        {
            return _kind == EmbeddedFontKind.Cid
                ? CffSubsetWriter.WriteCidKeyed(_cff, glyphs, charStrings)
                : CffSubsetWriter.WriteNameKeyed(_cff, glyphs, charStrings);
        }

        /// <summary>
        /// Releases the stream of the font program.
        /// </summary>
        public void Dispose() => Stream.Dispose();
    }
}
