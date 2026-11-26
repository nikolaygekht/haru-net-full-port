# HpdfCheckbox Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Inherits:** [HpdfField](HpdfField.md)

---

## Purpose

`HpdfCheckbox` is a button field (`/FT /Btn`) with two states: checked (`Yes`) and unchecked (`Off`).
Use it for yes/no answers such as "I agree to the terms". Its widgets draw their own frame and check mark,
so the box looks the same in every viewer and when printed.

---

## Getting an Instance

Create the field with its constructor, create its widget with `CreateWidget`, place the widget on a page and add the
field to the document's form. See the [Forms Guide](../../guides/FormsGuide.md) for the overall procedure.

**Declaration:**
```csharp
public HpdfCheckbox(HpdfXref xref, string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). `null` throws `HpdfException` (`InvalidParameter`). |
| name | `string` | The partial field name (`/T`), stored as a PDF text string (any Unicode characters). An empty or `null` name writes no `/T` entry. |

A new checkbox is unchecked: its value `/V` is the name `/Off`. It has no flags (neither `Radio` nor `Pushbutton`).

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var agree = new HpdfCheckbox(pdf.Xref, "AgreeToTerms");
agree.SetChecked(true);
agree.SetRequired(true);
page.AddWidgetAnnotation(agree.CreateWidget(new HpdfRect(50, 520, 65, 535)));
form.AddField(agree);

pdf.SaveToFile("checkbox.pdf");
```

---

## Properties

`HpdfCheckbox` declares no properties of its own. It inherits `Dict`, `FieldType`, `Flags` and `Widgets`
from [HpdfField](HpdfField.md); see [Inherited Members](#inherited-members).

---

## Methods

| Name | Description |
|------|-------------|
| [SetChecked()](#method-setchecked) | Checks or unchecks the box |
| [SetValue()](#method-setvalue) | Sets the state by name: `"Yes"` or `"Off"` |
| [SetDefaultChecked()](#method-setdefaultchecked) | Sets the state restored by a form reset (`/DV`) |
| [SetReadOnly()](#method-setreadonly) | Turns the `ReadOnly` flag on or off |
| [SetRequired()](#method-setrequired) | Turns the `Required` flag on or off |

### Inherited Members

| Name | Description |
|------|-------------|
| [Dict](HpdfField.md#property-dict) | The underlying field dictionary |
| [FieldType](HpdfField.md#property-fieldtype) | Always `HpdfFieldType.Btn` |
| [Flags](HpdfField.md#property-flags) | All field flags (`/Ff`) |
| [Widgets](HpdfField.md#property-widgets) | The widgets of the field |
| [CreateWidget()](HpdfField.md#method-createwidget) | Creates a widget with checked and unchecked appearances (see [Remarks](#remarks)) |
| [AddWidget()](HpdfField.md#method-addwidget) | Adds a widget created by you |
| [SetDefaultValue()](HpdfField.md#method-setdefaultvalue) | Stores `/DV` as a text string; use [SetDefaultChecked()](#method-setdefaultchecked) instead |
| [SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance) | Sets `/DA`; not used by the checkbox's own appearance |

---

## Detailed Descriptions

#### Method: *SetChecked()*

**Declaration:**
```csharp
public void SetChecked(bool value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `bool` | `true` to check the box, `false` to uncheck it |

**Return Value:**
`void`

**Description:**
Same as `SetValue("Yes")` or `SetValue("Off")`. The value can be set before or after the widgets are created: every
widget always shows the current state.

**Example:**
```csharp
var newsletter = new HpdfCheckbox(pdf.Xref, "Newsletter");
page.AddWidgetAnnotation(newsletter.CreateWidget(new HpdfRect(150, 620, 165, 635)));
newsletter.SetChecked(true);   // after the widget: the widget is updated
form.AddField(newsletter);
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
| value | `string` | `"Yes"` (checked), `"Off"` (unchecked) or `null` (unchecked) |

**Return Value:**
`void`

**Description:**
Stores the state as the name `/V /Yes` or `/V /Off` and sets the appearance state (`/AS`) of every widget to it.
The comparison is case-sensitive; any other value throws `HpdfException` with `HpdfErrorCode.InvalidParameter`
and leaves the field unchanged.

**Example:**
```csharp
var subscribed = new HpdfCheckbox(pdf.Xref, "Subscribed");
subscribed.SetValue("Yes");
```

---

#### Method: *SetDefaultChecked()*

**Declaration:**
```csharp
public void SetDefaultChecked(bool value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `bool` | `true` if the box is checked after a form reset |

**Return Value:**
`void`

**Description:**
Writes the default value `/DV` as the name `/Yes` or `/Off`. Viewers restore this state when the user resets the
form. It does not change the current value.

**Example:**
```csharp
var reminders = new HpdfCheckbox(pdf.Xref, "Reminders");
reminders.SetChecked(true);
reminders.SetDefaultChecked(true);
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
| readOnly | `bool` | `true` if the user may not change the state |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.ReadOnly`; other flags are kept.

**Example:**
```csharp
var verified = new HpdfCheckbox(pdf.Xref, "Verified");
verified.SetChecked(true);
verified.SetReadOnly(true);
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
Sets or clears `HpdfFieldFlags.Required`; other flags are kept. The check is done by the viewer, not by the library.

**Example:**
```csharp
var consent = new HpdfCheckbox(pdf.Xref, "Consent");
consent.SetRequired(true);
```

---

## Remarks

- **Appearance.** A widget made by `CreateWidget` gets two appearance streams, `Yes` (frame and check mark) and
  `Off` (frame only), and the caption of the appearance characteristics (`/MK /CA`) is the ZapfDingbats check mark.
  The frame is a black border; [HpdfWidgetAnnotation.SetAppearanceCharacteristics](HpdfWidgetAnnotation.md) replaces
  it with your colors and redraws both states. When a form contains a button field, the ZapfDingbats font (`ZaDb`)
  is added to the form's default resources on save, for viewers that redraw the box.
- **Widgets created by you.** A widget passed to `AddWidget` gets the current state (`/AS`) but no appearance
  streams. Call `widget.SetButtonAppearances(ButtonElementType.Checkbox)` to give it the standard ones
  (see [ButtonElementType](ButtonElementType.md), namespace `Haru.Forms.Appearance`).
- **Default value.** The inherited `SetDefaultValue` writes `/DV` as a text string, while a checkbox state is a name.
  Use `SetDefaultChecked` for checkboxes.
- The state names are fixed: `Yes` and `Off`. Export values other than `Yes` are not supported.

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields
- [HpdfRadioButton](HpdfRadioButton.md) - a group of options of which one is selected
- [HpdfFieldFlags](HpdfFieldFlags.md) - the flags set by `SetReadOnly` and `SetRequired`
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - the widget that places the field on a page
- [HpdfCheckboxAppearance](HpdfCheckboxAppearance.md) - draws the frame and the check mark
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
