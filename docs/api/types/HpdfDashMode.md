# HpdfDashMode Struct

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfDashMode` describes a line dash pattern: up to eight lengths of alternating dashes and gaps, plus a phase
(how far into the pattern the line starts). The library uses it to record the current dash pattern in the
graphics state.

Main uses in the public API:

| Member | Use |
|--------|-----|
| `HpdfGraphicsState.DashMode` | Current dash pattern (`page.GraphicsState.DashMode`) |
| `HpdfDashMode.MaxPatternLength` | Limit checked by `SetDash` |

The dash pattern is **set** with `SetDash(ushort[]? pattern, uint phase)` of
[HpdfPageGraphics](../extensions/HpdfPageGraphics.md#setdash), which writes the `d` operator and stores a new
`HpdfDashMode` in the graphics state. `SetDash` throws an `HpdfException` when the pattern has more than
`MaxPatternLength` elements or when all its elements are zero; `null` or an empty array selects a solid line.
No public method takes an `HpdfDashMode` as a parameter.

---

## Getting an Instance

```csharp
using Haru.Doc;
using Haru.Types;

var dashed = new HpdfDashMode(new ushort[] { 6, 3 });          // 6 on, 3 off, phase 0
var dotDash = new HpdfDashMode(new ushort[] { 1, 3, 6, 3 }, 2); // starts 2 units into the pattern
var solid = HpdfDashMode.Solid;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
HpdfDashMode current = page.GraphicsState.DashMode;
```

A new graphics state holds `HpdfDashMode.Solid`, as does the mode stored by `SetDash(null, 0)`. `default(HpdfDashMode)`
(no pattern array) also means a solid line and is equal to `HpdfDashMode.Solid` under [Equals()](#method-equals),
so a solid line can be tested with `== HpdfDashMode.Solid` or with `NumPattern == 0`.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

page.SetDash(new ushort[] { 6, 3 }, 0);
page.MoveTo(50, 700);
page.LineTo(300, 700);
page.Stroke();

HpdfDashMode dash = page.GraphicsState.DashMode;
Console.WriteLine(dash);                                    // [6 3] Phase:0
Console.WriteLine(string.Join(",", dash.GetActivePattern())); // 6,3

page.SetDash(null, 0);                                      // back to a solid line
Console.WriteLine(page.GraphicsState.DashMode.NumPattern);  // 0

pdf.SaveToFile("dash.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [Pattern](#property-pattern) | `ushort[]` | Pattern storage (eight elements when created by the constructor) |
| [NumPattern](#property-numpattern) | `uint` | Number of used elements in `Pattern` |
| [Phase](#property-phase) | `uint` | Offset into the pattern at which the line starts |
| [MaxPatternLength](#property-maxpatternlength) | `int` (constant) | Maximum number of pattern elements: 8 |
| [Solid](#property-solid) | `HpdfDashMode` | Static: a solid line (no dashes) |

---

## Methods

| Name | Description |
|------|-------------|
| [HpdfDashMode(ushort[], uint)](#constructor-hpdfdashmode) | Creates a dash pattern |
| [GetActivePattern()](#method-getactivepattern) | Returns a copy of the used pattern elements |
| [Equals()](#method-equals) | Compares the used elements, their number and the phase |
| [Equality operators](#method-equality-operators) | `==` and `!=` |
| [GetHashCode()](#method-gethashcode) | Hash code from the used elements and the phase |
| [ToString()](#method-tostring) | `Solid` or `[d1 d2 ...] Phase:n` |

---

## Detailed Descriptions

#### Property: *Pattern*

**Declaration:**
```csharp
public ushort[] Pattern { get; set; }
```

**Description:**
The array that holds the dash and gap lengths, in user space units. The constructor always allocates an array of
`MaxPatternLength` (8) elements and copies the given values to its start, so only the first `NumPattern` elements
are meaningful. The array is shared between copies of the struct. `default(HpdfDashMode)` has `Pattern == null`.
Use [GetActivePattern()](#method-getactivepattern) to read the used part.

---

#### Property: *NumPattern*

**Declaration:**
```csharp
public uint NumPattern { get; set; }
```

**Description:**
Number of used elements in `Pattern`; 0 means a solid line. The setter is not validated against the array length.

---

#### Property: *Phase*

**Declaration:**
```csharp
public uint Phase { get; set; }
```

**Description:**
Distance into the dash pattern at which the line starts.

---

#### Property: *MaxPatternLength*

**Declaration:**
```csharp
public const int MaxPatternLength = 8;
```

**Description:**
Maximum number of elements in a dash pattern. Both the constructor and `SetDash` reject longer patterns.

---

#### Property: *Solid*

**Declaration:**
```csharp
public static HpdfDashMode Solid { get; }
```

**Description:**
Returns a new mode with no pattern elements and phase 0 (an eight-element array with `NumPattern = 0`).

---

#### Constructor: *HpdfDashMode()*

**Declaration:**
```csharp
public HpdfDashMode(ushort[] pattern, uint phase = 0)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| pattern | `ushort[]` | Dash and gap lengths, alternating (dash, gap, dash, ...); `null` or empty for a solid line |
| phase | `uint` | Offset into the pattern at which the line starts (default 0) |

**Description:**
Copies `pattern` into a new eight-element array and sets `NumPattern` to its length.

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `ArgumentException` | `pattern` has more than `MaxPatternLength` (8) elements |

---

#### Method: *GetActivePattern()*

**Declaration:**
```csharp
public ushort[] GetActivePattern()
```

**Return Value:**
A new array with the first `NumPattern` elements of `Pattern`, or an empty array for a solid line (also when
`Pattern` is `null`).

---

#### Method: *Equals()*

**Declaration:**
```csharp
public bool Equals(HpdfDashMode other)
public override bool Equals(object obj)
```

**Return Value:**
`true` when `NumPattern` and `Phase` are equal and the first `NumPattern` elements match. Unused array elements
are ignored, and a missing element (a `null` array, or an array shorter than `NumPattern`) counts as 0. So
`default(HpdfDashMode) == HpdfDashMode.Solid` is `true`.

---

#### Method: *Equality operators*

**Declaration:**
```csharp
public static bool operator ==(HpdfDashMode left, HpdfDashMode right)
public static bool operator !=(HpdfDashMode left, HpdfDashMode right)
```

**Description:**
Same comparison as `Equals(HpdfDashMode)`.

---

#### Method: *GetHashCode()*

**Declaration:**
```csharp
public override int GetHashCode()
```

**Description:**
Combines `NumPattern`, `Phase` and the used pattern elements.

---

#### Method: *ToString()*

**Declaration:**
```csharp
public override string ToString()
```

**Return Value:**
`Solid` when `NumPattern` is 0, otherwise the used elements and the phase, for example `[6 3] Phase:0`.

---

[Back to Index](../../INDEX.md)
