# HpdfAcroForm Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

---

## Purpose

`HpdfAcroForm` is the interactive form (AcroForm) of a document. It lists the form fields and holds the settings that
apply to all of them: the default appearance string (font, size and color of text and choice fields), the
`NeedAppearances` flag and the calculation order. A document has at most one form.

See the [Forms Guide](../../guides/FormsGuide.md) for a walkthrough of all field types.

---

## Getting an Instance

Get the form from the document:

| Method | Description |
|--------|-------------|
| `HpdfDocument.GetOrCreateAcroForm()` | Returns the document's form, creating it on the first call. A new form gets `NeedAppearances` turned on |
| `HpdfDocument.GetAcroForm()` | Returns the document's form, or `null` if `GetOrCreateAcroForm()` has not been called yet |

```csharp
var pdf = new HpdfDocument();
var form = pdf.GetOrCreateAcroForm();     // created now, NeedAppearances = true
var same = pdf.GetAcroForm();             // the same instance
```

The class has no public constructor: the form returned by `GetOrCreateAcroForm()` is the one written to the
document catalog when the document is saved.

When the document is saved, the form is prepared automatically:

- the `/Fields` array is rebuilt from the fields added with [AddField()](#method-addfield) (an empty array when no
  field was added);
- every text field and choice field gets appearance streams (`/AP`) that show its current value, drawn with the
  standard font named by its default appearance string (see [HpdfField](HpdfField.md#method-setdefaultappearance));
- the fonts of the default appearance strings become the form's default resources (`/DR`), plus `ZaDb`
  (ZapfDingbats) when the form contains checkboxes or radio buttons, and the fonts of the default appearance strings
  of free text annotations and of the overlay text of redaction annotations (by their resource names; a name of two
  different fonts makes saving throw);
- `/SigFlags` is set when the form contains a signature field, so viewers offer signing;
- the document version is raised to PDF 1.5 if it is lower.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

// All text and choice fields: Helvetica 10 pt, dark blue
form.SetDefaultAppearance("/Helv 10 Tf 0 0 0.5 rg");

var name = new HpdfTextField(pdf.Xref, "Name");
name.SetValue("Jane Doe");
page.AddWidgetAnnotation(name.CreateWidget(new HpdfRect(150, 700, 400, 720)));
form.AddField(name);

pdf.SaveToFile("form.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying AcroForm dictionary |
| [Fields](#property-fields) | `IReadOnlyList<HpdfField>` | The fields added to the form |
| [DefaultAppearance](#property-defaultappearance) | `string` | The form-wide default appearance string (`/DA`) |
| [NeedAppearances](#property-needappearances) | `bool` | Whether viewers are asked to redraw the fields |

---

## Methods

| Name | Description |
|------|-------------|
| [AddField()](#method-addfield) | Adds a field to the form |
| [SetDefaultAppearance()](#method-setdefaultappearance) | Sets the form-wide default appearance string |
| [SetNeedAppearances()](#method-setneedappearances) | Turns the `NeedAppearances` flag on or off |
| [SetCalculationOrder()](#method-setcalculationorder) | Sets the order in which viewers calculate fields |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The AcroForm dictionary that is written to the document catalog. Use it only for entries the class does not
manage; `/Fields`, `/DR` and `/SigFlags` are rebuilt every time the document is saved, and `/DA` and
`/NeedAppearances` are replaced by the setter methods.

---

#### Property: *Fields*

**Declaration:**
```csharp
public IReadOnlyList<HpdfField> Fields { get; }
```

**Description:**
The fields added with [AddField()](#method-addfield), in the order they were added. The list is a read-only view.

**Example:**
```csharp
foreach (var field in form.Fields)
    Console.WriteLine($"{field.FieldType}: {field.Widgets.Count} widget(s)");
```

---

#### Property: *DefaultAppearance*

**Declaration:**
```csharp
public string DefaultAppearance { get; }
```

**Description:**
The form-wide default appearance string, as passed to [SetDefaultAppearance()](#method-setdefaultappearance).
A new form has `"/Helv 12 Tf 0 0 0 rg"` (Helvetica 12 pt, black). After `SetDefaultAppearance(null)` the property
returns an empty string.

---

#### Property: *NeedAppearances*

**Declaration:**
```csharp
public bool NeedAppearances { get; }
```

**Description:**
`true` if the form asks interactive viewers to redraw the field appearances themselves. A form from
`GetOrCreateAcroForm()` starts with `true`.

---

#### Method: *AddField()*

**Declaration:**
```csharp
public void AddField(HpdfField field)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| field | `HpdfField` | The field to add |

**Return Value:**
`void`

**Description:**
Adds a field to the form's `/Fields` array. A field that is not added to the form is not part of the form, even if
its widgets are placed on a page. The method does not check for duplicates: adding the same field twice lists it twice.
The order of the calls does not matter: the field can be added before or after its value and widgets are set.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `field` is `null` |

**Example:**
```csharp
var agree = new HpdfCheckbox(pdf.Xref, "Agree");
page.AddWidgetAnnotation(agree.CreateWidget(new HpdfRect(50, 600, 65, 615)));
form.AddField(agree);
```

---

#### Method: *SetDefaultAppearance()*

**Declaration:**
```csharp
public void SetDefaultAppearance(string appearance)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| appearance | `string` | A default appearance string such as `"/Helv 12 Tf 0 0 0 rg"`; `null` or empty removes `/DA` |

**Return Value:**
`void`

**Description:**
Sets the default appearance used by every text field and choice field that has no default appearance of its own
(see [HpdfField.SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance)). The string is
`/FontName size Tf` followed by a color operator: `g` (gray), `rg` (RGB) or `k` (CMYK), with components from 0 to 1.
A font size of `0` means auto size.

The font name must be a standard font: the short names `Helv`, `HeBo`, `HeOb`, `HeBO`, `TiRo`, `TiBo`, `TiIt`,
`TiBI`, `Cour`, `CoBo`, `CoOb`, `CoBO`, `Symb`, `ZaDb`, or a full PostScript name such as `Helvetica` or
`Times-Roman`. The string is stored as given and not validated; a field whose default appearance has no valid
`Tf` operator or an unknown font name gets no generated appearance when the document is saved.

**Example:**
```csharp
form.SetDefaultAppearance("/TiRo 0 Tf 0 g");   // Times-Roman, auto size, black
```

---

#### Method: *SetNeedAppearances()*

**Declaration:**
```csharp
public void SetNeedAppearances(bool needAppearances)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| needAppearances | `bool` | `true` writes `/NeedAppearances true`; `false` removes the entry |

**Return Value:**
`void`

**Description:**
`NeedAppearances` asks interactive viewers to build the field appearances themselves instead of using the
appearance streams in the file. The library generates appearances for all field types (text and choice fields
when the document is saved, buttons and signature fields when their widgets are created), so the flag can be turned
off when those are sufficient. Keep it on for text that the generated appearances cannot show: they use the
WinAnsi (Windows-1252) character set of the standard fonts only.

**Example:**
```csharp
form.SetNeedAppearances(false);
```

---

#### Method: *SetCalculationOrder()*

**Declaration:**
```csharp
public void SetCalculationOrder(params HpdfField[] fields)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| fields | `HpdfField[]` | The fields in calculation order; `null` entries are skipped |

**Return Value:**
`void`

**Description:**
Writes the `/CO` array: the order in which a viewer recalculates fields that have calculation actions. Each call
replaces the previous order; calling it with no fields (or `null`) removes `/CO`. The library does not create
calculation actions itself, and it does not check that the fields belong to the form.

**Example:**
```csharp
var subtotal = new HpdfTextField(pdf.Xref, "Subtotal");
var total = new HpdfTextField(pdf.Xref, "Total");
form.AddField(subtotal);
form.AddField(total);
form.SetCalculationOrder(subtotal, total);
```

---

## Remarks

- A form without fields is still written to the catalog once `GetOrCreateAcroForm()` has been called; its `/Fields`
  entry is an empty array then.
- The generated appearances use the WinAnsi character set; field values in other scripts are stored correctly but
  are shown only by viewers that redraw the fields (`NeedAppearances` on).

---

## Related Types

- [HpdfField](HpdfField.md) - Base class of all fields
- [HpdfTextField](HpdfTextField.md), [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md),
  [HpdfChoice](HpdfChoice.md), [HpdfSignature](HpdfSignature.md) - Field types
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - Where a field appears on a page
- [HpdfDocument](../core/HpdfDocument.md) - `GetOrCreateAcroForm()`, `GetAcroForm()` and saving
- [Forms Guide](../../guides/FormsGuide.md)

---

[Back to Index](../../INDEX.md)
