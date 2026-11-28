using Haru.Objects;
using Haru.Types;

namespace Haru.Encoding
{
    /// <summary>
    /// Interface for PDF text encoders.
    /// Maps between Unicode text and byte sequences for PDF fonts.
    /// </summary>
    public interface IHpdfEncoder
    {
        /// <summary>
        /// Gets the encoder name (e.g., "WinAnsiEncoding", "CP1251", "ISO8859-5").
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the base encoding name for the /BaseEncoding entry.
        /// </summary>
        string BaseEncodingName { get; }

        /// <summary>
        /// Gets whether this encoder requires a /Differences array.
        /// </summary>
        bool HasDifferences { get; }

        /// <summary>
        /// Gets the Unicode codepoint for a given byte value.
        /// </summary>
        /// <param name="byteValue">The byte value (0-255).</param>
        /// <returns>The Unicode codepoint, or 0 if unmapped.</returns>
        ushort GetUnicode(byte byteValue);

        /// <summary>
        /// Gets the byte value for a given Unicode codepoint.
        /// Returns null if the character cannot be encoded.
        /// </summary>
        byte? GetByteValue(char unicodeChar);

        /// <summary>
        /// Creates the /Differences array for this encoding.
        /// Returns null if no differences from base encoding.
        /// </summary>
        HpdfArray? CreateDifferencesArray();

        /// <summary>
        /// Encodes Unicode text to bytes using this encoder.
        /// </summary>
        byte[] EncodeText(string text);

        /// <summary>
        /// Gets the kind of the encoder (HPDF_Encoder_GetType); single-byte for the encoders of simple fonts.
        /// </summary>
        HpdfEncoderType Type => HpdfEncoderType.SingleByte;

        /// <summary>
        /// Gets the writing mode of the encoder (HPDF_Encoder_GetWritingMode); horizontal for simple fonts.
        /// </summary>
        HpdfWritingMode WritingMode => HpdfWritingMode.Horizontal;

        /// <summary>
        /// Gets the kind of a byte of encoded text (HPDF_Encoder_GetByteType): every byte of a single-byte encoding is
        /// a single byte.
        /// </summary>
        /// <param name="text">The encoded text.</param>
        /// <param name="index">The position of the byte.</param>
        /// <returns><see cref="HpdfByteType.Single"/>, or <see cref="HpdfByteType.Unknown"/> for a position outside the text.</returns>
        HpdfByteType GetByteType(System.ReadOnlySpan<byte> text, int index) =>
            index >= 0 && index < text.Length ? HpdfByteType.Single : HpdfByteType.Unknown;
    }
}
