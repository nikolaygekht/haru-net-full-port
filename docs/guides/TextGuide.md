# Text Guide

[← Back to Documentation Index](../INDEX.md)

This guide shows how to put text on a page: text objects, positioning, spacing and other text effects, measuring text,
wrapping it into a rectangle and rotating it. Choosing and loading fonts is covered in the [Fonts Guide](FontsGuide.md).

**Namespaces:** `Haru.Doc` (page text methods), `Haru.Font` (fonts and measuring), `Haru.Types` (alignment and
rendering mode enums)

---

## Text Objects

Text is drawn inside a **text object**, which starts with `BeginText()` and ends with `EndText()`. Inside it you
position the text and show strings; a font must be set first with `SetFontAndSize`:

```csharp
using Haru.Doc;
using Haru.Font;

var pdf = new HpdfDocument();
var page = pdf.AddPage();
var font = pdf.GetFont("Helvetica");

page.SetFontAndSize(font, 24);   // font and size in points
page.BeginText();
page.MoveTextPos(50, 750);       // baseline start, from the lower-left corner of the page
page.ShowText("Hello, PDF!");
page.EndText();

pdf.SaveToFile("text.pdf");
```

The later snippets assume the `pdf`, `page` and `font` variables of this example.

- Coordinates are in points (1/72 inch) from the **lower-left** corner of the page; `y` grows upwards. The page size
  is in `page.Width` and `page.Height` (595 x 842 for the default A4 page).
- The position is the start of the **baseline**: letters stand on it, descenders such as `g` and `p` go below it.
- `SetFontAndSize` can be called before or inside the text object. The font, the size and the other text settings
  described below belong to the page's graphics state: they stay in effect for the following text objects on the same
  page until you change them, and `GSave()`/`GRestore()` save and restore them.
- Showing text without a font throws `HpdfException` with `HpdfErrorCode.PageFontNotFound`. A new page has no font.
- The text color is the fill color: call `SetRgbFill` (or `SetGrayFill`, `SetCmykFill`) before `ShowText`; it can be
  changed inside the text object.

For a single string, `TextOut(x, y, text)` does `BeginText`, `MoveTextPos`, `ShowText` and `EndText` in one call:

```csharp
page.SetFontAndSize(font, 12);
page.SetRgbFill(0.8f, 0, 0);
page.TextOut(50, 700, "A red line printed with TextOut");
page.SetRgbFill(0, 0, 0);
```

Do not call `TextOut` between `BeginText` and `EndText`: it opens its own text object, and text objects cannot be
nested.

---

## Positioning Text

### Moving to a new line

`MoveTextPos(tx, ty)` moves to the start of a new line, **relative to the start of the current line** (not to the end
of the text just shown). `BeginText` resets the position to (0, 0), so the first `MoveTextPos` in a text object sets an
absolute position:

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.MoveTextPos(50, 650);       // absolute: (50, 650)
page.ShowText("First line");
page.MoveTextPos(0, -16);        // 16 points below the start of "First line"
page.ShowText("Second line");
page.MoveTextPos(20, -16);       // indented by 20 points
page.ShowText("Indented third line");
page.EndText();
```

Consecutive `ShowText` calls without a move continue where the previous text ended, which is how you switch font,
size or color within a line:

```csharp
var bold = pdf.GetFont("Helvetica-Bold");

