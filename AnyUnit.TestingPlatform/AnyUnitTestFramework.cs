//
//  Copyright 2013 AnyUnit Contributors
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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AnyUnit.Run;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Messages;
using Microsoft.Testing.Platform.Requests;

namespace AnyUnit.TestingPlatform
{
    /// <summary>
    /// Bridges AnyUnit's own discovery/execution engine (Runner.Create /
    /// Runner.RunAll) to Microsoft.Testing.Platform's ITestFramework, so
    /// any AnyUnit-based test project can be discovered and run through
    /// `dotnet test`, `dotnet run`, and IDE test explorers - independent
    /// of which style (NUnit/Xunit/FsUnit/AnyUnit's own) the test project
    /// actually uses, since this talks to Runner, not to style attributes.
    /// </summary>
    internal sealed class AnyUnitTestFramework : ITestFramework, IDataProducer
    {
        private readonly Assembly[] _testAssemblies;
        private readonly AnyUnitTrxReportCapability _trxReportCapability;

        // Null unless --report-anyunit-json was passed (see
        // AnyUnitJsonReportOptions), which is what makes the whole JSON
        // path opt-in: no flag, no ResultsFile, no file.
        private readonly string _anyUnitJsonPath;
        private readonly ResultsFile _resultsFile;

        // Set only by a RunTestExecutionRequest. CloseTestSessionAsync
        // can't tell a run that found nothing apart from a discovery-only
        // session (`--list-tests`) by looking at _resultsFile alone - both
        // have zero Results - and the two want opposite treatment. An
        // empty file from a real run is a genuine finding, and both
        // ConventionTestProcessor and .github/scripts/run-tests.sh
        // deliberately fail loudly on one; an empty file from
        // `--list-tests` would just be a lie those same checks would then
        // fail on. So discovery writes nothing at all, matching what
        // --report-trx does.
        private bool _ranTests;

        // Defaults to the entry assembly, matching a test project that
        // compiles its own tests directly into the MTP executable. A host
        // project that instead references its test assemblies as
        // libraries (see AnyUnitTestFrameworkExtensions.AddAnyUnitTestFramework's
        // own testAssemblies parameter) needs to say so explicitly - the
        // entry assembly would otherwise be the host itself, which has no
        // tests of its own, and Runner.Create would (silently) find zero.
        public AnyUnitTestFramework(IEnumerable<Assembly> testAssemblies, AnyUnitTrxReportCapability trxReportCapability, string anyUnitJsonPath, string platformSuffix = null)
        {
            var assemblies = (testAssemblies ?? Enumerable.Empty<Assembly>()).ToArray();
            _testAssemblies = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetEntryAssembly() ?? typeof(AnyUnitTestFramework).Assembly };
            _trxReportCapability = trxReportCapability;
            _anyUnitJsonPath = anyUnitJsonPath;
            _resultsFile = anyUnitJsonPath != null ? new ResultsFile() : null;
            _platformSuffix = platformSuffix;
        }

        private readonly string _platformSuffix;

        public string Uid => "AnyUnit.TestingPlatform";
        public string Version => "1.0.0";
        public string DisplayName => "AnyUnit";
        public string Description => "AnyUnit test framework adapter for Microsoft.Testing.Platform";

        public Task<bool> IsEnabledAsync() => Task.FromResult(true);

        public Type[] DataTypesProduced => new[] { typeof(TestNodeUpdateMessage) };

        public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
        {
            return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
        }

