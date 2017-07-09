using System;
using System.Diagnostics.CodeAnalysis;

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


namespace AnyUnit.Style.Xunit.Exceptions
{
    /// <summary>
    /// The base assert exception class
    /// </summary>
    public class AssertException : AssertionException
    {

        protected string UserMessage { get; set; }

        static AssertException()
        {
            var assertType = typeof(AssertEx);
            var recordType = typeof(AssertEx);

            XunitStackExclude = new[] { 
                string.Format("at {0}.", assertType.FullName),
                string.Format("at {0}.", recordType.FullName)
            };
        }

        protected static string[] XunitStackExclude;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertException"/> class.
        /// </summary>
        public AssertException()
        {
            ExcludeFromStackTrace.AddRange(XunitStackExclude);

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertException"/> class.
        /// </summary>
        /// <param name="userMessage">The user message to be displayed</param>
        public AssertException(string userMessage)
            : base(userMessage)
        {
            ExcludeFromStackTrace.AddRange(XunitStackExclude);
            UserMessage = userMessage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssertException"/> class.
        /// </summary>
        /// <param name="userMessage">The user message to be displayed</param>
        /// <param name="innerException">The inner exception</param>
        [SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
        protected AssertException(string userMessage, Exception innerException)
            : base(userMessage, innerException)
        {
            ExcludeFromStackTrace.AddRange(XunitStackExclude);
            UserMessage = userMessage;

        }

      

 
      
    

       

       


    }
}