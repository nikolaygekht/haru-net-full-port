# HpdfFieldFlags Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

---

## Purpose

`HpdfFieldFlags` is a set of bit flags (`[Flags]`) that control the behavior of a form field: read-only, required,
multiline, password, combo box and so on. The flags of a field are written as its field flags entry `/Ff`.
Most flags are set by the methods of the field classes; all of them can be read and changed through the
[Flags](HpdfField.md#property-flags) property of [HpdfField](HpdfField.md).

---

## Values

| Name | Value | PDF bit | Description |
|------|-------|---------|-------------|
| `None` | 0 | - | No flags. A field whose flags are `None` has no `/Ff` entry |
| `ReadOnly` | 1 (`1 << 0`) | 1 | The user may not change the value |
| `Required` | 2 (`1 << 1`) | 2 | The field must have a value when the form is submitted |
| `NoExport` | 4 (`1 << 2`) | 3 | The field is not exported when the form is submitted |
| `Multiline` | 4096 (`1 << 12`) | 13 | Text field: the text may have several lines |
| `Password` | 8192 (`1 << 13`) | 14 | Text field: the characters are masked |
| `Radio` | 32768 (`1 << 15`) | 16 | Button field: a radio button group rather than a checkbox |
| `Pushbutton` | 65536 (`1 << 16`) | 17 | Button field: a pushbutton |
| `Combo` | 131072 (`1 << 17`) | 18 | Choice field: a combo box; without it, a list box |
| `Edit` | 262144 (`1 << 18`) | 19 | Combo box: the user may type a value that is not in the list |
| `Sort` | 524288 (`1 << 19`) | 20 | Choice field: the options should be sorted |
| `MultiSelect` | 2097152 (`1 << 21`) | 22 | List box: several options can be selected |

Other field flags of the PDF specification (for example comb fields, `DoNotScroll` or `NoToggleToOff`) are not
defined by this enum.

---

## Which Field Types Use Which Flags

| Flag | Applies to | Set by |
|------|------------|--------|
| `ReadOnly` | All fields | `SetReadOnly(bool)` of [HpdfTextField](HpdfTextField.md#method-setreadonly), [HpdfCheckbox](HpdfCheckbox.md#method-setreadonly), [HpdfRadioButton](HpdfRadioButton.md#method-setreadonly), [HpdfChoice](HpdfChoice.md#method-setreadonly), [HpdfSignature](HpdfSignature.md#method-setreadonly) |
| `Required` | All fields | `SetRequired(bool)` of [HpdfTextField](HpdfTextField.md#method-setrequired), [HpdfCheckbox](HpdfCheckbox.md#method-setrequired), [HpdfRadioButton](HpdfRadioButton.md#method-setrequired), [HpdfChoice](HpdfChoice.md#method-setrequired), [HpdfSignature](HpdfSignature.md#method-setrequired) |
| `NoExport` | All fields | `Flags` only |
| `Multiline` | Text fields | [HpdfTextField.SetMultiline()](HpdfTextField.md#method-setmultiline) |
| `Password` | Text fields | [HpdfTextField.SetPassword()](HpdfTextField.md#method-setpassword) |
| `Radio` | Button fields | The [HpdfRadioButton](HpdfRadioButton.md) constructor; kept by every `Flags` assignment |
| `Pushbutton` | Button fields | `Flags` only (the library has no pushbutton class; a derived field type can use [SetTypeFlags()](HpdfField.md#method-settypeflags)) |
| `Combo` | Choice fields | The [HpdfChoice](HpdfChoice.md) constructor with `isComboBox: true` (the default); kept by every `Flags` assignment |
| `Edit` | Combo boxes | [HpdfChoice.SetEditable()](HpdfChoice.md#method-seteditable) (throws on a list box) |
| `Sort` | Choice fields | [HpdfChoice.SetSort()](HpdfChoice.md#method-setsort) |
| `MultiSelect` | List boxes | [HpdfChoice.SetMultiSelect()](HpdfChoice.md#method-setmultiselect) (throws on a combo box) |

The setter methods turn one flag on or off and keep all other flags. The library also uses some flags itself:
the generated appearance of a text field depends on `Multiline` and `Password`, and that of a choice field on `Combo`;
`HpdfChoice.SetEditable` and `SetMultiSelect` check `Combo`, and `SetSelectedValues` checks `MultiSelect` (several
values) and `Combo` with `Edit` (an editable combo box accepts values that are not options).
The other flags are only written to the file for the viewer.

**Assigning `Flags` replaces all flags except `Radio` and `Combo`**: the flags that define the kind of field, set by
the constructors, are always kept. Use a bitwise OR to add a flag and a bitwise AND with the complement to remove
one, so the other flags stay set.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var notes = new HpdfTextField(pdf.Xref, "InternalNotes");
notes.SetMultiline(true);                    // sets Multiline
notes.SetReadOnly(true);                     // sets ReadOnly
notes.Flags |= HpdfFieldFlags.NoExport;      // no setter method: use the property
notes.Flags &= ~HpdfFieldFlags.ReadOnly;     // removes ReadOnly, keeps the others

bool isMultiline = notes.Flags.HasFlag(HpdfFieldFlags.Multiline);   // true

page.AddWidgetAnnotation(notes.CreateWidget(new HpdfRect(150, 600, 400, 680)));
form.AddField(notes);
pdf.SaveToFile("flags.pdf");                 // /Ff 4100 (Multiline + NoExport)
```

---

## Related Types

- [HpdfField](HpdfField.md) - base class of all fields, with the `Flags` property
- [HpdfFieldType](HpdfFieldType.md) - the field types the flags refine
- [HpdfTextField](HpdfTextField.md), [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md),
  [HpdfChoice](HpdfChoice.md), [HpdfSignature](HpdfSignature.md) - the field classes and their flag setters
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
