using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Haru.Font;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Types;
using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Base class for all PDF form fields.
    /// Represents the data and structure of a form field.
    /// </summary>
    public abstract class HpdfField
    {
        private static readonly string[] FieldTypeNames = new[]
        {
            "Btn",  // Button
            "Tx",   // Text
            "Ch",   // Choice
            "Sig"   // Signature
        };

        protected readonly HpdfDict _dict;
        protected readonly HpdfXref _xref;
        protected readonly HpdfFieldType _fieldType;
        protected readonly List<HpdfWidgetAnnotation> _widgets;
        protected HpdfFieldFlags _flags;
        private HpdfFieldFlags _typeFlags;
        private string? _defaultAppearance;

        /// <summary>
        /// Gets the underlying dictionary object.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the field type.
        /// </summary>
        public HpdfFieldType FieldType => _fieldType;

        /// <summary>
        /// Gets or sets the field flags. The flags that define the kind of field (<see cref="HpdfFieldFlags.Radio"/>
        /// of a radio button group, <see cref="HpdfFieldFlags.Combo"/> of a combo box) are always kept.
        /// </summary>
        public HpdfFieldFlags Flags
        {
            get => _flags;
            set
            {
                _flags = value | _typeFlags;
                _dict.Remove("Ff");
                if (_flags != HpdfFieldFlags.None)
                    _dict.Add("Ff", new HpdfNumber((int)_flags));
            }
        }

        /// <summary>
        /// Gets the list of widget annotations for this field.
        /// </summary>
        public IReadOnlyList<HpdfWidgetAnnotation> Widgets => _widgets.AsReadOnly();

        /// <summary>
        /// Creates a new form field.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        /// <param name="fieldType">The field type.</param>
        /// <param name="name">The partial field name.</param>
        protected HpdfField(HpdfXref xref, HpdfFieldType fieldType, string name)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            _fieldType = fieldType;
            _dict = new HpdfDict();
            _widgets = new List<HpdfWidgetAnnotation>();
            _flags = HpdfFieldFlags.None;

            // Add to xref
            _xref.Add(_dict);

            // Set field type
            _dict.Add("FT", new HpdfName(FieldTypeNames[(int)fieldType]));

            // Set field name
            if (!string.IsNullOrEmpty(name))
                _dict.Add("T", new HpdfString(name));
        }

        /// <summary>
        /// Sets the field value.
        /// </summary>
        /// <param name="value">The field value.</param>
        public virtual void SetValue(string value)
        {
            _dict.Remove("V");
            if (value != null)
            {
                _dict.Add("V", new HpdfString(value));
            }
        }

        /// <summary>
        /// Sets the default field value.
        /// </summary>
        /// <param name="value">The default value.</param>
        public virtual void SetDefaultValue(string value)
        {
            _dict.Remove("DV");
            if (value != null)
            {
                _dict.Add("DV", new HpdfString(value));
            }
        }

        /// <summary>
        /// Adds a widget annotation to this field.
        /// </summary>
        /// <param name="widget">The widget annotation.</param>
        public void AddWidget(HpdfWidgetAnnotation widget)
        {
            if (widget is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Widget cannot be null");

            // Adding the same widget again changes nothing
            if (_widgets.Contains(widget))
                return;

            if (widget.Dict.TryGetValue("Parent", out var parent) && !ReferenceEquals(parent, _dict))
                throw new HpdfException(HpdfErrorCode.InvalidOperation, "The widget already belongs to another field");

            ValidateWidget(widget);

            _widgets.Add(widget);

            // Set up bidirectional linking
            widget.SetParent(this);

            // Update Kids array in field dictionary
            UpdateKidsArray();

            OnWidgetAdded(widget);
        }

        /// <summary>
        /// Creates and adds a widget annotation to this field.
        /// </summary>
        /// <param name="rect">The widget rectangle.</param>
        /// <returns>The created widget annotation.</returns>
        public HpdfWidgetAnnotation CreateWidget(HpdfRect rect)
        {
            EnsureCanCreateWidget();

            var widget = new HpdfWidgetAnnotation(_xref, rect);
            ConfigureWidget(widget);
            AddWidget(widget);
            return widget;
        }

        /// <summary>
        /// Gives a widget created by <see cref="CreateWidget"/> the appearance this field type needs.
        /// </summary>
        /// <param name="widget">The new widget, not yet added to the field.</param>
        protected virtual void ConfigureWidget(HpdfWidgetAnnotation widget)
        {
        }

        /// <summary>
        /// Called by <see cref="CreateWidget"/> before anything is created; a field type whose widgets
        /// must be created another way throws here.
        /// </summary>
        protected virtual void EnsureCanCreateWidget()
        {
        }

        /// <summary>
        /// Called by <see cref="AddWidget"/> before the widget is added; a field type throws here for
        /// a widget it cannot use.
        /// </summary>
        /// <param name="widget">The widget about to be added.</param>
        protected virtual void ValidateWidget(HpdfWidgetAnnotation widget)
        {
        }

        /// <summary>
        /// Records flags that define the kind of field (e.g. Radio, Combo): they are set now and
        /// kept by every later assignment of <see cref="Flags"/>.
        /// </summary>
        /// <param name="typeFlags">The flags to keep.</param>
        protected void SetTypeFlags(HpdfFieldFlags typeFlags)
        {
            _typeFlags = typeFlags;
            Flags = _flags;
        }

        /// <summary>
        /// Called after a widget is added, so the field can bring the widget in line with its current value.
        /// </summary>
        /// <param name="widget">The added widget.</param>
        protected virtual void OnWidgetAdded(HpdfWidgetAnnotation widget)
        {
        }

        /// <summary>
        /// Sets the default appearance string for this field.
        /// Format: /FontName FontSize Tf R G B rg
        /// Example: /Helv 12 Tf 0 0 0 rg
        /// </summary>
        /// <param name="appearance">The default appearance string.</param>
        public void SetDefaultAppearance(string appearance)
        {
            _defaultAppearance = string.IsNullOrEmpty(appearance) ? null : appearance;
            _dict.Remove("DA");
            if (_defaultAppearance != null)
            {
                _dict.Add("DA", new HpdfString(_defaultAppearance));
            }
        }

        /// <summary>
        /// Generates the appearance streams of the field's widgets from the field's current state.
        /// Called by the form when the document is saved. Fields whose widgets draw their own
        /// appearances (buttons, signatures) do nothing.
        /// </summary>
        /// <param name="formAppearance">The form's default appearance, used when the field has none.</param>
        /// <param name="fonts">The form fonts the default appearance refers to.</param>
        internal virtual void GenerateAppearances(HpdfDefaultAppearance? formAppearance, HpdfFormFonts fonts)
        {
        }

        /// <summary>
        /// Finds the default appearance of the field (its own or the form's) and the font it refers to.
        /// </summary>
        /// <returns>False if there is no valid default appearance or its font is not a standard font name.</returns>
        private protected bool TryResolveAppearance(HpdfDefaultAppearance? formAppearance, HpdfFormFonts fonts,
            [NotNullWhen(true)] out HpdfDefaultAppearance? appearance, [NotNullWhen(true)] out HpdfFont? font)
        {
            System.ArgumentNullException.ThrowIfNull(fonts);
            appearance = HpdfDefaultAppearance.Parse(_defaultAppearance) ?? formAppearance;
            font = appearance is null ? null : fonts.Resolve(appearance.FontName);
            return appearance != null && font != null;
        }

        /// <summary>
        /// Updates the Kids array in the field dictionary.
        /// </summary>
        private void UpdateKidsArray()
        {
            _dict.Remove("Kids");
            if (_widgets.Count > 0)
            {
                var kidsArray = new HpdfArray();
                foreach (var widget in _widgets)
                {
                    kidsArray.Add(widget.Dict);
                }
                _dict.Add("Kids", kidsArray);
            }
        }
    }
}
