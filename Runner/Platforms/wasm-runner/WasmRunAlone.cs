using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Playwright;

namespace WasmRunner;

public static class WasmRunAlone
{
    private const string PlatformId = "net10-wasm";

    // TODO: not yet a packaged dotnet tool - resolves the pre-published
    // generic host via a relative dev path. A real tool package would
    // embed this as bundled content instead (see wasm-runner.csproj).
    private static string DefaultHostWwwroot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "wasm-runner-host", "bin", "Release", "net10.0", "publish", "wwwroot"));

    public static async Task<bool> RunAsync(IReadOnlyList<string> dllPaths, IDictionary<string, string> outputs, bool teamCity)
    {
        var nameToPath = dllPaths.ToDictionary(p => Path.GetFileNameWithoutExtension(p)!, p => p, StringComparer.OrdinalIgnoreCase);

        var hostWwwroot = DefaultHostWwwroot;
        if (!Directory.Exists(hostWwwroot))
        {
            Console.Error.WriteLine($"wasm-runner-host not found at '{hostWwwroot}'.");
            Console.Error.WriteLine("Publish it first: dotnet publish Runner/Platforms/wasm-runner-host -c Release");
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

        app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
        app.MapFallbackToFile("index.html");
        await app.StartAsync();

        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        var assembliesParam = string.Join(",", nameToPath.Keys.Select(name => Uri.EscapeDataString(name)));

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();
        page.Console += (_, msg) => Console.Error.WriteLine($"[browser console:{msg.Type}] {msg.Text}");
        page.PageError += (_, msg) => Console.Error.WriteLine($"[browser error] {msg}");

        PrintStart(teamCity);

        await page.GotoAsync($"{address}/?assemblies={assembliesParam}");
        await page.WaitForSelectorAsync("#anyunit-done", new PageWaitForSelectorOptions
        {
            State = WaitForSelectorState.Attached,
            Timeout = 180000,
        });

        var hasErrorAttr = await page.GetAttributeAsync("#anyunit-done", "data-haserror");
        var summary = await page.InnerTextAsync("#anyunit-summary");
        var json = await page.InnerTextAsync("#anyunit-json");

        await app.StopAsync();

        // No live per-test streaming here (unlike net10-runner/net48-runner):
        // results only come back once the whole WASM run finishes, since
        // that's when the page's hooks are populated. Print the batch
        // summary in the same shape PrintOutAloneEnd uses instead.
        PrintEnd(summary, teamCity);

        foreach (var output in outputs)
        {
            File.WriteAllText(output.Value, json);
        }

        return string.Equals(hasErrorAttr, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static void PrintStart(bool teamCity)
    {
        if (teamCity)
        {
            Console.WriteLine("##teamcity[testSuiteStarted name='{0}']", PlatformId);
        }
        else
        {
            Console.WriteLine("Starting Tests for '{0}'", PlatformId);
        }
    }

    private static void PrintEnd(string summary, bool teamCity)
    {
        if (teamCity)
        {
            Console.WriteLine("##teamcity[testSuiteFinished name='{0}']", PlatformId);
        }
        else
        {
            Console.WriteLine("Finished");
            Console.WriteLine();
            Console.WriteLine(summary);
        }
    }
}
