/* JavaScript in a PDF: the script stream and the JavaScript action, shared by document scripts, links and 3D models. */

using Haru.Objects;
using Haru.Streams;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// Writes JavaScript programs: as a text stream (PDFDocEncoding for ASCII, otherwise UTF-16BE with a byte order
    /// mark, compressed) and as a JavaScript action (ISO 32000-1, 12.6.4.16).
    /// </summary>
    internal static class HpdfJavaScript
    {
        /// <summary>
        /// Checks the script argument of a public method.
        /// </summary>
        internal static void Validate(string script)
        {
            if (script is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Script cannot be null");
        }

        /// <summary>
        /// Creates the stream of a script, added to the cross-reference table.
        /// </summary>
        internal static HpdfStreamObject CreateStream(HpdfXref xref, string script)
        {
            var stream = new HpdfStreamObject { Filter = HpdfStreamFilter.FlateDecode };
            xref.Add(stream);
            Write(stream, script);
            return stream;
        }

        /// <summary>
        /// Replaces the script in a stream created by <see cref="CreateStream"/>.
        /// </summary>
        internal static void Write(HpdfStreamObject stream, string script)
        {
            stream.ClearStream();
            stream.WriteToStream(new HpdfString(script).Value);
        }

        /// <summary>
        /// Creates a JavaScript action &lt;&lt; /Type /Action /S /JavaScript /JS stream &gt;&gt; (PDF 1.3).
        /// </summary>
        internal static HpdfDict CreateAction(HpdfXref xref, string script)
        {
            var action = new HpdfDict();
            action.Add("Type", new HpdfName("Action"));
            action.Add("S", new HpdfName("JavaScript"));
            action.Add("JS", CreateStream(xref, script));
            xref.RequireVersion(HpdfVersion.Version13);
            return action;
        }
    }
}
