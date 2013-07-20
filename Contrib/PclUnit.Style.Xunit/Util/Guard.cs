/*
 * Copyright 2013 Outercurve Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *    http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace PclUnit.Style.Xunit.Util
{
    /// <summary>
    /// Guard class, used for guard clauses and argument validation
    /// </summary>
    internal static class Guard
    {
        /// <summary/>
        public static void ArgumentNotNull(string argName, object argValue)
        {
            if (argValue == null)
                throw new ArgumentNullException(argName);
        }

        /// <summary/>
        [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", MessageId = "obj", Justification = "No can do.")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method may not be called by all users of Guard.")]
        public static void ArgumentNotNullOrEmpty(string argName, IEnumerable argValue)
        {
            ArgumentNotNull(argName, argValue);

            foreach (object obj in argValue)
                return;

            throw new ArgumentException("Argument was empty", argName);
        }

        /// <summary/>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "This method may not be called by all users of Guard.")]
        public static void ArgumentValid(string argName, string message, bool test)
        {
            if (!test)
                throw new ArgumentException(message, argName);
        }
    }
}