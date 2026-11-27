/* A named destination: an entry of the Dests name tree that links and outline items go to by name. */

using Haru.Objects;
using Haru.Xref;

namespace Haru.Doc
{
    /// <summary>
    /// A named destination (ISO 32000-1, 12.3.2.3): a name in the document's Dests name tree that stands for a
    /// destination. Links and outline items that go to it write only its name, so it can be used before its target
    /// is known, and other documents and URLs (<c>file.pdf#nameddest=intro</c>) can open the document at it.
    /// Create it with <see cref="HpdfDocument.CreateNamedDestination"/>; it needs a target
    /// (<see cref="SetDestination"/>) by the time the document is saved.
    /// </summary>
    public sealed class HpdfNamedDestination
    {
        private readonly HpdfXref _xref;

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the target, or null while it is not set.
        /// </summary>
        public HpdfDestination? Destination { get; private set; }

        internal HpdfNamedDestination(HpdfXref xref, string name)
        {
            _xref = xref;
            Name = name;
        }

        /// <summary>
        /// Sets the target that the name stands for; a later call replaces it.
        /// </summary>
        /// <param name="destination">
        /// A destination on a page of the same document, with its view set (<see cref="HpdfDestination.SetXYZ"/>,
        /// <see cref="HpdfDestination.SetFit"/>, ...).
        /// </param>
        public void SetDestination(HpdfDestination destination)
        {
            if (destination is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Destination cannot be null");
            if (destination.Page.Xref != _xref)
                throw new HpdfException(HpdfErrorCode.InvalidDestination,
                    "The destination is on a page of another document");
            HpdfDestination.EnsureView(destination.DestArray);

            Destination = destination;
        }

        /// <summary>
        /// Checks that the named destination is one of the document of <paramref name="xref"/>, where a link or an
        /// outline item goes to it.
        /// </summary>
        internal void EnsureDocument(HpdfXref xref)
        {
            if (xref != _xref)
                throw new HpdfException(HpdfErrorCode.InvalidDestination,
                    $"The named destination {Name} is one of another document");
        }

        /// <summary>
        /// Creates the value of a /Dest entry that goes to this destination: the name as the text string that is
        /// also the key of the Dests name tree.
        /// </summary>
        internal HpdfString CreateReference() => new HpdfString(Name);
    }
}
