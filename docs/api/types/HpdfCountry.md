# HpdfCountry Class

[← Back to Documentation Index](../../INDEX.md)

**Namespace:** `Haru.Types`

---

## Purpose

`HpdfCountry` is a static class of ISO 3166-1 alpha-2 country codes, a subset of the `HPDF_COUNTRY_...` constants
of the C library.

This class is **not used by the public API at present**: no public member takes a country code. The constants are
plain strings you can use in your own code.

---

## Example

```csharp
using Haru.Types;

string country = HpdfCountry.De;
Console.WriteLine(country);   // DE
```

---

## Constants

All constants are `public const string`.

| Name | Value | Country |
|------|-------|---------|
| `Us` | `"US"` | United States |
| `Gb` | `"GB"` | United Kingdom |
| `Ca` | `"CA"` | Canada |
| `Au` | `"AU"` | Australia |
| `De` | `"DE"` | Germany |
| `Fr` | `"FR"` | France |
| `It` | `"IT"` | Italy |
| `Es` | `"ES"` | Spain |
| `Jp` | `"JP"` | Japan |
| `Cn` | `"CN"` | China |
| `In` | `"IN"` | India |
| `Br` | `"BR"` | Brazil |
| `Mx` | `"MX"` | Mexico |
| `Ru` | `"RU"` | Russia |
| `Kr` | `"KR"` | Korea (South) |

---

## Related Types

- [HpdfLang](HpdfLang.md) - language codes
- [HpdfConst](HpdfConst.md) - other constants from the same source file

---

[Back to Index](../../INDEX.md)
