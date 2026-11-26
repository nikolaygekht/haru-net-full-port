namespace Haru.Font.CID
{
    /// <summary>
    /// EUC-H encoder for Japanese.
    /// Uses Windows Code Page 20932 (EUC-JP encoding).
    /// The PDF viewer's built-in EUC-H CMap handles the byte to CID to glyph mapping.
    /// The encoders of the other predefined CMaps are created by name (<see cref="CIDEncoder.Create"/>).
    /// </summary>
    public class EucHEncoder : CIDEncoder
    {
        public EucHEncoder()
            : base(CMapDefinition.EucH)
        {
        }
    }
}
