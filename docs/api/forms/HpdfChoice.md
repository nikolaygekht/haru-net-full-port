# HpdfChoice Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

**Inherits:** [HpdfField](HpdfField.md)

---

## Purpose

`HpdfChoice` is a choice field (`/FT /Ch`) that lets the user pick from a list of options. It is a **combo box**
(drop-down list, optionally editable) by default, or a **list box** (a scrollable list, optionally with several
selected options) when created with `isComboBox: false`. When the document is saved, every widget gets an appearance
stream: a combo box shows the selected value, a list box shows the options with the selected ones highlighted.

---

## Getting an Instance

Create the field with its constructor, add the options, select a value, give the field a widget on a page and add it
to the document's form. See the [Forms Guide](../../guides/FormsGuide.md) for the overall procedure.

**Declaration:**
```csharp
public HpdfChoice(HpdfXref xref, string name, bool isComboBox = true)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). `null` throws `HpdfException` (`InvalidParameter`). |
| name | `string` | The partial field name (`/T`), stored as a PDF text string (any Unicode characters). An empty or `null` name writes no `/T` entry. |
| isComboBox | `bool` | `true` (default) for a combo box, which sets `HpdfFieldFlags.Combo`; `false` for a list box (no flags) |

A new choice field has no options and no value. The `Combo` flag of a combo box is kept by every later assignment of
`Flags`.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

// Combo box
var country = new HpdfChoice(pdf.Xref, "Country");
country.AddOptions("Canada", "Mexico", "USA");
country.SetValue("USA");
page.AddWidgetAnnotation(country.CreateWidget(new HpdfRect(150, 440, 300, 460)));
form.AddField(country);

// List box with two selected options
var skills = new HpdfChoice(pdf.Xref, "Skills", isComboBox: false);
skills.SetMultiSelect(true);
skills.AddOptions("C#", "Python", "JavaScript", "Java");
skills.SetSelectedValues("C#", "Java");
page.AddWidgetAnnotation(skills.CreateWidget(new HpdfRect(150, 340, 300, 420)));
form.AddField(skills);

pdf.SaveToFile("choice.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Options](#property-options) | `IReadOnlyList<string>` | The options added so far, in order |

`Dict`, `FieldType`, `Flags` and `Widgets` are inherited from [HpdfField](HpdfField.md); see
[Inherited Members](#inherited-members).

---

## Methods

| Name | Description |
|------|-------------|
| [AddOption()](#method-addoption) | Adds one option |
| [AddOptions()](#method-addoptions) | Adds several options |
| [ClearOptions()](#method-clearoptions) | Removes all options and their selection |
| [SetSelectedValues()](#method-setselectedvalues) | Selects one or several options; no values clears the selection |
| [SetValue()](#method-setvalue) | Selects one option; `null` clears the selection |
| [SetEditable()](#method-seteditable) | Combo boxes only: allows a typed value that is not one of the options |
| [SetMultiSelect()](#method-setmultiselect) | List boxes only: allows several selected options |
| [SetSort()](#method-setsort) | Turns the `Sort` flag on or off |
| [SetReadOnly()](#method-setreadonly) | Turns the `ReadOnly` flag on or off |
| [SetRequired()](#method-setrequired) | Turns the `Required` flag on or off |

### Inherited Members

| Name | Description |
|------|-------------|
| [Dict](HpdfField.md#property-dict) | The underlying field dictionary |
| [FieldType](HpdfField.md#property-fieldtype) | Always `HpdfFieldType.Ch` |
| [Flags](HpdfField.md#property-flags) | All field flags (`/Ff`); a combo box always includes `Combo`, even when assigned. Add flags with a bitwise OR to keep the others |
| [Widgets](HpdfField.md#property-widgets) | The widgets of the field |
| [SetDefaultValue()](HpdfField.md#method-setdefaultvalue) | Sets the value restored by a form reset (`/DV`); not checked against the options |
| [CreateWidget()](HpdfField.md#method-createwidget) | Creates a widget for the field; place it with `page.AddWidgetAnnotation` |
| [AddWidget()](HpdfField.md#method-addwidget) | Adds a widget created by you |
| [SetDefaultAppearance()](HpdfField.md#method-setdefaultappearance) | Sets the font, size and color of this field (`/DA`) |

---

## Detailed Descriptions

#### Property: *Options*

**Declaration:**
```csharp
public IReadOnlyList<string> Options { get; }
```

**Description:**
The options added with `AddOption` and `AddOptions`, in the order they were added. They are written to the field as
`/Opt`, an array of PDF text strings (the displayed text is also the exported value). The list is a read-only view;
change it with the methods of the field.

**Example:**
```csharp
var color = new HpdfChoice(pdf.Xref, "Color");
color.AddOptions("Red", "Green", "Blue");
Console.WriteLine(color.Options.Count);   // 3
```

---

#### Method: *AddOption()*

**Declaration:**
```csharp
public void AddOption(string option)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| option | `string` | The text of the option; not `null` or empty |

