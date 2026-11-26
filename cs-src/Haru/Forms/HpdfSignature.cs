using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Signature field for digital signatures.
    /// This creates an unsigned signature field that can be signed by the user.
    /// </summary>
    public class HpdfSignature : HpdfField
    {
        /// <summary>
        /// Creates a new unsigned signature field.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="name">The field name.</param>
        public HpdfSignature(HpdfXref xref, string name)
            : base(xref, HpdfFieldType.Sig, name)
        {
            // NOTE: Do NOT create a "V" (value) entry here.
            // An unsigned signature field should not have a "V" entry.
            // The "V" entry is only added when the field is actually signed.
        }

        // NOTE: Metadata methods like SetReason, SetLocation, SetSignerName, and SetContactInfo
        // have been removed because they don't apply to unsigned signature fields.
        // These properties are part of the signature dictionary that's created when the field
        // is actually signed with cryptographic data. For an unsigned field, these would have
        // no effect and could confuse PDF viewers.

        /// <summary>
        /// Gives the widget a frame appearance, so the empty field is visible in every viewer
        /// and where forms are not interactive (print, previews).
        /// </summary>
        protected override void ConfigureWidget(HpdfWidgetAnnotation widget)
        {
            System.ArgumentNullException.ThrowIfNull(widget);
            widget.SetFrameAppearance();
        }

        /// <summary>
        /// Sets whether this field is read-only.
        /// Signature fields are typically read-only after signing.
        /// </summary>
        /// <param name="readOnly">True for read-only, false for editable.</param>
        public void SetReadOnly(bool readOnly)
        {
            if (readOnly)
                Flags |= HpdfFieldFlags.ReadOnly;
            else
                Flags &= ~HpdfFieldFlags.ReadOnly;
        }
    }
}
