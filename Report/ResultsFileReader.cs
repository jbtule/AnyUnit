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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using AnyUnit.Run;

namespace AnyUnit.Report
{
    internal static class ResultsFileReader
    {
        // Base for the two ways a .json file can fail to be a results file
        // anyunit-report can convert, so ConvertCommand can catch one type
        // and report either as a clean CLI error instead of a raw
        // exception/stack trace.
        public abstract class InvalidResultsFileException : Exception
        {
            protected InvalidResultsFileException(string message) : base(message)
            {
            }
        }

        // Thrown for a .json file that doesn't carry AnyUnit's own "Tool"
        // marker - most likely the wrong file entirely (some other tool's
        // report, a results.json from years before this marker existed),
        // rather than a shape this reader could make any sense of at all.
        public class NotAnAnyUnitResultsFileException : InvalidResultsFileException
        {
            public NotAnAnyUnitResultsFileException(string found)
                : base(string.Format(
                    "This doesn't look like an AnyUnit results file (Tool='{0}', expected '{1}').",
                    found ?? "<missing>", ResultsFile.ToolName))
            {
            }
        }

        // Thrown for a results.json whose SchemaVersion this build doesn't
        // understand, so a version mismatch fails clearly (upgrade
        // anyunit-report, or re-generate results.json with a matching
        // anyunit-runner) instead of silently misreading a shape it wasn't
        // written for.
        public class UnsupportedSchemaVersionException : InvalidResultsFileException
        {
            public UnsupportedSchemaVersionException(int found)
                : base(string.Format(
                    "results.json has SchemaVersion {0}, but this version of anyunit-report only understands SchemaVersion {1}.",
                    found, ResultsFile.CurrentSchemaVersion))
            {
            }
        }

        // Same convention as WhoTestsTheTesters/ConventionTestProcessor's
        // own reader: Populate is required, not optional - see that
        // project's Program.cs for why (FixtureMeta.Assembly/TestMeta.
        // Fixture/Result.Test back-references only get set as a side
        // effect of adding each deserialized item into the parent's
        // existing CallBackList<T> instance).
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
        };

        public static ResultsFile Read(string path)
        {
            var json = File.ReadAllText(path);
            var results = JsonSerializer.Deserialize<ResultsFile>(json, Options);

            if (results.Tool != ResultsFile.ToolName)
                throw new NotAnAnyUnitResultsFileException(results.Tool);
            if (results.SchemaVersion != ResultsFile.CurrentSchemaVersion)
                throw new UnsupportedSchemaVersionException(results.SchemaVersion);

            return results;
        }
    }
}
