using AnyUnit.TestingPlatform;
using Microsoft.Testing.Platform.Builder;

var builder = await TestApplication.CreateBuilderAsync(args);
builder.AddAnyUnitTestFramework(typeof(BasicTests.Basic).Assembly);
using var app = await builder.BuildAsync();
return await app.RunAsync();
