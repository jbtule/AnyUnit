# AnyUnit net48 runner

The same standalone CLI as [`AnyUnit.Runner`](../net10) (`run`, `-o`,
`-p`, `-s`, `--teamcity` - it compiles the identical `../shared/*.cs`
sources), built for `net48` so an [AnyUnit](https://github.com/jbtule/AnyUnit)
test assembly targeting classic .NET Framework has somewhere to actually
run. `net48` is a real, still-in-use target that the `net10` runner
can't host.

## Not a NuGet package

This is the one runner that isn't published to nuget.org and isn't a
`dotnet tool`. Installing a .NET Framework console app through
`dotnet tool install` isn't a thing, so it ships only as a
**GitHub Release asset**, in both bitnesses:

- `anyunit-net48-runner-win32.exe`
- `anyunit-net48-runner-win64.exe`

Windows-only, because .NET Framework has no other host OS.

`win32`/`win64` rather than `x86`/`x64`: the 64-bit build is plain x64,
but it runs fine on Windows-on-ARM64 via the OS's built-in x64
emulation, so bitness is the more accurate label than an
Intel/AMD-implying architecture name.

## Usage

```
anyunit-net48-runner-win64.exe run [-o <file>] [-p <suffix>] [--teamcity] <assembly.dll> [...]
```

Identical to `anyunit-runner` - see [its README](../net10/README.md).
Results carry a `net48-win-x86`/`net48-win-x64` platform id, so a merged
results file can tell a net48 run apart from a net10 one (see
`AnyUnit/Util/PlatformId.cs`).

## Building

Classic .NET Framework has no self-contained/`RuntimeIdentifier`
deployment model - bitness is a plain PE-header `PlatformTarget`, so it
takes two separate builds rather than one multi-RID publish:

```
dotnet build Runner/Platforms/net48/net48-runner.csproj -c Release -p:Platform=x86
dotnet build Runner/Platforms/net48/net48-runner.csproj -c Release -p:Platform=x64
```

Output lands in `bin\x86\Release\net48\` and `bin\x64\Release\net48\`.
The project builds anywhere `dotnet build` runs, macOS and Linux
included - only *running* the result needs Windows.

## How it ships as a single .exe

The `net10` runner gets single-file deployment for free from a
self-contained publish; .NET Framework has no equivalent. So:

- **`net48-runner.csproj`**, `EmbedDependencies` target - zips every
  assembly the build was about to copy next to the `.exe`
  (`@(ReferenceCopyLocalPaths)`, the build's own answer to "what has to
  sit beside this", so the list can't drift) into an embedded resource,
  then empties that item so those files are *not* also written to the
  output directory. That second half matters: left on disk, the CLR
  would resolve them from there and the embedded path would never
  execute, meaning every local and CI run exercised an arrangement that
  isn't the one shipped.
- **`DiminishedProgram.cs`** - the runtime half. It's the
  `StartupObject` (the shared `Program.Main` is still compiled in but is
  no longer the entry point), installs an `AssemblyResolve` hook, and
  serves each dependency from the zip via `Assembly.Load(byte[])`.

Two details in `DiminishedProgram.cs` are load-bearing and look
removable:

- **It reaches the shared `Program.Main` by reflection**, from a
  `[MethodImpl(MethodImplOptions.NoInlining)]` method. The JIT resolves
  every type a method body references when it compiles that method -
  before its first statement runs - so naming an embedded type in the
  method that installs the resolver would need the resolver it is in the
  middle of installing.
- **It is deliberately not ILMerge/ILRepack.** Merging moves
  dependencies' types *into* this assembly, changing which assembly they
  live in. This is a test runner: it reflects over test assemblies
  compiled against a strong-named `AnyUnit.dll` and they ask the CLR for
  that exact identity at runtime. Embedding preserves each assembly's
  identity; merging breaks it.

The embed-then-resolve approach is
[jbtule/diminish-dependencies](https://github.com/jbtule/diminish-dependencies),
reimplemented here on the BCL's own `ZipArchive` - see
`DiminishedProgram.cs`'s own comment for why that project isn't
referenced directly.
