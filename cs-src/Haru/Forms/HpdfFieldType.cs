namespace Haru.Forms
{
    /// <summary>
    /// PDF form field types (/FT entry in field dictionary).
    /// </summary>
    public enum HpdfFieldType
    {
        /// <summary>
        /// Button field (pushbutton, checkbox, or radio button).
        /// </summary>
        Btn,

        /// <summary>
        /// Text field (single or multi-line text input).
        /// </summary>
        Tx,

        /// <summary>
        /// Choice field (list box or combo box).
        /// </summary>
        Ch,

        /// <summary>
        /// Signature field (digital signature).
        /// </summary>
        Sig
    }
}
