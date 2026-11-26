# HpdfRgbColor Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfRgbColor` holds an RGB color with components from 0.0 to 1.0. The library uses it to report the current RGB
fill and stroke colors of a page or canvas.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfPageExtensions.GetRGBFill(this HpdfPage)` | Returns the current RGB fill color of a page |
| `HpdfPageExtensions.GetRGBStroke(this HpdfPage)` | Returns the current RGB stroke color of a page |
| `HpdfGraphicsState.RgbFill`, `RgbStroke` | The same values in the graphics state (`page.GraphicsState`) |

Colors are **set** with the `float` methods of [HpdfPageGraphics](../extensions/HpdfPageGraphics.md):
`SetRgbFill(r, g, b)` and `SetRgbStroke(r, g, b)`. They throw an `HpdfException` for values outside 0..1 and then
store the color in the graphics state. No public method takes an `HpdfRgbColor` as a parameter. The initial value
in a new graphics state is black (0, 0, 0).

---

## Getting an Instance

```csharp
using Haru.Types;

var orange = new HpdfRgbColor(1f, 0.5f, 0f);
var teal = HpdfRgbColor.FromBytes(0, 128, 128);
var black = HpdfRgbColor.Black;
```

The constructor clamps each component to the range 0..1. The property setters do not clamp.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var brand = HpdfRgbColor.FromBytes(0, 102, 204);
page.SetRgbFill(brand.R, brand.G, brand.B);
page.Rectangle(50, 700, 200, 50);
page.Fill();

HpdfRgbColor fill = page.GetRGBFill();
Console.WriteLine(fill == brand);   // True

pdf.SaveToFile("color.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [R](#property-r) | `float` | Red component |
| [G](#property-g) | `float` | Green component |
| [B](#property-b) | `float` | Blue component |
| [Black, White, Red, Green, Blue](#property-predefined-colors) | `HpdfRgbColor` | Static: predefined colors |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfRgbColor(float, float, float)](#constructor-hpdfrgbcolor) | Creates a color; clamps the components to 0..1 |
| [FromBytes(byte, byte, byte)](#method-frombytes) | Static: creates a color from 0..255 values |
| [Equals()](#method-equals) | Compares the three components |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the components |
| [ToString()](#method-tostring) | Text form `RGB(r, g, b)` |

---

## Detailed Descriptions

#### Property: *R*

**Declaration:**
```csharp
public float R { get; set; }
```

**Description:**
Red component, 0.0 (none) to 1.0 (full). The setter stores the value without clamping.

---

#### Property: *G*

**Declaration:**
```csharp
public float G { get; set; }
```

**Description:**
Green component, 0.0 to 1.0. The setter does not clamp.

---

#### Property: *B*

**Declaration:**
```csharp
public float B { get; set; }
```

**Description:**
Blue component, 0.0 to 1.0. The setter does not clamp.

---

#### Property: *Predefined colors*

**Declaration:**
```csharp
public static HpdfRgbColor Black { get; }   // (0, 0, 0)
public static HpdfRgbColor White { get; }   // (1, 1, 1)
public static HpdfRgbColor Red { get; }     // (1, 0, 0)
public static HpdfRgbColor Green { get; }   // (0, 1, 0)
public static HpdfRgbColor Blue { get; }    // (0, 0, 1)
```

**Description:**
Each property returns a new value with the components shown.

---

#### Constructor: *HpdfRgbColor()*

**Declaration:**
```csharp
public HpdfRgbColor(float r, float g, float b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r | `float` | Red component, 0.0 to 1.0 |
| g | `float` | Green component, 0.0 to 1.0 |
| b | `float` | Blue component, 0.0 to 1.0 |

**Description:**
Values below 0 become 0 and values above 1 become 1 (`Math.Clamp`); no exception is thrown.

---

#### Method: *FromBytes()*

**Declaration:**
```csharp
public static HpdfRgbColor FromBytes(byte r, byte g, byte b)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| r | `byte` | Red, 0 to 255 |
| g | `byte` | Green, 0 to 255 |
| b | `byte` | Blue, 0 to 255 |

**Return Value:**
A color whose components are the byte values divided by 255.

**Example:**
```csharp
using Haru.Types;

var gray = HpdfRgbColor.FromBytes(128, 128, 128);   // about (0.502, 0.502, 0.502)
```

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfRgbColor other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when R, G and B are equal (exact `float` comparison, so `FromBytes(255, 0, 0) == HpdfRgbColor.Red`, but
colors computed in different ways may differ in the last bits).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfRgbColor left, HpdfRgbColor right)
public static bool operator !=(HpdfRgbColor left, HpdfRgbColor right)
```

**Description:**
Same comparison as `Equals(HpdfRgbColor)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of the three components.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
The components with three decimals, for example `RGB(1.000, 0.500, 0.000)`. The decimal separator follows the
current culture.

---

[Back to Index](../../INDEX.md)
