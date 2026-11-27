# Hpdf3DNode Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.ThreeD`

---

## Purpose

`Hpdf3DNode` is the state of a node of a 3D model in one view (`/Type /3DNode`, PDF 1.7): a part of the model, found
by its name, that the view shows or hides, makes transparent or moves. Use nodes for views such as an exploded view,
a view without the housing, or a view that highlights one part.

---

## Getting an Instance

Node states are created by the view they belong to:

```csharp
public Hpdf3DNode CreateNode(string name)   // Hpdf3DView
```

The node is added to `/NA` of the view with its name (`/N`), and the document version is raised to PDF 1.7. It sets
nothing else until its methods are called: what a node does not set is as in the model file. See
[Hpdf3DView.CreateNode()](Hpdf3DView.md#method-createnode).

The names are the names of the nodes in the model file, as the model tree of a 3D viewer shows them. The library does
not read the model and cannot check them.

---

## Example

```csharp
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var model = pdf.Load3DModel("part.prc");

var inside = model.CreateView("Without the cover");
var cover = inside.CreateNode("Cover");
cover.SetVisibility(false);

var ghost = model.CreateView("Transparent housing");
var housing = ghost.CreateNode("Housing");
housing.SetOpacity(0.3f);

var exploded = model.CreateView("Exploded");
var lid = exploded.CreateNode("Lid");
lid.SetMatrix(new Hpdf3DMatrix(1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 40));   // 40 units up
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Name](#property-name) | `string` | The name of the node in the model |
| [Dict](#property-dict) | `HpdfDict` | The node dictionary |

---

## Methods

| Name | Description |
|------|-------------|
| [SetOpacity()](#method-setopacity) | Sets the opacity of the node (`/O`) |
| [SetVisibility()](#method-setvisibility) | Shows or hides the node (`/V`) |
| [SetMatrix()](#method-setmatrix) | Sets the position of the node relative to its parent (`/M`) |

---

## Detailed Descriptions

#### Property: *Name*

**Declaration:**
```csharp
public string Name { get; }
```

**Description:**
The name given to `CreateNode()`, written as `/N`.

---

#### Property: *Dict*

**Declaration:**
```csharp
public HpdfDict Dict { get; }
```

**Description:**
The node dictionary, an element of `/NA` of the view. Use it to add entries the class does not set.

---

#### Method: *SetOpacity()*

**Declaration:**
```csharp
public void SetOpacity(float opacity)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| opacity | `float` | The opacity, from 0 (invisible) to 1 (opaque) |

**Return Value:**
`void`

**Description:**
Writes `/O`. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `opacity` is below 0, above 1 or NaN |

**Example:**
```csharp
node.SetOpacity(0.5f);
```

---

#### Method: *SetVisibility()*

**Declaration:**
```csharp
public void SetVisibility(bool visible)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| visible | `bool` | Whether the node is shown in the view |

**Return Value:**
`void`

**Description:**
Writes `/V true` or `/V false`. A later call replaces it.

**Example:**
```csharp
node.SetVisibility(false);
```

---

#### Method: *SetMatrix()*

**Declaration:**
```csharp
public void SetMatrix(Hpdf3DMatrix matrix)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| matrix | `Hpdf3DMatrix` | The transformation of the node relative to its parent node, as `[a b c d e f g h i tx ty tz]` |

**Return Value:**
`void`

**Description:**
Writes `/M` with the twelve values of the matrix: the node is drawn at this position in the view. To move a node,
use the identity rotation part and the translation. A later call replaces it.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | A value of the matrix is NaN or infinite |

**Example:**
```csharp
var moved = Hpdf3DMatrix.Identity;
moved.Tx = 25;
node.SetMatrix(moved);
```

---

## Related Types

- [Hpdf3DView](Hpdf3DView.md) - `CreateNode()`, `Nodes`
- [Hpdf3DMatrix](../types/Hpdf3DMatrix.md) - the node matrix
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
