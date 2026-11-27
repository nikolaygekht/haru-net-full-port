# Hpdf3DAnnotation Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

**Base class:** [HpdfAnnotation](HpdfAnnotation.md)

---

## Purpose

`Hpdf3DAnnotation` is a 3D annotation (PDF 1.6): an area of the page that shows a 3D model, which the reader can
rotate, zoom and switch between its views. It refers to the model's 3D stream (`/3DD`) and tells when the model is
activated (`/3DA`). Viewers that show 3D (Adobe Acrobat and Reader, with 3D content enabled in their preferences, and
Foxit) render the model; every other viewer, and a 3D viewer before the model is activated, shows the annotation's
appearance: a poster image, or a gray box with a cube when no image is given. It is not a markup annotation; comments
refer to its views with [HpdfMarkupAnnotation.Set3DView()](HpdfMarkupAnnotation.md#method-set3dview).

---

## Getting an Instance

Load the model with the document and create the annotation with the page it is on:

```csharp
public Hpdf3DModel Load3DModel(string path)            // HpdfDocument
public Hpdf3DModel Load3DModel(byte[] data)            // HpdfDocument

public Hpdf3DAnnotation Create3DAnnotation(HpdfRect rect, Hpdf3DModel model, string text = "3D Model",
    HpdfImage? poster = null)                          // HpdfPage
```

| Name | Type | Description |
|------|------|-------------|
| rect | `HpdfRect` | The area of the page that shows the model. Reversed edges are swapped |
| model | `Hpdf3DModel` | The model, loaded by `HpdfDocument.Load3DModel()` (see [Hpdf3DModel](../threed/Hpdf3DModel.md)) |
| text | `string` | The text of the annotation (`/Contents`), a description of the model for viewers and screen readers |
| poster | `HpdfImage?` | An image of the document shown before the model is activated and by viewers without 3D, or `null` |

The method creates the annotation (`/Subtype /3D`) and adds it to the page's `/Annots` array. It writes the
activation dictionary with the defaults (`/3DA << /A /PO /AIS /L /D /PC /DIS /U >>`: activated live when the page
opens, unloaded when the page closes), sets the Print flag (`/F 4`) and raises the document version to PDF 1.6. One
model can be shown by several annotations. PDF/A-1 does not allow 3D annotations: saving a PDF/A document with one
throws `HpdfException` (`InvalidOperation`).

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `model` or `text` is `null` |
| `HpdfException` (`InvalidParameter`) | A coordinate of `rect` is NaN or infinite |

### The appearance (poster)

The library draws the appearance stream (`/AP /N`) when the document is saved:

- With a poster image, the image is drawn scaled to the rectangle in a Form XObject (`/Resources /XObject`). Choose an
  image with the aspect ratio of the rectangle. (The C library uses the image itself as the appearance stream.)
- Without one, a light gray box with a gray frame and the outline of a cube in its middle.

The color setters and the border style of [HpdfAnnotation](HpdfAnnotation.md) are written but not used by this
appearance. A good poster is a picture of the model in its default view, so that the page looks right in print and in
viewers without 3D.

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
var front = model.CreateView("Front");
front.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(0, -1, 0), 1000, 0);
var top = model.CreateView("Top");
top.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(0, 0, 1), 1000, 0);

using var poster = pdf.LoadPngImageFromFile("poster.png");
var annotation = page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part", poster);
annotation.SetActivation(Hpdf3DActivation.Explicit);                       // click to load the model
annotation.SetUserInterface(showToolbar: true, showNavigationPanel: true);
annotation.SetDefaultView(top);

