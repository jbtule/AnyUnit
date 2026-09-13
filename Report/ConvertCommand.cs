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
using ManyConsole.CommandLineUtils;
using AnyUnit.Report.Formats;
using AnyUnit.Run;

namespace AnyUnit.Report
{
    public class ConvertCommand : ConsoleCommand
    {
        private string _format;
        private string _output;

        public ConvertCommand()
        {
            IsCommand("convert", "converts one or more AnyUnit JSON results files to another test-report format");
            this.HasOption("f|format=", "Output format: junit, trx, nunit, xunit, or ctrf.", v => _format = v);
            this.HasOption("o|output=", "Output file path.", v => _output = v);
            // null, not a fixed count: ManyConsole's own signature caps
            // additional arguments at whatever number is given here, it
            // doesn't mean "at least" - matches RunAloneCommand's own
            // HasAdditionalArguments(null, ...) for the same "variable-length
            // trailing list" shape (see Runner/Platforms/shared/Commands.cs).
            HasAdditionalArguments(null, " <results.json> [<results2.json> ...]");
        }

        public override int Run(string[] remainingArguments)
        {
            if (string.IsNullOrEmpty(_format))
                throw new ConsoleHelpAsException("Missing required option -format.");
            if (string.IsNullOrEmpty(_output))
                throw new ConsoleHelpAsException("Missing required option -output.");
            if (remainingArguments.Length < 1)
                throw new ConsoleHelpAsException("Expected at least one <results.json> argument.");

            var writer = CreateWriter(_format);
            ResultsFile results;
            try
            {
                results = ReadAndMerge(remainingArguments);
            }
            catch (ResultsFileReader.InvalidResultsFileException ex)
            {
                throw new ConsoleHelpAsException(ex.Message);
            }

            using (var stream = File.Create(_output))
            {
                writer.Write(results, stream);
            }

            return 0;
        }

        // A single results.json already carries multiple Results per test
        // when the same test ran under more than one platform (see
        // ResultsFile.Add's own dedup-by-platform), and every writer
        // already fans those out sensibly (see Formats/ResultsModel.cs).
        // Merging several separate results.json files - one per platform,
        // e.g. what run-tests.sh produces per runner - into one ResultsFile
        // first, via the exact same Add(), reuses that without any writer
        // needing to know inputs ever came from more than one file.
        private static ResultsFile ReadAndMerge(string[] paths)
        {
            var merged = new ResultsFile();
            foreach (var path in paths)
            {
                var file = ResultsFileReader.Read(path);
                foreach (var result in file.Results)
                    merged.Add(result);
            }
            return merged;
        }

        private static IResultsFormatWriter CreateWriter(string format)
        {
            switch (format.ToLowerInvariant())
            {
                case "junit":
                    return new JUnitXmlWriter();
                case "trx":
                    return new TrxXmlWriter();
                case "nunit":
                    return new NUnitXmlWriter();
                case "xunit":
                    return new XUnitXmlWriter();
                case "ctrf":
                    return new CtrfJsonWriter();
                default:
                    throw new ConsoleHelpAsException(string.Format(
                        "Unknown -format '{0}' - expected junit, trx, nunit, xunit, or ctrf.", format));
            }
        }
    }
}