**Return Value:**
`void`

**Description:**
Appends an option. A `null` or empty option throws `HpdfException` with `HpdfErrorCode.InvalidParameter`.
Duplicates are not checked.

**Example:**
```csharp
var title = new HpdfChoice(pdf.Xref, "Title");
title.AddOption("Mr.");
title.AddOption("Ms.");
title.AddOption("Dr.");
```

---

#### Method: *AddOptions()*

**Declaration:**
```csharp
public void AddOptions(params string[] options)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| options | `string[]` | The options to append |

**Return Value:**
`void`

**Description:**
Appends the options in order. Unlike `AddOption`, it does not throw: `null` and empty strings are skipped, and a
`null` or empty array does nothing.

**Example:**
```csharp
var month = new HpdfChoice(pdf.Xref, "Month");
month.AddOptions("January", "February", "March");
```

---

#### Method: *ClearOptions()*

**Declaration:**
```csharp
public void ClearOptions()
```

**Return Value:**
`void`

**Description:**
Removes all options and the `/Opt` entry, and the selection of the removed options. Only an editable combo box
([SetEditable(true)](#method-seteditable)) keeps a typed value that was not one of the options; any other choice field
is left without a value.

**Example:**
```csharp
var city = new HpdfChoice(pdf.Xref, "City");
city.AddOptions("Paris", "Rome");
city.SetValue("Rome");
city.ClearOptions();                 // also clears the selection "Rome"
city.AddOptions("Berlin", "Madrid");
city.SetValue("Madrid");
```

---

#### Method: *SetSelectedValues()*

**Declaration:**
```csharp
public void SetSelectedValues(params string[] values)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| values | `string[]` | The options to select, none of them `null`; no values (or `null`) clears the selection |

**Return Value:**
`void`

