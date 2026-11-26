namespace Haru.Font.CID
{
    /// <summary>
    /// GBK-EUC-H encoder for Simplified Chinese.
    /// Uses Windows Code Page 936 (GBK encoding).
    /// The PDF viewer's built-in GBK-EUC-H CMap handles byte→CID→glyph mapping.
    /// </summary>
    public class GBKEucHEncoder : CIDEncoder
    {
        public GBKEucHEncoder()
        {
            Name = "GBK-EUC-H";
            Registry = "Adobe";
            Ordering = "GB1";
            Supplement = 2;
            CodePage = 936; // Windows Code Page for GBK (Chinese Simplified)

            InitializeEncoding();
            CidRanges = CIDCMapData.GbkEucH;
        }

        /// <inheritdoc />
        protected override bool IsLeadByte(byte value) => value >= 0x81 && value <= 0xFE;

        /// <inheritdoc />
        protected override bool IsTrailByte(byte value) => value >= 0x40 && value <= 0xFE;
    }
}
