# HpdfAppearanceFactory Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Forms.Appearance`

---

## Purpose

`HpdfAppearanceFactory` is a static class that creates the button appearance generators
([IElementAppearance](IElementAppearance.md)). [HpdfWidgetAnnotation.SetButtonAppearances()](HpdfWidgetAnnotation.md#method-setbuttonappearances)
uses [CreateAppearance()](#method-createappearance) to draw checkbox and radio button widgets; call the factory
yourself only to draw button marks on your own canvas, such as a page.

---

## Getting an Instance

The class is static; call its methods directly.

---

## Example

```csharp
using Haru.Doc;
using Haru.Forms.Appearance;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A selected radio button with a large dot, drawn on the page
IElementAppearance radio = HpdfAppearanceFactory.CreateRadioButtonAppearance(fillRatio: 0.7f);

page.GSave();
page.Concat(1, 0, 0, 1, 100, 650);   // move the origin to the element's lower-left corner
radio.DrawFrame(page, 14, 14, new[] { 0f, 0f, 0f }, new[] { 1f, 1f, 1f });
radio.GenerateOnStateAppearance(page, 14, 14);
page.GRestore();

pdf.SaveToFile("radio-mark.pdf");
```

---

## Properties

The class has no properties.

---

## Methods

| Name | Description |
|------|-------------|
| [CreateAppearance()](#method-createappearance) | Creates the generator for a button element type |
| [CreateRadioButtonAppearance()](#method-createradiobuttonappearance) | Creates a radio button generator with a custom dot size |

---

## Detailed Descriptions

#### Method: *CreateAppearance()*

**Declaration:**
```csharp
public static IElementAppearance CreateAppearance(ButtonElementType elementType)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| elementType | `ButtonElementType` | `Checkbox` or `RadioButton` |

**Return Value:**
A new [HpdfCheckboxAppearance](HpdfCheckboxAppearance.md) for `Checkbox`, a new
[HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md) with a fill ratio of 0.5 for `RadioButton`.

**Description:**
This is the method `SetButtonAppearances()` uses, so it returns exactly the generators that draw the library's
checkbox and radio button widgets.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | `elementType` is not a `ButtonElementType` value |

**Example:**
```csharp
IElementAppearance checkbox = HpdfAppearanceFactory.CreateAppearance(ButtonElementType.Checkbox);
Console.WriteLine(checkbox.Caption);   // 4
```

---

#### Method: *CreateRadioButtonAppearance()*

**Declaration:**
```csharp
public static IElementAppearance CreateRadioButtonAppearance(float fillRatio = 0.5f)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| fillRatio | `float` | Diameter of the dot relative to the smaller side of the element; greater than 0 and at most 1 |

**Return Value:**
A new [HpdfRadioButtonAppearance](HpdfRadioButtonAppearance.md).

**Description:**
Creates a radio button generator with a dot of the given size. Widgets always use the default ratio 0.5:
`SetButtonAppearances()` has no parameter for it, so a custom ratio applies only to drawing you do yourself.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentOutOfRangeException` | `fillRatio` is 0 or less, or greater than 1 |

---

## Related Types

- [IElementAppearance](IElementAppearance.md) - The generator interface
- [ButtonElementType](ButtonElementType.md) - The element types
- [HpdfWidgetAnnotation](HpdfWidgetAnnotation.md) - `SetButtonAppearances()`

---

[Back to Index](../../INDEX.md)
