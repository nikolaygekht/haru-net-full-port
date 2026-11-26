# HpdfCmykColor Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfCmykColor` holds a CMYK color (cyan, magenta, yellow, black) with components from 0.0 to 1.0. The library
uses it to record the current CMYK fill and stroke colors in the graphics state.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfGraphicsState.CmykFill` | Current CMYK fill color (`page.GraphicsState.CmykFill`) |
| `HpdfGraphicsState.CmykStroke` | Current CMYK stroke color |

Colors are **set** with the `float` methods of [HpdfPageGraphics](../extensions/HpdfPageGraphics.md):
`SetCmykFill(c, m, y, k)` and `SetCmykStroke(c, m, y, k)`. They throw an `HpdfException` for values outside 0..1
and then store the color in the graphics state. No public method takes an `HpdfCmykColor` as a parameter, and there
is no `GetCMYKFill` helper; read `page.GraphicsState` instead. The initial value in a new graphics state is black
(0, 0, 0, 1).

---

## Getting an Instance

```csharp
using Haru.Types;

var cyan = new HpdfCmykColor(1, 0, 0, 0);
var black = HpdfCmykColor.Black;   // (0, 0, 0, 1)
```

The constructor clamps each component to the range 0..1. The property setters do not clamp.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var magenta = new HpdfCmykColor(0, 1, 0, 0);
page.SetCmykFill(magenta.C, magenta.M, magenta.Y, magenta.K);
page.Rectangle(50, 700, 200, 50);
page.Fill();

Console.WriteLine(page.GraphicsState.CmykFill);   // CMYK(0.000, 1.000, 0.000, 0.000)

pdf.SaveToFile("cmyk.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [C](#property-c) | `float` | Cyan component |
| [M](#property-m) | `float` | Magenta component |
| [Y](#property-y) | `float` | Yellow component |
| [K](#property-k) | `float` | Black (key) component |
| [Black, White](#property-black-white) | `HpdfCmykColor` | Static: predefined colors |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfCmykColor(float, float, float, float)](#constructor-hpdfcmykcolor) | Creates a color; clamps the components to 0..1 |
| [Equals()](#method-equals) | Compares the four components |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the components |
| [ToString()](#method-tostring) | Text form `CMYK(c, m, y, k)` |

---

## Detailed Descriptions

#### Property: *C*

**Declaration:**
```csharp
public float C { get; set; }
```

**Description:**
Cyan component, 0.0 (none) to 1.0 (full). The setter does not clamp.

---

#### Property: *M*

**Declaration:**
```csharp
public float M { get; set; }
```

**Description:**
Magenta component, 0.0 to 1.0. The setter does not clamp.

---

#### Property: *Y*

**Declaration:**
```csharp
public float Y { get; set; }
```

**Description:**
Yellow component, 0.0 to 1.0. The setter does not clamp.

---

#### Property: *K*

**Declaration:**
```csharp
public float K { get; set; }
```

**Description:**
Black (key) component, 0.0 to 1.0. The setter does not clamp.

---

#### Property: *Black, White*

**Declaration:**
```csharp
public static HpdfCmykColor Black { get; }   // (0, 0, 0, 1)
public static HpdfCmykColor White { get; }   // (0, 0, 0, 0)
```

**Description:**
Each property returns a new value with the components shown.

---

#### Constructor: *HpdfCmykColor()*

**Declaration:**
```csharp
public HpdfCmykColor(float c, float m, float y, float k)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| c | `float` | Cyan, 0.0 to 1.0 |
| m | `float` | Magenta, 0.0 to 1.0 |
| y | `float` | Yellow, 0.0 to 1.0 |
| k | `float` | Black, 0.0 to 1.0 |

**Description:**
Values below 0 become 0 and values above 1 become 1 (`Math.Clamp`); no exception is thrown.

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfCmykColor other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all four components are equal (exact `float` comparison).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfCmykColor left, HpdfCmykColor right)
public static bool operator !=(HpdfCmykColor left, HpdfCmykColor right)
```

**Description:**
Same comparison as `Equals(HpdfCmykColor)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of the four components.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
The components with three decimals, for example `CMYK(0.000, 1.000, 0.000, 0.000)`. The decimal separator follows
the current culture.

---

[Back to Index](../../INDEX.md)
