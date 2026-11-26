# HpdfTransMatrix Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfTransMatrix` is a 2D transformation matrix `[a b c d x y]` as used by the PDF operators `cm` (current
transformation matrix) and `Tm` (text matrix). It maps a point (x', y') to
`(a*x' + c*y' + x, b*x' + d*y' + y)`, so it can translate, scale, rotate and skew.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfPageText.SetTextMatrix(this HpdfPage, HpdfTransMatrix)` | Sets the text matrix and text line matrix (`Tm` operator) |
| `HpdfPageExtensions.SetTextMatrix(this HpdfPage, float a, float b, float c, float d, float x, float y)` | The same with six numbers |
| `HpdfPageGraphics.Concat(this IDrawable, HpdfTransMatrix)` | Multiplies the current transformation matrix (`cm` operator) |
| `HpdfPage.TextMatrix`, `HpdfPage.TextLineMatrix` | Text matrix and text line matrix as tracked by the library (read-only) |
| `HpdfGraphicsState.TransMatrix` | Current transformation matrix as tracked by the library (see the note below) |

See [HpdfPageText](../extensions/HpdfPageText.md#settextmatrix) and
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md).

**Tracked values:** `BeginText` resets `TextMatrix` and `TextLineMatrix` to the identity; `SetTextMatrix`,
`MoveTextPos`, `MoveTextPosAndSetLeading`, `MoveToNextLine`, `ShowTextNextLine` and `SetSpacingAndShowText`
update them. Showing text
does not advance `TextMatrix` (in PDF the text matrix moves with every glyph). `HpdfGraphicsState.TransMatrix` is
initialized to the identity; `Concat` multiplies it like the `cm` operator does (the given matrix times the
current one), and `GRestore` restores the saved value, so it reflects the matrix in effect on the page.

---

## Getting an Instance

Use the constructor, the [Identity](#property-identity) property or one of the factory methods:

```csharp
using Haru.Types;

var m1 = new HpdfTransMatrix(1, 0, 0, 1, 100, 200);       // translate by (100, 200)
var m2 = HpdfTransMatrix.Identity;
var m3 = HpdfTransMatrix.CreateTranslation(100, 200);     // same as m1
var m4 = HpdfTransMatrix.CreateScale(2, 0.5f);
var m5 = HpdfTransMatrix.CreateRotation(MathF.PI / 4);    // 45 degrees counterclockwise
```

`default(HpdfTransMatrix)` has all six values at 0, which is not the identity matrix. The struct has no matrix
multiplication; to combine a rotation with a position, set `X` and `Y` of the rotation matrix, or call `Concat`
several times.

---

## Example

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var font = new HpdfFont(pdf.Xref, HpdfStandardFont.Helvetica, "F1");

// Text rotated by 30 degrees, starting at (100, 400)
var tm = HpdfTransMatrix.CreateRotation(30 * MathF.PI / 180);
tm.X = 100;
tm.Y = 400;

page.BeginText();
page.SetFontAndSize(font, 18);
page.SetTextMatrix(tm);
page.ShowText("Rotated text");
page.EndText();

// A rectangle drawn in a scaled coordinate system
page.GSave();
page.Concat(HpdfTransMatrix.CreateTranslation(300, 600));
page.Concat(HpdfTransMatrix.CreateScale(2, 2));
page.Rectangle(0, 0, 50, 25);   // 100 x 50 points on the page
page.Stroke();
page.GRestore();

pdf.SaveToFile("matrix.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [A](#property-a) | `float` | Horizontal scaling (cosine part of a rotation) |
| [B](#property-b) | `float` | Vertical skewing (sine part of a rotation) |
| [C](#property-c) | `float` | Horizontal skewing (negative sine part of a rotation) |
| [D](#property-d) | `float` | Vertical scaling (cosine part of a rotation) |
| [X](#property-x) | `float` | Horizontal translation |
| [Y](#property-y) | `float` | Vertical translation |
| [Identity](#property-identity) | `HpdfTransMatrix` | Static: `[1 0 0 1 0 0]` |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfTransMatrix(float, float, float, float, float, float)](#constructor-hpdftransmatrix) | Creates a matrix from six values |
| [CreateTranslation(float, float)](#method-createtranslation) | Static: translation matrix |
| [CreateScale(float, float)](#method-createscale) | Static: scaling matrix |
| [CreateRotation(float)](#method-createrotation) | Static: rotation matrix (angle in radians) |
| [Equals()](#method-equals) | Compares all six values |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from all six values |
| [ToString()](#method-tostring) | Text form `[a b c d x y]` with three decimals |

---

## Detailed Descriptions

#### Property: *A*

**Declaration:**
```csharp
public float A { get; set; }
```

**Description:**
Horizontal scaling component (`a`).

---

#### Property: *B*

**Declaration:**
```csharp
public float B { get; set; }
```

**Description:**
Vertical skewing component (`b`).

---

#### Property: *C*

**Declaration:**
```csharp
public float C { get; set; }
```

**Description:**
Horizontal skewing component (`c`).

---

#### Property: *D*

**Declaration:**
```csharp
public float D { get; set; }
```

**Description:**
Vertical scaling component (`d`).

---

#### Property: *X*

**Declaration:**
```csharp
public float X { get; set; }
```

**Description:**
Horizontal translation (`e` in the PDF specification).

---

#### Property: *Y*

**Declaration:**
```csharp
public float Y { get; set; }
```

**Description:**
Vertical translation (`f` in the PDF specification).

---

#### Property: *Identity*

**Declaration:**
```csharp
public static HpdfTransMatrix Identity { get; }
```

**Description:**
Returns a new identity matrix `[1 0 0 1 0 0]`.

---

#### Constructor: *HpdfTransMatrix()*

**Declaration:**
```csharp
public HpdfTransMatrix(float a, float b, float c, float d, float x, float y)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| a | `float` | Horizontal scaling |
| b | `float` | Vertical skewing |
| c | `float` | Horizontal skewing |
| d | `float` | Vertical scaling |
| x | `float` | Horizontal translation |
| y | `float` | Vertical translation |

---

#### Method: *CreateTranslation()*

**Declaration:**
```csharp
public static HpdfTransMatrix CreateTranslation(float x, float y)
```

**Return Value:**
`[1 0 0 1 x y]`.

---

#### Method: *CreateScale()*

**Declaration:**
```csharp
public static HpdfTransMatrix CreateScale(float scaleX, float scaleY)
```

**Return Value:**
`[scaleX 0 0 scaleY 0 0]`.

---

#### Method: *CreateRotation()*

**Declaration:**
```csharp
public static HpdfTransMatrix CreateRotation(float angleRadians)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| angleRadians | `float` | Rotation angle in **radians**; positive values rotate counterclockwise |

**Return Value:**
`[cos sin -sin cos 0 0]`.

**Example:**
```csharp
using Haru.Types;

float degrees = 90;
var rotate = HpdfTransMatrix.CreateRotation(degrees * MathF.PI / 180);
```

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfTransMatrix other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all six values are equal (exact `float` comparison).

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfTransMatrix left, HpdfTransMatrix right)
public static bool operator !=(HpdfTransMatrix left, HpdfTransMatrix right)
```

**Description:**
Same comparison as `Equals(HpdfTransMatrix)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the hash codes of the six values.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
The six values with three decimals, for example `[1.000 0.000 0.000 1.000 100.000 200.000]`. The text does not
depend on the current culture (the decimal separator is always a point).

---

[Back to Index](../../INDEX.md)
