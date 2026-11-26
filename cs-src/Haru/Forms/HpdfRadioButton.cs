using System.Collections.Generic;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Radio button field where exactly one option can be selected from a group.
    /// Each radio button group has multiple widgets, each representing one option.
    /// </summary>
    public class HpdfRadioButton : HpdfField
    {
        private const string UncheckedState = "Off";
        private readonly Dictionary<string, HpdfWidgetAnnotation> _optionWidgets;
        private string _selected = UncheckedState;

        /// <summary>
        /// Creates a new radio button field.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="name">The field name (name of the radio button group).</param>
        public HpdfRadioButton(HpdfXref xref, string name)
            : base(xref, HpdfFieldType.Btn, name)
        {
            _optionWidgets = new Dictionary<string, HpdfWidgetAnnotation>();

            // The Radio flag makes the button field a radio button group; it is kept by later Flags assignments
            SetTypeFlags(HpdfFieldFlags.Radio);

            // Default value is Off (nothing selected)
            SetValue(UncheckedState);
        }

        /// <summary>
        /// Adds a radio button option with a specific state name.
        /// Each option represents one choice in the radio button group.
        /// </summary>
        /// <param name="stateName">The state name for this option (e.g., "Male", "Female", "Option1").</param>
        /// <param name="widget">The widget annotation for this option.</param>
        public void AddOption(string stateName, HpdfWidgetAnnotation widget)
        {
            ValidateStateName(stateName);

            if (widget is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Widget cannot be null");

            if (_widgets.Contains(widget))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "The widget is already an option of this group");

            // A widget with state appearances can show the option only if one of them is named after it
            if (widget.Dict.TryGetValue("AP", out var ap) && ap is HpdfDict apDict &&
                apDict.TryGetValue("N", out var normal) && normal is HpdfDict states && !states.ContainsKey(stateName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"The widget has no appearance state named '{stateName}'; create it with SetButtonAppearances(..., \"{stateName}\")");

            _optionWidgets[stateName] = widget;
            try
            {
                AddWidget(widget);
            }
            catch
            {
                _optionWidgets.Remove(stateName);
                throw;
            }

            widget.SetAppearanceState(stateName == _selected ? stateName : UncheckedState);
        }

        /// <summary>
        /// A state name identifies one option: it must be given, must not be the reserved "Off",
        /// and must not be used by another option of the group.
        /// </summary>
        private void ValidateStateName(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "State name cannot be null or empty");

            if (stateName == UncheckedState)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "State name cannot be 'Off' (reserved)");

            if (_optionWidgets.ContainsKey(stateName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter, $"The group already has an option named '{stateName}'");
        }

        /// <summary>
        /// Options are created with <see cref="CreateOption"/>, which gives each its state name.
        /// </summary>
        protected override void EnsureCanCreateWidget()
        {
            throw new HpdfException(HpdfErrorCode.InvalidOperation,
                "Use CreateOption to create the widgets of a radio button group");
        }

        /// <summary>
        /// Only option widgets (added by <see cref="AddOption"/> or <see cref="CreateOption"/>) belong to the group.
        /// </summary>
        protected override void ValidateWidget(HpdfWidgetAnnotation widget)
        {
            if (!_optionWidgets.ContainsValue(widget))
                throw new HpdfException(HpdfErrorCode.InvalidOperation,
                    "Use AddOption to add a widget to a radio button group");
        }

        /// <summary>
        /// Creates and adds a radio button option.
        /// </summary>
        /// <param name="stateName">The state name for this option.</param>
        /// <param name="rect">The widget rectangle.</param>
        /// <returns>The created widget annotation.</returns>
        public HpdfWidgetAnnotation CreateOption(string stateName, HpdfRect rect)
        {
            // Checked before any object is created, so a bad name leaves nothing behind
            ValidateStateName(stateName);

            var widget = new HpdfWidgetAnnotation(_xref, rect);

            // Set up appearance streams for radio button (stateName/Off states) with actual drawing commands
            widget.SetButtonAppearances(ButtonElementType.RadioButton, stateName, UncheckedState);

            AddOption(stateName, widget);
            return widget;
        }

        /// <summary>
        /// Sets the selected radio button option.
        /// </summary>
        /// <param name="stateName">The state name of the option to select, or "Off" for none.</param>
        public void SetSelected(string stateName)
        {
            SetValue(stateName);
        }

        /// <summary>
        /// Sets the field value using a string. The appearance state of every option widget follows the value.
        /// </summary>
        /// <param name="value">The state name to select.</param>
        public override void SetValue(string value)
        {
            // Allow null to mean no selection
            if (value is null)
                value = UncheckedState;

            // Validate that the value is either Off or one of the registered options
            if (value != UncheckedState && !_optionWidgets.ContainsKey(value))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"Invalid radio button value: {value}. Must be 'Off' or a registered option.");

            _selected = value;
            _dict.Remove("V");
            _dict.Add("V", new HpdfName(value));

            foreach (var option in _optionWidgets)
                option.Value.SetAppearanceState(option.Key == value ? value : UncheckedState);
        }

        /// <summary>
        /// Sets the default selected option.
        /// </summary>
        /// <param name="stateName">The state name of the default option, or "Off" for none.</param>
        public void SetDefaultSelected(string stateName)
        {
            if (stateName is null)
                stateName = UncheckedState;

            if (stateName != UncheckedState && !_optionWidgets.ContainsKey(stateName))
                throw new HpdfException(HpdfErrorCode.InvalidParameter,
                    $"Invalid radio button value: {stateName}. Must be 'Off' or a registered option.");

            _dict.Remove("DV");
            _dict.Add("DV", new HpdfName(stateName));
        }

        /// <summary>
        /// Sets the default option, like <see cref="SetDefaultSelected"/>: a button's default value is a state name.
        /// </summary>
        /// <param name="value">The state name of the default option, or "Off" (or null) for none.</param>
        public override void SetDefaultValue(string value)
        {
            SetDefaultSelected(value);
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
