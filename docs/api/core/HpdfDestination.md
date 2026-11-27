# HpdfDestination Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Purpose

`HpdfDestination` is an explicit destination: a page of the document together with a view (position and zoom) that a
viewer jumps to. Destinations are the targets of bookmarks ([HpdfOutline](HpdfOutline.md)), internal links
([HpdfLinkAnnotation](../annotations/HpdfLinkAnnotation.md)) and the document open action
([HpdfCatalog.SetOpenAction](HpdfCatalog.md#method-setopenaction)). A destination can also be given a name
([HpdfNamedDestination](HpdfNamedDestination.md)), which links and bookmarks refer to instead.

---

## Getting an Instance

Create a destination for a page with the `CreateDestination()` extension method of the page (from
[HpdfPageExtensions](../extensions/HpdfPageExtensions.md)) or with the constructor, then set its view with one of
the `Set...` methods. A new destination has no view, and a destination without a view is not valid in a PDF file.

```csharp
var destination = page.CreateDestination();      // same as new HpdfDestination(page)
destination.SetFit();
```

**Constructor declaration:**
```csharp
public HpdfDestination(HpdfPage page)
```

| Name | Type | Description |
|------|------|-------------|
| page | [HpdfPage](HpdfPage.md) | The target page. `null` throws `HpdfException` with `HpdfErrorCode.InvalidParameter`. |

The destination is not added to the cross-reference table: its array is written directly into each object that uses
it.

---

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var font = pdf.GetFont("Helvetica");

var first = pdf.AddPage();
var second = pdf.AddPage();
second.SetFontAndSize(font, 24);
second.TextOut(50, 700, "Second page");

// Show the top of the second page at 100 % zoom
var target = second.CreateDestination();
target.SetXYZ(0, second.Height, 1);

// A bookmark and a link on the first page both go there
pdf.CreateOutline("Second page").SetDestination(target);

first.SetFontAndSize(font, 12);
first.TextOut(50, 750, "Go to the second page");
first.CreateLinkAnnotation(new HpdfRect(45, 740, 200, 765), target);

pdf.SaveToFile("destination.pdf");
```

---

## Properties

| Name | Type | Description |
|------|------|-------------|
| [DestArray](#property-destarray) | `HpdfArray` | The destination array (`[page /Type parameters...]`) |
| [Page](#property-page) | [HpdfPage](HpdfPage.md) | The target page |

---

## Methods

| Name | PDF form | Description |
|------|----------|-------------|
| [SetXYZ()](#method-setxyz) | `[page /XYZ left top zoom]` | A position on the page at a zoom factor |
| [SetFit()](#method-setfit) | `[page /Fit]` | The whole page fitted into the window |
| [SetFitH()](#method-setfith) | `[page /FitH top]` | The page width fitted into the window, at a given top |
| [SetFitV()](#method-setfitv) | `[page /FitV left]` | The page height fitted into the window, at a given left |
| [SetFitR()](#method-setfitr) | `[page /FitR left bottom right top]` | A rectangle fitted into the window |
| [SetFitB()](#method-setfitb) | `[page /FitB]` | The bounding box of the page contents fitted into the window |
| [SetFitBH()](#method-setfitbh) | `[page /FitBH top]` | The width of the bounding box fitted into the window |
| [SetFitBV()](#method-setfitbv) | `[page /FitBV left]` | The height of the bounding box fitted into the window |

Each `Set...` method replaces the view set before: the array keeps only the page and gets the new type and
parameters. Coordinates are in default user space units (points, origin at the lower-left corner of the page).
None of the methods validates its arguments.

---

## Detailed Descriptions

#### Property: *DestArray*

**Declaration:**
```csharp
public HpdfArray DestArray { get; }
```

**Description:**
The destination array. Its first element is the page dictionary; after a `Set...` call it also holds the view type
and its parameters. Pass this array where an `HpdfArray` destination is expected, for example to
`HpdfPage.CreateLinkAnnotation(HpdfRect, HpdfArray)` or `HpdfOutline.SetDestination(HpdfArray)`; both also have an
overload that takes the `HpdfDestination` itself.

Every user of the destination stores this same array object, so a later `Set...` call changes the view of every
bookmark, link and open action that uses the destination.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetFit();
Console.WriteLine(destination.DestArray.Count);   // 2: the page and /Fit
```

---

#### Property: *Page*

**Declaration:**
```csharp
public HpdfPage Page { get; }
```

**Description:**
The page given when the destination was created.

---

#### Method: *SetXYZ()*

**Declaration:**
```csharp
public void SetXYZ(float left, float top, float zoom)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | The horizontal coordinate placed at the left edge of the window; 0 or more |
| top | `float` | The vertical coordinate placed at the top edge of the window; 0 or more |
| zoom | `float` | The zoom factor, from `0.08` (8 %) to `32` (3200 %): `1` is 100 %, `2` is 200 %. `0` keeps the zoom the viewer currently uses (PDF 1.7, 12.3.2.2) |

**Return Value:**
`void`

**Exceptions:**

| Exception | Condition |
|-----------|-----------|
| `HpdfException` (`InvalidParameter`) | `left` or `top` is negative or `NaN` |
| `HpdfException` (`InvalidParameter`) | `zoom` is not `0` and outside 0.08..32, or `NaN` |

**Description:**
Writes `[page /XYZ left top zoom]`. To show the top-left corner of a page, use `0` for `left` and the page height
for `top`. The values are checked as in the C library, except that a zoom of `0` is accepted; after an exception
the destination is unchanged.

The parameters cannot be `null`, so this method cannot express the PDF `null` value that keeps the current `left` or
`top`.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetXYZ(0, page.Height, 2);   // top-left corner at 200 %
```

---

#### Method: *SetFit()*

**Declaration:**
```csharp
public void SetFit()
```

**Return Value:**
`void`

**Description:**
Writes `[page /Fit]`: the whole page is shown, magnified as much as possible while still fitting the window both
horizontally and vertically.

---

#### Method: *SetFitH()*

**Declaration:**
```csharp
public void SetFitH(float top)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| top | `float` | The vertical coordinate placed at the top edge of the window |

**Return Value:**
`void`

**Description:**
Writes `[page /FitH top]`: the page width fits the window.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetFitH(400);   // full width, starting at y = 400
```

---

#### Method: *SetFitV()*

**Declaration:**
```csharp
public void SetFitV(float left)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | The horizontal coordinate placed at the left edge of the window |

**Return Value:**
`void`

**Description:**
Writes `[page /FitV left]`: the page height fits the window.

---

#### Method: *SetFitR()*

**Declaration:**
```csharp
public void SetFitR(float left, float bottom, float right, float top)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | Left edge of the rectangle |
| bottom | `float` | Bottom edge of the rectangle |
| right | `float` | Right edge of the rectangle |
| top | `float` | Top edge of the rectangle |

**Return Value:**
`void`

**Description:**
Writes `[page /FitR left bottom right top]`: the rectangle is magnified to fit the window. If the window and the
rectangle have different proportions, the viewer uses the smaller of the horizontal and vertical factors and centers
the rectangle in the other direction.

**Example:**
```csharp
var destination = page.CreateDestination();
destination.SetFitR(50, 500, 300, 700);   // zoom into a 250 x 200 point area
```

---

#### Method: *SetFitB()*

**Declaration:**
```csharp
public void SetFitB()
```

**Return Value:**
`void`

**Description:**
Writes `[page /FitB]`: like `SetFit()`, but for the bounding box of the page contents instead of the whole page.
The viewer computes the bounding box.

---

#### Method: *SetFitBH()*

**Declaration:**
```csharp
public void SetFitBH(float top)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| top | `float` | The vertical coordinate placed at the top edge of the window |

**Return Value:**
`void`

**Description:**
Writes `[page /FitBH top]`: the width of the bounding box of the page contents fits the window.

---

#### Method: *SetFitBV()*

**Declaration:**
```csharp
public void SetFitBV(float left)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| left | `float` | The horizontal coordinate placed at the left edge of the window |

**Return Value:**
`void`

**Description:**
Writes `[page /FitBV left]`: the height of the bounding box of the page contents fits the window.

---

## Remarks

- **Set a view before use.** `HpdfCatalog.SetOpenAction`, `HpdfOutline.SetDestination` and
  `HpdfPage.CreateLinkAnnotation` reject a destination without a view (`[page]` only) with `HpdfException`
  (`HpdfErrorCode.InvalidDestination`), whether it is passed as an `HpdfDestination` or as its `DestArray`.
- **Same document.** The target page must belong to the document that uses the destination; this is not checked.
- **Named destinations** (`/Dests`, `/Names`) are not supported: destinations are always written as explicit arrays.

---

## Related Types

- [HpdfOutline](HpdfOutline.md) - Bookmarks that navigate to a destination
- [HpdfLinkAnnotation](../annotations/HpdfLinkAnnotation.md) - Links on a page
- [HpdfNamedDestination](HpdfNamedDestination.md) - A name for a destination
- [HpdfCatalog](HpdfCatalog.md) - `SetOpenAction` for the first view of the document
- [HpdfPageExtensions](../extensions/HpdfPageExtensions.md) - `CreateDestination()`
- [Outlines Guide](../../guides/OutlinesGuide.md)

---

[Back to Index](../../INDEX.md)
