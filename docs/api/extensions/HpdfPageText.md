# HpdfPageText Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Doc`

---

## Overview

`HpdfPageText` is a static class with extension methods for [HpdfPage](../core/HpdfPage.md) that write text to the page content stream. Each method maps to a PDF text operator (`BT`, `Tf`, `Td`, `Tj`, ...), and `TextRect` prints a block of text wrapped inside a rectangle.

### Purpose

- **Text objects** - Start and end a block of text operations (`BeginText`, `EndText`)
- **Text state** - Font and size, character and word spacing, horizontal scaling, leading, rendering mode, rise
- **Positioning** - Move to a position, to the next line, or set the whole text matrix
- **Showing text** - Print strings, with or without moving to the next line
- **Text in a rectangle** - Wrap, align and justify text inside a rectangle (`TextRect`)

### Basic Usage

```csharp
using Haru.Doc;
using Haru.Font;
using Haru.Types;

var doc = new HpdfDocument();
var page = doc.AddPage();
var font = doc.GetFont("Helvetica", null);

page.SetFontAndSize(font, 12);

page.BeginText();
page.MoveTextPos(50, 780);
page.ShowText("A single line of text");
page.EndText();

page.BeginText();
page.TextRect(50, 760, 300, 600,
    "TextRect wraps this text inside the rectangle, line by line.",
    HpdfTextAlignment.Left);
page.EndText();

doc.SaveToFile("text.pdf");
```

The examples below assume `doc`, `page` and `font` created as above.

### Text Objects and Text State

- Text showing and text positioning methods must be called between `BeginText` and `EndText`. Paths and images go outside a text object.
- `BeginText` resets the text matrix to the identity matrix, so the first `MoveTextPos` in a text object moves to page coordinates.
- The text state (font and size, spacing, scaling, leading, rendering mode, rise) belongs to the page graphics state: it stays in effect across text objects and is saved and restored by `GSave` / `GRestore`. The text state setters may be called inside or outside a text object.
- All distances are in user space units (points, 72 points = 1 inch, unless the coordinate system has been transformed).

---

## Methods

