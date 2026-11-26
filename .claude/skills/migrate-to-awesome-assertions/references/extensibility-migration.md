# Migrating custom assertions: `Execute.Assertion` → `AssertionChain`

In FluentAssertions ≤7, custom assertions called the static `Execute.Assertion`, which
returned the ambient `AssertionScope`. In AwesomeAssertions 8+ that entry point is
gone. Instead, an `AssertionChain` instance is created once per `Should()` call and
flows through the entire fluent statement. This is what enables chained assertions
(`.Which...`) to share caller identification and stop after the first failure.

All code below is verified to compile against AwesomeAssertions 9.4.0. Namespaces shown
are the 9.x ones (`AwesomeAssertions.*`); for 8.x they would be `FluentAssertions.*`.

Every fluent-API member you used on the scope — `ForCondition`, `ForConstraint`,
`BecauseOf`, `Given`, `FailWith`, `.Then`, `WithDefaultIdentifier`, `UsingLineBreaks`,
`WithExpectation` — exists on `AssertionChain` with the same shape (except
`WithExpectation`, see below). So the body of most assertions survives; what changes is
**where the chain comes from**.

## Pattern 1: Your own assertions class

```csharp
// ===== OLD (v7) =====
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

public static class DirectoryInfoExtensions
{
    public static DirectoryInfoAssertions Should(this DirectoryInfo instance)
        => new DirectoryInfoAssertions(instance);
}

public class DirectoryInfoAssertions
    : ReferenceTypeAssertions<DirectoryInfo, DirectoryInfoAssertions>
{
    public DirectoryInfoAssertions(DirectoryInfo instance)
        : base(instance) { }

    protected override string Identifier => "directory";

    [CustomAssertion]
    public AndConstraint<DirectoryInfoAssertions> ContainFile(
        string filename, string because = "", params object[] becauseArgs)
    {
        Execute.Assertion
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(filename))
            .FailWith("You can't assert a file exists if you don't pass a proper name")
            .Then
            .Given(() => Subject.GetFiles())
            .ForCondition(files => files.Any(f => f.Name.Equals(filename)))
            .FailWith("Expected {context:directory} to contain {0}{reason}, but found {1}.",
                _ => filename, files => files.Select(f => f.Name));

        return new AndConstraint<DirectoryInfoAssertions>(this);
    }
}
```

```csharp
// ===== NEW (AwesomeAssertions 9.x) =====
using AwesomeAssertions;
using AwesomeAssertions.Execution;
using AwesomeAssertions.Primitives;

public static class DirectoryInfoExtensions
{
    public static DirectoryInfoAssertions Should(this DirectoryInfo instance)
        => new DirectoryInfoAssertions(instance, AssertionChain.GetOrCreate());
}

public class DirectoryInfoAssertions
    : ReferenceTypeAssertions<DirectoryInfo, DirectoryInfoAssertions>
{
    private readonly AssertionChain chain;

    public DirectoryInfoAssertions(DirectoryInfo instance, AssertionChain chain)
        : base(instance, chain)
    {
        this.chain = chain;
    }

    protected override string Identifier => "directory";

    [CustomAssertion]
    public AndConstraint<DirectoryInfoAssertions> ContainFile(
        string filename, string because = "", params object[] becauseArgs)
    {
        chain
            .BecauseOf(because, becauseArgs)
            .ForCondition(!string.IsNullOrEmpty(filename))
            .FailWith("You can't assert a file exists if you don't pass a proper name")
            .Then
            .Given(() => Subject.GetFiles())
            .ForCondition(files => files.Any(f => f.Name.Equals(filename)))
            .FailWith("Expected {context:directory} to contain {0}{reason}, but found {1}.",
                _ => filename, files => files.Select(f => f.Name));

        return new AndConstraint<DirectoryInfoAssertions>(this);
    }
}
```

The mechanical rules:

1. `Should()` creates the chain: `AssertionChain.GetOrCreate()`. Call it **once per
   `Should()`**, never inside individual assertion methods — `GetOrCreate` returns a
   previously reused chain only once, so calling it per-method breaks chained caller
   identification.
