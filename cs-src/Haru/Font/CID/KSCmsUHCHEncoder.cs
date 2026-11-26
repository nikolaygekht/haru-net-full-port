namespace Haru.Font.CID
{
    /// <summary>
    /// KSCms-UHC-H encoder for Korean.
    /// Uses Windows Code Page 949 (EUC-KR/UHC encoding).
    /// The PDF viewer's built-in KSCms-UHC-H CMap handles the byte to CID to glyph mapping.
    /// The encoders of the other predefined CMaps are created by name (<see cref="CIDEncoder.Create"/>).
    /// </summary>
    public class KSCmsUHCHEncoder : CIDEncoder
    {
        public KSCmsUHCHEncoder()
            : base(CMapDefinition.KscMsUhcH)
        {
        }
    }
}
