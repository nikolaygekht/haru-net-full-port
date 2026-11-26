using Haru.Objects;
using Haru.Types;

namespace Haru.Doc
{
    /// <summary>
    /// Represents a page label entry defining the numbering style for a range of pages.
    /// Page labels control how page numbers are displayed in PDF viewers.
    /// </summary>
    public class HpdfPageLabel
    {
        /// <summary>
        /// Gets or sets the numbering style for this page label range.
        /// </summary>
        public HpdfPageNumStyle Style
        {
            get => _style;
            set
            {
                if (value < HpdfPageNumStyle.Decimal || value >= HpdfPageNumStyle.Eof)
                    throw new HpdfException(HpdfErrorCode.PageNumStyleOutOfRange, $"Invalid page number style: {value}");
                _style = value;
            }
        }

        private HpdfPageNumStyle _style;
        private int _firstPage = 1;

        /// <summary>
        /// Gets or sets the value of the numeric portion for the first page in the range.
        /// Subsequent pages are numbered sequentially from this value.
        /// Default is 1.
        /// </summary>
        public int FirstPage
        {
            get => _firstPage;
            set
            {
                // /St must be at least 1 (PDF 12.4.2)
                if (value < 1)
                    throw new HpdfException(HpdfErrorCode.InvalidParameter, "The first page number must be at least 1");
                _firstPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the label prefix for page labels in this range.
        /// This string is prepended to the numeric portion of the page label.
        /// For example, "Chapter " would produce labels like "Chapter 1", "Chapter 2", etc.
        /// Default is null (no prefix).
        /// </summary>
        public string? Prefix { get; set; }

        /// <summary>
        /// Creates a new page label with default settings (decimal numbering, starting at 1, no prefix).
        /// </summary>
        public HpdfPageLabel()
        {
            Style = HpdfPageNumStyle.Decimal;
            FirstPage = 1;
            Prefix = (string?)null;
        }

        /// <summary>
        /// Creates a new page label with specified settings.
        /// </summary>
        /// <param name="style">The numbering style.</param>
        /// <param name="firstPage">The starting page number (default: 1).</param>
        /// <param name="prefix">Optional prefix string (default: null).</param>
        public HpdfPageLabel(HpdfPageNumStyle style, int firstPage = 1, string? prefix = null)
        {
            Style = style;
            FirstPage = firstPage;
            Prefix = prefix;
        }

        /// <summary>
        /// Converts this page label to a PDF dictionary object.
        /// </summary>
        /// <returns>A dictionary containing the page label entry.</returns>
        public HpdfDict ToDict()
        {
            var dict = new HpdfDict();

            // Add the numbering style
            // Map enum to PDF names as per PDF specification
            string styleName = Style switch
            {
                HpdfPageNumStyle.Decimal => "D",
                HpdfPageNumStyle.UpperRoman => "R",
                HpdfPageNumStyle.LowerRoman => "r",
                HpdfPageNumStyle.UpperLetters => "A",
                HpdfPageNumStyle.LowerLetters => "a",
                _ => throw new HpdfException(HpdfErrorCode.PageNumStyleOutOfRange, $"Invalid page number style: {Style}")
            };

            dict.Add("S", new HpdfName(styleName));

            // Add prefix if specified
            if (!string.IsNullOrEmpty(Prefix))
            {
                dict.Add("P", new HpdfString(Prefix));
            }

            // Add start value if not default (1)
            // Note: PDF "St" entry is the starting value for the numeric portion
            // If FirstPage is 1, we can omit it (PDF default)
            if (FirstPage != 1)
            {
                dict.Add("St", new HpdfNumber(FirstPage));
            }

            return dict;
        }

        /// <summary>
        /// Returns a string representation of this page label.
        /// </summary>
        public override string ToString()
        {
            string styleStr = Style switch
            {
                HpdfPageNumStyle.Decimal => "Decimal",
                HpdfPageNumStyle.UpperRoman => "UpperRoman",
                HpdfPageNumStyle.LowerRoman => "LowerRoman",
                HpdfPageNumStyle.UpperLetters => "UpperLetters",
                HpdfPageNumStyle.LowerLetters => "LowerLetters",
                _ => "Unknown"
            };

            string prefixStr = string.IsNullOrEmpty(Prefix) ? "" : $", Prefix='{Prefix}'";
            return $"PageLabel[Style={styleStr}, FirstPage={FirstPage}{prefixStr}]";
        }
    }
}
