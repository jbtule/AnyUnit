# Testing under WebAssembly: dynamic loading vs. native dependencies

`anyunit-wasm` (this folder + `../wasm-runner`) is a generic runner: point
it at any test assembly's `.dll` and it dynamically `Assembly.Load(byte[])`s
it into this host at runtime, fetched over HTTP, no rebuild needed per test
assembly. That works well for pure-managed test assemblies - which is all
of AnyUnit's own self-tests - but it has a real, structural limit: **it
cannot run a test assembly that needs its own P/Invoke-based native code**
(a native image processing/OCR/PDF library, anything shipped as a
`NativeFileReference`/Emscripten static archive, etc.).

## Why dynamic loading can't do native code

A WebAssembly app's native dependencies (say, libSkiaSharp, PDFium,
Tesseract) get **statically linked into that build's own
`dotnet.native.wasm`** at build time (`WasmBuildNative=true` +
`NativeFileReference`). That linking is a property of one specific
compiled binary - there's no way to hand a running wasm host a *different*
assembly's native code after the fact the way `Assembly.Load(byte[])`
hands it different *managed* code. The test assembly and the runtime
hosting it have to be compiled together, in the same project, so the
native linking step actually sees what that assembly needs.

So for a consumer whose tests need native wasm dependencies, `anyunit-wasm`
genuinely can't help - not a missing feature, a structural mismatch with
how wasm native linking works.

## The alternative: compile your own host

Reference `AnyUnit`/your style package(s) (e.g. `AnyUnit.Style.FSharp`)
directly from your own `Sdk.BlazorWebAssembly` project, alongside whatever
native-dependent packages your tests need, and drive the same
`AnyUnit.Run.Runner` API `anyunit-wasm`'s own CLI is built on, directly:

```fsharp
[<EntryPoint>]
let main _ =
    let runner = Runner.Create("wasm", [ Assembly.GetExecutingAssembly() ])
    let mutable failures = 0
    runner.RunAll(fun result ->
        match result.Kind with
        | ResultKind.Fail | ResultKind.Error -> failures <- failures + 1
        | _ -> ())
    if failures = 0 then 0 else 1
```

No CLI/argument parsing needed - `Runner.Create`/`RunAll` (`AnyUnit/Run/
Runner.cs`) is the same small, direct API `net10-runner`'s own entry point
uses under the hood.

Confirmed empirically (worked out testing `qr-link-pdf`'s own PDFium/Skia/
Tesseract-dependent suite - see that repo's `QrLinkPdf.Tests.Wasm/` for the
real, working example):

- **Use `Sdk.BlazorWebAssembly`, not the plain console `Sdk.WebAssembly`.**
  Both *can* statically link native code (`WasmBuildNative` itself isn't
  Blazor-specific), but several native-asset NuGet packages'
  own `.targets` (confirmed for `SkiaSharp.NativeAssets.WebAssembly`) gate
  their `NativeFileReference` contribution behind
  `UsingMicrosoftNETSdkWebAssembly`/`UsingMicrosoftNETSdkBlazorWebAssembly`
  - internal SDK-identity flags only `Sdk.WebAssembly`'s/
  `Sdk.BlazorWebAssembly`'s own `.props` set. `Sdk.BlazorWebAssembly` is
  the one confirmed to get every native dependency linked with zero
  manual property-setting.
- **Compile your test source directly** (`<Compile Include="../YourTests/Foo.fs" />`),
  not a `ProjectReference` to it - the native-linking-sensitive project has
  to be the one actually compiling the test code, not a downstream
  consumer of a library built for a different TFM.
- **You never need to build a real Blazor app.** `Program.fs`/`Program.cs`
  can be a plain console-style entry point (as above) - no
  `WebAssemblyHostBuilder`, no Razor components, no UI. `dotnet.js`
  detects a non-browser JS host (`node`/`bun`) and boots directly without
  one - confirmed identical results and exit codes under both. This is
  the pleasant surprise: despite using the Blazor SDK (for its correct
  native linking), running the tests needs **no headless browser at all**
  - unlike `anyunit-wasm` itself, which drives a real (headless) browser
  via PuppeteerSharp specifically because its own host *is* a real Blazor
  app.
- **`dotnet build` alone is enough** to produce a runnable
  `wwwroot/_framework/` (the linked runtime + your compiled assemblies) -
  no `dotnet publish` needed for that part. Boot it the same way a plain
  console `Sdk.WebAssembly` app already does (see e.g.
  `QrLinkPdf.Wasm.SmokeTest/wwwroot/runtests.mjs` in `qr-link-pdf`):
  ```js
  import { dotnet } from './_framework/dotnet.js';
  const { runMain } = await dotnet.withApplicationArguments(...process.argv.slice(2)).create();
  process.exitCode = await runMain();
  ```
- **Two gotchas that cost real time working this out:**
  - Blazor's static-web-assets pipeline (which would otherwise copy your
    own `wwwroot/` files - `index.html`, a JS harness script, etc.) only
    runs at `dotnet publish`, not `dotnet build`. If you want them present
    after a plain build, add them as explicit
    `<None Include="wwwroot/foo" CopyToOutputDirectory="PreserveNewest" />`
    items - that's plain MSBuild content-copy semantics, unrelated to the
    static-web-assets pipeline, and it does run at build time.
  - Any file your test code reads via plain POSIX file I/O
    (`File.Exists`/`File.OpenRead`, not something `dotnet`'s own asset
    loader manages) is invisible at runtime even if it's sitting right
    there in `wwwroot/` - Mono routes that kind of access to its own
    Emscripten-backed virtual filesystem, which a non-browser host has no
    reason to have pre-populated from disk. Intercepting `global.fetch`
    to serve such files off disk does **not** work - confirmed neither
    `_framework/*` loading nor this kind of file access goes through
    `fetch()` under `node`/`bun` at all (and overriding it can even hang
    the runtime). Instead, write the file directly into the virtual
    filesystem before calling `runMain()`, using the `Module` object
    `dotnet.create()` returns:
    ```js
    const dotnetInstance = await dotnet.create();
    const bytes = readFileSync('/real/path/to/your-data-file');
    try { dotnetInstance.Module.FS.mkdir('/your-dir'); } catch {}
    dotnetInstance.Module.FS.writeFile('/your-dir/your-file', bytes);
    process.exitCode = await dotnetInstance.runMain();
    ```