2. The assertions class stores the chain in a field and passes it to `base(...)`.
   *Every* built-in base class (`ReferenceTypeAssertions`, `ObjectAssertions`,
   `StringAssertions`, `GenericCollectionAssertions<T>`, …) gained a trailing
   `AssertionChain` constructor parameter — so any direct instantiation like
   `new ObjectAssertions(value)` also becomes
   `new ObjectAssertions(value, AssertionChain.GetOrCreate())`.
3. Replace every `Execute.Assertion` with the stored chain field. Then delete the
   now-unused `Execute` reference; the class no longer exists.

**Coming from v5:** assertion classes typically didn't call a base constructor at all —
they assigned the subject directly (`public FooAssertions(Foo instance) { Subject =
instance; }`). `Subject` no longer has an accessible setter; that assignment must
become the `base(instance, chain)` call shown above. Plain wrapper classes that are
*not* derived from any assertions base class but call `Execute.Assertion` directly
(a common v5-era pattern) get a local chain instead: call
`AssertionChain.GetOrCreate()` at the start of each public check method (here a fresh
chain per method is correct — there is no `Should()` entry point to share it from).

## Pattern 2: Extension method on a built-in assertions class

You don't control the constructor, so use the chain the class was created with — every
assertions class exposes it as `CurrentAssertionChain`:

```csharp
// OLD: Execute.Assertion.BecauseOf(...)...
// NEW:
public static AndConstraint<StringAssertions> BeShouting(
    this StringAssertions assertions, string because = "", params object[] becauseArgs)
{
    assertions.CurrentAssertionChain
        .BecauseOf(because, becauseArgs)
        .ForCondition(assertions.Subject == assertions.Subject?.ToUpperInvariant())
        .FailWith("Expected {context:string} to be all upper-case{reason}, but found {0}.",
            assertions.Subject);

    return new AndConstraint<StringAssertions>(assertions);
}
```

## Pattern 3: `WithExpectation` — nested lambda, no `ClearExpectation`

`WithExpectation` previously set a sticky message prefix you had to remove with
`ClearExpectation()` (and leaking it into the next assertion of an `AssertionScope` was
a classic bug). It now scopes the prefix to a nested lambda; `ClearExpectation` is gone:

```csharp
// OLD
Execute.Assertion
    .BecauseOf(because, becauseArgs)
    .WithExpectation("Expected {context:directory} to be empty{reason}, ")
    .ForCondition(Subject is not null)
    .FailWith("but found <null>.")
    .Then
    .ForCondition(Subject.GetFiles().Length == 0)
    .FailWith("but found {0} files.", Subject.GetFiles().Length)
    .Then
    .ClearExpectation();

// NEW — everything sharing the prefix moves inside the lambda
chain
    .BecauseOf(because, becauseArgs)
    .WithExpectation("Expected {context:directory} to be empty{reason}, ", c => c
        .ForCondition(Subject is not null)
        .FailWith("but found <null>.")
        .Then
        .ForCondition(Subject.GetFiles().Length == 0)
        .FailWith("but found {0} files.", Subject.GetFiles().Length));
```

## Pattern 4: Checking earlier outcomes — `chain.Succeeded`

When an assertion does follow-up work that is only safe/meaningful if the checks so far
passed (common before dereferencing `Subject`):

```csharp
chain.ForCondition(Subject is not null).FailWith("but found <null>.");

if (chain.Succeeded)
{
    // expensive or Subject-dereferencing follow-up
}
```

## Pattern 5: Supporting `.Which` chaining — `AndWhichConstraint`

To let callers write
`dir.Should().ContainFile("a.txt").Which.Length.Should().BeGreaterThan(0)`, return an
`AndWhichConstraint` and hand it the chain plus a caller-identifier postfix. The
constraint internally calls `ReuseOnce`/`WithCallerPostfix` so a failure in the chained
part reports e.g. `Expected dir/a.txt to ...` instead of just `dir`:

```csharp
return new AndWhichConstraint<DirectoryInfoAssertions, FileInfo>(
    this, matchedFile, chain, "/" + filename);
```

A v7-style `return new AndWhichConstraint<...>(this, matchedFile);` still compiles but
loses caller identification across the chain — always pass the chain. To replace the
detected caller name entirely instead of appending, use
`chain.OverrideCallerIdentifier(() => "...")` before constructing the constraint.

## `AssertionScope`: what stayed, what moved

`AssertionScope` kept its *aggregation* role — user-facing
`using (new AssertionScope()) { ... }` blocks for collecting multiple failures need no
changes, including `new AssertionScope("context name")` to override `{context}` and
`scope.FormattingOptions.AddFormatter(...)` for scoped formatters.

What moved: the assertion-*building* API (`ForCondition`, `FailWith`, `BecauseOf`, …)
is no longer on the scope. Code that did `Execute.Assertion` or called those methods on
a scope instance must go through an `AssertionChain` (patterns 1–2). A scope used purely
to set context for a nested custom assertion still works unchanged:

```csharp
foreach (DirectoryInfo sub in Subject.GetDirectories())
{
    using (new AssertionScope(sub.FullName))
    {
        sub.Should().ContainFile(filename, because, becauseArgs);
    }
}
```

## Value formatters: unchanged interface, renamed namespace

`IValueFormatter` (`CanHandle(object)` +
`Format(object, FormattedObjectGraph, FormattingContext, FormatChild)`) and
`Formatter.AddFormatter(...)`/`RemoveFormatter(...)` are unchanged — only the namespace
becomes `AwesomeAssertions.Formatting`. Scoped registration via
`scope.FormattingOptions.AddFormatter(...)` also still works. Global formatting limits
moved with the configuration host: `AssertionConfiguration.Current.Formatting.MaxLines`.

## Equivalency steps

`IEquivalencyStep.Handle` got a renamed third parameter type, and the "done" result was
renamed:

```csharp
// OLD (v7)
public EquivalencyResult Handle(Comparands comparands,
    IEquivalencyValidationContext context, IEquivalencyValidator nestedValidator)
{
    ...
    return EquivalencyResult.AssertionCompleted;   // or ContinueWithNext
}

// NEW (8+)
public EquivalencyResult Handle(Comparands comparands,
    IEquivalencyValidationContext context, IValidateChildNodeEquivalency valueChildNodes)
{
    ...
    return EquivalencyResult.EquivalencyProven;    // ContinueWithNext unchanged
}
```

Recursing into child nodes goes through the renamed validator interface
(`AssertEquivalencyOf` instead of `RecursivelyAssertEquality`). Registration moved to
`AssertionConfiguration.Current.Equivalency.Plan.Insert<TStep>()` (per-call
`options.Using(new TStep())` is unchanged).

AwesomeAssertions 9.x also reshaped `INode` (what `context.CurrentNode` returns): the
node no longer has its own `Name`/`Path`; it carries a `Subject` and an `Expectation`
pathway, each with the name/path members. So a v7 step that inspected
`context.CurrentNode.Name` reads `context.CurrentNode.Expectation.Name` in 9.x
(`IsRoot`, `Depth`, and `Type` are still on the node itself).

## Compile-error → fix cheat sheet

| Error mentions | Fix |
|---|---|
| `'Execute' does not exist` | Patterns 1–2: thread an `AssertionChain` through |
| base class `does not contain a constructor that takes 1 argument` | add `AssertionChain` parameter, pass to `base(subject, chain)` |
| `'ClearExpectation' not found` | Pattern 3: nested-lambda `WithExpectation` |
| `'IAssertionScope' not found` / fluent methods missing on scope | use `AssertionChain` (the scope no longer builds assertions) |
| `'AssertionOptions' does not exist` | `AssertionConfiguration.Current.*` (see breaking-changes.md) |
| `'IEquivalencyValidator' not found` | `IValidateChildNodeEquivalency`; result enum renamed too |
| `'AssertionCompleted' not found` | `EquivalencyResult.EquivalencyProven` |