| Name | Description |
|------|-------------|
| [BeginText](#begintext) | Starts a text object (`BT`) |
| [EndText](#endtext) | Ends a text object (`ET`) |
| [SetCharSpace](#setcharspace) | Sets the character spacing (`Tc`) |
| [SetWordSpace](#setwordspace) | Sets the word spacing (`Tw`) |
| [SetHorizontalScaling](#sethorizontalscaling) | Sets the horizontal scaling in percent (`Tz`) |
| [SetTextLeading](#settextleading) | Sets the distance between baselines (`TL`) |
| [SetFontAndSize](#setfontandsize) | Sets the font and font size (`Tf`) |
| [SetTextRenderingMode](#settextrenderingmode) | Sets fill, stroke, clipping or invisible text (`Tr`) |
| [SetTextRise](#settextrise) | Moves the baseline up or down (`Ts`) |
| [MoveTextPos](#movetextpos) | Moves to the start of a new line by an offset (`Td`) |
| [MoveTextPosAndSetLeading](#movetextposandsetleading) | Moves by an offset and sets the leading to `-ty` (`TD`) |
| [SetTextMatrix](#settextmatrix) | Sets the text matrix, e.g. for rotated text (`Tm`) |
| [MoveToNextLine](#movetonextline) | Moves to the start of the next line (`T*`) |
| [ShowText](#showtext) | Shows a string (`Tj`) |
| [ShowTextNextLine](#showtextnextline) | Moves to the next line and shows a string (`'`) |
| [SetSpacingAndShowText](#setspacingandshowtext) | Sets word and character spacing, moves to the next line and shows a string (`"`) |
| [TextRect](#textrect) | Prints text wrapped inside a rectangle |

---

## Text Object Methods

### BeginText

Begins a text object (`BT` operator). Must be paired with `EndText()`.

```csharp
public static void BeginText(this HpdfPage page)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |

**Description:**
Writes `BT` and resets the text matrix and the text line matrix to the identity matrix. The method does not check whether a text object is already open; nesting text objects produces an invalid PDF.

**Example:**

```csharp
page.BeginText();
page.SetFontAndSize(font, 14);
page.MoveTextPos(72, 720);
page.ShowText("Inside a text object");
page.EndText();

// Graphics (paths) go outside the text object
page.Rectangle(70, 715, 150, 20);
page.Stroke();
```

### EndText

Ends a text object (`ET` operator).

```csharp
public static void EndText(this HpdfPage page)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |

**Description:**
Writes `ET`. Call it once for every `BeginText()`.

---

## Text State Methods

### SetCharSpace

Sets the character spacing (`Tc` operator): extra space added after each character.

```csharp
public static void SetCharSpace(this HpdfPage page, float charSpace)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| charSpace | float | Character spacing in user space units; 0 is normal, negative values move characters closer |

**Example:**

```csharp
page.SetFontAndSize(font, 14);
page.BeginText();
page.MoveTextPos(72, 700);
page.SetCharSpace(3);
page.ShowText("S p a c e d   o u t");
page.SetCharSpace(0);
page.EndText();
```

### SetWordSpace

Sets the word spacing (`Tw` operator): extra space added to each space character.

```csharp
public static void SetWordSpace(this HpdfPage page, float wordSpace)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| wordSpace | float | Word spacing in user space units; 0 is normal |

**Note:** In PDF, word spacing applies only to the single-byte character code 32. Viewers ignore it for fonts with two-byte codes, such as CID fonts (CJK code pages, UTF-8 / Identity-H).

**Example:**

```csharp
page.SetFontAndSize(font, 14);
page.BeginText();
page.MoveTextPos(72, 700);
page.SetWordSpace(10);
page.ShowText("Wide gaps between words");
page.SetWordSpace(0);
page.EndText();
```

### SetHorizontalScaling

Sets the horizontal scaling (`Tz` operator), which stretches or condenses glyphs horizontally.

```csharp
public static void SetHorizontalScaling(this HpdfPage page, float scale)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| scale | float | Horizontal scaling in percent; 100 is normal |

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageOutOfRange` if `scale` is 0 or negative.

**Note:** `TextRect` takes horizontal scaling into account. `HpdfFont.MeasureText` and `TextWidth` do not: multiply their widths by `scale / 100` to get the width on the page.

**Example:**

```csharp
page.SetFontAndSize(font, 14);
page.BeginText();
page.MoveTextPos(72, 700);
page.SetHorizontalScaling(50);    // condensed
page.ShowText("Condensed");
page.SetHorizontalScaling(150);   // expanded
page.MoveTextPos(0, -20);
page.ShowText("Expanded");
page.SetHorizontalScaling(100);   // back to normal
page.EndText();
```

### SetTextLeading

Sets the text leading (`TL` operator): the vertical distance between the baselines of adjacent lines.

```csharp
public static void SetTextLeading(this HpdfPage page, float leading)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| leading | float | Text leading in user space units |

**Description:**
The leading is used by `MoveToNextLine`, `ShowTextNextLine`, `SetSpacingAndShowText` and `TextRect`. Its initial value is 0, so set it before using those methods (`TextRect` sets it itself when it is 0).

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.SetTextLeading(16);          // 16 points between baselines
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("First line");
page.ShowTextNextLine("Second line");
page.ShowTextNextLine("Third line");
page.EndText();
```

### SetFontAndSize

Sets the font and font size (`Tf` operator).

```csharp
public static void SetFontAndSize(this HpdfPage page, HpdfFont font, float size)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| font | HpdfFont | The font; see [HpdfFont](../core/HpdfFont.md) and `HpdfDocument.GetFont` |
| size | float | Font size in points |

**Exceptions:** `HpdfException` with `HpdfErrorCode.InvalidParameter` if `font` is null; `HpdfErrorCode.PageOutOfRange` if `size` is 0 or negative; `HpdfErrorCode.DuplicateRegistration` if the page already uses a different font object with the same resource name (`LocalName`). `HpdfDocument.GetFont` generates unique names (`F` followed by a number); give fonts you create with an explicit `localName` names of their own, such as `Ttf1`.

**Description:**
Adds the font to the page resources (once) and makes it the current font. A font must be set before `ShowText`, `TextWidth` and `TextRect`.

**Example:**

```csharp
var regular = doc.GetFont("Helvetica", null);
var bold = doc.GetFont("Helvetica-Bold", null);

page.BeginText();
page.MoveTextPos(72, 700);
page.SetFontAndSize(bold, 18);
page.ShowText("Heading");
page.MoveTextPos(0, -24);
page.SetFontAndSize(regular, 12);
page.ShowText("Body text");
page.EndText();
```

### SetTextRenderingMode

Sets the text rendering mode (`Tr` operator): whether glyphs are filled, stroked, used as a clipping path, or invisible.

```csharp
public static void SetTextRenderingMode(this HpdfPage page, HpdfTextRenderingMode mode)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| mode | HpdfTextRenderingMode | The rendering mode (namespace `Haru.Types`) |

**Rendering modes:**

| Value | PDF | Effect |
|-------|-----|--------|
| `Fill` | 0 | Fill glyphs with the fill color (default) |
| `Stroke` | 1 | Stroke glyph outlines with the stroke color and line width |
| `FillThenStroke` | 2 | Fill, then stroke |
| `Invisible` | 3 | Neither fill nor stroke (the text is still in the page, e.g. for searchable text over an image) |
| `FillClipping` | 4 | Fill and add to the clipping path |
| `StrokeClipping` | 5 | Stroke and add to the clipping path |
| `FillStrokeClipping` | 6 | Fill, stroke and add to the clipping path |
| `Clipping` | 7 | Add to the clipping path only |

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageOutOfRange` for `HpdfTextRenderingMode.RenderingModeEof` (an end marker, not a rendering mode) and any other value outside 0..7.

**Example:**

```csharp
var boldFont = doc.GetFont("Helvetica-Bold", null);

page.SetFontAndSize(boldFont, 36);
page.SetRgbFill(1, 0.8f, 0);
page.SetRgbStroke(0, 0, 0);
page.SetLineWidth(1);

page.BeginText();
page.MoveTextPos(72, 700);
page.SetTextRenderingMode(HpdfTextRenderingMode.Stroke);
page.ShowText("Outline");
page.MoveTextPos(0, -50);
page.SetTextRenderingMode(HpdfTextRenderingMode.FillThenStroke);
page.ShowText("Filled and outlined");
page.SetTextRenderingMode(HpdfTextRenderingMode.Fill);
page.EndText();
```

### SetTextRise

Sets the text rise (`Ts` operator): moves the baseline up (positive) or down (negative) for superscripts and subscripts.

```csharp
public static void SetTextRise(this HpdfPage page, float rise)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| rise | float | Text rise in user space units |

**Example:**

```csharp
page.BeginText();
page.MoveTextPos(72, 700);
page.SetFontAndSize(font, 14);
page.ShowText("E = mc");
page.SetFontAndSize(font, 9);
page.SetTextRise(6);              // superscript
page.ShowText("2");
page.SetTextRise(0);
page.EndText();
```

---

## Text Positioning Methods

### MoveTextPos

Moves to the start of the next line, offset from the start of the current line (`Td` operator).

```csharp
public static void MoveTextPos(this HpdfPage page, float tx, float ty)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| tx | float | Horizontal offset in text space |
| ty | float | Vertical offset in text space (negative moves down) |

**Description:**
The offset is relative to the start of the current line, not to the end of the text shown on it. Right after `BeginText` the line starts at (0, 0), so the first call positions the text in page coordinates.

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(72, 700);        // absolute: the text matrix starts at the origin
page.ShowText("Line at (72, 700)");
page.MoveTextPos(20, -20);        // relative to the start of the previous line
page.ShowText("Line at (92, 680)");
page.EndText();
```

### MoveTextPosAndSetLeading

Moves to the start of the next line and sets the leading (`TD` operator). Equivalent to `SetTextLeading(-ty); MoveTextPos(tx, ty);`.

```csharp
public static void MoveTextPosAndSetLeading(this HpdfPage page, float tx, float ty)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| tx | float | Horizontal offset in text space |
| ty | float | Vertical offset in text space; the leading becomes `-ty` |

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("First line");
page.MoveTextPosAndSetLeading(0, -18);   // moves down 18 and sets the leading to 18
page.ShowText("Second line");
page.ShowTextNextLine("Third line, 18 points lower again");
page.EndText();
```

### SetTextMatrix

Sets the text matrix and the text line matrix (`Tm` operator).

```csharp
public static void SetTextMatrix(this HpdfPage page, HpdfTransMatrix matrix)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| matrix | HpdfTransMatrix | The matrix `[a b c d x y]` (namespace `Haru.Types`); `x`, `y` is the start of the line |

**Description:**
Replaces the text matrix (it is not combined with the current one). Use it for rotated, skewed or scaled text, or to position text absolutely within a text object. Later `MoveTextPos` offsets are measured in the coordinate system of this matrix. An overload taking six `float` values is in `HpdfPageExtensions` (see [Related Helpers](#related-helpers-in-hpdfpageextensions)).

**Example:**

```csharp
float angle = 30 * MathF.PI / 180;
float cos = MathF.Cos(angle);
float sin = MathF.Sin(angle);

page.SetFontAndSize(font, 16);
page.BeginText();
page.SetTextMatrix(new HpdfTransMatrix(cos, sin, -sin, cos, 200, 500));
page.ShowText("Rotated by 30 degrees");
page.EndText();
```

### MoveToNextLine

Moves to the start of the next line (`T*` operator), one leading below the start of the current line.

```csharp
public static void MoveToNextLine(this HpdfPage page)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |

**Description:**
Uses the leading set by `SetTextLeading` or `MoveTextPosAndSetLeading`. With the default leading of 0 the position does not change.

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.SetTextLeading(15);
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("Line 1");
page.MoveToNextLine();
page.MoveToNextLine();            // leaves an empty line
page.ShowText("Line 3");
page.EndText();
```

---

## Text Showing Methods

All three methods encode the text for the current font: CID fonts write glyph IDs as a hex string, other fonts write the bytes of the font's encoding as a literal string.

A font must be set with `SetFontAndSize` first: showing text without a current font throws `HpdfException` with `HpdfErrorCode.PageFontNotFound`, as in the C library. Empty text is treated per method: `ShowText` writes nothing, `ShowTextNextLine` still moves to the next line, and `SetSpacingAndShowText` still sets the spacing and moves to the next line.

### ShowText

Shows a text string at the current text position (`Tj` operator).

```csharp
public static void ShowText(this HpdfPage page, string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| text | string | The text to show |

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageFontNotFound` if no font has been set on the page and `text` is not empty.

**Description:**
The text position advances by the width of the text, so a following `ShowText` continues on the same line. `ShowText` does not wrap or break lines; use `TextRect` for that. A null or empty `text` writes nothing.

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("Hello, ");
page.ShowText("World!");          // continues right after "Hello, "
page.EndText();
```

### ShowTextNextLine

Moves to the next line and shows a text string (`'` operator). Equivalent to `MoveToNextLine(); ShowText(text);`.

```csharp
public static void ShowTextNextLine(this HpdfPage page, string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| text | string | The text to show |

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageFontNotFound` if no font has been set on the page and `text` is not empty.

**Description:**
The line moves down by the current leading before the text is shown. If `text` is null or empty, the method still moves to the next line (it writes `T*`, like `MoveToNextLine`), so an empty string in a list of lines leaves an empty line.

**Example:**

```csharp
string[] lines = { "Apples", "Oranges", "Pears" };

page.SetFontAndSize(font, 12);
page.SetTextLeading(14);
page.BeginText();
page.MoveTextPos(72, 714);        // one leading above the first baseline (700)
foreach (string line in lines)
    page.ShowTextNextLine(line);
page.EndText();
```

### SetSpacingAndShowText

Sets the word and character spacing, moves to the next line and shows a text string (`"` operator).

```csharp
public static void SetSpacingAndShowText(this HpdfPage page, float wordSpace, float charSpace, string text)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| wordSpace | float | Word spacing, as in `SetWordSpace` |
| charSpace | float | Character spacing, as in `SetCharSpace` |
| text | string | The text to show |

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageFontNotFound` if no font has been set on the page (also for empty text). Nothing is written in that case.

**Description:**
The spacing values stay in effect after the call. If `text` is null or empty, the operator is still written with an empty string: the spacing is set and the position moves to the next line.

**Example:**

```csharp
page.SetFontAndSize(font, 12);
page.SetTextLeading(16);
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("Normal spacing");
page.SetSpacingAndShowText(8, 1, "Word and character spacing");
page.SetWordSpace(0);
page.SetCharSpace(0);
page.EndText();
```

---

## Text in a Rectangle

### TextRect

Prints text inside a rectangle, wrapping it into lines (port of `HPDF_Page_TextRect`).

```csharp
public static int TextRect(this HpdfPage page, float left, float top, float right, float bottom,
    string text, HpdfTextAlignment alignment)
```

**Parameters:**

| Name | Type | Description |
|------|------|-------------|
| page | HpdfPage | The page |
| left | float | Left edge of the rectangle |
| top | float | Top edge of the rectangle |
| right | float | Right edge of the rectangle |
| bottom | float | Bottom edge of the rectangle |
| text | string | The text to print |
| alignment | HpdfTextAlignment | Horizontal alignment of the lines: `Left`, `Right`, `Center` or `Justify` (namespace `Haru.Types`). With a font of vertical writing it aligns the columns along their height |

**Return Value:**
`int` - the number of characters of `text` that were printed, including the whitespace consumed at line breaks. It is less than `text.Length` if the rectangle is too small for the whole text, and 0 if `text` is null or empty or not even the first line fits.

**Exceptions:** `HpdfException` with `HpdfErrorCode.PageFontNotFound` if no font has been set on the page.

**Requirements:**

- Call it between `BeginText` and `EndText`, after `SetFontAndSize`. Only the font is checked; calling it outside a text object writes text operators outside a text object.
- The rectangle is given in page coordinates. The text matrix is expected to have no rotation or scaling (as after `BeginText`, `MoveTextPos` or a translation-only `SetTextMatrix`).
- The alignment type is `Haru.Types.HpdfTextAlignment`. Do not confuse it with `Haru.Forms.HpdfFieldAlignment`, which sets the alignment of form text fields.

#### How lines are formed

- Lines break at whitespace. Whitespace at the end of a line is not printed.
- A word wider than the rectangle is broken between characters.
- A line feed (`\n`) starts a new line; two line feeds leave an empty line. A `\r\n` pair and a carriage return alone (`\r`) work the same way.
- For `Left`, `Right` and `Center`, line widths are measured with the current character and word spacing. Word spacing is ignored for CID fonts, to which viewers do not apply it.
- Horizontal scaling (`SetHorizontalScaling`) is taken into account: at 50% about twice as much text fits on a line, and the scaled lines are aligned within the rectangle.

#### Vertical layout

- If the text leading is 0, `TextRect` sets it (with `SetTextLeading`) to the height of the font's bounding box at the current font size. The new leading stays in effect after the call.
- The first baseline is placed so that the top of the font's bounding box touches `top`; each further line is one leading lower.
- A line is printed only if the bottom of the font's bounding box stays at or above `bottom`. Printing stops at the first line that would go below it.

#### Alignment

| Alignment | Effect |
|-----------|--------|
| `Left` | Lines start at `left` |
| `Right` | Lines end at `right` |
| `Center` | Lines are centered between `left` and `right` |
| `Justify` | Every line except the last line of each paragraph (and the last line of the text) is stretched to the full width by changing the character spacing; the other lines start at `left` |

With `Justify`, lines are formed as if the character spacing were 0. The original character spacing is used for the last line of each paragraph and restored after the call.

#### Fonts of vertical writing

With a font of vertical writing (`HpdfFont.WritingMode` is `Vertical`, see the
[Fonts Guide](../../guides/FontsGuide.md#vertical-writing)) the text is laid out in columns instead of lines:

- A column runs from `top` down to `bottom`. It breaks as a line does: at whitespace, between characters if there is none (CJK text), and at a line feed.
- The first column is at the right edge of the rectangle; the next ones follow to the left, one text leading apart. If the text leading is 0, `TextRect` sets it to the width of a column, the font size.
- Columns are printed while they fit between `right` and `left`.
- The alignment works along the column:

| Alignment | Effect |
|-----------|--------|
| `Left` | Columns start at `top` |
| `Right` | Columns end at `bottom` |
| `Center` | Columns are centered between `top` and `bottom` |
| `Justify` | Every column except the last column of each paragraph is stretched to the full height by changing the character spacing; the other columns start at `top` |

```csharp
var vertical = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "JV", "fonts/noto-jp.ttf", 932, HpdfWritingMode.Vertical).AsFont();

page.BeginText();
page.SetFontAndSize(vertical, 14);
page.SetTextLeading(20);                 // the distance between the columns
int printed = page.TextRect(300, 760, 480, 560, "吾輩は猫である。名前はまだ無い。", HpdfTextAlignment.Justify);
page.EndText();
```

**Example: text in a box**

```csharp
string text = "Haru.NET prints this paragraph inside the rectangle. Lines break at spaces, "
    + "and the text stops at the bottom edge of the rectangle.";

float left = 50, top = 750, right = 250, bottom = 650;

// Frame, for illustration only
page.Rectangle(left, bottom, right - left, top - bottom);
page.Stroke();

page.SetFontAndSize(font, 11);
page.SetTextLeading(14);
page.BeginText();
int printed = page.TextRect(left, top, right, bottom, text, HpdfTextAlignment.Left);
page.EndText();

Console.WriteLine($"{printed} of {text.Length} characters printed");
```

**Example: the four alignments**

```csharp
var times = doc.GetFont("Times-Roman", null);
string text = "The quick brown fox jumps over the lazy dog. "
    + "Pack my box with five dozen liquor jugs.";

var alignments = new[]
{
    HpdfTextAlignment.Left, HpdfTextAlignment.Right,
    HpdfTextAlignment.Center, HpdfTextAlignment.Justify
};

page.SetFontAndSize(times, 12);
page.SetTextLeading(15);

float top = 780;
foreach (var alignment in alignments)
{
    page.BeginText();
    page.TextRect(72, top, 272, top - 80, text, alignment);
    page.EndText();
    top -= 100;
}
```

**Example: paragraphs and justification**

```csharp
string text = "The first paragraph is justified: all of its lines but the last one "
    + "are stretched to the full width of the rectangle.\n"
    + "\n"
    + "A line feed starts a new line; two line feeds leave an empty line.";

page.SetFontAndSize(font, 10);
page.SetTextLeading(13);
page.BeginText();
page.TextRect(72, 760, 300, 500, text, HpdfTextAlignment.Justify);
page.EndText();
```

**Example: continuing text on the next page**

The return value tells where to continue when the rectangle is full:

```csharp
var doc = new HpdfDocument();
var font = doc.GetFont("Helvetica", null);
string text = string.Concat(Enumerable.Repeat(
    "This long text does not fit on one page, so it continues on the next one. ", 200));

int start = 0;
while (start < text.Length)
{
    var page = doc.AddPage();
    page.SetFontAndSize(font, 11);
    page.SetTextLeading(14);

    page.BeginText();
    int printed = page.TextRect(50, page.Height - 50, page.Width - 50, 50,
        text.Substring(start), HpdfTextAlignment.Justify);
    page.EndText();

    if (printed == 0)
        break;                    // not even one character fits: stop instead of looping forever
    start += printed;
}

doc.SaveToFile("long-text.pdf");
```

**Example: handling a missing font**

```csharp
var doc = new HpdfDocument();
var page = doc.AddPage();

page.BeginText();
try
{
    page.TextRect(50, 750, 250, 650, "No font yet", HpdfTextAlignment.Left);
}
catch (HpdfException ex) when (ex.ErrorCode == HpdfErrorCode.PageFontNotFound)
{
    Console.WriteLine("Call SetFontAndSize before TextRect");
}
page.EndText();
```

`HpdfException` and `HpdfErrorCode` are in the `Haru` namespace.

#### Limitations

- The measurement ignores text rise (`SetTextRise`).
- Rotated or scaled text matrices are not supported.
- Differences from the C library: `HPDF_Page_TextRect` returns a status code (`HPDF_PAGE_INSUFFICIENT_SPACE` when the text does not fit) and the length through an output parameter; `TextRect` returns the length and does not throw when the text does not fit.

---

## Measuring Text

To lay out text yourself, `HpdfFont` measures widths (see [HpdfFont](../core/HpdfFont.md)):

- `font.MeasureText(text, fontSize)` returns the width of a string in user space units.
- The extension method `MeasureText(text, fontSize, width, charSpace, wordSpace, wordWrap, out realWidth)` in `HpdfFontExtensions` (namespace `Haru.Font`) returns how many characters fit into `width`. With `wordWrap` set to `true` it breaks only after whitespace; with `false` it breaks between any characters. It also stops after a line feed or a carriage return that is not followed by a line feed (the break character is included in the count). `wordSpace` is ignored for CID fonts. `realWidth` receives the width of the part that fits, without the whitespace at the break. `TextRect` uses this method to form its lines.

```csharp
string text = "How much of this sentence fits into 120 points?";

int fits = font.MeasureText(text, 12, 120, 0, 0, true, out float realWidth);
Console.WriteLine($"\"{text.Substring(0, fits)}\" ({realWidth:F1} pt)");
```

---

## Related Helpers in HpdfPageExtensions

`HpdfPageExtensions` (namespace `Haru.Doc`) adds a few text shortcuts to `HpdfPage`:

| Method | Description |
|--------|-------------|
| `TextOut(float x, float y, string text)` | `BeginText` + `MoveTextPos(x, y)` + `ShowText(text)` + `EndText` in one call; throws `HpdfException` (`PageFontNotFound`) before writing anything if no font is set |
| `TextWidth(string text)` | Width of `text` in the current font and size; throws `HpdfException` (`PageFontNotFound`) if no font is set |
| `GetCurrentFont()` | The current font, or null |
| `GetCurrentFontSize()` | The current font size |
| `SetTextMatrix(float a, float b, float c, float d, float x, float y)` | `SetTextMatrix` with six numbers instead of an `HpdfTransMatrix` |

```csharp
page.SetFontAndSize(font, 12);

// BeginText + MoveTextPos + ShowText + EndText in one call
page.TextOut(72, 700, "Quick text");

// Width of a string in the current font and size
float width = page.TextWidth("Measure me");

// Current font and size
HpdfFont? current = page.GetCurrentFont();
float size = page.GetCurrentFontSize();

// SetTextMatrix with six numbers instead of an HpdfTransMatrix
page.BeginText();
page.SetTextMatrix(1, 0, 0, 1, 72 + width + 10, 700);
page.ShowText($"{current?.BaseFont} {size}");
page.EndText();
```

---

## Best Practices

### 1. Set the Font First

`ShowText`, the other showing methods, `TextRect`, `TextOut` and `TextWidth` throw `HpdfException` (`PageFontNotFound`) without a current font.

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(72, 700);
page.ShowText("Text");
page.EndText();
```

### 2. Keep Text Operations Inside BeginText / EndText

Do not draw paths or images inside a text object, and do not nest text objects.

### 3. Set the Leading Before Line-Based Methods

`MoveToNextLine`, `ShowTextNextLine` and `SetSpacingAndShowText` move by the leading, which is 0 until it is set.

### 4. Reset Temporary Text State

Spacing, scaling, rise and rendering mode stay in effect for later text on the page. Reset them after use, or wrap the code in `GSave` / `GRestore`.

### 5. Check the Return Value of TextRect

If it is less than the text length, the rest of the text did not fit; continue it in another rectangle or on the next page.

---

## See Also

### Core Classes
- [HpdfPage](../core/HpdfPage.md) - Page properties and methods
- [HpdfFont](../core/HpdfFont.md) - Fonts and text measurement
- [HpdfDocument](../core/HpdfDocument.md) - Document management and `GetFont`
- [HpdfImage](../core/HpdfImage.md) - Images

### Extension Methods
- [HpdfPageGraphics](HpdfPageGraphics.md) - Paths, colors, line styles
- [HpdfPageShapes](HpdfPageShapes.md) - Circles, ellipses, arcs

### Guides
- [Getting Started](../../guides/GettingStarted.md) - Step-by-step tutorial
- [Usage](../../USAGE.md) - Including and using the library
- [Structure](../../STRUCTURE.md) - Library structure

---

[Back to Index](../../INDEX.md)
