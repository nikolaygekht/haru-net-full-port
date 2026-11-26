# API breaking changes: FluentAssertions ≤7.x → AwesomeAssertions 8/9

AwesomeAssertions 8.0 is API-identical to FluentAssertions 8.0, so all FluentAssertions
7→8 breaking changes apply when migrating from v7 or earlier. AwesomeAssertions 9.0
added no functional changes on top — only the namespace/assembly rename covered in
SKILL.md Step 3.

Everything below is grep-able: search the codebase for the "old" name; if absent, the
item doesn't apply. Coming from v6 or earlier, also expect members that were marked
`[Obsolete]` during v6 to be gone entirely — the compiler errors name them, and the
new name is usually the suggestion from the obsolete message.

## Renamed assertion methods

| Old (≤7) | New (8+) | On |
|---|---|---|
| `HaveCountGreaterOrEqualTo` | `HaveCountGreaterThanOrEqualTo` | collections |
| `HaveCountLessOrEqualTo` | `HaveCountLessThanOrEqualTo` | collections |
| `BeGreaterOrEqualTo` | `BeGreaterThanOrEqualTo` | `IComparable`, `TimeSpan`, execution time |
| `BeLessOrEqualTo` | `BeLessThanOrEqualTo` | `IComparable`, `TimeSpan`, execution time |
| `HaveAttribute` (with expected value) | `HaveAttributeWithValue` | `XElement` |

Note: numeric assertions already had the `...ThanOrEqualTo` names in v6/v7, so most
`BeGreaterThanOrEqualTo` calls in the source are already fine — only the short forms
need renaming.

## Renamed equivalency types and options

| Old (≤7) | New (8+) |
|---|---|
| `EquivalencyAssertionOptions` | `EquivalencyOptions` |
| `EquivalencyAssertionOptions<TExpectation>` | `EquivalencyOptions<TExpectation>` |
| `IEquivalencyAssertionOptions` | `IEquivalencyOptions` |
| `SelfReferenceEquivalencyAssertionOptions<TSelf>` | `SelfReferenceEquivalencyOptions<TSelf>` |
| `options.RespectingRuntimeTypes()` | `options.PreferringRuntimeMemberTypes()` |
| `options.RespectingDeclaredTypes()` | `options.PreferringDeclaredMemberTypes()` |
| `options.ExcludingNestedObjects()` | `options.WithoutRecursing()` |

The type renames are the ones that bite: any helper like

```csharp
// old
private static EquivalencyAssertionOptions<Order> Defaults(EquivalencyAssertionOptions<Order> o)
    => o.RespectingRuntimeTypes().ExcludingNestedObjects();

// new
private static EquivalencyOptions<Order> Defaults(EquivalencyOptions<Order> o)
    => o.PreferringRuntimeMemberTypes().WithoutRecursing();
```

## Global / static configuration: `AssertionOptions` is gone

The static `AssertionOptions` class was replaced by `AssertionConfiguration.Current`.
The `AssertionConfiguration` static class lives in the **root** `AwesomeAssertions`
namespace (so a plain `using AwesomeAssertions;` suffices); only the
`GlobalConfiguration` type it returns is in `AwesomeAssertions.Configuration`.
`AssertionEngine.Configuration` (also root namespace) reaches the same object and is
equally valid:

| Old (≤7) | New (8+) |
|---|---|
| `AssertionOptions.AssertEquivalencyUsing(o => ...)` | `AssertionConfiguration.Current.Equivalency.Modify(o => ...)` |
| `AssertionOptions.EquivalencyPlan.Insert<TStep>()` | `AssertionConfiguration.Current.Equivalency.Plan.Insert<TStep>()` |
| `AssertionOptions.FormattingOptions.MaxLines = n` | `AssertionConfiguration.Current.Formatting.MaxLines = n` |
| `AssertionOptions.CloneDefaults<T>()` | `AssertionConfiguration.Current.Equivalency.CloneDefaults<T>()` |

Global configuration must run exactly once before any assertion executes. On .NET 5+
use a `[ModuleInitializer]`, or on any framework use the library's own hook (the
attribute lives in `AwesomeAssertions.Extensibility`):

```csharp
[assembly: AssertionEngineInitializer(typeof(Initializer), nameof(Initializer.Initialize))]

public static class Initializer
{
    public static void Initialize()
    {
        AssertionConfiguration.Current.Equivalency.Modify(o => o.ComparingByValue<DirectoryInfo>());
    }
}
```

Configuring via `app.config` is no longer supported at all.

## Removed / moved features

| Removed from core | Replacement |
|---|---|
| `HttpResponseMessage` assertions | `AwesomeAssertions.Web` package |
| `DataSet`/`DataTable`/`DataRow`/`DataColumn` assertions | `AwesomeAssertions.DataSets` package |
| `Monitor<T>()` overload taking `utcNow` | parameterless `Monitor<T>()` |
| `BinaryFormatter`-based features (`BeBinarySerializable`) | none — drop the assertion |
| NSpec3 test-framework detection | none (dropped in v7) |

## Behavioral changes (compile fine, behave differently)

These need a human-judgment pass rather than a rename — they can silently change what
a test verifies:

- **`AllSatisfy` and `OnlyContain` now pass on empty collections.** Under v7 an empty
  collection failed these assertions. If a test relied on that to guard against
  "accidentally empty" data, add an explicit `.NotBeEmpty()` to preserve the old
  strength.
- **`BeLowerCased`/`BeUpperCased` semantics align with `ToLower`/`ToUpper`.** Strings
  containing caseless characters (digits, punctuation) are now judged the way
  `string.ToLower()` would produce them, not "every char must be a lowercase letter".
- **String-equality failure messages** were reworked (difference visualization). Only
  matters to tests that assert on the failure message text itself.

## Target framework changes

Direct targets for .NET Core 2.x/3.x were dropped (already in v7). Projects targeting
those still resolve the `netstandard2.0` build and keep working. AwesomeAssertions 9.x
ships `net47`, `net6.0`, `net8.0`, `netstandard2.0`, `netstandard2.1`.

## Breaking changes for extension authors

Constructor and entry-point changes (`Execute.Assertion` → `AssertionChain`, base-class
constructor signatures, `AndWhichConstraint`, `IEquivalencyStep.Handle`) are covered in
[extensibility-migration.md](extensibility-migration.md).
