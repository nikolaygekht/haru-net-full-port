/* Embedding permissions of a TrueType font (fsType of the OS/2 table) */
namespace Haru.Font.TrueType
{
    /// <summary>
    /// Interprets the embedding permissions a font vendor sets in <c>fsType</c> of the OS/2 table.
    /// </summary>
    internal static class TrueTypeEmbeddingRights
    {
        // Bits 1-3: the embedding level; they exclude each other
        private const int EmbeddingLevelMask = 0x000E;
        private const int RestrictedLicense = 0x0002;

        private const int NoSubsetting = 0x0100;
        private const int BitmapEmbeddingOnly = 0x0200;

        /// <summary>
        /// Throws if the font must not be embedded in a document: its license restricts embedding,
        /// or only its bitmaps may be embedded (a PDF embeds the outlines).
        /// </summary>
        /// <param name="fsType">fsType of the OS/2 table; 0 if the font has no OS/2 table.</param>
        /// <param name="fontName">The font name, for the error message.</param>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.TtfCannotEmbeddingFont"/></exception>
        public static void EnsureEmbeddable(ushort fsType, string fontName)
        {
            if ((fsType & EmbeddingLevelMask) == RestrictedLicense)
            {
                throw new HpdfException(HpdfErrorCode.TtfCannotEmbeddingFont,
                    $"The license of the font '{fontName}' does not permit embedding (fsType 0x{fsType:X4})");
            }

            if ((fsType & BitmapEmbeddingOnly) != 0)
            {
                throw new HpdfException(HpdfErrorCode.TtfCannotEmbeddingFont,
                    $"The font '{fontName}' permits embedding of its bitmaps only (fsType 0x{fsType:X4})");
            }
        }

        /// <summary>
        /// Gets whether the font may be embedded as a subset. A font that forbids subsetting is
        /// embedded in full.
        /// </summary>
        /// <param name="fsType">fsType of the OS/2 table; 0 if the font has no OS/2 table.</param>
        public static bool AllowsSubsetting(ushort fsType) => (fsType & NoSubsetting) == 0;
    }
}
