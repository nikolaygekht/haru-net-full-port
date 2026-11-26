# Migrating NUnit / xUnit built-in assertions to AwesomeAssertions

This covers converting classic assertion calls (`Assert.AreEqual`, `Assert.Equal`,
`Assert.That(...)`, `CollectionAssert.*`, `StringAssert.*`) to fluent
`.Should()` assertions. The test framework **stays** — NUnit or xUnit remains the
runner, attributes (`[Test]`, `[TestCase]`, `[Fact]`, `[Theory]`, setup/teardown) are
untouched, and AwesomeAssertions auto-detects the hosting framework so failures throw
the framework's own assertion exception and report normally. Only the assertion calls
inside test bodies change.

All mappings below were verified to compile and pass against AwesomeAssertions 9.4.0
under both the NUnit 4 and xUnit 2 runners.

Contents:
1. [Setup](#setup)
2. [Cross-cutting gotchas](#cross-cutting-gotchas) — read these first
3. [xUnit mappings](#xunit-assert--mappings)
4. [NUnit classic-model mappings](#nunit-classic-model-mappings)
5. [NUnit constraint-model mappings](#nunit-constraint-model-assertthat-mappings)
6. [What to keep unchanged](#what-stays-as-is)

## Setup

Add the package next to the existing framework packages (do not remove them):

```xml
<PackageReference Include="AwesomeAssertions" Version="9.*" />
```

Add `using AwesomeAssertions;` to each converted file (or one
`global using AwesomeAssertions;`). Keep `using NUnit.Framework;` / `using Xunit;` —
the attributes still need it. Both assertion styles coexist without conflict, so the
conversion can be done file-by-file with green tests at every step.

## Cross-cutting gotchas

**Argument order inverts.** Classic asserts put the *expected* value first
(`Assert.AreEqual(expected, actual)`, `Assert.Equal(expected, actual)`,
`Assert.Contains(expectedSubstring, actualString)`); fluent assertions start from the
*actual* value (`actual.Should().Be(expected)`). A mechanical swap that keeps the order
produces assertions that still pass (equality is symmetric) but report
expected/actual backwards in failure messages — and for non-symmetric assertions
(`Contain`, `StartWith`, `BeGreaterThan`) it inverts the meaning entirely. Identify
which argument is the subject before rewriting each call.

**Exception assertions differ in strictness.** Pick the fluent method that preserves
what the original demanded, or the test silently weakens/tightens:

| Original | Type match | Fluent equivalent |
|---|---|---|
| xUnit `Assert.Throws<T>` | exact | `.Should().ThrowExactly<T>()` |
| xUnit `Assert.ThrowsAny<T>` | T or derived | `.Should().Throw<T>()` |
| NUnit `Assert.Throws<T>` / `Throws.TypeOf<T>()` | exact | `.Should().ThrowExactly<T>()` |
| NUnit `Assert.Catch<T>` / `Throws.InstanceOf<T>()` | T or derived | `.Should().Throw<T>()` |

**`WithMessage` matches wildcards, not substrings.** `*` and `?` are wildcards and the
pattern must cover the whole message. `ex.Message.Contains("x")` or NUnit
`.With.Message.Contains("x")` becomes `.WithMessage("*x*")`; an exact-message check
becomes the literal text with no wildcards.

**Async exception asserts must be awaited.** NUnit's `Assert.ThrowsAsync<T>(...)`
blocks synchronously and works in a `void` test; `await func.Should().ThrowExactlyAsync<T>()`
requires the test method to be `async Task`. Change the signature when needed.

**Assertion messages become `because`.** The optional message argument maps to the
`because`/`becauseArgs` parameters: `Assert.IsTrue(ok, "order {0} must be valid", id)`
→ `ok.Should().BeTrue("order {0} must be valid", id)`. The `because` text goes through
composite formatting, so escape any literal `{` or `}` in migrated messages (`{{`, `}}`).

**Returned values become `.Which` / `.Subject`.** Classic asserts that return a value
(`var ex = Assert.Throws<T>(...)`, `var s = Assert.IsType<string>(o)`,
`var item = Assert.Single(list)`) map to the fluent chain's `.Which` (typed result of
`ThrowExactly`/`BeOfType`) or `.Subject` (`ContainSingle().Subject`).

**Preserve semantics, not appearances.** The goal is that every converted assertion
checks exactly the same condition as before. When a classic assert has no direct
fluent equivalent (rare — e.g. NUnit's `Has.Property("Name").EqualTo(...)` on an
`object`), rewrite it explicitly (cast/navigate, then assert) rather than dropping or
approximating it.

## xUnit `Assert.*` mappings

| xUnit | AwesomeAssertions |
|---|---|
| `Assert.Equal(exp, act)` | `act.Should().Be(exp)` |
| `Assert.Equal(exp, act, precision)` (floating point) | `act.Should().BeApproximately(exp, tolerance)` — note: precision is decimal *digits*, tolerance is an absolute delta; convert (`precision: 3` ≈ `tolerance: 0.0005…0.001`) |
| `Assert.Equal(expSeq, actSeq)` (collections, ordered) | `actSeq.Should().Equal(expSeq)` |
| `Assert.NotEqual(exp, act)` | `act.Should().NotBe(exp)` |
| `Assert.Same(exp, act)` / `NotSame` | `act.Should().BeSameAs(exp)` / `NotBeSameAs` |
| `Assert.True(c)` / `Assert.False(c)` | `c.Should().BeTrue()` / `BeFalse()` |
| `Assert.Null(o)` / `NotNull` | `o.Should().BeNull()` / `NotBeNull()` |
| `Assert.Empty(coll)` / `NotEmpty` | `coll.Should().BeEmpty()` / `NotBeEmpty()` |
| `Assert.Single(coll)` | `coll.Should().ContainSingle()` — item via `.Subject` |
| `Assert.Single(coll, predicate)` | `coll.Should().ContainSingle(predicate)` |
| `Assert.Contains(item, coll)` / `DoesNotContain` | `coll.Should().Contain(item)` / `NotContain` |
| `Assert.Contains(coll, predicate)` | `coll.Should().Contain(predicate)` |
| `Assert.Contains(substr, str)` / `DoesNotContain` | `str.Should().Contain(substr)` / `NotContain` |
| `Assert.StartsWith(exp, str)` / `EndsWith` | `str.Should().StartWith(exp)` / `EndWith` |
| `Assert.Matches(pattern, str)` | `str.Should().MatchRegex(pattern)` |
| `Assert.InRange(act, lo, hi)` | `act.Should().BeInRange(lo, hi)` |
| `Assert.IsType<T>(o)` (exact) | `o.Should().BeOfType<T>()` — typed value via `.Which` |
| `Assert.IsNotType<T>(o)` | `o.Should().NotBeOfType<T>()` |
| `Assert.IsAssignableFrom<T>(o)` | `o.Should().BeAssignableTo<T>()` |
| `Assert.Throws<T>(act)` (exact) | `act.Should().ThrowExactly<T>()` — exception via `.Which` |
| `Assert.ThrowsAny<T>(act)` | `act.Should().Throw<T>()` |
| `await Assert.ThrowsAsync<T>(f)` | `await f.Should().ThrowExactlyAsync<T>()` |
| `Assert.All(coll, action)` | `coll.Should().AllSatisfy(action)` |
| `Assert.Collection(coll, e1, e2, …)` | `coll.Should().SatisfyRespectively(e1, e2, …)` (also asserts the count) |
| `Assert.Equivalent(exp, act)` | `act.Should().BeEquivalentTo(exp)` |
| `Assert.Distinct(coll)` | `coll.Should().OnlyHaveUniqueItems()` |
| `Assert.Subset(superset, act)` | `act.Should().BeSubsetOf(superset)` |
| `Assert.Superset(subset, act)` | `subset.Should().BeSubsetOf(act)` |
| `Assert.Raises<T>(attach, detach, act)` | `using var monitor = src.Monitor();` then act, then `monitor.Should().Raise("EventName")` |

The lambdas inside `AllSatisfy`/`SatisfyRespectively` contain assertions too — convert
their bodies to fluent style as well (`x => x.Should().BePositive()`), since a bare
boolean expression in there asserts nothing.

After conversion the xUnit assertion analyzers (xUnit2xxx rules) no longer inspect
these calls; that is expected and not a regression.

## NUnit classic-model mappings

NUnit 4 moved these to `NUnit.Framework.Legacy.ClassicAssert` — the mappings are the
same whether the source says `Assert.AreEqual` (NUnit 3) or `ClassicAssert.AreEqual`
(NUnit 4). After conversion, remove any `using NUnit.Framework.Legacy;`.

| NUnit classic | AwesomeAssertions |
|---|---|
| `Assert.AreEqual(exp, act)` | `act.Should().Be(exp)` |
| `Assert.AreEqual(exp, act, delta)` (floating point) | `act.Should().BeApproximately(exp, delta)` |
| `Assert.AreEqual(expColl, actColl)` (collections — NUnit compares structurally!) | `actColl.Should().Equal(expColl)` |
| `Assert.AreNotEqual(exp, act)` | `act.Should().NotBe(exp)` |
| `Assert.AreSame` / `AreNotSame` | `BeSameAs` / `NotBeSameAs` |
| `Assert.IsTrue(c)` / `IsFalse(c)` (also `Assert.True/False`) | `c.Should().BeTrue()` / `BeFalse()` |
| `Assert.IsNull(o)` / `IsNotNull(o)` (also `Null/NotNull`) | `o.Should().BeNull()` / `NotBeNull()` |
| `Assert.IsEmpty(x)` / `IsNotEmpty(x)` (string or collection) | `x.Should().BeEmpty()` / `NotBeEmpty()` |
| `Assert.Greater(a, b)` / `GreaterOrEqual` | `a.Should().BeGreaterThan(b)` / `BeGreaterThanOrEqualTo(b)` |
| `Assert.Less(a, b)` / `LessOrEqual` | `a.Should().BeLessThan(b)` / `BeLessThanOrEqualTo(b)` |
| `Assert.Positive(n)` / `Negative(n)` | `n.Should().BePositive()` / `BeNegative()` |
| `Assert.Zero(n)` / `NotZero(n)` | `n.Should().Be(0)` / `NotBe(0)` |
| `Assert.IsInstanceOf<T>(o)` (derived allowed) | `o.Should().BeAssignableTo<T>()` |
| `Assert.IsNotInstanceOf<T>(o)` | `o.Should().NotBeAssignableTo<T>()` |
| `Assert.Contains(item, coll)` | `coll.Should().Contain(item)` |
| `Assert.Throws<T>(del)` (exact) | `del.Should().ThrowExactly<T>()` — exception via `.Which` |
| `Assert.Catch<T>(del)` (derived allowed) | `del.Should().Throw<T>()` |
| `Assert.DoesNotThrow(del)` | `del.Should().NotThrow()` |
| `Assert.ThrowsAsync<T>(f)` / `DoesNotThrowAsync` | `await f.Should().ThrowExactlyAsync<T>()` / `await f.Should().NotThrowAsync()` (test becomes `async Task`) |
| `Assert.Multiple(() => { … })` | `using (new AssertionScope()) { … }` (`using AwesomeAssertions.Execution;`) |

`StringAssert` and `CollectionAssert` (NUnit 4: `Legacy.StringAssert` / `Legacy.CollectionAssert`):

| NUnit | AwesomeAssertions |
|---|---|
| `StringAssert.Contains(substr, act)` / `DoesNotContain` | `act.Should().Contain(substr)` / `NotContain` |
| `StringAssert.StartsWith(exp, act)` / `EndsWith` | `act.Should().StartWith(exp)` / `EndWith` |
| `StringAssert.IsMatch(pattern, act)` | `act.Should().MatchRegex(pattern)` |
| `StringAssert.AreEqualIgnoringCase(exp, act)` | `act.Should().BeEquivalentTo(exp)` (for strings = case-insensitive equality) |
| `CollectionAssert.AreEqual(exp, act)` (ordered) | `act.Should().Equal(exp)` |
| `CollectionAssert.AreEquivalent(exp, act)` (any order) | `act.Should().BeEquivalentTo(exp)` |
| `CollectionAssert.Contains(coll, item)` / `DoesNotContain` ⚠ collection first | `coll.Should().Contain(item)` / `NotContain` |
| `CollectionAssert.IsEmpty` / `IsNotEmpty` | `BeEmpty()` / `NotBeEmpty()` |
| `CollectionAssert.AllItemsAreUnique(coll)` | `coll.Should().OnlyHaveUniqueItems()` |
| `CollectionAssert.AllItemsAreNotNull(coll)` | `coll.Should().NotContainNulls()` |
| `CollectionAssert.AllItemsAreInstancesOfType(coll, typeof(T))` | `coll.Should().AllBeAssignableTo<T>()` |
| `CollectionAssert.IsSubsetOf(subset, superset)` | `subset.Should().BeSubsetOf(superset)` |
| `CollectionAssert.IsOrdered(coll)` | `coll.Should().BeInAscendingOrder()` |

## NUnit constraint-model (`Assert.That`) mappings

`Assert.That(actual, constraint)` always has the subject first, then the constraint.
Compound constraints (`.And.` / `.Or.`) usually map to a fluent `.And` chain or to two
separate `Should()` statements; `Is.Not.X` maps to the `Not...` variant.

| NUnit constraint | AwesomeAssertions |
|---|---|
| `Assert.That(cond)` / `Assert.That(cond, Is.True)` | `cond.Should().BeTrue()` |
| `Is.False` / `Is.Null` / `Is.Not.Null` | `BeFalse()` / `BeNull()` / `NotBeNull()` |
| `Is.EqualTo(exp)` | `Be(exp)` — for collections: `Equal(exp)` (NUnit compares structurally, in order) |
| `Is.EqualTo(exp).Within(delta)` | `BeApproximately(exp, delta)` |
| `Is.EqualTo(exp).Within(p).Percent` | `BeApproximately(exp, exp * p / 100)` |
| `Is.EqualTo(exp).IgnoreCase` (strings) | `BeEquivalentTo(exp)` |
| `Is.Not.EqualTo(exp)` | `NotBe(exp)` |
| `Is.SameAs(exp)` / `Is.Not.SameAs` | `BeSameAs(exp)` / `NotBeSameAs` |
| `Is.GreaterThan(x)` / `Is.GreaterThanOrEqualTo(x)` / `Is.AtLeast(x)` | `BeGreaterThan(x)` / `BeGreaterThanOrEqualTo(x)` |
| `Is.LessThan(x)` / `Is.LessThanOrEqualTo(x)` / `Is.AtMost(x)` | `BeLessThan(x)` / `BeLessThanOrEqualTo(x)` |
| `Is.InRange(lo, hi)` | `BeInRange(lo, hi)` |
| `Is.Positive` / `Is.Negative` / `Is.Zero` / `Is.NaN` | `BePositive()` / `BeNegative()` / `Be(0)` / `Be(double.NaN)` |
| `Is.Empty` / `Is.Not.Empty` | `BeEmpty()` / `NotBeEmpty()` |
| `Is.Not.Null.And.Not.Empty` (strings) | `NotBeNullOrEmpty()` |
| `Is.InstanceOf<T>()` (derived allowed) | `BeAssignableTo<T>()` |
| `Is.TypeOf<T>()` (exact) | `BeOfType<T>()` |
| `Is.EquivalentTo(coll)` (same items, any order) | `BeEquivalentTo(coll)` |
| `Is.SubsetOf(coll)` | `BeSubsetOf(coll)` |
| `Is.Ordered` / `Is.Ordered.Descending` | `BeInAscendingOrder()` / `BeInDescendingOrder()` |
| `Is.Unique` | `OnlyHaveUniqueItems()` |
| `Is.All.GreaterThan(x)` / `Has.All...` | `OnlyContain(i => i > x)` (note: passes on empty — add `NotBeEmpty()` if emptiness must fail) |
| `Has.Count.EqualTo(n)` | `HaveCount(n)` |
| `Has.Length.EqualTo(n)` | `HaveLength(n)` (strings) / `HaveCount(n)` |
| `Has.Member(item)` / `Has.No.Member(item)` | `Contain(item)` / `NotContain(item)` |
| `Has.Some.Matches<T>(pred)` / `Has.Some.GreaterThan(x)` | `Contain(pred)` / `Contain(i => i > x)` |
| `Has.Exactly(1).Matches<T>(pred)` / `Has.One...` | `ContainSingle(pred)` |
| `Has.Exactly(n).Matches<T>(pred)` | manual: `coll.Count(pred).Should().Be(n)` |
| `Has.Property("Name").EqualTo(v)` | navigate directly: `subject.Name.Should().Be(v)` (cast first if subject is `object`) |
| `Does.Contain(s)` / `Does.Not.Contain(s)` | `Contain(s)` / `NotContain(s)` |
| `Does.StartWith(s)` / `Does.EndWith(s)` | `StartWith(s)` / `EndWith(s)` |
| `Does.Match(pattern)` | `MatchRegex(pattern)` |
| `Assert.That(del, Throws.TypeOf<T>())` | `del.Should().ThrowExactly<T>()` |
| `Assert.That(del, Throws.InstanceOf<T>())` | `del.Should().Throw<T>()` |
| `Throws.ArgumentException` etc. shorthands | `ThrowExactly<ArgumentException>()` |
| `Throws.TypeOf<T>().With.Message.Contains(s)` | `ThrowExactly<T>().WithMessage("*s*")` |
| `Assert.That(del, Throws.Nothing)` | `del.Should().NotThrow()` |
| `Assert.That(actual, constraint, "message")` | append the message as the `because` argument |

When `Assert.That` wraps a lambda or `async` delegate for exception testing, the
delegate becomes the fluent subject: `Assert.That(() => Parse(s), Throws.TypeOf<FormatException>())`
→ `Action act = () => Parse(s); act.Should().ThrowExactly<FormatException>();`
(or inline: `((Action)(() => Parse(s))).Should()...` — prefer the named local for readability).

## What stays as-is

These are runner directives, not assertions — they have no fluent equivalent and must
remain:

- NUnit: `Assert.Pass()`, `Assert.Fail(...)`, `Assert.Ignore(...)`, `Assert.Inconclusive(...)`,
  `Assert.Warn(...)`, `Assume.That(...)`
- xUnit: `Assert.Fail(...)` (and `Assert.Skip(...)` in xUnit v3)
- All attributes and lifecycle hooks: `[Test]`, `[TestCase]`, `[SetUp]`, `[Fact]`,
  `[Theory]`, `[InlineData]`, fixtures, collections, etc.

Because some `Assert.*` calls legitimately remain, "no `Assert.` left in the code" is
the wrong completion check. Verify instead that no *comparison* asserts remain, e.g.:

```bash
grep -rnE "Assert\.(That|AreEqual|AreNotEqual|AreSame|AreNotSame|IsTrue|IsFalse|True|False|IsNull|IsNotNull|Null|NotNull|IsEmpty|IsNotEmpty|Empty|NotEmpty|Greater|Less|Positive|Negative|Zero|NotZero|IsInstanceOf|IsNotInstanceOf|IsAssignableFrom|IsType|IsNotType|Equal|NotEqual|Same|NotSame|Contains|DoesNotContain|StartsWith|EndsWith|Matches|InRange|Single|All\b|Collection|Equivalent|Distinct|Subset|Superset|Throws|ThrowsAny|ThrowsAsync|Catch|DoesNotThrow|Multiple|Raises)" --include="*.cs" . | grep -v bin/ | grep -v obj/
grep -rnE "(CollectionAssert|StringAssert|ClassicAssert)\." --include="*.cs" . | grep -v bin/ | grep -v obj/
```

and that the test count before and after conversion is identical, with all tests green.
