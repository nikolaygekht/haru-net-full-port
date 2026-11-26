/* Hook for objects that finish their PDF objects right before the document is written */
namespace Haru.Xref
{
    /// <summary>
    /// Implemented by an object whose PDF objects depend on how the document was used (a font and
    /// the text shown with it). The object registers itself with <see cref="HpdfXref.RegisterBeforeWrite"/>
    /// and is called every time the cross-reference table is written. The call may run any number of
    /// times and must leave the same objects for an unchanged document: assign dictionary entries
    /// (<c>dict[key] = ...</c>) and reuse the objects that are already in the cross-reference table.
    /// </summary>
    internal interface IHpdfBeforeWrite
    {
        /// <summary>
        /// Brings the PDF objects up to date before they are written.
        /// </summary>
        void BeforeWrite();
    }
}
