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
        }
    }
}
