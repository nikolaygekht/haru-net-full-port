/* One content stream of a page: what was drawn into it, the resources it uses and whether it can be shared. */

using System.Collections.Generic;
using Haru.Objects;

namespace Haru.Doc
{
    /// <summary>
    /// A content stream of a page (HPDF_Page_New_Content_Stream). A page draws into one stream at a time; with
    /// <see cref="HpdfPage.NewContentStream"/> it continues in a new one, and a finished stream can be shown on other
    /// pages too (<see cref="HpdfPage.InsertSharedContentStream"/>), for example a header drawn once. The stream
    /// remembers the resources (fonts, images, ...) it uses, which go to every page that shows it, and whether it is
    /// self-contained enough to be shared.
    /// </summary>
    public sealed class HpdfContentStream
    {
        private readonly Dictionary<(string Category, string Name), (HpdfObject Value, HpdfObject Identity)> _resources = new();
        private readonly int _startDepth;
        private int _minimumDepth;
        private string? _notShareableReason;

        internal HpdfContentStream(HpdfPage page, HpdfStreamObject streamObject, int depth, bool inText, bool inPath)
        {
            Page = page;
            StreamObject = streamObject;
            _startDepth = depth;
            _minimumDepth = depth;
            if (inText || inPath)
                _notShareableReason = "it starts inside a text object or a path";
        }

        /// <summary>
        /// Gets the page the stream was drawn on.
        /// </summary>
        public HpdfPage Page { get; }

        /// <summary>
        /// Gets the stream object (an entry of the /Contents array of the pages that show it).
        /// </summary>
        public HpdfStreamObject StreamObject { get; }

        /// <summary>
        /// Gets whether drawing into the stream has ended: its page went on to a new content stream.
        /// </summary>
        public bool IsFinished { get; private set; }

        /// <summary>
        /// Gets whether the finished stream can be shown on other pages: it starts and ends outside text objects and
        /// paths, its GSave/GRestore calls are balanced and never restore a state saved before it, and the text it
        /// shows uses a font set inside it.
        /// </summary>
        public bool IsShareable => IsFinished && _notShareableReason is null;

        /// <summary>
        /// Why the stream cannot be shared, or null.
        /// </summary>
        internal string? NotShareableReason => IsFinished ? _notShareableReason : "drawing into it has not ended";

        /// <summary>
        /// The resources the stream uses, to be added to the pages that show it.
        /// </summary>
        internal IEnumerable<(string Category, string Name, HpdfObject Value, HpdfObject Identity)> Resources
        {
            get
            {
                foreach (var ((category, name), (value, identity)) in _resources)
                    yield return (category, name, value, identity);
            }
        }

        internal void UseResource(string category, string name, HpdfObject value, HpdfObject identity) =>
            _resources[(category, name)] = (value, identity);

        internal void DepthChanged(int depth)
        {
            if (depth < _minimumDepth)
                _minimumDepth = depth;
        }

        internal void MarkNotShareable(string reason) => _notShareableReason ??= reason;

        /// <summary>
        /// Ends drawing into the stream and records whether it is self-contained.
        /// </summary>
        internal void Finish(int depth, bool inText, bool inPath)
        {
            IsFinished = true;
            if (inText || inPath)
                MarkNotShareable("it ends inside a text object or a path");
            if (depth != _startDepth)
                MarkNotShareable("its GSave and GRestore calls are not balanced");
            if (_minimumDepth < _startDepth)
                MarkNotShareable("it restores a graphics state saved before it");
        }
    }
}
