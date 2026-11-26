namespace Haru.Font.CID
{
    /// <summary>
    /// KSCms-UHC-H encoder for Korean.
    /// Uses Windows Code Page 949 (EUC-KR/UHC encoding).
    /// The PDF viewer's built-in KSCms-UHC-H CMap handles byte→CID→glyph mapping.
    /// </summary>
    public class KSCmsUHCHEncoder : CIDEncoder
    {
        public KSCmsUHCHEncoder()
        {
            Name = "KSCms-UHC-H";
            Registry = "Adobe";
            Ordering = "Korea1";
            Supplement = 1;
            CodePage = 949; // Windows Code Page for EUC-KR/UHC (Korean)

            InitializeEncoding();
            CidRanges = CIDCMapData.KscMsUhcH;
        }

        /// <inheritdoc />
        protected override bool IsLeadByte(byte value) => value >= 0x81 && value <= 0xFE;

        /// <inheritdoc />
        protected override bool IsTrailByte(byte value) => value >= 0x41 && value <= 0xFE;
    }
}
