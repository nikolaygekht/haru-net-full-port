# HpdfCaretSymbol Enum

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Annotations`

---

## Purpose

`HpdfCaretSymbol` is the symbol associated with a caret annotation: none, or a paragraph sign that asks for a new
paragraph. It is passed to [HpdfCaretAnnotation.SetSymbol()](HpdfCaretAnnotation.md#method-setsymbol), written as
`/Sy`, and returned by [HpdfCaretAnnotation.Symbol](HpdfCaretAnnotation.md#property-symbol).

## Values

| Name | Value | Description |
|------|-------|-------------|
| `None` | 0 | No symbol. Writes `/Sy /None`. The generated appearance is the caret alone |
| `Paragraph` | 1 | A new paragraph. Writes `/Sy /P`. The generated appearance draws the caret in the left part of the box and a pilcrow (¶) beside it |

Without a call to `SetSymbol()`, `/Sy` is absent, which means `None`. A value outside the enum makes `SetSymbol()`
throw `HpdfException` (`InvalidParameter`).

## Example

```csharp
using Haru.Annotations;
using Haru.Doc;
using Haru.Types;

var pdf = new HpdfDocument();
var page = pdf.AddPage();

var caret = page.CreateCaretAnnotation(new HpdfRect(200, 700, 216, 712), "Start a new paragraph here");
caret.SetSymbol(HpdfCaretSymbol.Paragraph);

pdf.SaveToFile("caret.pdf");
```

## Related Types

- [HpdfCaretAnnotation](HpdfCaretAnnotation.md) - `SetSymbol()`, `Symbol`

---

[Back to Index](../../INDEX.md)
