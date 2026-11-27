# Hpdf3DMatrix Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`Hpdf3DMatrix` is a 3D transformation matrix of twelve numbers: a 3 x 3 rotation/scale part (`A` to `I`, row by
row) and a translation (`Tx`, `Ty`, `Tz`), in the order of a PDF 3D transformation matrix
`[a b c d e f g h i tx ty tz]` (ISO 32000-1, 3D coordinate systems). It corresponds to `HPDF_3DMatrix` of the C library. The struct
only stores the values; it has no multiplication or other matrix operations.

Where it is used:

| Member | Use |
|--------|-----|
| [Hpdf3DView.SetCameraMatrix(Hpdf3DMatrix cameraToWorld, float centerOfOrbitDistance)](../threed/Hpdf3DView.md#method-setcameramatrix) | The camera of a 3D view (`/C2W`): `A, B, C` the camera's x axis, `D, E, F` its y axis (up), `G, H, I` its z axis (the viewing direction), `Tx, Ty, Tz` its position, in model coordinates |
| [Hpdf3DNode.SetMatrix(Hpdf3DMatrix matrix)](../threed/Hpdf3DNode.md#method-setmatrix) | The position of a part of a 3D model relative to its parent (`/M`) |

[Hpdf3DView.SetCamera()](../threed/Hpdf3DView.md#method-setcamera) computes the camera matrix from a point, a
direction, a distance and a roll angle, which is easier than writing it by hand.

---

## Getting an Instance

Use the constructor with all twelve values, or the [Identity](#property-identity) property:

```csharp
using Haru.Types;

var identity = Hpdf3DMatrix.Identity;
var moved = new Hpdf3DMatrix(1, 0, 0,
                             0, 1, 0,
                             0, 0, 1,
                             10, 20, 30);
```

`default(Hpdf3DMatrix)` has all twelve values at 0, which is not the identity matrix.

---

## Example

```csharp
using Haru.Types;

var m = Hpdf3DMatrix.Identity;
m.Tz = -100;   // move the view back along the Z axis

Console.WriteLine(m == Hpdf3DMatrix.Identity);   // False
Console.WriteLine(m);
// [1.00 0.00 0.00] [0.00 1.00 0.00] [0.00 0.00 1.00] T:[0.00 0.00 -100.00]
```

A camera above the origin, looking straight down:

```csharp
using Haru.Doc;
using Haru.ThreeD;
using Haru.Types;

var pdf = new HpdfDocument();
var model = pdf.Load3DModel("part.prc");
var top = model.CreateView("Top");

var camera = new Hpdf3DMatrix(-1, 0, 0,     // x axis
                              0, 1, 0,      // y axis (up)
                              0, 0, -1,     // z axis: looking along -z
                              0, 0, 100);   // position
top.SetCameraMatrix(camera, 100);
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [A, B, C](#property-a-b-c) | `float` | Row 1 of the 3 x 3 part |
| [D, E, F](#property-d-e-f) | `float` | Row 2 of the 3 x 3 part |
| [G, H, I](#property-g-h-i) | `float` | Row 3 of the 3 x 3 part |
| [Tx, Ty, Tz](#property-tx-ty-tz) | `float` | Translation |
| [Identity](#property-identity) | `Hpdf3DMatrix` | Static: the identity matrix |

---

## Methods

| Name | Description |
|------|-------------|
| [Hpdf3DMatrix(float, ... float)](#constructor-hpdf3dmatrix) | Creates a matrix from twelve values |
| [Equals()](#method-equals) | Compares all twelve values |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from all twelve values |
| [ToString()](#method-tostring) | Text form with two decimals |

---

## Detailed Descriptions

#### Property: *A, B, C*

**Declaration:**
```csharp
public float A { get; set; }
public float B { get; set; }
public float C { get; set; }
```

**Description:**
First row of the 3 x 3 part: the image of the x axis. In a camera matrix, the camera's x axis.

---

#### Property: *D, E, F*

**Declaration:**
```csharp
public float D { get; set; }
public float E { get; set; }
public float F { get; set; }
```

**Description:**
Second row of the 3 x 3 part: the image of the y axis. In a camera matrix, the camera's up direction.

---

#### Property: *G, H, I*

**Declaration:**
```csharp
public float G { get; set; }
public float H { get; set; }
public float I { get; set; }
```

**Description:**
Third row of the 3 x 3 part: the image of the z axis. In a camera matrix, the direction the camera looks in.

---

#### Property: *Tx, Ty, Tz*

**Declaration:**
```csharp
public float Tx { get; set; }
public float Ty { get; set; }
public float Tz { get; set; }
```

**Description:**
Translation along the X, Y and Z axes. In a camera matrix, the position of the camera.

---

#### Property: *Identity*

**Declaration:**
```csharp
public static Hpdf3DMatrix Identity { get; }
```

**Description:**
Returns a new matrix with `A = E = I = 1` and all other values 0.

---

#### Constructor: *Hpdf3DMatrix()*

**Declaration:**
```csharp
public Hpdf3DMatrix(float a, float b, float c, float d, float e, float f,
                    float g, float h, float i, float tx, float ty, float tz)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| a, b, c | `float` | Row 1 |
| d, e, f | `float` | Row 2 |
| g, h, i | `float` | Row 3 |
| tx, ty, tz | `float` | Translation |

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(Hpdf3DMatrix other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all twelve values are equal (exact `float` comparison).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(Hpdf3DMatrix left, Hpdf3DMatrix right)
public static bool operator !=(Hpdf3DMatrix left, Hpdf3DMatrix right)
```

**Description:**
Same comparison as `Equals(Hpdf3DMatrix)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of all twelve values.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
The three rows and the translation with two decimals, for example
`[1.00 0.00 0.00] [0.00 1.00 0.00] [0.00 0.00 1.00] T:[0.00 0.00 0.00]`. The text does not depend on the current
culture (the decimal separator is always a point).

---

## Related Types

- [Hpdf3DView](../threed/Hpdf3DView.md) - `SetCameraMatrix()`, `SetCamera()`
- [Hpdf3DNode](../threed/Hpdf3DNode.md) - `SetMatrix()`
- [HpdfPoint3D](HpdfPoint3D.md) - points of 3D models
- [3D Guide](../../guides/ThreeDGuide.md)

---

[Back to Index](../../INDEX.md)
