using Haru.Objects;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// Represents a PDF outline (bookmark) entry in the document.
    /// Outlines allow navigation within a PDF document.
    /// </summary>
    public class HpdfOutline
    {
        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;
        private readonly System.Collections.Generic.List<HpdfOutline> _children = new();
        private bool _opened;
        private bool _isRoot;

        /// <summary>
        /// Gets the underlying dictionary object.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets or sets whether the outline is opened (children visible) by default.
        /// </summary>
        public bool Opened
        {
            get => _opened;
            set => _opened = value;
        }

        /// <summary>
        /// Creates the root outline object for a document.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        internal HpdfOutline(HpdfXref xref)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            _dict = new HpdfDict();
            _opened = true;

            // Add to xref
            _xref.Add(_dict);

            _dict.Add("Type", new HpdfName("Outlines"));
            _isRoot = true;
        }

        /// <summary>
        /// Creates a new outline entry as a child of this outline.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="parent">The parent outline.</param>
        /// <param name="title">The title of the outline entry.</param>
        internal HpdfOutline(HpdfXref xref, HpdfOutline parent, string title)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            if (parent is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Parent cannot be null");
            if (string.IsNullOrEmpty(title))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Title cannot be null or empty");

            _dict = new HpdfDict();
            _opened = true;

            // Add to xref
            _xref.Add(_dict);

            _dict.Add("Title", new HpdfString(title));

            // Add to parent's children
            AddToParent(parent);
        }

        /// <summary>
        /// Creates a child outline entry.
        /// </summary>
        /// <param name="title">The title of the child outline.</param>
        /// <returns>The newly created child outline.</returns>
        public HpdfOutline CreateChild(string title)
        {
            return new HpdfOutline(_xref, this, title);
        }

        /// <summary>
        /// Sets whether the outline is opened (children visible) by default.
        /// </summary>
        /// <param name="opened">True to show children by default, false to hide them.</param>
        public void SetOpened(bool opened)
        {
            _opened = opened;
        }

        /// <summary>
        /// Sets the destination for this outline (where it navigates to when clicked).
        /// </summary>
        /// <param name="destination">The destination array.</param>
        public void SetDestination(HpdfArray destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");

            // [page /View ...]: a destination without a view is not valid
            if (destination.Count < 2)
                throw new HpdfException(HpdfErrorCode.InvalidDestination,
                    "The destination has no view: call SetXYZ, SetFit or another Set method first");

            _dict["Dest"] = destination;
        }

        /// <summary>
        /// Sets the destination for this outline (where it navigates to when clicked).
        /// </summary>
        /// <param name="destination">The destination object.</param>
        public void SetDestination(HpdfDestination destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");

            SetDestination(destination.DestArray);
        }

        /// <summary>
        /// Adds this outline as a child of the specified parent.
        /// </summary>
        /// <param name="parent">The parent outline.</param>
        private void AddToParent(HpdfOutline parent)
        {
            // Get first and last children of parent
            HpdfDict? firstChild = null;
            HpdfDict? lastChild = null;

            if (parent._dict.TryGetValue("First", out var firstObj))
                firstChild = firstObj as HpdfDict;

            if (parent._dict.TryGetValue("Last", out var lastObj))
                lastChild = lastObj as HpdfDict;

            // If no first child, this becomes the first
            if (firstChild is null)
                parent._dict.Add("First", _dict);

            // If there's a last child, link them
            if (lastChild != null)
            {
                lastChild.Add("Next", _dict);
                _dict.Add("Prev", lastChild);
            }

            // This becomes the last child (replace if it exists)
            parent._dict["Last"] = _dict;

            // Set parent reference
            _dict.Add("Parent", parent._dict);

            parent._children.Add(this);
        }

        /// <summary>
        /// Prepares this outline and all its descendants for writing by calculating their Count fields.
        /// </summary>
        internal void BeforeWrite()
        {
            foreach (var child in _children)
                child.BeforeWrite();

            int count = CountChildren();

            // Assigned rather than added: the document can be saved more than once
            if (count > 0)
            {
                // If opened, count is positive; if closed, count is negative
                // The root's count is always positive: the outline root cannot be closed
                int signedCount = _opened || _isRoot ? count : -count;
                _dict["Count"] = new HpdfNumber(signedCount);
            }
            else
            {
                _dict.Remove("Count");
            }
        }

        /// <summary>
        /// Counts the descendants that are visible when this outline is open: every child, plus the
        /// descendants of each child that is itself open (PDF 1.7, 12.3.3, /Count).
        /// </summary>
        /// <returns>The count of visible descendants.</returns>
        private int CountChildren()
        {
            int count = 0;
            foreach (var child in _children)
            {
                count++;
                if (child._opened)
                    count += child.CountChildren();
            }
            return count;
        }
    }
}
