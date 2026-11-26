# NUnit → xUnit v3 (reference)

Mappings below come from a verified migration run (8 test files, 36 tests, NUnit 4.x → xunit.v3) plus the official comparison table at https://xunit.net/docs/comparisons. Where a signature is marked *verify with compiler*, don't trust the table — write the obvious call and let `dotnet build` confirm.

## Packages and csproj

| NUnit stack | xUnit v3 replacement |
|---|---|
| `NUnit` | `xunit.v3` |
| `NUnit3TestAdapter` | `xunit.runner.visualstudio` **3.x** |
| `NUnit.Analyzers` | `xunit.analyzers` |
| `Microsoft.NET.Test.Sdk` | keep; update to latest |

Plus the standard v3 csproj change: `<OutputType>Exe</OutputType>`, TFM **net8.0+** or **net472+**.

Usings: remove `using NUnit.Framework;` (and `using NUnit.Framework.Legacy;` if present), add `using Xunit;`.

## Attribute mapping

| NUnit | xUnit v3 |
|---|---|
| `[TestFixture]` | remove — xUnit needs no class attribute |
| `[TestFixture(args)]` (parameterized fixture) | no equivalent — convert to a theory or fixture data |
| `[Test]` | `[Fact]` |
| `[TestCase(1, 2)]` | `[Theory]` + `[InlineData(1, 2)]` |
| `[TestCase(..., ExpectedResult = x)]` | rewrite — see Parameterized tests below |
| `[TestCaseSource(nameof(M))]` | `[Theory]` + `[MemberData(nameof(M))]` — source must be reshaped, see below |
| `[Values]`/`[Range]`/`[Random]` on parameters | no combinatorial support — expand to explicit `[InlineData]`/`[MemberData]` rows |
| `[SetUp]` | class constructor |
| `[TearDown]` | `IDisposable.Dispose()` (or `IAsyncLifetime`) |
| `[OneTimeSetUp]`/`[OneTimeTearDown]` | `IClassFixture<T>` — fixture's constructor/`Dispose` |
| `[SetUpFixture]` (namespace-level) | `ICollectionFixture<T>`, or v3 assembly fixture: `[assembly: AssemblyFixture(typeof(T))]` |
| `[Ignore("why")]` | `[Fact(Skip = "why")]` |
| `[Explicit]` | `[Fact(Explicit = true)]` — native in v3 (not in v2) |
| `[Category("X")]` | `[Trait("Category", "X")]` — `dotnet test --filter "Category=X"` keeps working |
| `[Property("k", "v")]`, `[Author]`, `[Description]` | `[Trait("k", "v")]` or drop |
| `[Order(n)]` | custom `ITestCaseOrderer` — see `extensibility.md` |
| `[Timeout(ms)]` | `[Fact(Timeout = ms)]` (v3; async tests) |
| `[Retry(n)]`, `[Repeat(n)]` | no built-in — loop inside the test or drop |
| `[Parallelizable]`/`[NonParallelizable]` | collection-based model — see Behavior differences |
| `[Apartment(ApartmentState.STA)]` | no built-in — third-party `Xunit.StaFact` package |
| `[Culture]`/`[SetCulture]` | set `CultureInfo.CurrentCulture` in the test/constructor, or a custom `BeforeAfterTestAttribute` |

## Lifecycle: per-test instance semantics

**xUnit creates a new test-class instance per test; NUnit reuses one instance for the whole fixture.** This is the biggest semantic shift:

- `[SetUp]` body → constructor: equivalent (runs before each test either way).
- `[OneTimeSetUp]` body moved into the constructor runs **per test, not once** — correct only if cheap. If it's expensive or stateful, use a fixture:

```csharp
public class DbFixture : IDisposable          // [OneTimeSetUp]/[OneTimeTearDown] body goes here
{
    public DbFixture() { /* once per class */ }
    public void Dispose() { /* once per class */ }
}

public class MyTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _db;
    public MyTests(DbFixture db) { _db = db; }  // injected, shared across the class's tests
}
```

- Instance fields that NUnit tests used to share/mutate across tests are silently reset per test in xUnit — grep setup methods for field assignments that later tests read.

## Parameterized tests

`TestCaseSource` sources return `IEnumerable<TestCaseData>` (or `object[]`); xUnit needs `TheoryData<>` (preferred) or `IEnumerable<object[]>`. `ExpectedResult`/`.Returns(...)` has no equivalent — fold the expected value into the data as an extra parameter and assert explicitly:

```csharp
// NUnit
static IEnumerable<TestCaseData> Cases() { yield return new TestCaseData(1, 2).Returns(3); }
[TestCaseSource(nameof(Cases))]
public int Add(int a, int b) => a + b;

// xUnit v3
public static TheoryData<int, int, int> Cases => new() { { 1, 2, 3 } };
[Theory, MemberData(nameof(Cases))]
public void Add(int a, int b, int expected) => Assert.Equal(expected, a + b);
```

Theory rows must be serializable for individual Test Explorer listing (custom types: implement `IXunitSerializable`); unserializable rows still *run* but collapse into one listed case — this is why the baseline comparison must count executed tests, not listed ones.

## Assertions: NUnit → xUnit `Assert`

### Classic model (`ClassicAssert.*` in NUnit 4, `Assert.*` in NUnit 3)

| NUnit | xUnit |
|---|---|
| `AreEqual(exp, act)` | `Assert.Equal(exp, act)` — same argument order |
| `AreEqual(exp, act, delta)` | `Assert.Equal(exp, act, tolerance)` |
| `AreNotEqual` / `AreSame` / `AreNotSame` | `Assert.NotEqual` / `Assert.Same` / `Assert.NotSame` |
| `IsTrue(x)` / `IsFalse(x)` | `Assert.True(x)` / `Assert.False(x)` |
| `IsNull(x)` / `IsNotNull(x)` | `Assert.Null(x)` / `Assert.NotNull(x)` |
| `IsEmpty(x)` / `IsNotEmpty(x)` | `Assert.Empty(x)` / `Assert.NotEmpty(x)` |
| `IsInstanceOf<T>(x)` | `Assert.IsAssignableFrom<T>(x)` — NUnit accepts derived types; `Assert.IsType<T>` is exact-match |
| `IsNotInstanceOf<T>(x)` | `Assert.IsNotType<T>(x)` (exact) — for "not assignable", `Assert.False(x is T)` |
| `Greater(a, b)` / `Less` / `GreaterOrEqual` / `LessOrEqual` | `Assert.True(a > b)` etc., or `Assert.InRange(act, lo, hi)` |
| `Contains(item, collection)` | `Assert.Contains(item, collection)` |
| `Zero(x)` / `NotZero(x)` / `IsNaN(x)` | `Assert.Equal(0, x)` / `Assert.NotEqual(0, x)` / `Assert.True(double.IsNaN(x))` |

### Constraint model (`Assert.That`)

**Argument-order trap:** `Assert.That(actual, Is.EqualTo(expected))` puts *actual* first; `Assert.Equal(expected, actual)` puts *expected* first. Flip when converting.

| NUnit | xUnit |
|---|---|
| `Assert.That(a, Is.EqualTo(e))` | `Assert.Equal(e, a)` |
| `Assert.That(a, Is.Not.EqualTo(e))` | `Assert.NotEqual(e, a)` |
| `Assert.That(a, Is.EqualTo(e).Within(t))` | `Assert.Equal(e, a, t)` (numeric tolerance) |
| `Assert.That(x, Is.True / Is.False)` | `Assert.True(x)` / `Assert.False(x)` |
| `Assert.That(x, Is.Null / Is.Not.Null)` | `Assert.Null(x)` / `Assert.NotNull(x)` |
| `Assert.That(x, Is.Empty)` | `Assert.Empty(x)` (collections and strings) |
| `Assert.That(c, Is.EquivalentTo(e))` | `Assert.Equivalent(e, c)` (order-insensitive) |
| `Assert.That(c, Has.Count.EqualTo(n))` | `Assert.Equal(n, c.Count)`; for n=1 prefer `Assert.Single(c)` |
| `Assert.That(c, Has.Member(m))` | `Assert.Contains(m, c)` |
| `Assert.That(s, Does.Contain(sub))` | `Assert.Contains(sub, s)` |
| `Assert.That(s, Does.StartWith / Does.EndWith)` | `Assert.StartsWith` / `Assert.EndsWith` |
| `Assert.That(s, Does.Match(re))` | `Assert.Matches(re, s)` |
| `Assert.That(x, Is.InstanceOf<T>() / Is.TypeOf<T>())` | `Assert.IsAssignableFrom<T>(x)` / `Assert.IsType<T>(x)` |
| `Assert.That(x, Is.GreaterThan(e))` etc. | `Assert.True(x > e)` |
| `Assert.That(code, Throws.TypeOf<T>())` | `Assert.Throws<T>(code)` |

### StringAssert / CollectionAssert