        // Written here rather than at the end of ExecuteRequestAsync
        // because one session can carry more than one request; close is
        // the only point at which "everything that was going to run has
        // run" is actually true.
        public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
        {
            if (_resultsFile != null && _ranTests)
            {
                var directory = Path.GetDirectoryName(_anyUnitJsonPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                // File.WriteAllText's no-encoding overload is UTF-8
                // without a BOM, matching Runner/Platforms/shared/
                // WriteResults.cs byte for byte. (Readers cope with a BOM
                // either way - ResultsFileReader and run-tests.sh both
                // read as utf-8-sig - but "identical to what the console
                // runners emit" is the actual requirement here, and a
                // stray BOM would break that.)
                File.WriteAllText(_anyUnitJsonPath, _resultsFile.ToListJson());
            }

            return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
        }

        public async Task ExecuteRequestAsync(ExecuteRequestContext context)
        {
            // Fully qualified: AnyUnit.Run.Runner, the discovery/execution
            // engine - not just "Runner", to keep it unambiguous alongside
            // this namespace's own TestingPlatformBuilderHook/AnyUnitTestFramework.
            // PlatformId.Current (see AnyUnit.Util), not a hardcoded "mtp" -
            // that alone couldn't distinguish an MTP host built for net10
            // vs one built for net48, or one OS/arch from another; "-mtp"
            // stays appended so it's still distinguishable from the same
            // assembly run through the plain console runner instead.
            // Any --platform-suffix goes after "-mtp", the same place the
            // console runners' -p puts theirs after their own id.
            var platform = AnyUnit.Util.PlatformId.Current + "-mtp";
            if (!string.IsNullOrEmpty(_platformSuffix))
                platform += "-" + _platformSuffix;
            var runner = AnyUnit.Run.Runner.Create(platform, _testAssemblies);

            if (context.Request is DiscoverTestExecutionRequest discover)
            {
                foreach (var test in runner.Tests)
                {
                    await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                        discover.Session.SessionUid,
                        ToTestNode(test, new DiscoveredTestNodeStateProperty(), null)));
                }
            }
            else if (context.Request is RunTestExecutionRequest run)
            {
                _ranTests = true;
                runner.RunAll(result =>
                {
                    // ResultsFile.Add walks Assembly -> Fixture -> Test by
                    // UniqueName and dedups by Platform, so accumulating
                    // the whole tree is genuinely this one line - the same
                    // line Runner/Platforms/shared and Runner/Bootstrap
                    // use. Nothing here reshapes or re-serializes
                    // anything: the file written in CloseTestSessionAsync
                    // comes out of ResultsFile.ToListJson(), the one
                    // serializer, so there is no second implementation of
                    // the format to drift out of sync with the console
                    // runners'.
                    if (_resultsFile != null)
                        _resultsFile.Add(result);

                    var property = ToStateProperty(result);
                    var node = ToTestNode(result.Test, property, result);
                    context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                        run.Session.SessionUid, node)).GetAwaiter().GetResult();
                });
            }

            context.Complete();
        }

        // `result` is null during discovery (no test has actually run yet,
        // so there's nothing to report an exception/output for) and
        // non-null once a test has actually executed - only then can the
        // TRX-specific properties below (which need a real outcome, not
        // just static test metadata) be attached.
        private TestNode ToTestNode(TestMeta test, IProperty stateProperty, Result result)
        {
            var properties = new List<IProperty> { stateProperty };

            if (_trxReportCapability != null && _trxReportCapability.IsEnabled)
            {
                properties.Add(new TrxFullyQualifiedTypeNameProperty(StripPrefix(test.Fixture.UniqueName)));

                var categories = test.Category.Concat(test.Fixture.Category).Distinct().ToArray();
                if (categories.Length > 0)
                    properties.Add(new TrxCategoriesProperty(categories));

                // Key/value metadata beyond categories - TestMetadataProperty
                // is MTP's own flat key->value pair, so a key carrying more
                // than one value becomes more than one property, the same
                // way TRX's own <Property> list does.
                foreach (var source in new[] { test.Fixture.Properties, test.Properties })
                {
                    if (source == null)
                        continue;
                    foreach (var pair in source)
                    {
                        foreach (var value in pair.Value ?? new List<string>())
                            properties.Add(new TestMetadataProperty(pair.Key, value ?? string.Empty));
                    }
                }

                if (result != null)
                {
                    // `?? result.Output` throughout: Message/StackTrace are
                    // null for a result that carries no exception, and
                    // before 1.2 there were no such fields at all - Output,
                    // the whole captured log, was the only thing available
                    // for either slot.
                    if (result.Kind == ResultKind.Fail || result.Kind == ResultKind.Error)
                    {
                        properties.Add(new TrxExceptionProperty(
                            result.Message ?? result.Output,
                            result.StackTrace ?? result.Output));

                        // The log itself is no longer lost on a failing
                        // test now that it isn't doubling as the exception
                        // message.
                        if (!string.IsNullOrEmpty(result.Output))
                            properties.Add(new TrxMessagesProperty(new TrxMessage[] { new StandardOutputTrxMessage(result.Output) }));
                    }
                    else if (!string.IsNullOrEmpty(result.Output))
                    {
                        properties.Add(new TrxMessagesProperty(new TrxMessage[] { new StandardOutputTrxMessage(result.Output) }));
                    }
                }
            }

            return new TestNode
            {
                Uid = new TestNodeUid(test.UniqueName),
                DisplayName = test.Name,
                Properties = new PropertyBag(properties.ToArray()),
            };
        }

        // FixtureMeta.UniqueName carries a "T:"-style discovery-kind prefix
        // (see FixtureMeta's constructor: "T:{namespace}.{name}") - a TRX
        // viewer's className is expected to look like a real .NET type
        // name, so strip it (same convention AnyUnit.Report's own writers
        // use for the same reason - see ResultsModel.StripPrefix there).
        private static string StripPrefix(string uniqueName)
        {
            if (string.IsNullOrEmpty(uniqueName))
                return uniqueName;
            var colon = uniqueName.IndexOf(':');
            return colon >= 0 && colon < 3 ? uniqueName.Substring(colon + 1) : uniqueName;
        }

        private static IProperty ToStateProperty(Result result)
        {
            switch (result.Kind)
            {
                case ResultKind.Success:
                case ResultKind.NoError:
                    return new PassedTestNodeStateProperty();
                case ResultKind.Ignore:
                    // The (string explanation) overload - a test explorer
                    // shows it next to the skipped test, where previously
                    // the reason only existed as text inside the log.
                    return result.SkipReason == null
                        ? new SkippedTestNodeStateProperty()
                        : new SkippedTestNodeStateProperty(result.SkipReason);
                case ResultKind.Fail:
                    return new FailedTestNodeStateProperty(result.Message ?? result.Output);
                case ResultKind.Error:
                    return new ErrorTestNodeStateProperty(result.Message ?? result.Output);
                default:
                    return new ErrorTestNodeStateProperty("Unknown result kind: " + result.Kind);
            }
        }
    }
}
