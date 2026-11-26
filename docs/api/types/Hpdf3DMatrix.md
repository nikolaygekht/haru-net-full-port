# Hpdf3DMatrix Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`Hpdf3DMatrix` is a 3D transformation matrix of twelve numbers: a 3 x 3 rotation/scale part (`A` to `I`, row by
row) and a translation (`Tx`, `Ty`, `Tz`). It corresponds to `HPDF_3DMatrix` of the C library, where it describes
the camera of a 3D view.

This type is **not used by the public API at present**: 3D annotations (U3D) are not implemented in Haru.NET, and no
public member takes or returns an `Hpdf3DMatrix`. The struct only stores the values; it has no multiplication or
other matrix operations.

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
First row of the 3 x 3 part.

---

#### Property: *D, E, F*

**Declaration:**
```csharp
public float D { get; set; }
public float E { get; set; }
public float F { get; set; }
```

**Description:**
Second row of the 3 x 3 part.

---

#### Property: *G, H, I*

**Declaration:**
```csharp
public float G { get; set; }
public float H { get; set; }
public float I { get; set; }
```

**Description:**
Third row of the 3 x 3 part.

---

#### Property: *Tx, Ty, Tz*

**Declaration:**
```csharp
public float Tx { get; set; }
public float Ty { get; set; }
public float Tz { get; set; }
```

**Description:**
Translation along the X, Y and Z axes.

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

[Back to Index](../../INDEX.md)
