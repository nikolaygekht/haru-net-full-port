# HpdfTextWidth Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfTextWidth` holds the results of a text measurement: number of characters, words and spaces, and the text
width. It corresponds to `HPDF_TextWidth` of the C library, which returns it from `HPDF_Font_TextWidth`.

This type is **not used by the public API at present**: no public member takes or returns an `HpdfTextWidth`.
Text is measured with `HpdfFont.MeasureText(string text, float fontSize)` (see [HpdfFont](../core/HpdfFont.md))
or the `TextWidth(this HpdfCanvas canvas, string text)` extension method, both of which return the width in points as a
`float`.

---

## Getting an Instance

```csharp
using Haru.Types;

var tw = new HpdfTextWidth(numChars: 11, numWords: 2, width: 5000, numSpace: 1);
```

`default(HpdfTextWidth)` has all values at 0.

---

## Example

```csharp
using Haru.Types;

var a = new HpdfTextWidth(11, 2, 5000, 1);
var b = a;
b.Width = 5200;

Console.WriteLine(a);        // Chars:11, Words:2, Width:5000, Spaces:1
Console.WriteLine(a == b);   // False
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [NumChars](#property-numchars) | `uint` | Number of characters |
| [NumWords](#property-numwords) | `uint` | Number of words |
| [Width](#property-width) | `uint` | Width of the text |
| [NumSpace](#property-numspace) | `uint` | Number of spaces |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfTextWidth(uint, uint, uint, uint)](#constructor-hpdftextwidth) | Creates a measurement result |
| [Equals()](#method-equals) | Compares all four values |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the four values |
| [ToString()](#method-tostring) | Text form `Chars:n, Words:n, Width:n, Spaces:n` |

---

## Detailed Descriptions

#### Property: *NumChars*

**Declaration:**
```csharp
public uint NumChars { get; set; }
```

**Description:**
Number of characters in the text.

---

#### Property: *NumWords*

**Declaration:**
```csharp
public uint NumWords { get; set; }
```

**Description:**
Number of words in the text. The source notes that this value may change in future versions and suggests
`NumSpace` as an alternative.

---

#### Property: *Width*

**Declaration:**
```csharp
public uint Width { get; set; }
```

**Description:**
Width of the text. In the C library this is the sum of the glyph widths in 1/1000 em units; the struct itself does
not define a unit, as nothing in Haru.NET fills it.

---

#### Property: *NumSpace*

**Declaration:**
```csharp
public uint NumSpace { get; set; }
```

**Description:**
Number of space characters in the text.

---

#### Constructor: *HpdfTextWidth()*

**Declaration:**
```csharp
public HpdfTextWidth(uint numChars, uint numWords, uint width, uint numSpace)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| numChars | `uint` | Number of characters |
| numWords | `uint` | Number of words |
| width | `uint` | Width of the text |
| numSpace | `uint` | Number of spaces |

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfTextWidth other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when all four values are equal.

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfTextWidth left, HpdfTextWidth right)
public static bool operator !=(HpdfTextWidth left, HpdfTextWidth right)
```

**Description:**
Same comparison as `Equals(HpdfTextWidth)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines the four values.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
For example `Chars:11, Words:2, Width:5000, Spaces:1`.

---

[Back to Index](../../INDEX.md)
