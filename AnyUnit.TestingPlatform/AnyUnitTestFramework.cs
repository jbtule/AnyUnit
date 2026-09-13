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

        // Defaults to the entry assembly, matching a test project that
        // compiles its own tests directly into the MTP executable. A host
        // project that instead references its test assemblies as
        // libraries (see AnyUnitTestFrameworkExtensions.AddAnyUnitTestFramework's
        // own testAssemblies parameter) needs to say so explicitly - the
        // entry assembly would otherwise be the host itself, which has no
        // tests of its own, and Runner.Create would (silently) find zero.
        public AnyUnitTestFramework(IEnumerable<Assembly> testAssemblies, AnyUnitTrxReportCapability trxReportCapability)
        {
            var assemblies = (testAssemblies ?? Enumerable.Empty<Assembly>()).ToArray();
            _testAssemblies = assemblies.Length > 0
                ? assemblies
                : new[] { Assembly.GetEntryAssembly() ?? typeof(AnyUnitTestFramework).Assembly };
            _trxReportCapability = trxReportCapability;
        }

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

        public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
        {
            return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
        }

        public async Task ExecuteRequestAsync(ExecuteRequestContext context)
        {
            // Fully qualified: AnyUnit.Run.Runner, the discovery/execution
            // engine - not just "Runner", to keep it unambiguous alongside
            // this namespace's own TestingPlatformBuilderHook/AnyUnitTestFramework.
            var runner = AnyUnit.Run.Runner.Create("mtp", _testAssemblies);

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
                runner.RunAll(result =>
                {
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

                if (result != null)
                {
                    // AnyUnit.Run.Result has no separate exception-message/
                    // stack-trace field - Output is the test's whole
                    // captured log (see AnyUnit.Report's own writers for
                    // the same caveat), so it's the only thing available
                    // for either.
                    if (result.Kind == ResultKind.Fail || result.Kind == ResultKind.Error)
                    {
                        properties.Add(new TrxExceptionProperty(result.Output, result.Output));
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
                    return new SkippedTestNodeStateProperty();
                case ResultKind.Fail:
                    return new FailedTestNodeStateProperty(result.Output);
                case ResultKind.Error:
                    return new ErrorTestNodeStateProperty(result.Output);
                default:
                    return new ErrorTestNodeStateProperty("Unknown result kind: " + result.Kind);
            }
        }
    }
}
