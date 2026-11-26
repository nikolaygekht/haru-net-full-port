using System;
using System.Text;

namespace Haru.Font.CID
{
    /// <summary>
    /// Base class for CID font encoders: a predefined Adobe CMap (e.g. "GBK-EUC-H") that a
    /// CIDFontType0 names as its encoding.
    /// The text is encoded to bytes with the code page of the CMap; the PDF viewer's built-in CMap
    /// maps these bytes to CIDs and glyphs. The encoder knows the same mapping from character code
    /// to CID, because the widths of a CID font are given by CID.
    /// </summary>
    public abstract class CIDEncoder
    {
        /// <summary>
        /// Gets the encoder name (e.g., "GBK-EUC-H", "ETen-B5-H").
        /// This is the predefined Adobe CMap name.
        /// </summary>
        public string Name { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the CID system info registry (typically "Adobe").
        /// </summary>
        public string Registry { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the CID system info ordering (e.g., "GB1", "CNS1", "Japan1", "Korea1").
        /// </summary>
        public string Ordering { get; protected set; } = string.Empty;

        /// <summary>
        /// Gets the CID system info supplement number.
        /// </summary>
        public int Supplement { get; protected set; }

        /// <summary>
        /// Gets the Windows code page number (e.g., 936 for GBK, 950 for Big5).
        /// </summary>
        public int CodePage { get; protected set; }

        /// <summary>
        /// Gets the .NET encoding for this code page.
        /// </summary>
        protected System.Text.Encoding Encoding { get; private set; } = System.Text.Encoding.UTF8;

        /// <summary>
        /// Gets or sets the code to CID mapping of the CMap: ranges sorted by code that do not
        /// overlap, each as three numbers - the first code, the last code and the CID of the first
        /// code. A code that is in no range has CID 0.
        /// </summary>
        protected ushort[] CidRanges { get; set; } = Array.Empty<ushort>();

        protected CIDEncoder()
        {
            // Ensure code page provider is registered
            try
            {
                System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            }
            catch
            {
                // Already registered
            }
        }

        /// <summary>
        /// Creates the encoder of a predefined CMap by its name.
        /// </summary>
        /// <param name="encodingName">The CMap name: "GBK-EUC-H", "ETen-B5-H", "90ms-RKSJ-H", "EUC-H"
        /// or "KSCms-UHC-H".</param>
        /// <exception cref="HpdfException"><see cref="HpdfErrorCode.InvalidEncodingName"/> if the
        /// library has no encoder for the name.</exception>
        public static CIDEncoder Create(string encodingName)
        {
            return encodingName switch
            {
                "GBK-EUC-H" => new GBKEucHEncoder(),
                "ETen-B5-H" => new ETenB5HEncoder(),
                "90ms-RKSJ-H" => new Ms90RKSJHEncoder(),
                "EUC-H" => new EucHEncoder(),
                "KSCms-UHC-H" => new KSCmsUHCHEncoder(),
                _ => throw new HpdfException(HpdfErrorCode.InvalidEncodingName,
                    $"No encoder for the CMap '{encodingName}'; pass a CIDEncoder for it")
            };
        }

        /// <summary>
        /// Initializes the encoding after CodePage is set.
        /// Call this from derived class constructors after setting CodePage.
        /// </summary>
        protected void InitializeEncoding()
        {
            if (CodePage <= 0)
                throw new InvalidOperationException("CodePage must be set before initializing encoding");

            Encoding = System.Text.Encoding.GetEncoding(CodePage);
        }

        /// <summary>
        /// Encodes Unicode text to bytes using the code page.
        /// For CIDFontType0, these bytes are interpreted by the PDF viewer's predefined CMap.
        /// </summary>
        /// <param name="text">The Unicode text to encode.</param>
        /// <returns>Encoded bytes for use in PDF content stream.</returns>
        public virtual byte[] EncodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<byte>();

            return Encoding.GetBytes(text);
        }

        /// <summary>
        /// Gets whether a byte is the first byte of a two-byte character code.
        /// The base implementation knows single-byte codes only.
        /// </summary>
        protected virtual bool IsLeadByte(byte value) => false;

        /// <summary>
        /// Gets whether a byte can be the second byte of a two-byte character code.
        /// </summary>
        protected virtual bool IsTrailByte(byte value) => false;

        /// <summary>
        /// Gets the CID of a character code (one byte, or two bytes with the first byte in the high
        /// order), as the CMap maps it.
        /// </summary>
        /// <param name="code">The character code.</param>
        /// <returns>The CID; 0 if the CMap has no CID for the code.</returns>
        public ushort GetCID(ushort code)
        {
            ushort[] ranges = CidRanges;

            // Find the last range that starts at or before the code
            int low = 0;
            int high = ranges.Length / 3 - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (ranges[middle * 3] <= code)
                    low = middle;
                else
                    high = middle - 1;
            }

            if (high < 0 || code < ranges[low * 3] || code > ranges[low * 3 + 1])
                return 0;

            return (ushort)(ranges[low * 3 + 2] + (code - ranges[low * 3]));
        }

        /// <summary>
        /// Reads the character code at a position of encoded text and returns its CID: two bytes if
        /// they are a lead byte and a trail byte of the encoding, one byte otherwise.
        /// </summary>
        /// <param name="encoded">The encoded text (see <see cref="EncodeText"/>).</param>
        /// <param name="position">The position of the code; moved to the next code.</param>
        /// <returns>The CID of the code; 0 if the CMap has no CID for it.</returns>
        public ushort ReadCID(ReadOnlySpan<byte> encoded, ref int position)
        {
            int code = encoded[position++];
            if (IsLeadByte((byte)code) && position < encoded.Length && IsTrailByte(encoded[position]))
            {
                code = (code << 8) | encoded[position++];
            }

            return GetCID((ushort)code);
        }

        /// <summary>
        /// Gets the CID system info for this encoder.
        /// </summary>
        public CIDSystemInfo GetSystemInfo()
        {
            return new CIDSystemInfo
            {
                Registry = Registry,
                Ordering = Ordering,
                Supplement = Supplement
            };
        }
    }
}