**Description:**
Sets the value `/V`: one value is stored as a text string, several values as an array of text strings. Every value
must be one of the [Options](#property-options) (exact, case-sensitive match), so add the options **before**
selecting. The exception is an editable combo box ([SetEditable(true)](#method-seteditable)), whose value can be any
text, as if the user had typed it. Several values require a list box with `SetMultiSelect(true)`.

All values are checked before anything changes: when an exception is thrown, the current selection stays as it was.

The selection is drawn when the document is saved, so the widgets can be created before or after it: a combo box shows
the first selected value (left-aligned), a list box highlights every selected option.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidOperation`) | Several values, but the field is not a list box with `MultiSelect` |
| `HpdfException` (`InvalidParameter`) | A value is `null` |
| `HpdfException` (`InvalidParameter`) | A value is not one of the options (except for an editable combo box) |

**Example:**
```csharp
var toppings = new HpdfChoice(pdf.Xref, "Toppings", isComboBox: false);
toppings.SetMultiSelect(true);
toppings.AddOptions("Cheese", "Ham", "Mushrooms", "Olives");
toppings.SetSelectedValues("Cheese", "Olives");
page.AddWidgetAnnotation(toppings.CreateWidget(new HpdfRect(150, 300, 300, 380)));
form.AddField(toppings);
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
| value | `string` | The option to select (any text for an editable combo box), or `null` to clear the selection |

**Return Value:**
`void`

**Description:**
`SetValue(value)` is `SetSelectedValues(value)`, and `SetValue(null)` clears the selection. The value must be one of
the options, unless the field is an editable combo box; see [SetSelectedValues()](#method-setselectedvalues).

**Example:**
```csharp
var language = new HpdfChoice(pdf.Xref, "Language");
language.AddOptions("English", "Français", "Deutsch");
language.SetValue("Français");
```

---

#### Method: *SetEditable()*

**Declaration:**
```csharp
public void SetEditable(bool editable)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| editable | `bool` | `true` to let the user type a value that is not in the list |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Edit`; other flags are kept. Only for combo boxes: on a list box (no `Combo` flag) it
throws `HpdfException` with `HpdfErrorCode.InvalidOperation`. An editable combo box lets the user type a value, and
[SetValue()](#method-setvalue) and [SetSelectedValues()](#method-setselectedvalues) accept a value that is not one of
the options as well. Call it before setting such a value.

`SetEditable(false)` clears the value of a combo box whose value is not one of its options: a non-editable combo box
can only show an option. A value that is an option is kept.

**Example:**
```csharp
var company = new HpdfChoice(pdf.Xref, "Company");
company.AddOptions("Contoso", "Fabrikam");
company.SetEditable(true);
company.SetValue("Northwind");   // not an option: allowed for an editable combo box
```

---

#### Method: *SetMultiSelect()*

**Declaration:**
```csharp
public void SetMultiSelect(bool multiSelect)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| multiSelect | `bool` | `true` to allow several selected options |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.MultiSelect`; other flags are kept. Only for list boxes: on a combo box it throws
`HpdfException` with `HpdfErrorCode.InvalidOperation`. Call it before selecting several values with
`SetSelectedValues`.

`SetMultiSelect(false)` on a list box with several selected values keeps only the first of them.

**Example:**
```csharp
var days = new HpdfChoice(pdf.Xref, "Days", isComboBox: false);
days.SetMultiSelect(true);
days.AddOptions("Mon", "Tue", "Wed", "Thu", "Fri");
days.SetSelectedValues("Mon", "Wed");
```

---

#### Method: *SetSort()*

**Declaration:**
```csharp
public void SetSort(bool sort)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| sort | `bool` | `true` to set the `Sort` flag |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.Sort`; other flags are kept. The library does not sort anything: `Options`, `/Opt` and
the generated list box appearance keep the order in which the options were added. Add the options in the order you
want to show.

**Example:**
```csharp
var fruit = new HpdfChoice(pdf.Xref, "Fruit");
fruit.AddOptions("Apple", "Banana", "Cherry");   // already sorted
fruit.SetSort(true);
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
| readOnly | `bool` | `true` if the user may not change the selection |

**Return Value:**
`void`

**Description:**
Sets or clears `HpdfFieldFlags.ReadOnly`; other flags are kept.

**Example:**
```csharp
var region = new HpdfChoice(pdf.Xref, "Region");
region.AddOptions("North", "South");
region.SetValue("North");
region.SetReadOnly(true);
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
var state = new HpdfChoice(pdf.Xref, "State");
state.SetRequired(true);
```

---

## Remarks

- **Generated appearance.** On save, each widget is drawn with the font, size and color of the default appearance
  (the field's own, or else the form's). A combo box draws the first selected value on one line, left-aligned and
  centered vertically; a list box draws the options from the top and highlights the selected ones. Font size `0`
  means auto size (fit to the field for a combo box, 12 points for a list box). Only WinAnsi (Windows-1252) text can
  be drawn; the stored options and values may use any characters. An unknown font name in the default appearance
  leaves the widgets without an appearance stream.
- **Not supported:** export values that differ from the displayed text (`/Opt` pairs), the top index of a scrolled
  list box (`/TI`), the selected indices (`/I`), text alignment (`/Q`) and `CommitOnSelChange`.
- **Order of calls.** Add the options before calling `SetValue` or `SetSelectedValues`, call `SetMultiSelect(true)`
  before selecting several values, and `SetEditable(true)` before setting a value that is not an option.

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields
- [HpdfFieldFlags](HpdfFieldFlags.md) - `Combo`, `Edit`, `Sort`, `MultiSelect` and the common flags
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - the widget that places the field on a page
- [HpdfAcroForm](HpdfAcroForm.md) - the document's form
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
