# HpdfReplyType Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfReplyType` tells how a markup annotation relates to the annotation it refers to: as a reply in its thread of
comments, or as a member of a group. It is passed to
[HpdfMarkupAnnotation.SetInReplyTo()](HpdfMarkupAnnotation.md#method-setinreplyto) and returned by
[HpdfMarkupAnnotation.ReplyType](HpdfMarkupAnnotation.md#property-replytype).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `Reply` | 0 | A reply to the other annotation (the default). Viewers show it in the comment thread of the annotation it answers. `/RT` is not written, since `/R` is its default |
| `Group` | 1 | Grouped with the other annotation: viewers treat the two as one unit. Writes `/RT /Group` (PDF 1.6) |

A value outside the enum makes `SetInReplyTo()` throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

// A square and an arrow that belong together
var square = page.CreateSquareAnnotation(new HpdfRect(300, 600, 450, 700), "This figure is wrong");
var arrow = page.CreateLineAnnotation(new HpdfPoint(150, 560), new HpdfPoint(300, 640), "");
arrow.SetLineEndingStyle(HpdfLineEndingStyle.None, HpdfLineEndingStyle.OpenArrow);
arrow.SetInReplyTo(square, HpdfReplyType.Group);

pdf.SaveToFile("group.pdf");
```

## Related Types

- [HpdfMarkupAnnotation](HpdfMarkupAnnotation.md) - `SetInReplyTo()`, `InReplyTo`, `ReplyType`

---

[Back to Index](../../INDEX.md)