page.BeginText();
page.MoveTextPos(50, 590);
page.SetFontAndSize(font, 12);
page.ShowText("Mixed ");
page.SetFontAndSize(bold, 12);
page.ShowText("bold");
page.SetFontAndSize(font, 12);
page.ShowText(" and regular text");
page.EndText();
```

### Leading and next lines

The **leading** is the distance between baselines. Once it is set, `MoveToNextLine()` moves down by it, and
`ShowTextNextLine(text)` moves down and shows text in one call:

```csharp
page.SetFontAndSize(font, 12);
page.SetTextLeading(16);
page.BeginText();
page.MoveTextPos(50, 560);
page.ShowText("Line 1");
page.ShowTextNextLine("Line 2");
page.ShowTextNextLine("");           // an empty line: the position still moves down
page.ShowTextNextLine("Line 4");
page.MoveToNextLine();
page.ShowText("Line 6");
page.EndText();
```

`MoveTextPosAndSetLeading(tx, ty)` moves like `MoveTextPos` and sets the leading to `-ty` at the same time. A
leading of about 1.2 times the font size reads well.

### Absolute positions with the text matrix

`SetTextMatrix(a, b, c, d, x, y)` sets the text position directly to (`x`, `y`), regardless of the current line.
With `a = d = 1` and `b = c = 0` it is a plain move; other values rotate, scale or skew the text
(see [Rotated and Transformed Text](#rotated-and-transformed-text)).

```csharp
page.SetFontAndSize(font, 12);
page.BeginText();
page.SetTextMatrix(1, 0, 0, 1, 300, 560);    // absolute position, no transformation
page.ShowText("At (300, 560)");
page.EndText();
```

---

## Text Spacing and Effects

| Method | Effect | Default |
|--------|--------|---------|
| `SetCharSpace(points)` | Extra space after every character | 0 |
| `SetWordSpace(points)` | Extra space after every space character (not for CID fonts) | 0 |
| `SetHorizontalScaling(percent)` | Stretches (> 100) or condenses (< 100) the text horizontally; must be positive | 100 |
| `SetTextRise(points)` | Raises (positive) or lowers (negative) the text from the baseline | 0 |
| `SetSpacingAndShowText(wordSpace, charSpace, text)` | Sets both spacings, moves to the next line and shows text | |

```csharp
page.SetFontAndSize(font, 14);
page.SetTextLeading(22);
page.BeginText();
page.MoveTextPos(50, 440);

page.SetCharSpace(3);
page.ShowText("Character spacing 3");
page.SetCharSpace(0);

page.SetWordSpace(10);
page.ShowTextNextLine("Word spacing 10 between words");
page.SetWordSpace(0);

page.SetHorizontalScaling(70);
page.ShowTextNextLine("Horizontal scaling 70%");
page.SetHorizontalScaling(150);
page.ShowTextNextLine("Scaling 150%");
page.SetHorizontalScaling(100);

page.EndText();
```

Word spacing applies only to the space character of single-byte fonts (standard, TrueType and Type 1 fonts). Viewers
ignore it for CID fonts (CJK fonts and Unicode fonts with code page 65001); use character spacing there.

### Superscript and subscript

Text rise moves the text up or down without changing the line position. Combine it with a smaller size:

```csharp
page.BeginText();
page.MoveTextPos(50, 340);
page.SetFontAndSize(font, 14);
page.ShowText("E = mc");
page.SetFontAndSize(font, 9);
page.SetTextRise(6);
page.ShowText("2");
page.SetTextRise(0);
page.SetFontAndSize(font, 14);
page.ShowText("   H");
page.SetFontAndSize(font, 9);
page.SetTextRise(-3);
page.ShowText("2");
page.SetTextRise(0);
page.SetFontAndSize(font, 14);
page.ShowText("O");
page.EndText();
```

---

## Rendering Modes

`SetTextRenderingMode` chooses whether glyphs are filled, outlined, invisible or used as a clipping path. The enum is
`HpdfTextRenderingMode` (namespace `Haru.Types`):

| Mode | Effect |
|------|--------|
| `Fill` | Filled with the fill color (default) |
| `Stroke` | Outlined with the stroke color and line width |
| `FillThenStroke` | Filled, then outlined |
| `Invisible` | Not drawn, but still selectable and searchable (useful over scanned images) |
| `FillClipping`, `StrokeClipping`, `FillStrokeClipping` | Drawn as above, then added to the clipping path |
| `Clipping` | Only added to the clipping path |

```csharp
using Haru.Types;

var heavy = pdf.GetFont("Helvetica-Bold");
page.SetFontAndSize(heavy, 40);

// Outlined text
page.SetRgbStroke(0, 0, 0.6f);
page.SetLineWidth(1);
page.SetTextRenderingMode(HpdfTextRenderingMode.Stroke);
page.TextOut(50, 250, "OUTLINE");

// Filled and outlined
page.SetRgbFill(1, 0.8f, 0);
page.SetTextRenderingMode(HpdfTextRenderingMode.FillThenStroke);
page.TextOut(300, 250, "BOTH");

// Back to normal text
page.SetTextRenderingMode(HpdfTextRenderingMode.Fill);
page.SetRgbFill(0, 0, 0);
```

The rendering mode stays in effect, like the other text settings: set it back to `Fill` when you are done.

### Text as a clipping path

With a clipping mode, the shapes of the glyphs limit everything drawn afterwards. The clipping takes effect at
`EndText`, so draw the clipped graphics after the text object, and wrap everything in `GSave`/`GRestore` to remove
the clipping again:

```csharp
using Haru.Types;

