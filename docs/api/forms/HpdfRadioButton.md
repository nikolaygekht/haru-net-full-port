# HpdfRadioButton Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Inherits:** [HpdfField](HpdfField.md)

---

## Purpose

`HpdfRadioButton` is a radio button group: one button field (`/FT /Btn` with the `Radio` flag) whose options are
widgets. Each option has its own state name (for example `"Male"`, `"Female"`), and at most one option is selected:
the field's value is the state name of the selected option, or `Off` when none is selected.

---

## Getting an Instance

Create one field for the whole group, create one widget per option with
[CreateOption()](#method-createoption), place **every** option widget on a page with `page.AddWidgetAnnotation`,
select an option and add the field to the form. See the [Forms Guide](../../guides/FormsGuide.md) for the overall
procedure.

**Declaration:**
```csharp
public HpdfRadioButton(HpdfXref xref, string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). `null` throws `HpdfException` (`InvalidParameter`). |
| name | `string` | The name of the group (`/T`), stored as a PDF text string (any Unicode characters). An empty or `null` name writes no `/T` entry. |

A new group has the `HpdfFieldFlags.Radio` flag, no options and the value `/V /Off` (nothing selected). The
`Radio` flag is kept by every later assignment of `Flags`.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var size = new HpdfRadioButton(pdf.Xref, "Size");
page.AddWidgetAnnotation(size.CreateOption("Small", new HpdfRect(150, 480, 165, 495)));
page.AddWidgetAnnotation(size.CreateOption("Medium", new HpdfRect(220, 480, 235, 495)));
page.AddWidgetAnnotation(size.CreateOption("Large", new HpdfRect(290, 480, 305, 495)));
size.SetSelected("Medium");          // after the options: the name must exist
size.SetDefaultSelected("Medium");
form.AddField(size);

pdf.SaveToFile("radio.pdf");
```

---

## Properties

`HpdfRadioButton` declares no properties of its own. It inherits `Dict`, `FieldType`, `Flags` and `Widgets`
from [HpdfField](HpdfField.md); see [Inherited Members](#inherited-members).

---

## Methods

| Name | Description |
|------|-------------|
| [CreateOption()](#method-createoption) | Creates an option widget with the standard radio button appearance |
| [AddOption()](#method-addoption) | Adds an option for a widget you created yourself |
| [SetSelected()](#method-setselected) | Selects an option by state name, or none with `"Off"` |
| [SetValue()](#method-setvalue) | Same as `SetSelected` |
| [SetDefaultSelected()](#method-setdefaultselected) | Sets the option selected after a form reset (`/DV`) |
| [SetDefaultValue()](#method-setdefaultvalue) | Same as `SetDefaultSelected` |
| [SetReadOnly()](#method-setreadonly) | Turns the `ReadOnly` flag on or off |
| [SetRequired()](#method-setrequired) | Turns the `Required` flag on or off |

### Inherited Members

| Name | Description |
|------|-------------|
| [Dict](HpdfField.md#property-dict) | The underlying field dictionary |
| [FieldType](HpdfField.md#property-fieldtype) | Always `HpdfFieldType.Btn` |
| [Flags](HpdfField.md#property-flags) | All field flags (`/Ff`); always includes `Radio`, even when assigned |
| [Widgets](HpdfField.md#property-widgets) | The option widgets, in the order they were added |
| [CreateWidget()](HpdfField.md#method-createwidget) | Throws `HpdfException` (`InvalidOperation`): use `CreateOption` |
| [AddWidget()](HpdfField.md#method-addwidget) | Accepts only the widgets of the group's options; any other widget throws `HpdfException` (`InvalidOperation`). Use `AddOption` |
| [SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance) | Sets `/DA`; not used by the radio button's own appearance |

---

## Detailed Descriptions

#### Method: *CreateOption()*

**Declaration:**
```csharp
public HpdfWidgetAnnotation CreateOption(string stateName, HpdfRect rect)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| stateName | `string` | The state name of the option (the value of the field when it is selected); not `null`, not empty, not `"Off"` and not the name of another option of the group |
| rect | `HpdfRect` | The position of the button on the page: `new HpdfRect(left, bottom, right, top)` in points |

**Return Value:**
The new [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md). Add it to a page with `page.AddWidgetAnnotation(widget)`;
an option that is not on a page is not shown.

**Description:**
Creates a widget with two appearance streams, one under the state name (frame and filled circle) and one under `Off`
(frame only), with a black border and the ZapfDingbats filled circle as caption (`/MK /CA`), then adds it with
[AddOption()](#method-addoption). The widget shows the selected state if `stateName` is already the selected value.

The state name is checked before any object is created, so a rejected name leaves nothing behind.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `stateName` is `null`, empty or `"Off"` |
| `HpdfException` (`InvalidParameter`) | The group already has an option named `stateName` |

**Example:**
```csharp
var payment = new HpdfRadioButton(pdf.Xref, "Payment");
var card = payment.CreateOption("Card", new HpdfRect(150, 440, 165, 455));
card.SetAppearanceCharacteristics(borderColor: new[] { 0f, 0f, 0.6f });  // blue frame
page.AddWidgetAnnotation(card);
page.AddWidgetAnnotation(payment.CreateOption("Cash", new HpdfRect(220, 440, 235, 455)));
payment.SetSelected("Card");
form.AddField(payment);
```

---

#### Method: *AddOption()*

**Declaration:**
```csharp
public void AddOption(string stateName, HpdfWidgetAnnotation widget)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| stateName | `string` | The state name of the option; not `null`, not empty, not `"Off"` and not the name of another option of the group |
| widget | [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) | The widget of the option; not already an option of this group and not a widget of another field |

**Return Value:**
`void`

**Description:**
Registers the widget as the option `stateName`, adds it to the field's widgets (`/Kids`) and sets its appearance
state (`/AS`) to the state name if that option is selected, or to `Off`. When an exception is thrown, the group is
unchanged.

The widget keeps the appearance it has. To get the standard radio button appearance, call
`widget.SetButtonAppearances(ButtonElementType.RadioButton, stateName)` before adding it (namespace
`Haru.Forms.Appearance`), or use [CreateOption()](#method-createoption). A widget that already has state appearances
(`/AP /N`) must have one named `stateName`, otherwise it could never show the option as selected; a widget without
appearances is accepted.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `stateName` is `null`, empty or `"Off"` |
| `HpdfException` (`InvalidParameter`) | The group already has an option named `stateName` |
| `HpdfException` (`InvalidParameter`) | `widget` is `null`, or already an option of this group |
| `HpdfException` (`InvalidParameter`) | `widget` has state appearances (`/AP /N`), but none named `stateName` |
| `HpdfException` (`InvalidOperation`) | `widget` belongs to another field |

**Example:**
```csharp
using Haru.Forms.Appearance;

var answer = new HpdfRadioButton(pdf.Xref, "Answer");
var yes = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(150, 400, 165, 415));
yes.SetButtonAppearances(ButtonElementType.RadioButton, "Yes");
answer.AddOption("Yes", yes);
page.AddWidgetAnnotation(yes);
form.AddField(answer);
```

---

#### Method: *SetSelected()*

**Declaration:**
```csharp
public void SetSelected(string stateName)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| stateName | `string` | The state name of the option to select; `"Off"` or `null` selects none |

**Return Value:**
`void`

**Description:**
Same as [SetValue()](#method-setvalue).

**Example:**
```csharp
var plan = new HpdfRadioButton(pdf.Xref, "Plan");
page.AddWidgetAnnotation(plan.CreateOption("Basic", new HpdfRect(150, 360, 165, 375)));
page.AddWidgetAnnotation(plan.CreateOption("Premium", new HpdfRect(220, 360, 235, 375)));
plan.SetSelected("Premium");
plan.SetSelected("Off");   // nothing selected
form.AddField(plan);
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
| value | `string` | The state name of the option to select; `"Off"` or `null` selects none |

**Return Value:**
`void`

**Description:**
Stores the value as a name (`/V /Premium`, `/V /Off`) and sets the appearance state of every option: the selected
option shows its state name, all other options show `Off`.

The value must be `"Off"`, `null` or the state name of an option that was already added (case-sensitive); any other
value throws `HpdfException` with `HpdfErrorCode.InvalidParameter`. Therefore select an option **after** creating
it. Options added later show the current selection automatically.

**Example:**
```csharp
var shipping = new HpdfRadioButton(pdf.Xref, "Shipping");
page.AddWidgetAnnotation(shipping.CreateOption("Standard", new HpdfRect(150, 320, 165, 335)));
shipping.SetValue("Standard");
form.AddField(shipping);
```

---

#### Method: *SetDefaultSelected()*

**Declaration:**
```csharp
public void SetDefaultSelected(string stateName)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| stateName | `string` | The state name of the option selected after a form reset; `"Off"` or `null` for none |

**Return Value:**
`void`

**Description:**
Writes the default value `/DV` as a name. The same validation as in [SetValue()](#method-setvalue) applies: the
option must already exist, otherwise `HpdfException` with `HpdfErrorCode.InvalidParameter` is thrown. The current
selection does not change.

**Example:**
```csharp
var contact = new HpdfRadioButton(pdf.Xref, "Contact");
page.AddWidgetAnnotation(contact.CreateOption("Email", new HpdfRect(150, 280, 165, 295)));
page.AddWidgetAnnotation(contact.CreateOption("Phone", new HpdfRect(220, 280, 235, 295)));
contact.SetDefaultSelected("Email");
form.AddField(contact);
```

---

#### Method: *SetDefaultValue()*

**Declaration:**
```csharp
public override void SetDefaultValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | The state name of the option selected after a form reset; `"Off"` or `null` for none |

**Return Value:**
`void`

**Description:**
Same as [SetDefaultSelected()](#method-setdefaultselected): the default value of a button field is written as a name
(`/DV /Email`), not as a text string.

---

#### Method: *SetReadOnly()*

**Declaration:**
```csharp
public void SetReadOnly(bool readOnly)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| readOnly | `bool` | `true` if the user may not change the selection |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.ReadOnly`; the `Radio` flag and other flags are kept.

**Example:**
```csharp
var level = new HpdfRadioButton(pdf.Xref, "Level");
level.SetReadOnly(true);
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
| required | `bool` | `true` if an option must be selected when the form is submitted |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Required`; the `Radio` flag and other flags are kept. The check is done by the
viewer, not by the library.

**Example:**
```csharp
var rating = new HpdfRadioButton(pdf.Xref, "Rating");
rating.SetRequired(true);
```

---

## Remarks

- **The group is one field.** Add only the `HpdfRadioButton` to the form (`form.AddField`), not the option widgets;
  but add every option widget to a page (`page.AddWidgetAnnotation`). Options can be on different pages.
- **`Off` is reserved** for "nothing selected" and cannot be a state name. Each state name is used by one option
  only.
- **Options only.** Every widget of the group is an option with a state name: create widgets with
  [CreateOption()](#method-createoption) or add them with [AddOption()](#method-addoption); the inherited
  `CreateWidget()` and `AddWidget()` reject other widgets.
- **Appearance.** The frame of an option is a black border by default;
  [HpdfWidgetAnnotation.SetAppearanceCharacteristics](HpdfWidgetAnnotation.md) replaces it and redraws both states.
  When a form contains a button field, the ZapfDingbats font (`ZaDb`) is added to the form's default resources on
  save, for viewers that redraw the buttons.
- **Flags.** The constructor sets `HpdfFieldFlags.Radio`, and assigning `Flags` keeps it. Use
  `radio.Flags |= HpdfFieldFlags.NoExport` to add a flag while keeping `ReadOnly` and `Required`.

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields
- [HpdfCheckbox](HpdfCheckbox.md) - a single on/off button
- [HpdfFieldFlags](HpdfFieldFlags.md) - the `Radio` flag and the flags set by `SetReadOnly` and `SetRequired`
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - the widget of each option
- [HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md) - draws the frame and the filled circle
- [ButtonElementType](ButtonElementType.md) - selects the radio button appearance for `SetButtonAppearances`
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
