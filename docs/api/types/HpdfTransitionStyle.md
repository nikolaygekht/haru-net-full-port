# HpdfTransitionStyle Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfTransitionStyle` is the visual effect a viewer uses when it moves to a page in a presentation (full-screen
slide show). It is set per page with `HpdfPage.SetSlideShow`, which writes a `/Trans` transition dictionary: the
style `/S` plus, depending on the value, the direction `/Di`, the dimension `/Dm` (horizontal or vertical) and the
motion `/M` (inward or outward).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `WipeRight` | 0 | A line sweeps across the page from left to right. Writes `/S /Wipe /Di 0`. |
| `WipeUp` | 1 | A line sweeps from bottom to top. Writes `/S /Wipe /Di 90`. |
| `WipeLeft` | 2 | A line sweeps from right to left. Writes `/S /Wipe /Di 180`. |
| `WipeDown` | 3 | A line sweeps from top to bottom. Writes `/S /Wipe /Di 270`. |
| `BarnDoorsHorizontalOut` | 4 | Two horizontal lines move outward from the center. Writes `/S /Split /Dm /H /M /O`. |
| `BarnDoorsHorizontalIn` | 5 | Two horizontal lines move inward from the edges. Writes `/S /Split /Dm /H /M /I`. |
| `BarnDoorsVerticalOut` | 6 | Two vertical lines move outward from the center. Writes `/S /Split /Dm /V /M /O`. |
| `BarnDoorsVerticalIn` | 7 | Two vertical lines move inward from the edges. Writes `/S /Split /Dm /V /M /I`. |
| `BoxOut` | 8 | A rectangle grows from the center to the edges. Writes `/S /Box /M /O`. |
| `BoxIn` | 9 | A rectangle shrinks from the edges to the center. Writes `/S /Box /M /I`. |
| `BlindsHorizontal` | 10 | Horizontal stripes reveal the page, like window blinds. Writes `/S /Blinds /Dm /H`. |
| `BlindsVertical` | 11 | Vertical stripes reveal the page. Writes `/S /Blinds /Dm /V`. |
| `Dissolve` | 12 | The old page dissolves gradually into the new one. Writes `/S /Dissolve`. |
| `GlitterRight` | 13 | Like `Dissolve`, with the effect sweeping from left to right. Writes `/S /Glitter /Di 0`. |
| `GlitterDown` | 14 | Like `Dissolve`, sweeping from top to bottom. Writes `/S /Glitter /Di 270`. |
| `GlitterTopLeftToBottomRight` | 15 | Like `Dissolve`, sweeping diagonally from the top-left to the bottom-right corner. Writes `/S /Glitter /Di 315`. |
| `Replace` | 16 | The new page simply replaces the old one, with no effect. Writes `/S /R`. |
| `Eof` | 17 | Sentinel kept from the C library. **Not a valid argument:** it is written as `/S /R` (no effect). |

`SetSlideShow(type, displayTime, transTime)` also writes the transition duration `/D` (`transTime`, in seconds)
and, when `displayTime` is greater than 0, the page display duration `/Dur` (seconds before the viewer advances to
the next page). Call it once per page: a second call on the same page throws, because the page already has a
`/Trans` entry.

## Example

```csharp
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
pdf.Catalog.PageMode = HpdfPageMode.FullScreen;

var styles = new[] { HpdfTransitionStyle.WipeRight, HpdfTransitionStyle.BoxOut, HpdfTransitionStyle.Dissolve };
foreach (var style in styles)
{
    var page = pdf.AddPage();
    page.SetSlideShow(style, 5.0f, 1.0f);  // show 5 s, transition 1 s
}

pdf.SaveToFile("slideshow.pdf");
```

## Where It Is Used

| Member | Description |
|--------|-------------|
| `HpdfPage.SetSlideShow(HpdfTransitionStyle type, float displayTime, float transTime)` | Sets the transition effect and timing of the page. See [HpdfPage](../core/HpdfPage.md). |

## Related Types

- [HpdfPageMode](HpdfPageMode.md) - `FullScreen` opens the document as a presentation
- [HpdfPage](../core/HpdfPage.md) - `SetSlideShow`

---

[Back to Index](../../INDEX.md)
