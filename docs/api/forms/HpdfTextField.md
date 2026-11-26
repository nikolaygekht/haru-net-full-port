# HpdfTextField Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Inherits:** [HpdfField](HpdfField.md)

---

## Purpose

`HpdfTextField` is a form field (`/FT /Tx`) into which the user types text: a single line, several lines
(multiline) or a masked password. Use it for names, addresses, comments and any other free-text input.
When the document is saved, every widget of the field gets an appearance stream that shows the current value.

---

## Getting an Instance

Create the field with its constructor, give it a widget on a page and add it to the document's form.
See the [Forms Guide](../../guides/FormsGuide.md) for the overall procedure.

**Declaration:**
```csharp
public HpdfTextField(HpdfXref xref, string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). `null` throws `HpdfException` (`InvalidParameter`). |
| name | `string` | The partial field name (`/T`), stored as a PDF text string (any Unicode characters). An empty or `null` name writes no `/T` entry. |

A new text field has no value, no flags and no default appearance of its own; it uses the form's default
appearance (`"/Helv 12 Tf 0 0 0 rg"` unless changed with `HpdfAcroForm.SetDefaultAppearance`).

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var email = new HpdfTextField(pdf.Xref, "Email");
email.SetValue("jane@example.com");
email.SetMaxLength(60);
email.SetRequired(true);
page.AddWidgetAnnotation(email.CreateWidget(new HpdfRect(150, 700, 400, 720)));
form.AddField(email);

pdf.SaveToFile("text-field.pdf");
```

---

## Properties