pdf.SaveToFile("model.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Model](#property-model) | `Hpdf3DModel` | The model the annotation shows |
| [Flags](HpdfAnnotation.md#property-flags) | `HpdfAnnotationFlags` | The annotation flags (`/F`); `Print` by default (inherited) |
| [Dict](HpdfAnnotation.md#property-dict) | `HpdfDict` | The annotation dictionary (inherited) |
| [Type](HpdfAnnotation.md#property-type) | `HpdfAnnotationType` | Always `HpdfAnnotationType.ThreeD` (inherited) |

---

## Methods

| Name | Description |
|------|-------------|
| [SetActivation()](#method-setactivation) | Sets when the model is activated and in what state (`/3DA /A /AIS`) |
| [SetDeactivation()](#method-setdeactivation) | Sets when the model is deactivated and in what state (`/3DA /D /DIS`) |
| [SetUserInterface()](#method-setuserinterface) | Shows or hides the 3D toolbar and the model tree (`/3DA /TB /NP`) |
| [SetDefaultView()](#method-setdefaultview) | Sets the view the annotation shows first (`/3DV`) |
| [SetBorderStyle()](HpdfAnnotation.md#method-setborderstyle), [SetRgbColor()](HpdfAnnotation.md#method-setrgbcolor), [SetCmykColor()](HpdfAnnotation.md#method-setcmykcolor), [SetGrayColor()](HpdfAnnotation.md#method-setgraycolor), [SetNoColor()](HpdfAnnotation.md#method-setnocolor) | Write `/BS` and `/C` (inherited); the generated appearance does not use them |

---

## Detailed Descriptions

#### Property: *Model*

**Declaration:**
```csharp
public Hpdf3DModel Model { get; }
```

**Description:**
The model given at creation. Its views ([Hpdf3DModel.Views](../threed/Hpdf3DModel.md#property-views)) are the views
the viewer lists for this annotation.

---

#### Method: *SetActivation()*

**Declaration:**
```csharp
public void SetActivation(Hpdf3DActivation when, Hpdf3DArtworkState state = Hpdf3DArtworkState.Live)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| when | `Hpdf3DActivation` | `PageOpen` (the default of a new annotation), `PageVisible` or `Explicit` (see [Hpdf3DActivation](../threed/Hpdf3DActivation.md)) |
| state | `Hpdf3DArtworkState` | `Live` (default) or `Instantiated` (see [Hpdf3DArtworkState](../threed/Hpdf3DArtworkState.md)) |

**Return Value:**
`void`

**Description:**
Writes `/A` (`/PO`, `/PV` or `/XA`) and `/AIS` (`/L` or `/I`) of the activation dictionary. With `Explicit`, the
poster is shown until the reader clicks it, which keeps a page with large models quick to open. A later call
replaces both values.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `when` is not an `Hpdf3DActivation` value |
| `HpdfException` (`InvalidParameter`) | `state` is `Uninstantiated` or not an `Hpdf3DArtworkState` value |

**Example:**
```csharp
annot3d.SetActivation(Hpdf3DActivation.PageVisible);
```

---

#### Method: *SetDeactivation()*

**Declaration:**
```csharp
public void SetDeactivation(Hpdf3DDeactivation when, Hpdf3DArtworkState state = Hpdf3DArtworkState.Uninstantiated)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| when | `Hpdf3DDeactivation` | `PageClose` (the default of a new annotation), `PageInvisible` or `Explicit` (see [Hpdf3DDeactivation](../threed/Hpdf3DDeactivation.md)) |
| state | `Hpdf3DArtworkState` | The state after deactivation: `Uninstantiated` (default), `Instantiated` or `Live` |

**Return Value:**
`void`

**Description:**
Writes `/D` (`/PC`, `/PI` or `/XD`) and `/DIS` (`/U`, `/I` or `/L`) of the activation dictionary. `Uninstantiated`
unloads the model and shows the poster again. A later call replaces both values.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `when` is not an `Hpdf3DDeactivation` value, or `state` not an `Hpdf3DArtworkState` value |

**Example:**
```csharp
annot3d.SetDeactivation(Hpdf3DDeactivation.PageInvisible);
```

---

#### Method: *SetUserInterface()*

**Declaration:**
```csharp
public void SetUserInterface(bool showToolbar, bool showNavigationPanel)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| showToolbar | `bool` | Whether the viewer shows its 3D toolbar when the model is activated (shown by default) |
| showNavigationPanel | `bool` | Whether the viewer shows the model tree, the list of the parts of the model (hidden by default) |

**Return Value:**
`void`

**Description:**
Writes `/TB` and `/NP` of the activation dictionary and raises the document version to PDF 1.7. The toolbar lists
the views of the model and offers rotation, zoom, rendering modes and lighting; the model tree lists the nodes.

**Example:**
```csharp
annot3d.SetUserInterface(showToolbar: true, showNavigationPanel: true);
```

---

#### Method: *SetDefaultView()*

**Declaration:**
```csharp
public void SetDefaultView(Hpdf3DView view)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| view | `Hpdf3DView` | A view of the annotation's model |

**Return Value:**
`void`

**Description:**
Writes `/3DV` with the view: the annotation shows this view when the model is activated. Without it, the default view
of the model is shown ([Hpdf3DModel.SetDefaultView()](../threed/Hpdf3DModel.md#method-setdefaultview), the first view
until it is called). Use it when several annotations show the same model from different sides. A later call replaces
the view.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `view` is `null` or not a view of the annotation's model |

**Example:**
```csharp
annot3d.SetDefaultView(view);
```

---

## Related Types

- [Hpdf3DModel](../threed/Hpdf3DModel.md), [Hpdf3DView](../threed/Hpdf3DView.md) - the model and its views
- [Hpdf3DActivation](../threed/Hpdf3DActivation.md), [Hpdf3DDeactivation](../threed/Hpdf3DDeactivation.md),
  [Hpdf3DArtworkState](../threed/Hpdf3DArtworkState.md) - activation settings
- [HpdfProjectionAnnotation](HpdfProjectionAnnotation.md) - 3D measurements in the comments list
- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - `Set3DView()`: a comment that shows a view
- [HpdfImage](../core/HpdfImage.md) - the poster image
- [HpdfPage](../core/HpdfPage.md) - `Create3DAnnotation()`
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
