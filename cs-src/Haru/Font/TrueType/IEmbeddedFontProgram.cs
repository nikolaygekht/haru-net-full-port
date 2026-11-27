/* The font program of an OpenType font (TrueType or CFF outlines) embedded in a document */
using System;
using System.Collections.Generic;
using Haru.Objects;

namespace Haru.Font.TrueType
{
    /// <summary>
    /// The kind of PDF font a font program is embedded for.
    /// </summary>
    internal enum EmbeddedFontKind
    {
        /// <summary>A simple font (one byte per character): TrueType, or Type1 for CFF outlines.</summary>
        Simple,

        /// <summary>A descendant of a Type 0 font: CIDFontType2, or CIDFontType0 for CFF outlines.</summary>
        Cid,
    }

    /// <summary>
    /// The embedded font program of a font made from an OpenType font file: the stream, the key of
    /// the font descriptor it goes to and the name the font has with it. The program is written
    /// when the document is saved, as a subset with the glyphs in use ("ABCDEF+Name") or as the
    /// whole font.
    /// </summary>
    internal interface IEmbeddedFontProgram : IDisposable
    {
        /// <summary>Gets the stream of the font program.</summary>
        HpdfStreamObject Stream { get; }

        /// <summary>Gets the key of the font descriptor for the stream: FontFile2 or FontFile3.</summary>
        string DescriptorKey { get; }

        /// <summary>Gets the name of the font as it is embedded: with a subset tag if the program
        /// written last is a subset.</summary>
        string FontName { get; }

        /// <summary>
        /// Brings the font program up to date.
        /// </summary>
        /// <param name="embedSubset">True to write a subset, false to write the whole font.</param>
        /// <param name="usedGlyphs">The glyphs a subset holds, in any order. Glyphs are only ever
        /// added to the glyphs in use of a font, never removed.</param>
        /// <returns>True if the program was written and <see cref="FontName"/> may have changed;
        /// false if what is written is still right.</returns>
        bool Update(bool embedSubset, IReadOnlyCollection<ushort> usedGlyphs);
    }

    /// <summary>
    /// Creates the font program of a font file for the outlines it has.
    /// </summary>
    internal static class EmbeddedFontProgram
    {
        /// <summary>
        /// Creates the font program of a font.
        /// </summary>
        /// <param name="file">The font file.</param>
        /// <param name="baseFont">The name of the font in the document.</param>
        /// <param name="kind">The kind of PDF font the program is embedded for.</param>
        /// <exception cref="HpdfException">The font does not permit embedding
        /// (<see cref="HpdfErrorCode.TtfCannotEmbeddingFont"/>) or has no outlines
        /// (<see cref="HpdfErrorCode.TtfMissingTable"/>).</exception>
        public static IEmbeddedFontProgram Create(TrueTypeFontFile file, string baseFont, EmbeddedFontKind kind)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (file.Cff != null)
                return new CffFontProgram(file.Cff, file.FsType, baseFont, kind);

            if (file.HasTrueTypeOutlines)
            {
                return new TrueTypeFontProgram(file, baseFont,
                    kind == EmbeddedFontKind.Cid ? TrueTypeSubsetter.CidFontTables : TrueTypeSubsetter.SimpleFontTables);
            }

            throw new HpdfException(HpdfErrorCode.TtfMissingTable,
                $"The font {baseFont} has neither TrueType nor CFF outlines and cannot be embedded");
        }
    }
}
