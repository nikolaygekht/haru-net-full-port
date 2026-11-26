# HpdfSignature Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Inherits:** [HpdfField](HpdfField.md)

---

## Purpose

`HpdfSignature` is an **unsigned** signature field (`/FT /Sig`): a place on the page where the user can later sign
the document in a PDF viewer that supports digital signatures. The library creates the empty field only; it does not
sign documents.

---

## Getting an Instance

Create the field with its constructor, create its widget with `CreateWidget`, place the widget on a page and add the
field to the document's form. See the [Forms Guide](../../guides/FormsGuide.md) for the overall procedure.

**Declaration:**
```csharp
public HpdfSignature(HpdfXref xref, string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). `null` throws `HpdfException` (`InvalidParameter`). |
| name | `string` | The partial field name (`/T`), stored as a PDF text string (any Unicode characters). An empty or `null` name writes no `/T` entry. |

A new signature field has no value (`/V`): an unsigned field must not have one.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var signature = new HpdfSignature(pdf.Xref, "Signature");
page.AddWidgetAnnotation(signature.CreateWidget(new HpdfRect(150, 250, 400, 300)));
form.AddField(signature);   // the form gets /SigFlags 1

pdf.SaveToFile("signature.pdf");
```

---

## Properties

`HpdfSignature` declares no properties of its own. It inherits `Dict`, `FieldType`, `Flags` and `Widgets`
from [HpdfField](HpdfField.md); see [Inherited Members](#inherited-members).

---

## Methods

| Name | Description |
|------|-------------|
| [SetReadOnly()](#method-setreadonly) | Turns the `ReadOnly` flag on or off |
| [SetRequired()](#method-setrequired) | Turns the `Required` flag on or off |
| [SetValue()](#method-setvalue) | Not supported: always throws |
| [SetDefaultValue()](#method-setdefaultvalue) | Not supported: always throws |

### Inherited Members

| Name | Description |
|------|-------------|
| [Dict](HpdfField.md#property-dict) | The underlying field dictionary |
| [FieldType](HpdfField.md#property-fieldtype) | Always `HpdfFieldType.Sig` |
| [Flags](HpdfField.md#property-flags) | All field flags (`/Ff`) |
| [Widgets](HpdfField.md#property-widgets) | The widgets of the field |
| [CreateWidget()](HpdfField.md#method-createwidget) | Creates a widget with a frame appearance (see [Remarks](#remarks)) |
| [AddWidget()](HpdfField.md#method-addwidget) | Adds a widget created by you; it keeps its own appearance |
| [SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance) | Sets `/DA`; not used by the frame appearance |

---

## Detailed Descriptions

#### Method: *SetReadOnly()*

**Declaration:**
```csharp
public void SetReadOnly(bool readOnly)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| readOnly | `bool` | `true` to set the `ReadOnly` flag, `false` to clear it |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.ReadOnly`; other flags are kept. How a viewer handles a read-only unsigned signature
field is up to the viewer.

**Example:**
```csharp
var witness = new HpdfSignature(pdf.Xref, "Witness");
witness.SetReadOnly(true);
```

---

#### Method: *SetRequired()*

**Declaration:**
```csharp
public void SetRequired(bool required)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| required | `bool` | `true` if the field must be signed before the form is submitted |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Required`; other flags are kept. The check is done by the viewer, not by the library.

**Example:**
```csharp
var applicant = new HpdfSignature(pdf.Xref, "Applicant");
applicant.SetRequired(true);
```

---

#### Method: *SetValue()*

**Declaration:**
```csharp
public override void SetValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | Not used |

**Return Value:**
`void`

**Description:**
Not supported. The value of a signature field is the signature dictionary that a viewer writes when the field is
signed; the library creates unsigned fields only, which must not have a value.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidOperation`) | Always |

---

#### Method: *SetDefaultValue()*

**Declaration:**
```csharp
public override void SetDefaultValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | Not used |

**Return Value:**
`void`

**Description:**
Not supported: a signature field has no default value.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidOperation`) | Always |

---

## Remarks

- **Appearance.** A widget made by `CreateWidget` gets a normal appearance that draws only its frame: a black border
  by default, so the empty field is visible in every viewer and where forms are not interactive (printing, previews).
  [HpdfWidgetAnnotation.SetAppearanceCharacteristics](HpdfWidgetAnnotation.md) replaces the border and background
  and redraws the frame:

  ```csharp
  var approval = new HpdfSignature(pdf.Xref, "Approval");
  var box = approval.CreateWidget(new HpdfRect(150, 180, 400, 230));
  box.SetAppearanceCharacteristics(
      borderColor: new[] { 0f, 0f, 0.6f },
      backgroundColor: new[] { 0.95f, 0.95f, 1f });
  page.AddWidgetAnnotation(box);
  form.AddField(approval);
  ```

  Signature fields get no generated text appearance when the document is saved.
- **Signature flags.** When a signature field is added to the form, the form gets `/SigFlags 1` (SignaturesExist),
  which tells viewers that the document contains signature fields.
- **No signing.** The library writes no signature dictionary, no byte range and no certificate data; signing is done
  by the viewer or another tool. Metadata such as reason, location or signer name belongs to the signature and cannot
  be set on the unsigned field.
- **Value.** The value of a signed field is a signature dictionary written by the viewer, so `SetValue` and
  `SetDefaultValue` throw `HpdfException` (`InvalidOperation`) instead of storing text.

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields
- [HpdfFieldType](HpdfFieldType.md) - `Sig`, the field type of signature fields
- [HpdfWidgetFrame](HpdfWidgetFrame.md) - draws the frame of the widget
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - the widget that places the field on a page
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form, which sets `/SigFlags`
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