var heavy = pdf.GetFont("Helvetica-Bold");

page.GSave();
page.SetFontAndSize(heavy, 60);
page.SetTextRenderingMode(HpdfTextRenderingMode.Clipping);
page.TextOut(50, 150, "STRIPES");

// Horizontal stripes, visible only inside the letters
for (int i = 0; i < 12; i++)
{
    page.SetRgbFill(i % 2 == 0 ? 0.9f : 0.1f, 0.3f, i % 2 == 0 ? 0.1f : 0.9f);
    page.Rectangle(50, 140 + i * 6, 320, 6);
    page.Fill();
}
page.GRestore();   // removes the clipping path and restores the rendering mode
```

---

## Measuring Text

Two methods return the width of a string in points:

| Method | Measures with |
|--------|---------------|
| `font.MeasureText(text, fontSize)` | The given font and size |
| `page.TextWidth(text)` | The page's current font and size; throws `HpdfException` (`PageFontNotFound`) without a font |

Both return the width of the glyphs only: character spacing, word spacing and horizontal scaling are not included.

### Centering and right alignment

```csharp
const string title = "Centered Title";
const float size = 20;

float width = font.MeasureText(title, size);
page.SetFontAndSize(font, size);
page.TextOut((page.Width - width) / 2, 800, title);              // centered on the page

page.SetFontAndSize(font, 10);
string note = "Right-aligned note";
page.TextOut(page.Width - 50 - page.TextWidth(note), 780, note); // ends 50 points from the right edge
```

For vertical layout use the font metrics (ascent, descent, bounding box) described in the
[Fonts Guide](FontsGuide.md#font-metrics).

### How much text fits

The `MeasureText` overload in `HpdfFontExtensions` (namespace `Haru.Font`) returns how many characters of a string fit
into a width, breaking at whitespace or anywhere:

```csharp
int MeasureText(this HpdfFont font, string text, float fontSize, float width,
    float charSpace, float wordSpace, bool wordWrap, out float realWidth)
```

| Parameter | Description |
|-----------|-------------|
| `width` | Available width in points |
| `charSpace`, `wordSpace` | The spacing you will print with (pass 0 if you do not use spacing); word spacing is ignored for CID fonts |
| `wordWrap` | `true`: break only where a line may be broken (see below); `false`: break between any two characters |
| `realWidth` | Receives the width of the part that fits |
| return value | Number of characters that fit, including the whitespace at the break |

A line feed (`\n`), a carriage return (`\r`) or a `\r\n` pair always ends the measured part; the returned count then
includes the line break characters. If not even one word fits, the result with `wordWrap: true` is 0; measure again
with `wordWrap: false` to break the word.

With `wordWrap: true` a line may be broken after whitespace. Chinese and Japanese text is written without spaces, so
it also breaks between characters - but not before a character that must not begin a line (a closing bracket, a full
stop or comma, a small kana such as `っ`, the prolonged sound mark `ー`) and not after an opening bracket: the
character before goes to the next line with it. Latin words and Korean words stay whole.

The following loop wraps a paragraph into a column by hand:

```csharp
string text = "Haru.NET measures text with the metrics of the font. This paragraph is wrapped "
    + "into a column of 200 points by measuring how many characters fit on each line.\n"
    + "A line feed starts a new paragraph.";
const float size = 11, maxWidth = 200, leading = 14;

page.SetFontAndSize(font, size);
page.SetTextLeading(leading);
page.BeginText();
page.MoveTextPos(50, 700 + leading);   // ShowTextNextLine moves down first

int start = 0;
while (start < text.Length)
{
    string rest = text.Substring(start);
    int count = font.MeasureText(rest, size, maxWidth, 0, 0, true, out _);
    if (count == 0)
        count = font.MeasureText(rest, size, maxWidth, 0, 0, false, out _);   // a word longer than the line
    if (count == 0)
        break;                                                                // not even one character fits

    page.ShowTextNextLine(rest.Substring(0, count).TrimEnd());
    start += count;
}
page.EndText();
```

`TextRect`, described next, does this work for you, including alignment and the bottom edge.

---

## Text in a Rectangle

`TextRect(left, top, right, bottom, text, alignment)` wraps text into a rectangle. Call it inside a text object after
setting the font:

```csharp
using Haru.Types;

string text = "TextRect breaks the text into lines at spaces, aligns every line and stops at the "
    + "bottom edge of the rectangle. Line feeds start new paragraphs.\nThis is the second paragraph.";
