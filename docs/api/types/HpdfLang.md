# HpdfLang Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfLang` is a static class of ISO 639-1 two-letter language codes, a subset of the `HPDF_LANG_...` constants of
the C library.

This class is **not used by the public API at present**: Haru.NET has no public member that sets a document or
content language (the catalog `/Lang` entry is not supported). The constants are plain strings you can use in your
own code.

---

## Example

```csharp
using Haru.Types;

string language = HpdfLang.Ja;
Console.WriteLine(language);   // ja
```

---

## Constants

All constants are `public const string`.

| Name | Value | Language |
|------|-------|----------|
| `En` | `"en"` | English |
| `De` | `"de"` | German |
| `Fr` | `"fr"` | French |
| `Es` | `"es"` | Spanish |
| `It` | `"it"` | Italian |
| `Ja` | `"ja"` | Japanese |
| `Zh` | `"zh"` | Chinese |
| `Pt` | `"pt"` | Portuguese |
| `Ru` | `"ru"` | Russian |
| `Ar` | `"ar"` | Arabic |
| `Hi` | `"hi"` | Hindi |
| `Ko` | `"ko"` | Korean |
| `Nl` | `"nl"` | Dutch |
| `Sv` | `"sv"` | Swedish |
| `Pl` | `"pl"` | Polish |

---

## Related Types

- [HpdfCountry](HpdfCountry.md) - country codes
- [HpdfConst](HpdfConst.md) - other constants from the same source file

---

[Back to Index](../../INDEX.md)
