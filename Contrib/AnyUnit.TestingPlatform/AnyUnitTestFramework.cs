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
            var assembly = Assembly.GetEntryAssembly() ?? typeof(AnyUnitTestFramework).Assembly;
            var runner = Runner.Create("mtp", new[] { assembly });

            if (context.Request is DiscoverTestExecutionRequest discover)
            {
                foreach (var test in runner.Tests)
                {
                    await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                        discover.Session.SessionUid,
                        ToTestNode(test, new DiscoveredTestNodeStateProperty())));
                }
            }
            else if (context.Request is RunTestExecutionRequest run)
            {
                runner.RunAll(result =>
                {
                    var property = ToStateProperty(result);
                    var node = ToTestNode(result.Test, property);
                    context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
                        run.Session.SessionUid, node)).GetAwaiter().GetResult();
                });
            }

            context.Complete();
        }

        private static TestNode ToTestNode(TestMeta test, IProperty stateProperty)
        {
            return new TestNode
            {
                Uid = new TestNodeUid(test.UniqueName),
                DisplayName = test.Name,
                Properties = new PropertyBag(stateProperty),
            };
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