float left = 50, top = 600, right = 300, bottom = 480;

// Frame, for illustration only
page.SetLineWidth(0.5f);
page.Rectangle(left, bottom, right - left, top - bottom);
page.Stroke();

page.SetFontAndSize(font, 11);
page.SetTextLeading(14);
page.BeginText();
int printed = page.TextRect(left, top, right, bottom, text, HpdfTextAlignment.Justify);
page.EndText();

Console.WriteLine($"{printed} of {text.Length} characters printed");
```

The alignment is `HpdfTextAlignment` from `Haru.Types` (not `HpdfFieldAlignment`, which is for form fields):

| Alignment | Effect |
|-----------|--------|
| `Left` | Lines start at `left` |
| `Right` | Lines end at `right` |
| `Center` | Lines are centered between `left` and `right` |
| `Justify` | Lines are stretched to the full width by adjusting the character spacing; the last line of each paragraph is left-aligned |

How the text is laid out:

- Lines break at whitespace; a word wider than the rectangle is broken between characters. `\n`, `\r\n` and a lone
  `\r` start a new line, and two line breaks leave an empty line.
- Chinese and Japanese text is written without spaces, so it also breaks between characters - but not
  before a character that must not begin a line (a closing bracket, a full stop or comma, a small kana such as `っ`,
  the prolonged sound mark `ー`) and not after an opening bracket: the character before goes to the next line with
  it. Latin words and Korean words stay whole.
- The first line is placed so that the top of the font's bounding box touches `top`; each further line is one leading
  lower. Lines are printed while the bottom of the font's bounding box stays above `bottom`.
- If the leading is 0, `TextRect` sets it to the height of the font's bounding box at the current size (about 1.16
  times the size for Helvetica). Set the leading yourself for tighter or looser lines.
- The current character spacing, word spacing (not for CID fonts) and horizontal scaling are taken into account.
  `Justify` changes the character spacing while it prints and restores it afterwards.
- The text matrix must not be rotated or scaled: `TextRect` cannot print rotated text.

### Continuing on the next page

The return value is the number of characters printed, including the whitespace consumed at line breaks. When it is
less than the length of the text, the rectangle was too small; continue with the rest on another page:

```csharp
using Haru.Types;

string article = string.Join("\n", Enumerable.Repeat(
    "Long text flows from one page to the next. TextRect prints as many lines as fit into the "
    + "rectangle and returns how many characters it used, so the rest can go on the next page.", 60));

string rest = article;
while (rest.Length > 0)
{
    var articlePage = pdf.AddPage();
    articlePage.SetFontAndSize(font, 11);
    articlePage.SetTextLeading(15);
    articlePage.BeginText();
    int used = articlePage.TextRect(50, articlePage.Height - 50, articlePage.Width - 50, 50,
        rest, HpdfTextAlignment.Justify);
    articlePage.EndText();

    if (used == 0)
        break;          // the rectangle cannot hold even one line
    rest = rest.Substring(used);
}
```

Each new page starts with no font and a leading of 0, so set both on every page. Always stop when the result is 0;
otherwise a rectangle that is too small for a single line loops forever.

---

## Rotated and Transformed Text

The text matrix `SetTextMatrix(a, b, c, d, x, y)` places text at (`x`, `y`) and transforms it. For a rotation by an
angle, use its cosine and sine:

```csharp
page.SetFontAndSize(font, 16);

for (int angle = 0; angle < 360; angle += 45)
{
    double rad = angle * Math.PI / 180;
    float cos = (float)Math.Cos(rad);
    float sin = (float)Math.Sin(rad);

    page.BeginText();
    page.SetTextMatrix(cos, sin, -sin, cos, 400, 400);   // rotate around (400, 400)
    page.ShowText($"    {angle} degrees");
    page.EndText();
}
```

| Transformation | Matrix `a, b, c, d` |
|----------------|---------------------|
| None | `1, 0, 0, 1` |
| Rotation by angle θ (counterclockwise) | `cos θ, sin θ, -sin θ, cos θ` |
| Vertical text, reading upwards | `0, 1, -1, 0` |
| Scaling by `sx`, `sy` | `sx, 0, 0, sy` |
| Skew by angles α (baseline) and β (verticals) | `1, tan α, tan β, 1` |

After `SetTextMatrix`, `MoveTextPos` and `ShowTextNextLine` move along the rotated axes, so several lines of rotated
text keep their layout:

```csharp
page.SetFontAndSize(font, 12);
page.SetTextLeading(15);
page.BeginText();
page.SetTextMatrix(0, 1, -1, 0, 560, 100);    // vertical, reading upwards, along the right edge
page.ShowText("Printed on 2026-11-26");
page.ShowTextNextLine("Second rotated line");
page.EndText();
```

The measuring methods ignore the text matrix: measured widths are in unrotated, unscaled text space, and `TextRect`
cannot print rotated text.
To rotate a whole block, including graphics, rotate the coordinate system with `Concat` inside `GSave`/`GRestore`
(see the [Graphics Guide](GraphicsGuide.md)).

---

## Text in Several Languages

`ShowText` takes a normal .NET string; the font converts it to the codes the PDF needs. What can be printed depends
on the font:

- Standard, TrueType and Type 1 fonts print the characters of one single-byte code page per font object (Western
  European by default). Characters outside it are printed as `?`.
- CID fonts print Chinese, Japanese and Korean, and with code page 65001 any character in the font file.

To mix scripts, switch fonts between `ShowText` calls, or load one Unicode CID font that covers all of them:

```csharp
using Haru.Font.CID;