| NUnit | xUnit |
|---|---|
| `StringAssert.Contains` / `StartsWith` / `EndsWith` | `Assert.Contains` / `Assert.StartsWith` / `Assert.EndsWith` |
| `StringAssert.AreEqualIgnoringCase(e, a)` | `Assert.Equal(e, a, ignoreCase: true)` |
| `StringAssert.IsMatch(re, a)` | `Assert.Matches(re, a)` |
| `CollectionAssert.AreEqual(e, a)` | `Assert.Equal(e, a)` (ordered) |
| `CollectionAssert.AreEquivalent(e, a)` | `Assert.Equivalent(e, a)` (unordered) |
| `CollectionAssert.Contains` / `DoesNotContain` | `Assert.Contains` / `Assert.DoesNotContain` |
| `CollectionAssert.IsEmpty` | `Assert.Empty` |
| `CollectionAssert.AllItemsAreUnique(c)` | `Assert.Distinct(c)` |
| `CollectionAssert.AllItemsAreNotNull(c)` | `Assert.All(c, Assert.NotNull)` |
| `CollectionAssert.AllItemsAreInstancesOfType` | `Assert.All(c, i => Assert.IsAssignableFrom<T>(i))` |
| `CollectionAssert.IsSubsetOf(sub, sup)` | `Assert.Subset(supersetAsISet, subsetAsISet)` — `ISet<T>` only, *verify with compiler* |

### Exceptions, skips, and special cases

| NUnit | xUnit v3 |
|---|---|
| `Assert.Throws<T>(() => ...)` | `Assert.Throws<T>(() => ...)` — identical, returns the exception |
| `Assert.Catch<T>` / `Throws.InstanceOf<T>` | `Assert.ThrowsAny<T>` (accepts derived types) |
| `Assert.ThrowsAsync<T>(async () => ...)` | `await Assert.ThrowsAsync<T>(...)` — **must be awaited** (NUnit's blocks synchronously); make the test `async Task` |
| `Assert.DoesNotThrow(code)` | run the code directly (any exception fails the test); to assert explicitly: `Assert.Null(Record.Exception(code))` |
| `Assert.DoesNotThrowAsync` | `Assert.Null(await Record.ExceptionAsync(code))` |
| `Assert.Fail(msg)` | `Assert.Fail(msg)` |
| `Assert.Pass()` | `return;` (no equivalent) |
| `Assert.Ignore(r)` / `Assert.Inconclusive(r)` | `Assert.Skip(r)` — native dynamic skip in v3 |
| `Assume.That(cond)` | `Assert.SkipUnless(cond, "reason")` / `Assert.SkipWhen(...)` (v3) |
| `Assert.Multiple(() => { ... })` | no equivalent — remove the wrapper; assertions run sequentially and the first failure stops the test |
| `Warn.If` / `Warn.Unless` | no equivalent — convert to a real assertion or output via `ITestOutputHelper` |

**Messages:** NUnit's optional message argument has no counterpart on most xUnit asserts (deliberate design). Drop the message; if it carries real diagnostic value, switch that one call to `Assert.True(cond, msg)` or precede with `Assert.Fail(msg)` logic.

## TestContext

| NUnit | xUnit v3 |
|---|---|
| `TestContext.WriteLine` / `TestContext.Out` | constructor-injected `ITestOutputHelper.WriteLine`; from fixtures/helpers: `TestContext.Current.TestOutputHelper?.WriteLine` |
| `TestContext.CurrentContext.Test.Name` | `TestContext.Current.Test?.TestDisplayName` — *verify with compiler* |
| `TestContext.CurrentContext.CancellationToken` | `TestContext.Current.CancellationToken` |
| `TestContext.CurrentContext.TestDirectory` / `WorkDirectory` | `AppContext.BaseDirectory` / `Directory.GetCurrentDirectory()` |

## Behavior differences

- **New instance per test** (see Lifecycle) — the #1 source of subtle breakage.
- **Parallelism inverts:** NUnit is sequential unless `[Parallelizable]`; xUnit runs test *collections* (default: one per class) in parallel by default, tests within a class always sequentially. Tests that conflict across classes: put them in one `[Collection("name")]`, or disable globally with `[assembly: CollectionBehavior(DisableTestParallelization = true)]`.
- **Test count accounting:** each NUnit `[TestCase]`/`[Values]` combination is one test; after migration each `[InlineData]`/`MemberData` row is one test case — counts must match 1:1 in an executed run.
- **v3 test projects are executables** — runnable directly without `dotnet test`, useful for diagnosing discovery issues.
