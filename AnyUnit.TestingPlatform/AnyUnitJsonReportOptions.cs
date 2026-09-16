//
//  Copyright 2026 AnyUnit Contributors
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Configurations;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.CommandLine;
using Microsoft.Testing.Platform.Services;

namespace AnyUnit.TestingPlatform
{
    /// <summary>
    /// Declares `--report-anyunit-json [path]`, which makes an MTP run
    /// write AnyUnit's own results.json - the exact same file the console
    /// runners produce with `-o`, from the exact same serializer
    /// (ResultsFile.ToListJson()).
    /// </summary>
    // Why this exists at all, given MTP already has --report-trx: TRX is a
    // lossy destination (one <UnitTestResult> per test, no notion of
    // AnyUnit's Platform dimension) and, more to the point, it isn't the
    // format AnyUnit's own tooling reads. anyunit-report converts *from*
    // results.json, and WhoTestsTheTesters/ConventionTestProcessor - the
    // real self-test gate, which checks each result against the outcome its
    // own method name declares - reads results.json and nothing else. Until
    // this option existed, the *.Mtp self-test projects could only be
    // checked for "a .trx came out and parses as XML", which says nothing
    // about whether the MTP path actually reports the right outcomes.
    //
    // This is a plain ICommandLineOptionsProvider rather than an
    // ITestSessionLifetimeHandler-style "reporter extension" of its own
    // because the data it needs (AnyUnit.Run.Result objects) only exists
    // inside AnyUnitTestFramework's own run loop. Routing those back out
    // through the message bus, only for an extension living in the same
    // assembly to route them back in, would be pure ceremony - so this
    // type's whole job is parsing/validating the flag and deciding the
    // output path; AnyUnitTestFramework does the accumulating and writing.
    internal sealed class AnyUnitJsonReportOptions : ICommandLineOptionsProvider
    {
        // Option names are declared WITHOUT the leading "--" - MTP adds the
        // prefix itself (same as the platform's own "results-directory").
        public const string OptionName = "report-anyunit-json";

        // The MTP spelling of anyunit-runner's own -p/-platform-suffix (see
        // Runner/Platforms/shared/Commands.cs): a label appended to the
        // auto-detected platform id, for telling apart two runs of the same
        // assembly on the same OS/arch/framework that are nonetheless
        // different things - the motivating cases both being this repo's
        // own CI: the same assembly run through the console runner and
        // through MTP on one OS, and the in-repo .Mtp projects vs a
        // consumer built against the packed nupkg, each pair of which would
        // otherwise report the identical "net10-linux-x64" and collapse
        // into one column of the merged report.
        public const string PlatformSuffixOptionName = "platform-suffix";

        public string Uid => "AnyUnit.TestingPlatform.AnyUnitJsonReport";
        public string Version => "1.0.0";
        public string DisplayName => "AnyUnit JSON report";
        public string Description => "Writes AnyUnit's own results.json from a Microsoft.Testing.Platform run";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
        {
            return new[]
            {
                // ZeroOrOne, not ExactlyOne: `--report-trx` is usable with
                // no argument at all (the file lands in --results-directory
                // under a generated name) and this deliberately behaves the
                // same way, so someone who already knows the TRX flag
                // doesn't have to learn a different shape here.
                new CommandLineOption(
                    OptionName,
                    "Write AnyUnit's own results.json. Optionally takes an output path; with none, a generated name under --results-directory is used.",
                    ArgumentArity.ZeroOrOne,
                    isHidden: false),
                new CommandLineOption(
                    PlatformSuffixOptionName,
                    "Optional label appended to the auto-detected platform id (e.g. --platform-suffix ci-nightly).",
                    ArgumentArity.ExactlyOne,
                    isHidden: false),
            };
        }

        public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
        {
            // Arity already rejects "two or more arguments", so the only
            // thing left worth catching is a present-but-useless one - e.g.
            // `--report-anyunit-json ""`, which would otherwise be
            // indistinguishable from "no path given" by the time
            // ResolveOutputPath sees it, silently writing somewhere the
            // caller never asked for.
            if (commandOption.Name == OptionName && arguments.Length == 1 && string.IsNullOrWhiteSpace(arguments[0]))
                return ValidationResult.InvalidTask("--" + OptionName + " was given an empty path.");

            if (commandOption.Name == PlatformSuffixOptionName && string.IsNullOrWhiteSpace(arguments[0]))
                return ValidationResult.InvalidTask("--" + PlatformSuffixOptionName + " was given an empty label.");

            return ValidationResult.ValidTask;
        }

        public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
            => ValidationResult.ValidTask;

        /// <summary>
        /// The file to write, or null when the flag wasn't passed at all.
        /// </summary>
        // Resolved from the service provider at test-framework construction
        // time (see AnyUnitTestFrameworkExtensions) rather than being state
        // on this instance: MTP builds the options provider through its own
        // factory, so the instance that parsed the flag is not one this
        // assembly ever gets a reference to. ICommandLineOptions is the
        // supported way to read a parsed option back, from anywhere.
        public static string ResolveOutputPath(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetCommandLineOptions();

            string[] arguments;
            if (!options.TryGetOptionArgumentList(OptionName, out arguments))
                return null;

            if (arguments != null && arguments.Length > 0 && !string.IsNullOrWhiteSpace(arguments[0]))
            {
                // Relative paths resolve against the current working
                // directory, NOT against --results-directory. That matches
                // `anyunit-runner run -o <path>` (and every other CLI's -o),
                // which is what a caller passing an explicit path here is
                // almost certainly translating from. --results-directory
                // still governs the no-argument case below, where the
                // caller expressed no opinion about where the file goes.
                return Path.GetFullPath(arguments[0]);
            }

            var configuration = serviceProvider.GetConfiguration();
            var directory = configuration.GetTestResultDirectory();

            // Timestamped, like the TRX extension's own generated name, and
            // for the same reason: a second run in the same results
            // directory should sit beside the first rather than silently
            // replacing it. An explicit path (above) is left alone - there,
            // overwriting is exactly what the caller asked for.
            var name = string.Format(
                "{0}_{1:yyyy-MM-dd_HH_mm_ss}.json",
                EntryAssemblyName(),
                DateTimeOffset.Now);

            return Path.Combine(directory, name);
        }

        /// <summary>
        /// The label to append to the platform id, or null when the flag
        /// wasn't passed.
        /// </summary>
        public static string ResolvePlatformSuffix(IServiceProvider serviceProvider)
        {
            string[] arguments;
            if (!serviceProvider.GetCommandLineOptions().TryGetOptionArgumentList(PlatformSuffixOptionName, out arguments))
                return null;
            return arguments != null && arguments.Length > 0 ? arguments[0] : null;
        }

        private static string EntryAssemblyName()
        {
            var entry = Assembly.GetEntryAssembly();
            return entry != null ? entry.GetName().Name : "AnyUnit";
        }
    }
}
