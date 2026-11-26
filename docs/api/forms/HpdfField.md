# HpdfField Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

---

## Purpose

`HpdfField` is the abstract base class of all form fields. A field holds the name, the value, the flags and the default
appearance of a form field; its widgets ([HpdfWidgetAnnotation](HpdfWidgetAnnotation.md)) are the rectangles where it
appears on pages. The members described here are shared by every field type.

---

## Getting an Instance

`HpdfField` is abstract. Create one of the derived field types with the document's cross-reference table
(`pdf.Xref`) and a field name:

| Type | Field |
|------|-------|
| [HpdfTextField](HpdfTextField.md) | Text input |
| [HpdfCheckbox](HpdfCheckbox.md) | Checkbox |
| [HpdfRadioButton](HpdfRadioButton.md) | Radio button group |
| [HpdfChoice](HpdfChoice.md) | Combo box or list box |
| [HpdfSignature](HpdfSignature.md) | Empty signature field |

Every field goes through the same steps: create the field, create a widget with [CreateWidget()](#method-createwidget)
and add it to a page with `HpdfPage.AddWidgetAnnotation()`, then add the field to the form with
[HpdfAcroForm.AddField()](HpdfAcroForm.md#method-addfield).

You can derive your own field type from `HpdfField` (see [Deriving a Field Type](#deriving-a-field-type)).

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

HpdfField field = new HpdfTextField(pdf.Xref, "Comment");
field.SetValue("Hello");
field.SetDefaultValue("");
field.SetDefaultAppearance("/Cour 10 Tf 0 g");
field.Flags |= HpdfFieldFlags.Required;

var widget = field.CreateWidget(new HpdfRect(50, 700, 300, 720));
page.AddWidgetAnnotation(widget);
form.AddField(field);

pdf.SaveToFile("field.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying field dictionary |
| [FieldType](#property-fieldtype) | `HpdfFieldType` | The field type (`Btn`, `Tx`, `Ch` or `Sig`) |
| [Flags](#property-flags) | `HpdfFieldFlags` | The field flags (`/Ff`) |
| [Widgets](#property-widgets) | `IReadOnlyList<HpdfWidgetAnnotation>` | The widgets of the field |

### Protected Fields (for derived types)

For classes deriving from `HpdfField`.

| Name | Type | Description |
|------|------|-------------|
| [_dict](#protected-fields) | `HpdfDict` | The field dictionary (same object as `Dict`) |
| [_xref](#protected-fields) | `HpdfXref` | The cross-reference table the field was created with |
| [_fieldType](#protected-fields) | `HpdfFieldType` | The field type (same value as `FieldType`) |
| [_widgets](#protected-fields) | `List<HpdfWidgetAnnotation>` | The widgets of the field |
| [_flags](#protected-fields) | `HpdfFieldFlags` | The stored flags (use `Flags` to change them) |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfField()](#constructor-hpdffield) | Protected constructor for derived field types |
| [SetValue()](#method-setvalue) | Sets the field value (`/V`) |
| [SetDefaultValue()](#method-setdefaultvalue) | Sets the value restored by a form reset (`/DV`) |
| [SetDefaultAppearance()](#method-setdefaultappearance) | Sets the field's own default appearance string (`/DA`) |
| [CreateWidget()](#method-createwidget) | Creates a widget for the field and adds it |
| [AddWidget()](#method-addwidget) | Adds a widget created elsewhere |
| [ConfigureWidget()](#method-configurewidget) | Protected: prepares a widget created by `CreateWidget()` |
| [OnWidgetAdded()](#method-onwidgetadded) | Protected: called after a widget is added |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The field dictionary. It is registered in the cross-reference table when the field is created and contains `/FT`
(field type), `/T` (the name, if not empty) and the entries written by the setter methods. Use it only for
entries the field classes do not manage.

---

#### Property: *FieldType*

**Declaration:**
```csharp
public HpdfFieldType FieldType { get; }
```

**Description:**
The field type given to the constructor, written as `/FT`. See [HpdfFieldType](HpdfFieldType.md).
Checkboxes and radio buttons are both `Btn`.

---

#### Property: *Flags*

**Declaration:**
```csharp
public HpdfFieldFlags Flags { get; set; }
```

**Description:**
The field flags (`/Ff`), a bit set of [HpdfFieldFlags](HpdfFieldFlags.md). Setting the property rewrites `/Ff`;
`HpdfFieldFlags.None` removes the entry. The derived types set flags through their own methods (`SetReadOnly`,
`SetMultiline`, ...) and in their constructors (`Radio` for radio buttons, `Combo` for combo boxes); combine flags
with `|=` and clear them with `&= ~` so these flags are kept.

**Example:**
```csharp
var notes = new HpdfTextField(pdf.Xref, "Notes");
notes.Flags |= HpdfFieldFlags.Multiline | HpdfFieldFlags.NoExport;
notes.Flags &= ~HpdfFieldFlags.NoExport;
```

---

#### Property: *Widgets*

**Declaration:**
```csharp
public IReadOnlyList<HpdfWidgetAnnotation> Widgets { get; }
```

**Description:**
The widgets added with [CreateWidget()](#method-createwidget) or [AddWidget()](#method-addwidget), in the order
they were added. The list is a read-only view; the widgets are written as the field's `/Kids` array.

---

#### Protected Fields

**Declaration:**
```csharp
protected readonly HpdfDict _dict;
protected readonly HpdfXref _xref;
protected readonly HpdfFieldType _fieldType;
protected readonly List<HpdfWidgetAnnotation> _widgets;
protected HpdfFieldFlags _flags;
```

**Description:**

| Field | Description |
|-------|-------------|
| `_dict` | The field dictionary, the same object as [Dict](#property-dict). Derived types write their own entries into it |
| `_xref` | The cross-reference table passed to the constructor. Register new PDF objects (streams, dictionaries) here, and pass it to new widgets |
| `_fieldType` | The field type passed to the constructor |
| `_widgets` | The widgets of the field. Add widgets with [AddWidget()](#method-addwidget), not by changing the list: `AddWidget()` also updates `/Kids` and the widget's `/Parent` |
| `_flags` | The current flags. Assigning `_flags` directly does not update `/Ff`; use the [Flags](#property-flags) property |

---

#### Constructor: *HpdfField()*

**Declaration:**
```csharp
protected HpdfField(HpdfXref xref, HpdfFieldType fieldType, string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The document's cross-reference table (`pdf.Xref`) |
| fieldType | `HpdfFieldType` | The field type, written as `/FT` |
| name | `string` | The partial field name, written as `/T`; `null` or empty writes no name |

**Description:**
Creates the field dictionary, registers it in the cross-reference table and writes `/FT` and `/T`. The flags start
as `None`. Field names should be unique within the form.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `xref` is `null` |

---

#### Method: *SetValue()*

**Declaration:**
```csharp
public virtual void SetValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | The field value; `null` removes `/V` |

**Return Value:**
`void`

**Description:**
The base implementation writes the value as a text string (`/V`). The derived types override it: a checkbox accepts
only `"Yes"` and `"Off"` and a radio button only its option names (both write a name and update the widgets'
appearance states), a choice field accepts only its options, and a text field also keeps the value for its
generated appearance.

---

#### Method: *SetDefaultValue()*

**Declaration:**
```csharp
public virtual void SetDefaultValue(string value)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| value | `string` | The value a form reset restores; `null` removes `/DV` |

**Return Value:**
`void`

**Description:**
Writes the value as a text string (`/DV`). Checkboxes and radio buttons have their own methods for the default
state (`SetDefaultChecked`, `SetDefaultSelected`), which write a name as button fields require.

---

#### Method: *SetDefaultAppearance()*

**Declaration:**
```csharp
public void SetDefaultAppearance(string appearance)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| appearance | `string` | A default appearance string such as `"/Helv 12 Tf 0 0 0 rg"`; `null` or empty removes it |

**Return Value:**
`void`

**Description:**
Sets the field's own default appearance (`/DA`): the font, size and color of its text. Without one, the field uses
the form's default appearance ([HpdfAcroForm.SetDefaultAppearance()](HpdfAcroForm.md#method-setdefaultappearance)).
The format is `/FontName size Tf` followed by a color operator (`g`, `rg` or `k`); size `0` means auto size.

When the document is saved, text fields and choice fields get appearance streams (`/AP`) drawn with this font.
The font must be a standard font, named by its short name (`Helv`, `HeBo`, `HeOb`, `HeBO`, `TiRo`, `TiBo`, `TiIt`,
`TiBI`, `Cour`, `CoBo`, `CoOb`, `CoBO`, `Symb`, `ZaDb`) or its full PostScript name (`Helvetica`, `Courier-Bold`,
...); the text is written in the WinAnsi character set. With an unknown font name or a string without a valid `Tf`
operator, the field's widgets get no `/AP`. The string itself is stored unchanged. The font is added to the form's
default resources (`/DR`).

Checkboxes, radio buttons and signature fields draw their appearances themselves; their default appearance string
is written but not used by the library.

**Example:**
```csharp
var code = new HpdfTextField(pdf.Xref, "Code");
code.SetDefaultAppearance("/CoBo 0 Tf 0.6 0 0 rg");   // Courier-Bold, auto size, dark red
```

---

#### Method: *CreateWidget()*

**Declaration:**
```csharp
public HpdfWidgetAnnotation CreateWidget(HpdfRect rect)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The widget rectangle, `new HpdfRect(left, bottom, right, top)` in page coordinates |

**Return Value:**
The new [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md), already added to the field.

**Description:**
Creates a widget, lets the field type prepare it ([ConfigureWidget()](#method-configurewidget): a checkbox gets its
check mark appearance, a signature field its frame) and adds it to the field ([AddWidget()](#method-addwidget)).
The widget still has to be placed on a page with `HpdfPage.AddWidgetAnnotation()`. A field can have several
widgets, for example the same value shown on several pages.

For radio buttons, use `HpdfRadioButton.CreateOption()` instead, which also gives the widget its state name.

**Example:**
```csharp
var total = new HpdfTextField(pdf.Xref, "Total");
page.AddWidgetAnnotation(total.CreateWidget(new HpdfRect(400, 100, 550, 120)));
form.AddField(total);
```

---

#### Method: *AddWidget()*

**Declaration:**
```csharp
public void AddWidget(HpdfWidgetAnnotation widget)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| widget | `HpdfWidgetAnnotation` | A widget that does not belong to a field yet |

**Return Value:**
`void`

**Description:**
Adds a widget created with the `HpdfWidgetAnnotation` constructor: the widget gets the field as its `/Parent`, the
field lists it in `/Kids`, and [OnWidgetAdded()](#method-onwidgetadded) runs (a checkbox sets the widget's
appearance state to its current value). Unlike [CreateWidget()](#method-createwidget), it does not call
[ConfigureWidget()](#method-configurewidget): give the widget its appearance yourself, for example with
`SetButtonAppearances()` for a checkbox.

A widget belongs to one field. Adding a widget that already has a parent (including adding it to the same field
twice) throws `ArgumentException` from the field dictionary, after the widget has been added to the field's list.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `widget` is `null` |
| `ArgumentException` | The widget already has a parent field |

**Example:**
```csharp
var check = new HpdfCheckbox(pdf.Xref, "Confirmed");
var checkWidget = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(50, 500, 65, 515));
checkWidget.SetAppearanceCharacteristics(borderColor: new[] { 0f, 0.4f, 0f });
checkWidget.SetButtonAppearances(ButtonElementType.Checkbox);   // "Yes" / "Off"
check.AddWidget(checkWidget);
check.SetChecked(true);
page.AddWidgetAnnotation(checkWidget);
form.AddField(check);
```

---

#### Method: *ConfigureWidget()*

**Declaration:**
```csharp
protected virtual void ConfigureWidget(HpdfWidgetAnnotation widget)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| widget | `HpdfWidgetAnnotation` | The new widget, not yet added to the field |

**Return Value:**
`void`

**Description:**
Called by [CreateWidget()](#method-createwidget) before the widget is added, so a field type can give its widgets the
appearance it needs. The base implementation does nothing. `HpdfCheckbox` calls
`SetButtonAppearances(ButtonElementType.Checkbox, "Yes", "Off")`, `HpdfSignature` calls `SetFrameAppearance()`.
It is not called for widgets passed to [AddWidget()](#method-addwidget).

---

#### Method: *OnWidgetAdded()*

**Declaration:**
```csharp
protected virtual void OnWidgetAdded(HpdfWidgetAnnotation widget)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| widget | `HpdfWidgetAnnotation` | The widget that was just added |

**Return Value:**
`void`

**Description:**
Called at the end of [AddWidget()](#method-addwidget) (and therefore also by [CreateWidget()](#method-createwidget)),
after the widget's `/Parent` and the field's `/Kids` are set, so the field can bring the widget in line with its
current value. The base implementation does nothing; `HpdfCheckbox` sets the widget's appearance state (`/AS`).

---

## Deriving a Field Type

A derived type passes the field type and name to the protected constructor, sets its flags, and prepares its
widgets in [ConfigureWidget()](#method-configurewidget). The appearances that text and choice fields get when the
document is saved are produced by library-internal code that derived types cannot override; a custom field type
draws its own appearance, for example with the widget's `SetFrameAppearance()` or `SetButtonAppearances()`.

```csharp
using Haru.Forms;
using Haru.Xref;

// A push button without an action: a framed box.
public class FramedPushButton : HpdfField
{
    public FramedPushButton(HpdfXref xref, string name)
        : base(xref, HpdfFieldType.Btn, name)
    {
        Flags = HpdfFieldFlags.Pushbutton;
    }

    protected override void ConfigureWidget(HpdfWidgetAnnotation widget)
    {
        widget.SetAppearanceCharacteristics(borderColor: new[] { 0.3f, 0.3f, 0.3f },
                                            backgroundColor: new[] { 0.85f, 0.85f, 0.85f });
        widget.SetFrameAppearance();
    }
}
```

```csharp
var button = new FramedPushButton(pdf.Xref, "Button");
page.AddWidgetAnnotation(button.CreateWidget(new HpdfRect(50, 400, 150, 420)));
form.AddField(button);
```

---

## Related Types

- [HpdfAcroForm](HpdfAcroForm.md) - The form that lists the fields
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - The widgets of a field
- [HpdfTextField](HpdfTextField.md), [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md),
  [HpdfChoice](HpdfChoice.md), [HpdfSignature](HpdfSignature.md) - Derived field types
- [HpdfFieldType](HpdfFieldType.md), [HpdfFieldFlags](HpdfFieldFlags.md) - Field type and flags
- [Forms Guide](../../guides/FormsGuide.md)

---

[Back to Index](../../INDEX.md)
