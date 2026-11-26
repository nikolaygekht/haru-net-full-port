using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Checkbox field that can be checked or unchecked.
    /// </summary>
    public class HpdfCheckbox : HpdfField
    {
        private const string CheckedState = "Yes";
        private const string UncheckedState = "Off";
        private string _state = UncheckedState;

        /// <summary>
        /// Creates a new checkbox field.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="name">The field name.</param>
        public HpdfCheckbox(HpdfXref xref, string name)
            : base(xref, HpdfFieldType.Btn, name)
        {
            // Checkbox doesn't have Radio or Pushbutton flags set
            // Default value is Off (unchecked)
            SetValue(UncheckedState);
        }

        /// <summary>
        /// Sets the checkbox value (checked or unchecked).
        /// </summary>
        /// <param name="value">The value (true for checked, false for unchecked).</param>
        public void SetChecked(bool value)
        {
            SetValue(value ? CheckedState : UncheckedState);
        }

        /// <summary>
        /// Sets the field value using a string. The appearance state of every widget follows the value.
        /// </summary>
        /// <param name="value">The state name ("Yes" for checked, "Off" for unchecked).</param>
        public override void SetValue(string value)
        {
            // Validate the value
            if (value != CheckedState && value != UncheckedState)
            {
                // Allow null to mean unchecked
                if (value is null)
                    value = UncheckedState;
                else
                    throw new HpdfException(HpdfErrorCode.InvalidParameter,
                        $"Checkbox value must be '{CheckedState}' or '{UncheckedState}'");
            }

            _state = value;
            _dict.Remove("V");
            _dict.Add("V", new HpdfName(value));

            foreach (var widget in _widgets)
                widget.SetAppearanceState(value);
        }

        /// <summary>
        /// A widget shows the current value whenever it is added, before or after the value is set.
        /// </summary>
        protected override void OnWidgetAdded(HpdfWidgetAnnotation widget)
        {
            System.ArgumentNullException.ThrowIfNull(widget);
            widget.SetAppearanceState(_state);
        }

        /// <summary>
        /// Sets up the appearance streams for the checkbox states (Yes/Off) with actual drawing commands.
        /// </summary>
        protected override void ConfigureWidget(HpdfWidgetAnnotation widget)
        {
            System.ArgumentNullException.ThrowIfNull(widget);
            widget.SetButtonAppearances(ButtonElementType.Checkbox, CheckedState, UncheckedState);
        }

        /// <summary>
        /// Sets the default checkbox value.
        /// </summary>
        /// <param name="value">The default state (true for checked, false for unchecked).</param>
        public void SetDefaultChecked(bool value)
        {
            _dict.Remove("DV");
            _dict.Add("DV", new HpdfName(value ? CheckedState : UncheckedState));
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
