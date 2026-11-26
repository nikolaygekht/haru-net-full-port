using System.Collections.Generic;
using Haru.Forms.Appearance;
using Haru.Objects;
using Haru.Xref;

namespace Haru.Forms
{
    /// <summary>
    /// Document-level AcroForm container.
    /// Manages all form fields in a PDF document.
    /// </summary>
    public class HpdfAcroForm
    {
        private const int SignaturesExist = 1;

        private readonly HpdfDict _dict;
        private readonly HpdfXref _xref;
        private readonly List<HpdfField> _fields;
        private readonly HpdfFormFonts _fonts;
        private string _defaultAppearance = string.Empty;
        private bool _needAppearances;

        /// <summary>
        /// Gets the underlying dictionary object.
        /// </summary>
        public HpdfDict Dict => _dict;

        /// <summary>
        /// Gets the list of fields in this form.
        /// </summary>
        public IReadOnlyList<HpdfField> Fields => _fields.AsReadOnly();

        /// <summary>
        /// Creates a new AcroForm.
        /// </summary>
        /// <param name="xref">The cross-reference table.</param>
        public HpdfAcroForm(HpdfXref xref)
        {
            _xref = xref ?? throw new HpdfException(HpdfErrorCode.InvalidParameter, "Xref cannot be null");
            _dict = new HpdfDict();
            _fields = new List<HpdfField>();
            _fonts = new HpdfFormFonts(_xref);
            _needAppearances = false;

            // Add to xref
            _xref.Add(_dict);

            // Set default appearance (Helvetica 12pt, black text)
            SetDefaultAppearance("/Helv 12 Tf 0 0 0 rg");
        }

        /// <summary>
        /// Sets the default appearance for form fields.
        /// This defines the default font and color for text fields.
        /// </summary>
        /// <param name="appearance">The default appearance string (e.g., "/Helv 12 Tf 0 0 0 rg").</param>
        public void SetDefaultAppearance(string appearance)
        {
            _defaultAppearance = appearance;
            _dict.Remove("DA");
            if (!string.IsNullOrEmpty(appearance))
            {
                _dict.Add("DA", new HpdfString(appearance));
            }
        }

        /// <summary>
        /// Gets the default appearance string.
        /// </summary>
        public string DefaultAppearance => _defaultAppearance;

        /// <summary>
        /// Sets whether PDF viewers should generate field appearances.
        /// If true, the viewer will generate appearances for fields.
        /// If false, the document must provide appearances.
        /// </summary>
        /// <param name="needAppearances">Whether appearances need to be generated.</param>
        public void SetNeedAppearances(bool needAppearances)
        {
            _needAppearances = needAppearances;
            _dict.Remove("NeedAppearances");
            if (needAppearances)
            {
                _dict.Add("NeedAppearances", new HpdfBoolean(true));
            }
        }

        /// <summary>
        /// Gets whether the form needs appearances to be generated.
        /// </summary>
        public bool NeedAppearances => _needAppearances;

        /// <summary>
        /// Adds a field to the form.
        /// </summary>
        /// <param name="field">The field to add.</param>
        public void AddField(HpdfField field)
        {
            if (field is null)
                throw new HpdfException(HpdfErrorCode.InvalidParameter, "Field cannot be null");

            _fields.Add(field);

            // Update the Fields array in the dictionary
            UpdateFieldsArray();
        }

        /// <summary>
        /// Sets the calculation order for fields.
        /// This determines the order in which field calculations are performed.
        /// </summary>
        /// <param name="fields">The fields in calculation order.</param>
        public void SetCalculationOrder(params HpdfField[] fields)
        {
            _dict.Remove("CO");
            if (fields != null && fields.Length > 0)
            {
                var coArray = new HpdfArray();
                foreach (var field in fields)
                {
                    if (field != null)
                    {
                        coArray.Add(field.Dict);
                    }
                }
                _dict.Add("CO", coArray);
            }
        }

        /// <summary>
        /// Updates the Fields array in the AcroForm dictionary.
        /// </summary>
        private void UpdateFieldsArray()
        {
            if (_fields.Count == 0)
            {
                _dict.Remove("Fields");
                return;
            }

            var fieldsArray = new HpdfArray();
            foreach (var field in _fields)
            {
                fieldsArray.Add(field.Dict);
            }

            // Remove existing Fields entry before adding new one
            _dict.Remove("Fields");
            _dict.Add("Fields", fieldsArray);

            UpdateSignatureFlags();
        }

        /// <summary>
        /// Sets /SigFlags bit 1 (SignaturesExist) when the form contains a signature field,
        /// which tells viewers to look for signature fields and offer signing.
        /// </summary>
        private void UpdateSignatureFlags()
        {
            _dict.Remove("SigFlags");
            if (_fields.Exists(field => field.FieldType == HpdfFieldType.Sig))
                _dict.Add("SigFlags", new HpdfNumber(SignaturesExist));
        }

        /// <summary>
        /// Prepares the AcroForm for writing to the PDF.
        /// This method should be called before saving the document.
        /// Text and choice fields get appearance streams that show their current values, and the
        /// fonts of the default appearance strings become the form's default resources (/DR),
        /// which viewers use when the user edits a field.
        /// </summary>
        internal void PrepareForWriting()
        {
            // Ensure Fields array is up to date
            UpdateFieldsArray();

            var formAppearance = HpdfDefaultAppearance.Parse(_defaultAppearance);
            if (formAppearance != null)
                _fonts.Resolve(formAppearance.FontName);

            // Viewers that redraw checkboxes and radio buttons draw their caption (/MK /CA) in ZapfDingbats
            if (_fields.Exists(field => field.FieldType == HpdfFieldType.Btn))
                _fonts.Resolve("ZaDb");

            foreach (var field in _fields)
                field.GenerateAppearances(formAppearance, _fonts);

            _dict.Remove("DR");
            if (!_fonts.IsEmpty)
                _dict.Add("DR", _fonts.CreateResources());
        }
    }
}
