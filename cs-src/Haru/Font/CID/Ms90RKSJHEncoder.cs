namespace Haru.Font.CID
{
    /// <summary>
    /// 90ms-RKSJ-H encoder for Japanese.
    /// Uses Windows Code Page 932 (Shift-JIS encoding).
    /// The PDF viewer's built-in 90ms-RKSJ-H CMap handles byte→CID→glyph mapping.
    /// </summary>
    public class Ms90RKSJHEncoder : CIDEncoder
    {
        public Ms90RKSJHEncoder()
        {
            Name = "90ms-RKSJ-H";
            Registry = "Adobe";
            Ordering = "Japan1";
            Supplement = 2;
            CodePage = 932; // Windows Code Page for Shift-JIS (Japanese)

            InitializeEncoding();
            CidRanges = CIDCMapData.Ms90RksjH;
        }

        /// <inheritdoc />
        protected override bool IsLeadByte(byte value) => (value >= 0x81 && value <= 0x9F) || (value >= 0xE0 && value <= 0xFC);

        /// <inheritdoc />
        protected override bool IsTrailByte(byte value) => value >= 0x40 && value <= 0xFC && value != 0x7F;
    }
}
