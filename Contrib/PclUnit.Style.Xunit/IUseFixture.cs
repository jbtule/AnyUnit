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

namespace PclUnit.Style.Xunit
{
        /// <summary>
        /// Used to decorate xUnit.net test classes that utilize fixture classes.
        /// An instance of the fixture data is initialized just before the first
        /// test in the class is run, and if it implements IDisposable, is disposed
        /// after the last test in the class is run.
        /// </summary>
        /// <typeparam name="T">The type of the fixture</typeparam>
        public interface IUseFixture<T> where T : new()
        {
            /// <summary>
            /// Called on the test class just before each test method is run,
            /// passing the fixture data so that it can be used for the test.
            /// All test runs share the same instance of fixture data.
            /// </summary>
            /// <param name="data">The fixture data</param>
            void SetFixture(T data);
        }
}
