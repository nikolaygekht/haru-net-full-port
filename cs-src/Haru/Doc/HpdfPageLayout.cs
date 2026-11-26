namespace Haru.Doc
{
    /// <summary>
    /// Specifies the page layout to be used when the document is opened.
    /// </summary>
    public enum HpdfPageLayout
    {
        /// <summary>
        /// Display one page at a time.
        /// </summary>
        SinglePage = 0,

        /// <summary>
        /// Display pages in one column.
        /// </summary>
        OneColumn,

        /// <summary>
        /// Display pages in two columns, with odd-numbered pages on the left.
        /// </summary>
        TwoColumnLeft,

        /// <summary>
        /// Display pages in two columns, with odd-numbered pages on the right.
        /// </summary>
        TwoColumnRight,

        /// <summary>
        /// Display pages two at a time, with odd-numbered pages on the left (PDF 1.5).
        /// </summary>
        TwoPageLeft,

        /// <summary>
        /// Display pages two at a time, with odd-numbered pages on the right (PDF 1.5).
        /// </summary>
        TwoPageRight
    }
}
