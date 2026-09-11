// Drives the AnyUnit browser-runner Blazor WebAssembly app headlessly via
// Microsoft.Playwright and converts its in-page results back into the same
// console/JSON output shape as the net10/net48 console runners, so the
// browser target can be scripted from CI the same way the others are.
//
// One-time setup (downloads the headless browser binaries; no pwsh needed,
// the package ships its own node runtime):
//   dotnet build Runner/Platforms/browser-host/browser-host.csproj
//   BIN=Runner/Platforms/browser-host/bin/Debug/net10.0
//   "$BIN/.playwright/node/<rid>/node" "$BIN/.playwright/package/cli.js" install chromium
//
// Usage (point it at a *published* browser-runner wwwroot -- `dotnet build`
// alone does not merge index.html/css into wwwroot, only `dotnet publish`
// does):
//   dotnet publish Runner/Platforms/browser/browser-runner.csproj -c Release -o /tmp/browser-publish
//   dotnet run --project Runner/Platforms/browser-host -- [-o results.json] /tmp/browser-publish/wwwroot

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Playwright;

var outputIndex = Array.IndexOf(args, "-o");
string? outputPath = null;
if (outputIndex >= 0 && outputIndex + 1 < args.Length)
{
    outputPath = args[outputIndex + 1];
}

var positional = args
    .Where((a, i) => a != "-o" && !(outputIndex >= 0 && i == outputIndex + 1))
    .ToArray();

var wwwroot = positional.Length > 0
    ? positional[0]
    : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "browser", "bin", "Release", "net10.0", "publish", "wwwroot");
wwwroot = Path.GetFullPath(wwwroot);

if (!Directory.Exists(wwwroot))
{
    Console.Error.WriteLine($"wwwroot not found at '{wwwroot}'. Publish Runner/Platforms/browser/browser-runner.csproj first.");
    return 1;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = wwwroot });
builder.WebHost.UseUrls("http://127.0.0.1:0");
var app = builder.Build();
// ServeUnknownFileTypes: the Blazor WASM publish output includes extensions
// (.dat for ICU data, among others) the default content type provider
// doesn't recognize, which would otherwise 404.
app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });
app.MapFallbackToFile("index.html");
await app.StartAsync();

var address = app.Services.GetRequiredService<IServer>()
    .Features.Get<IServerAddressesFeature>()!
    .Addresses.First();

using var playwright = await Playwright.CreateAsync();
await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync();
page.Console += (_, msg) => Console.Error.WriteLine($"[browser console:{msg.Type}] {msg.Text}");
page.PageError += (_, msg) => Console.Error.WriteLine($"[browser error] {msg}");

await page.GotoAsync(address);
// State defaults to "visible", but #anyunit-done is deliberately
// style="display:none" (a machine-readable hook, not UI) - waiting for
// visible never resolves and just burns the full timeout.
await page.WaitForSelectorAsync("#anyunit-done", new PageWaitForSelectorOptions
{
    State = WaitForSelectorState.Attached,
    Timeout = 180000,
});

var hasError = await page.GetAttributeAsync("#anyunit-done", "data-haserror");
var summary = await page.InnerTextAsync("#anyunit-summary");
var json = await page.InnerTextAsync("#anyunit-json");

Console.WriteLine("Starting Tests for 'net10-browser'");
Console.WriteLine("Finished");
Console.WriteLine();
Console.WriteLine(summary);

if (outputPath != null)
{
    File.WriteAllText(outputPath, json);
}

await app.StopAsync();

return string.Equals(hasError, "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
