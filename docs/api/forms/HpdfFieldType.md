# HpdfFieldType Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

---

## Purpose

`HpdfFieldType` is the type of a form field, written as the field type entry `/FT` of the field dictionary.
Each field class sets it in its constructor; read it with the [FieldType](HpdfField.md#property-fieldtype)
property of [HpdfField](HpdfField.md), for example to handle the fields of a form by type.

---

## Values

| Name | Value | `/FT` | Description | Field classes |
|------|-------|-------|-------------|---------------|
| `Btn` | 0 | `/Btn` | Button field: checkbox, radio button group or pushbutton | [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md) |
| `Tx` | 1 | `/Tx` | Text field: single-line, multiline or password input | [HpdfTextField](HpdfTextField.md) |
| `Ch` | 2 | `/Ch` | Choice field: combo box or list box | [HpdfChoice](HpdfChoice.md) |
| `Sig` | 3 | `/Sig` | Signature field | [HpdfSignature](HpdfSignature.md) |

Checkboxes and radio buttons share the type `Btn`; they differ in the `Radio` flag of
[HpdfFieldFlags](HpdfFieldFlags.md). The library has no pushbutton field class.

The type is fixed when the field is created and cannot be changed. The form uses it as well: a `Btn` field adds the
ZapfDingbats font to the form's default resources, and a `Sig` field sets `/SigFlags`
(see [HpdfAcroForm](HpdfAcroForm.md)).

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var form = pdf.GetOrCreateAcroForm();
form.AddField(new HpdfTextField(pdf.Xref, "Name"));
form.AddField(new HpdfCheckbox(pdf.Xref, "Newsletter"));
form.AddField(new HpdfSignature(pdf.Xref, "Signature"));

foreach (var field in form.Fields)
{
    string kind = field.FieldType switch
    {
        HpdfFieldType.Tx => "text",
        HpdfFieldType.Btn => "button",
        HpdfFieldType.Ch => "choice",
        HpdfFieldType.Sig => "signature",
        _ => "unknown"
    };
    Console.WriteLine(kind);
}
```

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields, with the `FieldType` property
- [HpdfFieldFlags](HpdfFieldFlags.md) - flags that refine the field type (radio, combo box, multiline, ...)
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