`HpdfTextField` declares no properties of its own. It inherits `Dict`, `FieldType`, `Flags` and `Widgets`
from [HpdfField](HpdfField.md); see [Inherited Members](#inherited-members).

---

## Methods

| Name | Description |
|------|-------------|
| [SetValue()](#method-setvalue) | Sets the text of the field (`/V`); `null` removes it |
| [SetMaxLength()](#method-setmaxlength) | Limits the number of characters (`/MaxLen`) |
| [SetAlignment()](#method-setalignment) | Sets the text alignment (`/Q`): left, center or right |
| [SetMultiline()](#method-setmultiline) | Turns the `Multiline` flag on or off |
| [SetPassword()](#method-setpassword) | Turns the `Password` flag on or off |
| [SetReadOnly()](#method-setreadonly) | Turns the `ReadOnly` flag on or off |
| [SetRequired()](#method-setrequired) | Turns the `Required` flag on or off |

### Inherited Members

| Name | Description |
|------|-------------|
| [Dict](HpdfField.md#property-dict) | The underlying field dictionary |
| [FieldType](HpdfField.md#property-fieldtype) | Always `HpdfFieldType.Tx` |
| [Flags](HpdfField.md#property-flags) | All field flags (`/Ff`) |
| [Widgets](HpdfField.md#property-widgets) | The widgets of the field |
| [SetDefaultValue()](HpdfField.md#method-setdefaultvalue) | Sets the value restored by a form reset (`/DV`) |
| [CreateWidget()](HpdfField.md#method-createwidget) | Creates a widget for the field; place it with `page.AddWidgetAnnotation` |
| [AddWidget()](HpdfField.md#method-addwidget) | Adds a widget created by you |
| [SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance) | Sets the font, size and color of this field (`/DA`) |

---

## Detailed Descriptions

#### Method: *SetValue()*

**Declaration:**
```csharp
public override void SetValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | The text of the field, or `null` to remove the value |

**Return Value:**
`void`

**Description:**
Stores the value as `/V`, a PDF text string, so any language can be stored. The value is not checked against
`SetMaxLength`. The widgets are drawn when the document is saved, so the value can be set before or after the widgets
are created, and changing it after a save is shown by the next save.

The generated appearance draws the value with the font, size and color of the default appearance (the field's own,
or else the form's):

- a single-line field shows one line, centered vertically and aligned as set by `SetAlignment`;
- a multiline field wraps the text between words from the top of the field and starts a new line at each `\n`;
- a password field shows one asterisk per character;
- font size `0` (auto size) picks the largest size that fits the field.

The appearance uses the WinAnsi (Windows-1252) character set of the standard fonts: characters outside it (for
example Cyrillic or CJK) cannot be drawn by the generated appearance, but the stored value is correct and interactive
viewers show it. If the default appearance names a font that is not a standard font name (see the
[Forms Guide](../../guides/FormsGuide.md)), the widgets get no appearance stream.

**Example:**
```csharp
var name = new HpdfTextField(pdf.Xref, "Name");
name.SetValue("Jane Doe");
name.SetDefaultValue("");
page.AddWidgetAnnotation(name.CreateWidget(new HpdfRect(150, 700, 400, 720)));
form.AddField(name);
```

---

#### Method: *SetMaxLength()*

**Declaration:**
```csharp
public void SetMaxLength(int maxLength)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| maxLength | `int` | The maximum number of characters the user can type; must be greater than 0 |

**Return Value:**
`void`

**Description:**
Writes `/MaxLen`, which interactive viewers use to limit the input. A value of 0 or less throws `HpdfException` with
`HpdfErrorCode.InvalidParameter`. The limit applies to the user only: a longer value passed to `SetValue` is stored
and drawn in full.

**Example:**
```csharp
var zip = new HpdfTextField(pdf.Xref, "ZipCode");
zip.SetMaxLength(5);
```

---

#### Method: *SetAlignment()*

**Declaration:**
```csharp
public void SetAlignment(HpdfFieldAlignment alignment)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| alignment | [HpdfFieldAlignment](HpdfFieldAlignment.md) | `Left`, `Center` or `Right` |

**Return Value:**
`void`

**Description:**
Writes the quadding entry `/Q` (the numeric value of the enum) and aligns the text of the generated appearance
horizontally, in single-line and multiline fields. Without a call the field has no `/Q` entry, which means left
alignment. Use [HpdfFieldAlignment](HpdfFieldAlignment.md), not `Haru.Types.HpdfTextAlignment`: the two enums
have different values.

**Example:**
```csharp
var amount = new HpdfTextField(pdf.Xref, "Amount");
amount.SetAlignment(HpdfFieldAlignment.Right);
amount.SetValue("1,250.00");
```

---

#### Method: *SetMultiline()*

**Declaration:**
```csharp
public void SetMultiline(bool multiline)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| multiline | `bool` | `true` to allow several lines, `false` for a single line |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Multiline`; other flags are kept. In a multiline field the generated appearance starts
at the top of the field, wraps the text between words at the field width (inside a word only when the word alone is
too wide) and starts a new line at each line feed. With font size `0` the largest size from 12 points down that fits
all lines is used.

**Example:**
```csharp
var comments = new HpdfTextField(pdf.Xref, "Comments");
comments.SetMultiline(true);
comments.SetValue("First paragraph.\nSecond paragraph starts on a new line.");
page.AddWidgetAnnotation(comments.CreateWidget(new HpdfRect(150, 560, 400, 640)));
form.AddField(comments);
```

---

#### Method: *SetPassword()*

**Declaration:**
```csharp
public void SetPassword(bool password)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| password | `bool` | `true` to mask the characters, `false` for normal text |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Password`; other flags are kept. Viewers mask what the user types. The generated
appearance shows one asterisk for each character of the value (a surrogate pair or combining sequence counts as one
character). Note that the value itself is stored in the file as plain text.

**Example:**
```csharp
var pin = new HpdfTextField(pdf.Xref, "Pin");
pin.SetPassword(true);
pin.SetMaxLength(4);
```

---

#### Method: *SetReadOnly()*

**Declaration:**
```csharp
public void SetReadOnly(bool readOnly)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| readOnly | `bool` | `true` if the user may not change the value |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.ReadOnly`; other flags are kept.

**Example:**
```csharp
var id = new HpdfTextField(pdf.Xref, "CustomerId");
id.SetValue("C-10442");
id.SetReadOnly(true);
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
| required | `bool` | `true` if the field must have a value when the form is submitted |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Required`; other flags are kept. The check is done by the viewer when the form is
submitted; the library does not check it.

**Example:**
```csharp
var lastName = new HpdfTextField(pdf.Xref, "LastName");
lastName.SetRequired(true);
```

---

## Remarks

- The font, size and color come from the default appearance string: `field.SetDefaultAppearance("/Cour 0 Tf 0 g")`
  for one field, `form.SetDefaultAppearance(...)` for all fields. Size `0` means auto size.
- The frame (border, background) and rotation of a widget are set with
  [HpdfWidgetAnnotation.SetAppearanceCharacteristics](HpdfWidgetAnnotation.md); a text widget has no frame unless it
  is set. Rotated widgets draw their text rotated.
- Comb fields and rich text are not supported.

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields
- [HpdfFieldAlignment](HpdfFieldAlignment.md) - values for `SetAlignment`
- [HpdfFieldFlags](HpdfFieldFlags.md) - the flags set by `SetMultiline`, `SetPassword`, `SetReadOnly` and `SetRequired`
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - the widget that places the field on a page
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
