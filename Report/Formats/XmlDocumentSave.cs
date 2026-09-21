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

using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace AnyUnit.Report.Formats
{
    /// <summary>
    /// The save every XML writer here goes through, so a test's own output
    /// cannot abort the conversion.
    /// </summary>
    /// <remarks>
    /// A test's log, failure message or stack trace is whatever the test
    /// wrote, and a results.json can carry any of it: JSON escapes control
    /// characters (<c>\b</c>, <c>\f</c>, <c></c>) and reads them back
    /// as themselves. XML 1.0 has no representation for them at all - not
    /// even a character reference - so XmlWriter throws
    /// ArgumentException ("hexadecimal value 0x08, is an invalid
    /// character") partway through writing, and the tool died with an
    /// unhandled exception and a half-written file. All four XML formats
    /// (junit, trx, nunit, xunit) did; ctrf/html/markdown were unaffected.
    /// Found by BasicTests' JsonEscaping tests going through
    /// test-packed-report-tool.
    ///
    /// Stripped rather than substituted: these characters are noise in a
    /// log (a bell, a backspace), the surrounding text is what a reader
    /// wants, and any stand-in would be a value the test did not write.
    /// </remarks>
    internal static class XmlDocumentSave
    {
        public static void Save(XDocument document, Stream output)
        {
            foreach (var text in document.DescendantNodes().OfType<XText>())
                text.Value = XmlSafe(text.Value);

            foreach (var attribute in document.Descendants().Attributes())
                attribute.Value = XmlSafe(attribute.Value);

            document.Save(output);
        }

        /// <summary>
        /// <paramref name="value"/> with every character XML 1.0 cannot
        /// represent removed. The legal set is tab, newline, carriage
        /// return, and everything from #x20 up bar the surrogate and
        /// noncharacter ranges (see the XML spec's Char production);
        /// nothing else can be written, escaped or otherwise.
        /// </summary>
        public static string XmlSafe(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // The common case by far is that there is nothing to remove,
            // and then the original string is handed back untouched.
            var firstBad = -1;
            for (var i = 0; i < value.Length; i++)
            {
                if (!IsLegal(value[i]))
                {
                    firstBad = i;
                    break;
                }
            }

            if (firstBad < 0)
                return value;

            var builder = new StringBuilder(value.Length);
            builder.Append(value, 0, firstBad);
            for (var i = firstBad; i < value.Length; i++)
            {
                if (IsLegal(value[i]))
                    builder.Append(value[i]);
            }
            return builder.ToString();
        }

        private static bool IsLegal(char c)
        {
            // Surrogates are legal here: a surrogate PAIR is one legal
            // astral character, and XmlWriter itself rejects an unpaired
            // one - which this cannot tell apart character by character,
            // and which no results file this tool reads can contain anyway
            // (a JSON reader will not produce one).
            return c == '\t'
                   || c == '\n'
                   || c == '\r'
                   || (c >= ' ' && c <= '�');
        }
    }
}
