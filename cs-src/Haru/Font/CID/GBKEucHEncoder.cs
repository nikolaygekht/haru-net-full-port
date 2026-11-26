namespace Haru.Font.CID
{
    /// <summary>
    /// GBK-EUC-H encoder for Simplified Chinese.
    /// Uses Windows Code Page 936 (GBK encoding).
    /// The PDF viewer's built-in GBK-EUC-H CMap handles the byte to CID to glyph mapping.
    /// The encoders of the other predefined CMaps are created by name (<see cref="CIDEncoder.Create"/>).
    /// </summary>
    public class GBKEucHEncoder : CIDEncoder
    {
        public GBKEucHEncoder()
            : base(CMapDefinition.GbkEucH)
        {
        }
    }
}
