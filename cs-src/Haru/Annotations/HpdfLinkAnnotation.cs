using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Annotations
{
    /// <summary>
    /// Link annotation - creates clickable links to URIs or internal destinations.
    /// </summary>
    public class HpdfLinkAnnotation : HpdfAnnotation
    {
        /// <summary>
        /// Creates a new URI link annotation (use <see cref="Doc.HpdfPage.CreateLinkAnnotation(HpdfRect, string)"/>).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="uri">The URI to link to.</param>
        internal HpdfLinkAnnotation(HpdfXref xref, HpdfRect rect, string uri)
            : base(xref, HpdfAnnotationType.Link, ValidateUri(rect, uri))
        {
            // Create action dictionary for URI link
            var action = new HpdfDict();
            action.Add("Type", new HpdfName("Action"));
            action.Add("S", new HpdfName("URI"));
            action.Add("URI", new HpdfString(EncodeUri(uri)));

            _dict.Add("A", action);
        }

        /// <summary>
        /// Encodes a URI as the 7-bit ASCII string a URI action requires: characters outside
        /// ASCII (an IRI such as a non-Latin domain path) are percent-encoded as UTF-8 bytes.
        /// </summary>
        private static byte[] EncodeUri(string uri)
        {
            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(uri);
            var result = new System.Collections.Generic.List<byte>(utf8.Length);
            foreach (byte b in utf8)
            {
                if (b < 0x80)
                {
                    result.Add(b);
                }
                else
                {
                    result.Add((byte)'%');
                    result.Add((byte)"0123456789ABCDEF"[b >> 4]);
                    result.Add((byte)"0123456789ABCDEF"[b & 0x0F]);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Creates a new internal link annotation (GoTo action).
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="rect">The annotation rectangle.</param>
        /// <param name="destination">The destination array.</param>
        internal HpdfLinkAnnotation(HpdfXref xref, HpdfRect rect, HpdfArray destination)
            : base(xref, HpdfAnnotationType.Link, ValidateDestination(rect, destination))
        {
            _dict.Add("Dest", destination);
        }

        // The arguments are checked before the base constructor registers the annotation object,
        // so invalid arguments leave nothing behind in the document.

        private static HpdfRect ValidateUri(HpdfRect rect, string uri)
        {
            if (string.IsNullOrEmpty(uri))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "URI cannot be null or empty");
            return rect;
        }

        private static HpdfRect ValidateDestination(HpdfRect rect, HpdfArray destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");

            // [page /View ...]: a destination without a view is not valid
            if (destination.Count < 2)
                throw new HpdfException(HpdfErrorCode.InvalidDestination,
                    "The destination has no view: call SetXYZ, SetFit or another Set method first");
            return rect;
        }

        /// <summary>
        /// Sets the highlight mode for the link.
        /// </summary>
        /// <param name="mode">The highlight mode.</param>
        public void SetHighlightMode(HpdfHighlightMode mode)
        {
            string modeName = mode switch
            {
                HpdfHighlightMode.NoHighlight => "N",
                HpdfHighlightMode.InvertBox => "I",
                HpdfHighlightMode.InvertBorder => "O",
                HpdfHighlightMode.DownAppearance => "P",
                _ => throw new HpdfException(HpdfErrorCode.InvalidParameter, "Invalid highlight mode")
            };

            _dict["H"] = new HpdfName(modeName);
        }
    }
}