var unicode = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "Noto", "fonts/noto.ttf", 65001).AsFont();
var japanese = HpdfCIDFont.LoadFromTrueTypeFile(pdf, "NotoJP", "fonts/noto-jp.ttf", 65001).AsFont();

page.SetFontAndSize(unicode, 14);
page.SetTextLeading(20);
page.BeginText();
page.MoveTextPos(50, 300);
page.ShowText("English, Deutsch: Grüße, Русский: Привет, Ελληνικά: Γειά");
page.ShowTextNextLine("Measured width: " + unicode.MeasureText("Привет", 14).ToString("0.0") + " pt");
page.MoveToNextLine();
page.SetFontAndSize(japanese, 14);
page.ShowText("日本語: こんにちは");
page.EndText();
```

Measuring, `TextRect` and character spacing work with every font type. See the [Fonts Guide](FontsGuide.md) for the
available fonts, code pages and which to choose.

---

## Pitfalls

- **`HpdfException` (`PageFontNotFound`)** from `ShowText`, `ShowTextNextLine`, `TextRect`, `TextOut` or
  `TextWidth`: no font was set on this page. Call `SetFontAndSize` first, on every new page.
- **Text at the wrong place**: `MoveTextPos` is relative to the start of the current line, and the origin is the
  lower-left corner. To position from the top, use `page.Height - distance`.
- **Nothing visible**: the rendering mode is still `Invisible` or a clipping mode from earlier drawing, or the fill
  color is white. Reset with `SetTextRenderingMode(HpdfTextRenderingMode.Fill)` and set the fill color.
- **Graphics operators inside a text object**: draw paths (`Rectangle`, `LineTo`, `Stroke`, ...) and images outside
  `BeginText`/`EndText`, and do not nest text objects (`TextOut` inside `BeginText`).
- **Settings that leak**: spacing, scaling, rise, leading and rendering mode stay in effect for the following text on
  the page. Reset them, or wrap the changed part in `GSave`/`GRestore`.
- **Word spacing has no effect** with CJK or Unicode (CID) fonts. Use `SetCharSpace`.
- **`?` instead of characters**: the font object's code page does not contain them. See the
  [Fonts Guide](FontsGuide.md).
- **`TextRect` loops**: stop when it returns 0, which happens when not even one line fits.

---

## See Also

- [Fonts Guide](FontsGuide.md) - Choosing and loading fonts, code pages, CJK and Unicode text
- [HpdfPageText](../api/extensions/HpdfPageText.md) - Text operators, `TextRect`
- [HpdfPageExtensions](../api/extensions/HpdfPageExtensions.md) - `TextOut()`, `TextWidth()`, `SetTextMatrix()` with
  six numbers
- [HpdfFont](../api/core/HpdfFont.md) - Font metrics and `MeasureText()`
- [HpdfTextAlignment](../api/types/HpdfTextAlignment.md), [HpdfTextRenderingMode](../api/types/HpdfTextRenderingMode.md),
  [HpdfTransMatrix](../api/types/HpdfTransMatrix.md)
- [Graphics Guide](GraphicsGuide.md) - Colors, paths, `GSave`/`GRestore` and coordinate transformations
- Demos in `cs-src/Haru.Demos`: `TextDemo.cs`, `AdvancedTextDemo.cs`, `TextWrappingDemo.cs`

---

[← Back to Documentation Index](../INDEX.md)
