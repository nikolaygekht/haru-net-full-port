namespace Haru.Font.CID
{
    /// <summary>
    /// ETen-B5-H encoder for Traditional Chinese.
    /// Uses Windows Code Page 950 (Big5 encoding).
    /// The PDF viewer's built-in ETen-B5-H CMap handles the byte to CID to glyph mapping.
    /// The encoders of the other predefined CMaps are created by name (<see cref="CIDEncoder.Create"/>).
    /// </summary>
    public class ETenB5HEncoder : CIDEncoder
    {
        public ETenB5HEncoder()
            : base(CMapDefinition.ETenB5H)
        {
        }
    }
}
