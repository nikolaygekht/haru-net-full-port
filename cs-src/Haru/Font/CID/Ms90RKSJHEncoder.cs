namespace Haru.Font.CID
{
    /// <summary>
    /// 90ms-RKSJ-H encoder for Japanese.
    /// Uses Windows Code Page 932 (Shift-JIS encoding).
    /// The PDF viewer's built-in 90ms-RKSJ-H CMap handles the byte to CID to glyph mapping.
    /// The encoders of the other predefined CMaps are created by name (<see cref="CIDEncoder.Create"/>).
    /// </summary>
    public class Ms90RKSJHEncoder : CIDEncoder
    {
        public Ms90RKSJHEncoder()
            : base(CMapDefinition.Ms90RksjH)
        {
        }
    }
}
