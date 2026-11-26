# HpdfPoint3D Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfPoint3D` is a point in three-dimensional space (X, Y and Z). It corresponds to `HPDF_Point3D` of the C library,
where it is used for 3D annotations and 3D views.

This type is **not used by the public API at present**: 3D annotations (U3D) are not implemented in Haru.NET, and no
public member takes or returns an `HpdfPoint3D`. It is kept as a plain value type for future use.

---

## Getting an Instance

```csharp
using Haru.Types;

var p = new HpdfPoint3D(1, 2, 3);
```

`default(HpdfPoint3D)` is (0, 0, 0).

---

## Example

```csharp
using Haru.Types;

var a = new HpdfPoint3D(1.5f, 0, -2);
var b = new HpdfPoint3D(1.5f, 0, -2);

Console.WriteLine(a);        // (1.5, 0, -2) in an English culture
Console.WriteLine(a == b);   // True
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [X](#property-x) | `float` | X coordinate |
| [Y](#property-y) | `float` | Y coordinate |
| [Z](#property-z) | `float` | Z coordinate |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfPoint3D(float, float, float)](#constructor-hpdfpoint3d) | Creates a point |
| [Equals()](#method-equals) | Compares X, Y and Z |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the three coordinates |
| [ToString()](#method-tostring) | Text form `(x, y, z)` |

---

## Detailed Descriptions

#### Property: *X*

**Declaration:**
```csharp
public float X { get; set; }
```

---

#### Property: *Y*

**Declaration:**
```csharp
public float Y { get; set; }
```

---

#### Property: *Z*

**Declaration:**
```csharp
public float Z { get; set; }
```

---

#### Constructor: *HpdfPoint3D()*

**Declaration:**
```csharp
public HpdfPoint3D(float x, float y, float z)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| x | `float` | X coordinate |
| y | `float` | Y coordinate |
| z | `float` | Z coordinate |

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfPoint3D other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all three coordinates are equal (exact `float` comparison).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfPoint3D left, HpdfPoint3D right)
public static bool operator !=(HpdfPoint3D left, HpdfPoint3D right)
```

**Description:**
Same comparison as `Equals(HpdfPoint3D)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of the three coordinates.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
A string such as `(1.5, 0, -2)`, using the default `float` formatting of the current culture.

---

[Back to Index](../../INDEX.md)
