# HpdfFieldAlignment Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms`

---

## Purpose

`HpdfFieldAlignment` is the horizontal alignment of the text in a form field (the quadding entry `/Q`). It is passed
to [HpdfTextField.SetAlignment()](HpdfTextField.md#method-setalignment); the numeric value of each member is the
`/Q` value written to the field.

---

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Left` | 0 | Left-aligned text; the default (a field without `/Q` is left-aligned) |
| `Center` | 1 | Centered text |
| `Right` | 2 | Right-aligned text |

The generated appearance of a text field aligns its text accordingly, in single-line and multiline fields.
Choice fields have no alignment setting: a combo box always draws its value left-aligned.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var form = pdf.GetOrCreateAcroForm();

var total = new HpdfTextField(pdf.Xref, "Total");
total.SetAlignment(HpdfFieldAlignment.Right);   // writes /Q 2
total.SetValue("1,250.00");
page.AddWidgetAnnotation(total.CreateWidget(new HpdfRect(300, 700, 450, 720)));
form.AddField(total);

pdf.SaveToFile("alignment.pdf");
```

---

## Not the Same as HpdfTextAlignment

`Haru.Types.HpdfTextAlignment` aligns text printed on a page with
[HpdfPageText.TextRect](../extensions/HpdfPageText.md#textrect). It is a different enum with a different order and an
additional member:

| Member | `HpdfFieldAlignment` | `HpdfTextAlignment` |
|--------|----------------------|---------------------|
| Left | 0 | 0 |
| Right | 2 | 1 |
| Center | 1 | 2 |
| Justify | - | 3 |

Never cast one enum to the other: `(HpdfFieldAlignment)HpdfTextAlignment.Right` is `Center`. If you need both,
map the members by name:

```csharp
static HpdfFieldAlignment ToFieldAlignment(HpdfTextAlignment alignment) => alignment switch
{
    HpdfTextAlignment.Center => HpdfFieldAlignment.Center,
    HpdfTextAlignment.Right => HpdfFieldAlignment.Right,
    _ => HpdfFieldAlignment.Left   // Left and Justify
};
```

---

## Related Types

- [HpdfTextField](HpdfTextField.md) - the field that uses this enum in `SetAlignment`
- [HpdfPageText](../extensions/HpdfPageText.md) - `TextRect` and `HpdfTextAlignment` for page text
- [Forms Guide](../../guides/FormsGuide.md) - how to build forms

---

[Back to Index](../../INDEX.md)
