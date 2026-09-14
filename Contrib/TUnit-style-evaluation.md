# Evaluated: a TUnit style

**Decision (1.2): don't build one. Revisit after async test support has
landed and settled, if real demand appears.**

Written down so the question doesn't get re-opened from scratch every
time someone notices [TUnit](https://tunit.dev/) isn't in the list. The
answer isn't "TUnit is bad" - it's genuinely good, and that's part of
the reasoning below.

## What a "style" has to be, for this to make sense

Every existing style here (`AnyUnit.Style.Nunit`, `.Xunit`, `.FSharp`,
`.FsUnit`) earns its place the same way: someone has an existing test
suite in that framework's syntax, and needs it to run somewhere that
framework can't reach. The style gets close enough to the real
framework's syntax that the *logic* of those tests - attributes and
assertions - often carries over unchanged, and then AnyUnit's
`netstandard2.0` core takes them to browser-wasm, net48, MTP, and seven
real desktop RIDs.

So the test isn't "could we implement a `[Test]` attribute?" It's
"whose stranded suite does this rescue?"

## Why TUnit doesn't clear that bar

### 1. Nobody's TUnit suite is stranded

This is the one that actually settles it. AnyUnit's own pitch is to
reach for it *when you need tests to run somewhere a real
NUnit/xUnit/MSTest install can't*. TUnit is built on
Microsoft.Testing.Platform and supports Native AOT - it already reaches
everywhere modern .NET does. There is no population of TUnit users
blocked from a platform that AnyUnit would unblock.

Contrast MSTest and Expecto, both of which are being added in 1.2:
those have real bodies of existing tests, often old ones, whose authors
genuinely can't get them onto every target they need.

### 2. Async isn't optional in TUnit, it's structural

TUnit's own documentation is explicit: if you use `Assert.That(...)`,
your test **must** be `async Task`, because assertions return awaitable
objects that don't execute without `await` - and `async void` is a
build error (`TUnit0031`).

So a TUnit style is gated on AnyUnit's core awaiting returned `Task`s.
That work is happening in 1.2 (target T3) - but note *why* it's being
done: until it landed, `Test.RunHelper` discarded a returned `Task`
unawaited, so an async test's failures vanished and it passed
regardless. That was a live correctness bug affecting people porting
ordinary modern NUnit and xUnit suites today, and it was worth fixing on
its own merits. It is not TUnit scaffolding.

A TUnit style built before that work is not a partial port. It's a
silently-broken one, where every assertion is a no-op.

### 3. `netstandard2.0` can't hold the shape of TUnit's API

TUnit targets .NET 8 and up, and leans on the modern language and
runtime surface that assumes - generic math, required members, records,
and source generators throughout.

AnyUnit's core is `netstandard2.0` on purpose, and that isn't
incidental: it's what lets the same library load under Mono's
browser-wasm interpreter and under .NET Framework 4.8. It won't be
moving.

`AnyUnit.Style.Nunit` and `.Xunit` are recognisable subsets of their
frameworks - close enough that a real suite mostly compiles. A
`netstandard2.0` "TUnit style" would be a much thinner shadow of TUnit
than those are of theirs, which inverts the value proposition: the
closer a style gets, the more it's worth having, and this one couldn't
get close.

### 4. TUnit suites assume parallelism this engine doesn't have

`Runner.RunAll` is strictly sequential, and that's load-bearing, not an
oversight - `Fixture`'s one-time setup/teardown bookkeeping explicitly
relies on it ("the Runner doesn't run tests concurrently, so no locking
here"). TUnit runs tests in parallel by default and has `[NotInParallel]`
for the exceptions, so real TUnit suites are written against parallel
semantics and would be running under something meaningfully different
here.

## What would change the answer

- Someone turns up with a real TUnit suite that genuinely needs a target
  TUnit can't reach. That's the bar the other styles cleared.
- Async support (T3) proves out across every platform, browser-wasm
  included - which is where it's hardest, since the single-threaded
  runtime can't just block on a `Task`.
- Parallel execution becomes a thing this engine does, rather than
  something it documents that it doesn't.

The first of those is the one that matters. The other two are
prerequisites, not reasons.

## Also evaluated

**Expecto** - accepted for 1.2, and a much better fit for roughly the
same effort. It's value-based (`testList "x" [ testCase "y" <| fun () ->
... ]`), and `AnyUnit.Style.FSharp/Discovery.fs` is already working,
documented proof that AnyUnit's `TestFixtureDiscoveryAttributeBase` plus
a `Fixture.GetHarnesses()` override handles exactly that shape. Its one
real obstacle is that `testList` nests arbitrarily while AnyUnit's model
is a fixed Assembly→Fixture→Test three levels.
