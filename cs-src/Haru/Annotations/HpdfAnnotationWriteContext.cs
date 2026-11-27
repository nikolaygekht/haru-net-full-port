namespace Haru.Annotations
{
    /// <summary>
    /// What an annotation needs to know about the document when it prepares its objects for writing.
    /// </summary>
    /// <param name="Compress">Whether generated appearance streams are compressed (the document compresses text).</param>
    /// <param name="PdfA">Whether the document is written as PDF/A-1, which allows no transparency.</param>
    internal readonly record struct HpdfAnnotationWriteContext(bool Compress, bool PdfA);
}
