using System.Runtime.InteropServices;
using AnyUnit.Util;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using PuppeteerSharp;

namespace BrowserWasmRunner;

public static class WasmRunAlone
{
    // Same layout whether run from source or installed as a packed tool:
    // browser-wasm-runner.csproj's CopyBrowserWasmRunnerHostToOutput target
    // lays the host out at $(OutDir)browser-wasm-host\wwwroot (i.e. right
    // next to this assembly), and its own pack Content item embeds that
    // same relative shape as tool content under
    // tools/<tfm>/any/browser-wasm-host/ - the folder a packed tool's own
    // assembly runs from too, so AppContext.BaseDirectory resolves it
    // identically either way.
    private static string DefaultHostWwwroot =>
        Path.Combine(AppContext.BaseDirectory, "browser-wasm-host", "wwwroot");

    public static async Task<bool> RunAsync(IReadOnlyList<string> dllPaths, IDictionary<string, string> outputs, bool teamCity, bool forceNoSandbox = false, string? platformSuffix = null)
    {
        var testAssemblyNames = dllPaths.Select(p => Path.GetFileNameWithoutExtension(p)!).ToList();
        var nameToPath = dllPaths.ToDictionary(p => Path.GetFileNameWithoutExtension(p)!, p => p, StringComparer.OrdinalIgnoreCase);

        // Most real test assemblies aren't self-contained - they reference
        // their own dependencies (style libraries, the system under test,
        // other NuGet packages). dotnet build/publish already flattens all
        // of that into the same output folder as the test assembly itself
        // (the same assumption net10-runner/net48-runner's own
        // AssemblyResolve fallback in RunTests.cs relies on) - so serve and
        // load every .dll found alongside each given assembly too, not
        // just the ones named on the command line. These extra ones are
        // loaded for dependency resolution only, not treated as test
        // assemblies to run (see the "extra" vs "assemblies" query
        // parameters below).
        //
        // This only works if the test project's own build actually put
        // its NuGet package dependencies there too, not just its
        // ProjectReferences - a plain Library-output project does NOT
        // copy PackageReferences locally by default (only an exe-shaped
        // project does), so a test project with real package
        // dependencies (e.g. FsUnitTests needing FSharp.Core, via
        // AnyUnit.Style.FsUnit) needs
        // <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
        // itself to get its whole resolved package graph copied locally -
        // see FsUnitTests.fsproj. This is the general fix (works for any
        // NuGet package, not just FSharp.Core specifically), so it's the
        // right thing to ask of any external test project pointed at
        // this runner too, rather than this runner trying to guess or
        // special-case individual package names.
        foreach (var dir in dllPaths.Select(Path.GetDirectoryName).Distinct())
        {
            if (dir == null || !Directory.Exists(dir))
            {
                continue;
            }

            foreach (var siblingDll in Directory.GetFiles(dir, "*.dll"))
            {
                var name = Path.GetFileNameWithoutExtension(siblingDll);
                nameToPath.TryAdd(name, siblingDll);
            }
        }

        var hostWwwroot = DefaultHostWwwroot;
        if (!Directory.Exists(hostWwwroot))
        {
            Console.Error.WriteLine($"browser-wasm-runner-host not found at '{hostWwwroot}'.");
            Console.Error.WriteLine("Publish it first: dotnet publish Runner/Platforms/browser-wasm-runner-host -c Release");
            return true;
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = hostWwwroot });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        var app = builder.Build();

        // Serve each caller-supplied test assembly by its own simple name,
        // straight from wherever it actually lives on disk - no copying.
        app.MapGet("/testassemblies/{name}.dll", async context =>
        {
            var name = (string)context.Request.RouteValues["name"]!;
            if (!nameToPath.TryGetValue(name, out var path) || !File.Exists(path))
            {
                context.Response.StatusCode = 404;
                return;
            }
            context.Response.ContentType = "application/octet-stream";
            await context.Response.SendFileAsync(path);
        });

