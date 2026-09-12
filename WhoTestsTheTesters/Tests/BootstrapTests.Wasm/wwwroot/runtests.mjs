// Boots the WebAssembly build under node/bun and forwards its exit code -
// same trick QrLinkPdf.Tests.Wasm's own runtests.mjs uses (dotnet.js
// detects a non-browser JS host and loads the runtime itself, no browser
// or web server needed). No tessdata/Module.FS write needed here (unlike
// that project) - this self-test has no data files to inject into the
// virtual filesystem at all.
import { dotnet } from './_framework/dotnet.js';

const { runMain } = await dotnet
    .withApplicationArguments(...process.argv.slice(2))
    .create();

process.exitCode = await runMain();
