# Hpdf3DCommentNote Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

**Base class:** [Hpdf3DMeasure](Hpdf3DMeasure.md)

---

## Purpose

`Hpdf3DCommentNote` is a comment note in a 3D model (`/Type /3DMeasure /Subtype /3DC`; Adobe ExtensionLevel 3): a text
placed in the model with a leader line to a point of the model. It moves with the model when the reader rotates it.
Use it for remarks on a part, such as "Drill after assembly", that should stay attached to the place they are about.

---

## Getting an Instance

Comment notes are created by the model:

```csharp
public Hpdf3DCommentNote CreateCommentNote(HpdfPoint3D anchor, HpdfPoint3D textPosition)   // Hpdf3DModel
```

| Name | Type | Description |
|------|------|-------------|
| anchor | `HpdfPoint3D` | The point the leader line points at (`/A1`), in model coordinates |
| textPosition | `HpdfPoint3D` | The position of the text (`/TP`), in model coordinates |

Creating one declares Adobe ExtensionLevel 3 and raises the document version to PDF 1.7. Add it to the views that
show it with [Hpdf3DView.AddMeasure()](Hpdf3DView.md#method-addmeasure), and list it among the comments with
`HpdfPage.CreateProjectionAnnotation()`.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate is NaN or infinite |

---

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var model = pdf.Load3DModel("part.prc");
var view = model.CreateView("Isometric");
view.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(1, -1, 1), 1000, 0);   // a PRC model needs a camera
page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");

var note = model.CreateCommentNote(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(20, 20, 20));
note.SetName("Origin");
note.SetText("The origin of the model");
note.SetTextBoxSize(120, 30);
note.SetColor(0.8f, 0f, 0f);
view.AddMeasure(note);
page.CreateProjectionAnnotation("The origin of the model", note);

pdf.SaveToFile("comment-note.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Dict](Hpdf3DMeasure.md#property-dict) | `HpdfDict` | The measurement dictionary (inherited) |
| [ProjectionAnnotation](Hpdf3DMeasure.md#property-projectionannotation) | `HpdfProjectionAnnotation?` | The projection annotation, or `null` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetText()](#method-settext) | Sets the text of the note (`/UT`) |
| [SetTextBoxSize()](#method-settextboxsize) | Sets the size of the box of the text (`/TB`) |
| [SetName()](Hpdf3DMeasure.md#method-setname) | Sets the name shown in the list of measurements (`/TRL`) (inherited) |
| [SetColor()](Hpdf3DMeasure.md#method-setcolor) | Sets the color (`/C`) (inherited) |
| [SetTextSize()](Hpdf3DMeasure.md#method-settextsize) | Sets the size of the text (`/TS`) (inherited) |

---

## Detailed Descriptions

#### Method: *SetText()*

**Declaration:**
```csharp
public void SetText(string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| text | `string` | The text of the note, in any language; may be empty |

**Return Value:**
`void`

**Description:**
Writes `/UT` as a PDF text string. A later call replaces it. The text of the projection annotation is separate: it is
what the comments list shows.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `text` is `null` |

**Example:**
```csharp
commentNote.SetText("Drill after assembly");
```

---

#### Method: *SetTextBoxSize()*

**Declaration:**
```csharp
public void SetTextBoxSize(int width, int height)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| width | `int` | The width of the text box, positive |
| height | `int` | The height of the text box, positive |

**Return Value:**
`void`

**Description:**
Writes `/TB [width height]`, the size of the box the viewer draws the text in. Without it, the viewer sizes the box.
A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `width` or `height` is not positive |

**Example:**
```csharp
commentNote.SetTextBoxSize(150, 40);
```

---

## Related Types

- [Hpdf3DMeasure](Hpdf3DMeasure.md) - base class: name, color, text size, projection annotation
- [Hpdf3DPerpendicularMeasure](Hpdf3DPerpendicularMeasure.md) - the other measurement kind
- [Hpdf3DModel](Hpdf3DModel.md) - `CreateCommentNote()`
- [HpdfProjectionAnnotation](../annotations/HpdfProjectionAnnotation.md) - lists the note among the comments
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