        // Backup logging channel: Console.WriteLine is documented to route
        // to the browser's JS console under Blazor WASM, and normally
        // page.Console below picks that straight up - but calls from
        // inside a Razor component's OnInitializedAsync weren't reliably
        // showing up there in practice (unclear why; not worth chasing
        // further). Posting back to our own Kestrel host, which we
        // already fully control, sidesteps that uncertainty entirely.
        app.MapPost("/log", async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var message = await reader.ReadToEndAsync();
            Console.Error.WriteLine($"[wasm log] {message}");
        });

        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
        app.MapFallbackToFile("index.html");
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        // Console-message label only (see PrintStart/PrintEnd below) -
        // computed once, here, not per print call - the actual Platform
        // value in the JSON is computed independently, inside the
        // browser-wasm-runner-host process itself (see AnyUnitRunnerPage.
        // razor's own use of PlatformId.Current), since that's genuinely a
        // different process/runtime than this launcher. Both derive from
        // the same AnyUnit.Util.PlatformId logic, so in practice they
        // agree, but there's no way to literally share the one computed
        // value across that process boundary without adding IPC just for
        // a log line.
        var platformLabel = string.IsNullOrEmpty(platformSuffix) ? PlatformId.Current : PlatformId.Current + "-" + platformSuffix;

        var assembliesParam = string.Join(",", testAssemblyNames.Select(name => Uri.EscapeDataString(name)));
        var extraParam = string.Join(",", nameToPath.Keys.Except(testAssemblyNames, StringComparer.OrdinalIgnoreCase).Select(name => Uri.EscapeDataString(name)));
        var platformSuffixParam = string.IsNullOrEmpty(platformSuffix) ? "" : $"&platformSuffix={Uri.EscapeDataString(platformSuffix)}";

        // PuppeteerSharp instead of Microsoft.Playwright: pure .NET, no
        // bundled Node.js driver (Playwright's own package bundled ALL
        // platforms' Node runtimes for a cross-platform "any"-RID dotnet
        // tool like this one, ~106MB dead weight - see the package
        // breakdown this replaced). DownloadAsync is a no-op after the
        // first run (cached locally), so no separate install step is
        // needed the way `playwright install` was.
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        // --no-sandbox, Linux CI only (or -no-sandbox forces it):
        // Chromium's own sandbox needs unprivileged user namespaces,
        // which Linux CI containers (confirmed on GitHub-hosted
        // ubuntu-latest) commonly run with AppArmor restrictions that
        // block - without it, launch fails outright there with "No
        // usable sandbox!". A real Linux desktop/dev machine has a
        // working sandbox, so this isn't a blanket "Linux" workaround -
        // it's gated on the CI env var that GitHub Actions (and
        // virtually every other CI system, by long-standing convention)
        // sets, so a local Linux dev run keeps the real sandbox. Not
        // needed on macOS/Windows either way (different, non-namespace-
        // based sandboxing). forceNoSandbox (RunCommand's -no-sandbox)
        // is an explicit override for when that auto-detection guesses
        // wrong, e.g. a self-hosted/Docker environment without CI set.
        var isLinuxCi = RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            && Environment.GetEnvironmentVariable("CI") is not null;
        var args = isLinuxCi || forceNoSandbox ? new[] { "--no-sandbox" } : [];
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true, Args = args });
        var page = await browser.NewPageAsync();
        page.Console += (_, e) => Console.Error.WriteLine($"[browser console:{e.Message.Type}] {e.Message.Text}");
        page.PageError += (_, e) => Console.Error.WriteLine($"[browser error] {e.Message}");

        PrintStart(teamCity, platformLabel);

        await page.GoToAsync($"{address}/?assemblies={assembliesParam}&extra={extraParam}{platformSuffixParam}");
        // Default WaitForSelectorOptions (neither Visible nor Hidden set)
        // waits for present-in-DOM regardless of visibility - exactly what
        // #anyunit-done (deliberately style="display:none") needs, no
        // override required (unlike Playwright, whose default is
        // visible-only and needed an explicit State=Attached override).
        await page.WaitForSelectorAsync("#anyunit-done", new WaitForSelectorOptions { Timeout = 180000 });

        var doneElement = await page.QuerySelectorAsync("#anyunit-done");
        var hasErrorAttr = await doneElement.EvaluateFunctionAsync<string?>("e => e.getAttribute('data-haserror')");
        var summaryElement = await page.QuerySelectorAsync("#anyunit-summary");
        var summary = await summaryElement.EvaluateFunctionAsync<string>("e => e.innerText");
        var jsonElement = await page.QuerySelectorAsync("#anyunit-json");
        var json = await jsonElement.EvaluateFunctionAsync<string>("e => e.innerText");

        await app.StopAsync();

        // No live per-test streaming here (unlike net10-runner/net48-runner):
        // results only come back once the whole WASM run finishes, since
        // that's when the page's hooks are populated. Print the batch
        // summary in the same shape PrintOutAloneEnd uses instead.
        PrintEnd(summary, teamCity, platformLabel);

        foreach (var output in outputs)
        {
            File.WriteAllText(output.Value, json);
        }

        return string.Equals(hasErrorAttr, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintStart(bool teamCity, string platformLabel)
    {
        if (teamCity)
        {
            Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", platformLabel);
        }
        else
        {
            Console.WriteLine("Starting Tests for '{0}'", platformLabel);
        }
    }

    private static void PrintEnd(string summary, bool teamCity, string platformLabel)
    {
        if (teamCity)
        {
            Console.WriteLine("##teamcity[testSuiteFinished name='{0}']", platformLabel);
        }
        else
        {
            Console.WriteLine("Finished");
            Console.WriteLine();
            Console.WriteLine(summary);
        }
    }
}
