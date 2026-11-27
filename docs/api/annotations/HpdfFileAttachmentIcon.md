# HpdfFileAttachmentIcon Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfFileAttachmentIcon` is the icon of a file attachment annotation. It is passed to
`HpdfPage.CreateFileAttachmentAnnotation()` and
[HpdfFileAttachmentAnnotation.SetIcon()](HpdfFileAttachmentAnnotation.md#method-seticon), written as `/Name`, and
returned by [HpdfFileAttachmentAnnotation.Icon](HpdfFileAttachmentAnnotation.md#property-icon).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `PushPin` | 0 | A push pin (the default). Writes `/Name /PushPin` |
| `Graph` | 1 | A bar chart. Writes `/Name /Graph` |
| `Paperclip` | 2 | A paper clip. Writes `/Name /Paperclip` |
| `Tag` | 3 | A label tag. Writes `/Name /Tag` |

The library draws the icon itself in the appearance stream, in the annotation color (blue until it is changed), so it
looks the same in every viewer. A value outside the enum throws `HpdfException` (`AnnotInvalidIcon`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var file = pdf.AttachFile("data.csv", System.Text.Encoding.UTF8.GetBytes("a,b\n1,2\n"), "text/csv");

float x = 60;
foreach (var icon in Enum.GetValues<HpdfFileAttachmentIcon>())
{
    page.CreateFileAttachmentAnnotation(new HpdfRect(x, 700, x + 24, 724), file, $"data.csv ({icon})", icon);
    x += 50;
}

pdf.SaveToFile("icons.pdf");
```

## Related Types

- [HpdfFileAttachmentAnnotation](HpdfFileAttachmentAnnotation.md) - `SetIcon()`, `Icon`
- [HpdfAnnotationIcon](HpdfAnnotationIcon.md) - the icons of sticky notes

---

[Back to Index](../../INDEX.md)
