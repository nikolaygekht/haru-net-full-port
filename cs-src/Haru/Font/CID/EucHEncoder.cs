namespace Haru.Font.CID
{
    /// <summary>
    /// EUC-H encoder for Japanese.
    /// Uses Windows Code Page 20932 (EUC-JP encoding).
    /// The PDF viewer's built-in EUC-H CMap handles byte→CID→glyph mapping.
    /// </summary>
    public class EucHEncoder : CIDEncoder
    {
        public EucHEncoder()
        {
            Name = "EUC-H";
            Registry = "Adobe";
            Ordering = "Japan1";
            Supplement = 2;
            CodePage = 20932; // Windows Code Page for EUC-JP (Japanese)

            InitializeEncoding();
        }
    }
}
