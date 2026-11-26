# ButtonElementType Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

---

## Purpose

`ButtonElementType` selects the kind of button appearance that
[HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances) and
[HpdfAppearanceFactory.CreateAppearance()](HpdfAppearanceFactory.md#method-createappearance) produce: a check mark
in a square for checkboxes, or a dot in a circle for radio buttons. It is needed only for widgets created with the
`HpdfWidgetAnnotation` constructor; `HpdfCheckbox.CreateWidget()` and `HpdfRadioButton.CreateOption()` choose the
type themselves.

---

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Checkbox` | 0 | Square frame; the "on" state draws a black check mark ([HpdfCheckboxAppearance](HpdfCheckboxAppearance.md)). Caption `4` in ZapfDingbats |
| `RadioButton` | 1 | Round frame; the "on" state draws a black filled circle of half the frame size ([HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md)). Caption `l` in ZapfDingbats |

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Forms.Appearance;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var accept = new HpdfCheckbox(pdf.Xref, "Accept");
var widget = new HpdfWidgetAnnotation(pdf.Xref, new HpdfRect(50, 700, 65, 715));
widget.SetButtonAppearances(ButtonElementType.Checkbox, "Yes", "Off");
accept.AddWidget(widget);
page.AddWidgetAnnotation(widget);
form.AddField(accept);

pdf.SaveToFile("button-type.pdf");
```

---

## Related Types

- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - `SetButtonAppearances()`
- [HpdfAppearanceFactory](HpdfAppearanceFactory.md) - Maps the value to an appearance generator
- [IElementAppearance](IElementAppearance.md) - The generator interface
- [HpdfCheckbox](HpdfCheckbox.md), [HpdfRadioButton](HpdfRadioButton.md) - The button fields

---

[Back to Index](../../INDEX.md)
