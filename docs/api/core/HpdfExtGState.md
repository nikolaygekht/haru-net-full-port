# HpdfExtGState Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfExtGState` is an extended graphics state dictionary (`/Type /ExtGState`): a set of graphics state parameters
that a page applies in one step with the `gs` operator. The library supports transparency (separate opacity for
filling and stroking) and blend modes. All of these need PDF 1.4, which the document version is raised to
automatically.

---

## Getting an Instance

Create the object with its constructor, passing the document's cross-reference table and a resource name of your
choice; then apply it on a page with `page.SetExtGState(...)` from
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md).

**Constructor declaration:**
```csharp
public HpdfExtGState(HpdfXref xref, string localName)
```

| Name | Type | Description |
|------|------|-------------|
| xref | `HpdfXref` | The cross-reference table of the document (`pdf.Xref`). The dictionary is added to it, so it is written once to the file however often it is used. |
| localName | `string` | The resource name under which pages refer to the state (for example `"GS1"`); see [Resource names](#resource-names) |

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `xref` is `null`, or `localName` is `null` or empty |

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A solid red square
page.SetRgbFill(1, 0, 0);
page.Rectangle(100, 500, 200, 200);
page.Fill();

// A blue square at 50 % opacity, overlapping the red one
var halfTransparent = new HpdfExtGState(pdf.Xref, "GS_Half");
halfTransparent.SetAlphaFill(0.5f);

page.GSave();
page.SetExtGState(halfTransparent);
page.SetRgbFill(0, 0, 1);
page.Rectangle(200, 400, 200, 200);
page.Fill();
page.GRestore();   // back to full opacity

pdf.SaveToFile("transparency.pdf");   // PDF 1.4
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](#property-dict) | `HpdfDict` | The underlying `/ExtGState` dictionary |
| [LocalName](#property-localname) | `string` | The resource name used on pages |

---

## Methods

| Name | Description |
|------|-------------|
| [SetAlphaStroke()](#method-setalphastroke) | Sets the opacity of stroking operations (`/CA`) |
| [SetAlphaFill()](#method-setalphafill) | Sets the opacity of filling operations, text and images (`/ca`) |
| [SetBlendMode()](#method-setblendmode) | Sets the blend mode (`/BM`) |

---

## Detailed Descriptions

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The dictionary written to the file. It always contains `/Type /ExtGState`, plus the entries set by the methods of
this class.

---

#### Property: *LocalName*

**Declaration:**
```csharp
public string LocalName { get; }
```

**Description:**
The name given to the constructor. `SetExtGState` adds the dictionary to the page's `/Resources /ExtGState` under
this name and writes `/LocalName gs` into the content stream.

---

#### Method: *SetAlphaStroke()*

**Declaration:**
```csharp
public void SetAlphaStroke(float alpha)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| alpha | `float` | Opacity from `0` (fully transparent) to `1` (opaque) |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`ExtGStateOutOfRange`) | `alpha` is less than 0, greater than 1 or `NaN` |

**Description:**
Writes the constant stroking opacity `/CA`: lines and outlines drawn with `Stroke()` while the state is active are
blended with what is behind them. Raises the document version to at least 1.4.

**Example:**
```csharp
var faintLines = new HpdfExtGState(pdf.Xref, "GS_Lines30");
faintLines.SetAlphaStroke(0.3f);

page.GSave();
page.SetExtGState(faintLines);
page.SetLineWidth(10);
page.MoveTo(100, 100);
page.LineTo(400, 400);
page.Stroke();
page.GRestore();
```

---

#### Method: *SetAlphaFill()*

**Declaration:**
```csharp
public void SetAlphaFill(float alpha)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| alpha | `float` | Opacity from `0` (fully transparent) to `1` (opaque) |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`ExtGStateOutOfRange`) | `alpha` is less than 0, greater than 1 or `NaN` |

**Description:**
Writes the constant non-stroking opacity `/ca`. It applies to filled paths, filled text and images, so it is also the
way to draw a whole image semi-transparently (see [HpdfImage](HpdfImage.md)). Raises the document version to at
least 1.4.

**Example:**
```csharp
var watermark = new HpdfExtGState(pdf.Xref, "GS_Watermark");
watermark.SetAlphaFill(0.2f);

page.GSave();
page.SetExtGState(watermark);
page.SetFontAndSize(pdf.GetFont("Helvetica-Bold"), 72);
page.TextOut(100, 400, "DRAFT");
page.GRestore();
```

---

#### Method: *SetBlendMode()*

**Declaration:**
```csharp
public void SetBlendMode(HpdfBlendMode mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| mode | [HpdfBlendMode](../types/HpdfBlendMode.md) | How colors painted while the state is active combine with the colors behind them |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`ExtGStateOutOfRange`) | `mode` is `HpdfBlendMode.Eof` or another value outside the enumeration |

**Description:**
Writes `/BM` with the name of the enumeration value (`/Normal`, `/Multiply`, `/Screen`, `/Overlay`, `/Darken`,
`/Lighten`, `/ColorDodge`, `/ColorBurn`, `/HardLight`, `/SoftLight`, `/Difference` or `/Exclusion`). Raises the
document version to at least 1.4.

**Example:**
```csharp
var multiply = new HpdfExtGState(pdf.Xref, "GS_Multiply");
multiply.SetBlendMode(HpdfBlendMode.Multiply);

page.SetRgbFill(1, 1, 0);
page.Rectangle(100, 100, 200, 200);
page.Fill();

page.GSave();
page.SetExtGState(multiply);
page.SetRgbFill(0, 1, 1);
page.Rectangle(200, 200, 200, 200);
page.Fill();          // the overlap is green
page.GRestore();
```

---

## Remarks

### Applying and resetting a state

`SetExtGState` changes the current graphics state from that point of the content stream on; there is no method that
"removes" it. Enclose the drawing in `GSave()`/`GRestore()` as in the examples, or apply a second state with opaque
values (`SetAlphaFill(1)`, `HpdfBlendMode.Normal`).

### Resource names

A page refers to a state by its `LocalName`. Within one page each name must stand for one object:

- Applying the same `HpdfExtGState` object several times on a page (or on many pages) is fine; it is added to the
  page resources once.
- Applying a **different** `HpdfExtGState` object whose `LocalName` is already used on that page throws
  `HpdfException` with `HpdfErrorCode.DuplicateRegistration`. Give each state its own name, for example by numbering
  them (`"GS1"`, `"GS2"`, ...).

```csharp
var first = new HpdfExtGState(pdf.Xref, "GS1");
first.SetAlphaFill(0.5f);
var second = new HpdfExtGState(pdf.Xref, "GS1");   // same name, different object
second.SetAlphaFill(0.8f);

page.SetExtGState(first);
try
{
    page.SetExtGState(second);
}
catch (HpdfException ex) when (ex.ErrorCode == HpdfErrorCode.DuplicateRegistration)
{
    Console.WriteLine("Use a different local name for the second state");
}
```

### Changing a state after use

The dictionary is written when the document is saved, so it contains the values set last. Changing a state after it
has been applied changes every place it is used. Create a separate object for each combination of values.

### PDF version

Each method raises the document version to at least 1.4 through `HpdfXref.RequireVersion`. Setting
`HpdfDocument.Version` to a lower value has no effect on the saved file.

---

## Related Types

- [HpdfPageGraphics](../extensions/HpdfPageGraphics.md) - `SetExtGState`, `GSave`, `GRestore` and drawing operations
- [HpdfBlendMode](../types/HpdfBlendMode.md) - Blend mode values
- [HpdfImage](HpdfImage.md) - Image transparency (soft masks, whole-image opacity)
- [Graphics Guide](../../guides/GraphicsGuide.md)

---

[Back to Index](../../INDEX.md)
