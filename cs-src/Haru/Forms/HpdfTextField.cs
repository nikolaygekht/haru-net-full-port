using System.Globalization;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Alignment of the text in a form field (quadding, /Q); the values are the /Q values.
    /// For text printed on a page use <see cref="Haru.Types.HpdfTextAlignment"/>.
    /// </summary>
    public enum HpdfFieldAlignment
    {
        /// <summary>
        /// Left-aligned text (default).
        /// </summary>
        Left = 0,

        /// <summary>
        /// Center-aligned text.
        /// </summary>
        Center = 1,

        /// <summary>
        /// Right-aligned text.
        /// </summary>
        Right = 2
    }

    /// <summary>
    /// Text field for single or multi-line text input.
    /// </summary>
    public class HpdfTextField : HpdfField
    {
        private string? _value;
        private HpdfFieldAlignment _alignment = HpdfFieldAlignment.Left;

        /// <summary>
        /// Creates a new text field.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="name">The field name.</param>
        public HpdfTextField(HpdfXref xref, string name)
            : base(xref, HpdfFieldType.Tx, name)
        {
        }

        /// <summary>
        /// Sets the maximum length of the text field.
        /// </summary>
        /// <param name="maxLength">The maximum number of characters.</param>
        public void SetMaxLength(int maxLength)
        {
            if (maxLength <= 0)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Max length must be positive");

            _dict.Remove("MaxLen");
            _dict.Add("MaxLen", new HpdfNumber(maxLength));
        }

        /// <summary>
        /// Sets the text alignment (quadding) for the field.
        /// </summary>
        /// <param name="alignment">The text alignment.</param>
        public void SetAlignment(HpdfFieldAlignment alignment)
        {
            _alignment = alignment;
            _dict.Remove("Q");
            _dict.Add("Q", new HpdfNumber((int)alignment));
        }

        /// <summary>
        /// Sets the field value; the widgets show it when the document is saved.
        /// </summary>
        /// <param name="value">The text, or null to remove the value.</param>
        public override void SetValue(string value)
        {
            base.SetValue(value);
            _value = value;
        }

        /// <summary>
        /// Draws the value into every widget, in the font of the default appearance; a password
        /// field shows an asterisk for each character. Widgets get no appearance when the default
        /// appearance refers to a font that is not a standard font name.
        /// </summary>
        internal override void GenerateAppearances(HpdfDefaultAppearance? formAppearance, HpdfFormFonts fonts)
        {
            if (!TryResolveAppearance(formAppearance, fonts, out var appearance, out var font))
                return;

            string text = _value ?? string.Empty;
            if ((Flags & HpdfFieldFlags.Password) != 0)
                text = new string('*', new StringInfo(text).LengthInTextElements);

            bool multiline = (Flags & HpdfFieldFlags.Multiline) != 0;
            foreach (var widget in _widgets)
            {
                widget.SetVariableTextAppearance(font, (canvas, width, height) =>
                {
                    if (multiline)
                        HpdfVariableTextAppearance.DrawMultiline(canvas, width, height, appearance, font, text, _alignment);
                    else
                        HpdfVariableTextAppearance.DrawSingleLine(canvas, width, height, appearance, font, text, _alignment);
                });
            }
        }

        /// <summary>
        /// Sets whether this is a multiline text field.
        /// </summary>
        /// <param name="multiline">True for multiline, false for single line.</param>
        public void SetMultiline(bool multiline)
        {
            if (multiline)
                Flags |= HpdfFieldFlags.Multiline;
            else
                Flags &= ~HpdfFieldFlags.Multiline;
        }

        /// <summary>
        /// Sets whether this is a password field (characters are masked).
        /// </summary>
        /// <param name="password">True for password field, false for normal text.</param>
        public void SetPassword(bool password)
        {
            if (password)
                Flags |= HpdfFieldFlags.Password;
            else
                Flags &= ~HpdfFieldFlags.Password;
        }

        /// <summary>
        /// Sets whether this field is read-only.
        /// </summary>
        /// <param name="readOnly">True for read-only, false for editable.</param>
        public void SetReadOnly(bool readOnly)
        {
            if (readOnly)
                Flags |= HpdfFieldFlags.ReadOnly;
            else
                Flags &= ~HpdfFieldFlags.ReadOnly;
        }

        /// <summary>
        /// Sets whether this field is required.
        /// </summary>
        /// <param name="required">True if the field must have a value.</param>
        public void SetRequired(bool required)
        {
            if (required)
                Flags |= HpdfFieldFlags.Required;
            else
                Flags &= ~HpdfFieldFlags.Required;
        }
    }
}
