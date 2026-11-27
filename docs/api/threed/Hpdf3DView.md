# Hpdf3DView Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DView` is a view of a 3D model (`/Type /3DView`): a named way of looking at the model, with its camera,
projection, background, lighting, cross section, the state of the model's parts (nodes) and the measurements shown.
Viewers list the views of a model in their 3D toolbar, and a comment can switch to a view
([HpdfMarkupAnnotation.Set3DView()](../annotations/HpdfMarkupAnnotation.md#method-set3dview)). Every setting is
optional: what a view does not set is taken from the model file or the viewer. The camera is the exception in practice:
a view of a PRC model needs one (see [The camera](#the-camera)).

---

## Getting an Instance

Views are created by their model:

```csharp
public Hpdf3DView CreateView(string name)   // Hpdf3DModel
```

The view is added to `/VA` of the model's 3D stream; the first one is the default view. A new view writes only its
name (`/XN` and `/IN`). See [Hpdf3DModel.CreateView()](Hpdf3DModel.md#method-createview).

### The camera

A view places the camera in one of two ways:

- [SetCamera()](#method-setcamera): the camera is on an orbit around a point and looks at it, from a direction and a
  distance, turned around its viewing direction by a roll angle. This is the usual way.
- [SetCameraMatrix()](#method-setcameramatrix): the camera-to-world matrix and the distance to the center of the orbit
  are given directly.

A view without a camera uses the camera stored in the model file. Adobe Acrobat (tested) shows such a view well when
the file has a camera, as U3D files usually do. PRC files usually have none, and then Acrobat shows nothing: the model
is outside the view until the reader changes it. A 3D annotation whose model has no views at all is fitted to the
model by Acrobat. So give every view of a PRC model a camera, with the center of the model as the center of the orbit
and a distance large enough for the whole model, or define no views.

Model coordinates are those of the model file; in models from CAD software the z axis often points up.

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

// Looking at the origin from the front-right, from above, 200 units away
var view = model.CreateView("Front right");
view.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(1, -1, 0.6f), 1000, 0);
view.SetPerspectiveProjection(30);
view.SetBackgroundColor(0.95f, 0.95f, 1f);
view.SetLighting(Hpdf3DLighting.CAD);

// A section through the middle, with the cut drawn in red
var section = model.CreateView("Section");
section.SetCamera(new HpdfPoint3D(0, 0, 0), new HpdfPoint3D(1, -1, 0.6f), 1000, 0);
section.SetCrossSection(new HpdfPoint3D(0, 0, 0), 0, 90, 0.3f, true);

page.Create3DAnnotation(new HpdfRect(50, 350, 550, 750), model, "A machine part");
pdf.SaveToFile("views.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Name](#property-name) | `string` | The name of the view |
| [Model](#property-model) | `Hpdf3DModel` | The model the view belongs to |
| [Nodes](#property-nodes) | `IReadOnlyList<Hpdf3DNode>` | The nodes whose state the view sets |
| [Dict](#property-dict) | `HpdfDict` | The view dictionary |

---

## Methods

| Name | Description |
|------|-------------|
| [SetCamera()](#method-setcamera) | Places the camera on an orbit around a point (`/C2W`, `/CO`) |
| [SetCameraMatrix()](#method-setcameramatrix) | Sets the camera-to-world matrix and the orbit distance (`/C2W`, `/CO`) |
| [SetPerspectiveProjection()](#method-setperspectiveprojection) | Uses a perspective projection with a field of view (`/P`) |
| [SetOrthographicProjection()](#method-setorthographicprojection) | Uses an orthographic projection with a scale (`/P`) |
| [SetBackgroundColor()](#method-setbackgroundcolor) | Sets the background color (`/BG`) |
| [SetLighting()](#method-setlighting) | Sets the lighting scheme (`/LS`) |
| [SetCrossSection()](#method-setcrosssection) | Cuts the model with a plane (`/SA`) |
| [RemoveCrossSection()](#method-removecrosssection) | Shows the model without a cross section (an empty `/SA`) |
| [CreateNode()](#method-createnode) | Sets the state of a node of the model in this view (`/NA`) |
| [AddMeasure()](#method-addmeasure) | Shows a 3D measurement in this view (`/MA`) |

---

## Detailed Descriptions

#### Property: *Name*

**Declaration:**
```csharp
public string Name { get; }
```

**Description:**
The name given to `CreateView()`, written as the external name `/XN` (shown by viewers) and the internal name `/IN`.

---

#### Property: *Model*

**Declaration:**
```csharp
public Hpdf3DModel Model { get; }
```

**Description:**
The model whose `CreateView()` created the view.

---

#### Property: *Nodes*

**Declaration:**
```csharp
public IReadOnlyList<Hpdf3DNode> Nodes { get; }
```

**Description:**
The node states created by [CreateNode()](#method-createnode), in the order of `/NA`.

---

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The view dictionary, an indirect object of the document. 3D annotations refer to it as `/3DV`, comments as `/3DV` of
their external data. Use it to add entries the class does not set, such as a render mode (`/RM`).

---

#### Method: *SetCamera()*

**Declaration:**
```csharp
public void SetCamera(HpdfPoint3D centerOfOrbit, HpdfPoint3D centerToCamera, float radius, float roll)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| centerOfOrbit | `HpdfPoint3D` | The point the camera looks at, in model coordinates; viewers rotate the camera around it |
| centerToCamera | `HpdfPoint3D` | The direction from that point to the camera; its length does not matter. (0, 0, 0) means (0, -1, 0): the camera in front of the point on the negative y axis |
| radius | `float` | The distance from the point to the camera; its sign is ignored |
| roll | `float` | The rotation of the camera around its viewing direction, in degrees |

**Return Value:**
`void`

**Description:**
Computes the camera-to-world matrix (`HPDF_3DView_SetCamera` of the C library) and writes it as `/C2W` with
`/MS /M` and the orbit distance `/CO`. The camera looks along `-centerToCamera`; its up direction is the z axis of the
model, as far as it is perpendicular to the viewing direction, turned by `roll`. Looking straight down, up is the
y axis (looking straight up, the negative y axis). The matrix is orthonormal for any roll: the C library adds the sine of the roll angle
to two vectors, which makes them non-orthogonal; Haru.NET rotates them.

| Camera | `centerToCamera` |
|--------|------------------|
| Front (looking along +y) | `(0, -1, 0)` |
| Right side (looking along -x) | `(1, 0, 0)` |
| Top (looking down) | `(0, 0, 1)` |
| Front right, from above | `(1, -1, 0.6)` |

A later call, or [SetCameraMatrix()](#method-setcameramatrix), replaces the camera.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A value is NaN or infinite |

**Example:**
```csharp
// The top view of a part centered at (0, 0, 10), 150 units above it
view.SetCamera(new HpdfPoint3D(0, 0, 10), new HpdfPoint3D(0, 0, 1), 1000, 0);
```

---

#### Method: *SetCameraMatrix()*

**Declaration:**
```csharp
public void SetCameraMatrix(Hpdf3DMatrix cameraToWorld, float centerOfOrbitDistance)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| cameraToWorld | `Hpdf3DMatrix` | The camera-to-world matrix: `A, B, C` the camera's x axis, `D, E, F` its y axis (up), `G, H, I` its z axis (the viewing direction), `Tx, Ty, Tz` its position, all in model coordinates |
| centerOfOrbitDistance | `float` | The distance from the camera to the center of the orbit, along the viewing direction; not negative |

**Return Value:**
`void`

**Description:**
Writes the matrix as `/C2W [a b c d e f g h i tx ty tz]` with `/MS /M`, and the distance as `/CO`. Use it for a camera
taken from another program; the three axes should be unit vectors perpendicular to each other, with x = y × z.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `centerOfOrbitDistance` is negative, NaN or infinite |
| `HpdfException` (`InvalidParameter`) | A value of the matrix is NaN or infinite |

**Example:**
```csharp
// Looking straight down from (0, 0, 100) at the origin
view.SetCameraMatrix(new Hpdf3DMatrix(-1, 0, 0,  0, 1, 0,  0, 0, -1,  0, 0, 100), 100);
```

---

#### Method: *SetPerspectiveProjection()*

**Declaration:**
```csharp
public void SetPerspectiveProjection(float fieldOfView)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| fieldOfView | `float` | The field of view in degrees, from 0 to 180 |

**Return Value:**
`void`

**Description:**
Writes `/P << /Subtype /P /PS /Min /FOV fieldOfView >>`: a perspective projection whose field of view spans the
smaller side of the annotation. A later call, or
[SetOrthographicProjection()](#method-setorthographicprojection), replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `fieldOfView` is below 0, above 180 or NaN |

**Example:**
```csharp
view.SetPerspectiveProjection(30);
```

---

#### Method: *SetOrthographicProjection()*

**Declaration:**
```csharp
public void SetOrthographicProjection(float scale)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| scale | `float` | The scale factor (`/OS`), positive |

**Return Value:**
`void`

**Description:**
Writes `/P << /Subtype /O /OS scale >>`: an orthographic (parallel) projection, as in technical drawings, in which
parallel edges stay parallel. A later call, or [SetPerspectiveProjection()](#method-setperspectiveprojection),
replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `scale` is not a positive number |

**Example:**
```csharp
view.SetOrthographicProjection(1);
```

---

#### Method: *SetBackgroundColor()*

**Declaration:**
```csharp
public void SetBackgroundColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r, g, b | `float` | The red, green and blue components, from 0 to 1 |

**Return Value:**
`void`

**Description:**
Writes `/BG << /Type /3DBG /C [r g b] >>`. The color array holds the components only: the color space of a 3D
background is `/CS`, DeviceRGB by default. A 3D measurement color is written the same way
([Hpdf3DMeasure.SetColor()](Hpdf3DMeasure.md#method-setcolor)); only the intersection color of a cross section names
its color space (`/IC [/DeviceRGB 1 0 0]`). A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A component is below 0, above 1 or NaN |

**Example:**
```csharp
view.SetBackgroundColor(0.85f, 0.9f, 1f);
```

---

#### Method: *SetLighting()*

**Declaration:**
```csharp
public void SetLighting(Hpdf3DLighting lighting)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| lighting | `Hpdf3DLighting` | The lighting scheme (see [Hpdf3DLighting](Hpdf3DLighting.md)) |

**Return Value:**
`void`

**Description:**
Writes `/LS << /Type /3DLightingScheme /Subtype /name >>` and raises the document version to PDF 1.7. Without a
lighting scheme, the lights of the model file are used. `Artwork` uses them too: with it, a model without lights
(typical for PRC files, such as the output of Asymptote) looks black in Adobe Acrobat. Give such a model a scheme with
lights of its own, such as `CAD`, `Day` or `Headlamp`. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `lighting` is not an `Hpdf3DLighting` value |

**Example:**
```csharp
view.SetLighting(Hpdf3DLighting.Day);
```

---

#### Method: *SetCrossSection()*

**Declaration:**
```csharp
public void SetCrossSection(HpdfPoint3D center, float roll, float pitch, float opacity, bool showIntersection)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| center | `HpdfPoint3D` | A point of the cutting plane, in model coordinates |
| roll | `float` | The rotation of the plane around the y axis, in degrees |
| pitch | `float` | The rotation of the plane around the z axis, in degrees |
| opacity | `float` | The opacity of the plane, from 0 (invisible) to 1 |
| showIntersection | `bool` | Whether the line where the plane cuts the model is drawn (in red) |

**Return Value:**
`void`

**Description:**
Cuts the model with a plane: writes `/SA [<< /Type /3DCrossSection /C center
/O [null roll pitch] /PO opacity /IV showIntersection /IC [/DeviceRGB 1 0 0] >>]` and raises the document version to
PDF 1.7. The plane starts perpendicular to the x axis (the `null` of `/O`) and is turned around the y axis by `roll`
and around the z axis by `pitch`. A view has one cross section; a later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A coordinate or angle is NaN or infinite |
| `HpdfException` (`InvalidParameter`) | `opacity` is below 0, above 1 or NaN |

**Example:**
```csharp
// A plane through the origin perpendicular to the y axis (turned 90 degrees around z)
view.SetCrossSection(new HpdfPoint3D(0, 0, 0), 0, 90, 0.25f, true);
```

---

#### Method: *RemoveCrossSection()*

**Declaration:**
```csharp
public void RemoveCrossSection()
```

**Return Value:**
`void`

**Description:**
Writes an empty `/SA []`, which turns off the cross sections stored in the model file for this view, and raises the
document version to PDF 1.7. It also replaces a cross section set by [SetCrossSection()](#method-setcrosssection).

---

#### Method: *CreateNode()*

**Declaration:**
```csharp
public Hpdf3DNode CreateNode(string name)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| name | `string` | The name of the node (a part of the model) as the model file names it |

**Return Value:**
A new [Hpdf3DNode](Hpdf3DNode.md), added to `/NA` of the view.

**Description:**
Creates the state of a node of the model in this view: whether it is visible, its opacity and its position. The node
is found by its name in the model file; the names are those the viewer shows in its model tree. Raises the document
version to PDF 1.7.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `name` is `null` or empty |

**Example:**
```csharp
var cover = view.CreateNode("Cover");
cover.SetVisibility(false);
```

---

#### Method: *AddMeasure()*

**Declaration:**
```csharp
public void AddMeasure(Hpdf3DMeasure measure)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| measure | `Hpdf3DMeasure` | A measurement created by [Hpdf3DModel.CreateCommentNote()](Hpdf3DModel.md#method-createcommentnote) or [CreatePerpendicularMeasure()](Hpdf3DModel.md#method-createperpendicularmeasure) |

**Return Value:**
`void`

**Description:**
Adds the measurement to `/MA` of the view, so it is shown when the view is. A measurement can be shown by several
views; adding it twice to one view adds it once. Declares Adobe ExtensionLevel 3 and raises the document version to
PDF 1.7.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `measure` is `null`, or a measurement of another model |

**Example:**
```csharp
view.AddMeasure(dimension);
```

---

## Related Types

- [Hpdf3DModel](Hpdf3DModel.md) - creates the views
- [Hpdf3DNode](Hpdf3DNode.md) - the state of a part of the model
- [Hpdf3DMeasure](Hpdf3DMeasure.md) - measurements shown in a view
- [Hpdf3DLighting](Hpdf3DLighting.md) - lighting schemes
- [HpdfPoint3D](../types/HpdfPoint3D.md), [Hpdf3DMatrix](../types/Hpdf3DMatrix.md) - points and the camera matrix
- [Hpdf3DAnnotation](../annotations/Hpdf3DAnnotation.md) - `SetDefaultView()`
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
