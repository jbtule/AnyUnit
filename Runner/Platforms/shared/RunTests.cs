//
//  Copyright 2013 PclUnit Contributors
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
using System.Threading;
using PclUnit.Run;
using System.Text;
using System.Net;
using System.IO;

namespace SatelliteRunner.Shared
{
    public partial class RunTests
    {

        public ResultsFile RunAlone(string id, IEnumerable<string> dlls)
        {

#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

#if SILVERLIGHT
            var am = dlls.Select(Assembly.Load).ToList();
#else
            var am = dlls.Select(Assembly.LoadFile).ToList();
#endif

            var runner = Runner.Create(id, am);
            PrintOutAloneStart(id);
            var file = new ResultsFile();
            runner.RunAll(r =>
                              {
                                  PrintOutAloneResults(r);
                                  file.Add(r);
                              });
            PrintOutAloneEnd(id,file);
            return file;
        }

		public static bool PostHttp(string baseUri, string api, string json){
	
			var url = baseUri + api;
			var byteArray = Encoding.UTF8.GetBytes(json);

			var request = WebRequest.Create(url);
			request.Method = "POST";
			request.ContentLength = byteArray.Length;
			request.ContentType = @"application/json";

			using (Stream dataStream = request.GetRequestStream())
			{
				dataStream.Write(byteArray, 0, byteArray.Length);
			}

			using (var response = (HttpWebResponse)request.GetResponse())
			{
				return response.StatusCode == HttpStatusCode.OK;
			}
		}

		public static string GetHttp(string baseUri, string api){

			var url = baseUri + api;

			var request = WebRequest.Create(url);
			Console.WriteLine (url);
			request.Method = "GET";
			request.ContentType = "text/plain";
			using (var response = (HttpWebResponse)request.GetResponse())
			using (var reader = new StreamReader(response.GetResponseStream()))
			{
				var text=reader.ReadToEnd();
				if (response.StatusCode != HttpStatusCode.OK){
					Console.Write (text);
					throw new Exception ("500 Error");
				}
				return text;
			}
		}

		public static bool IsReadySetupFilter(string check, TestFilter filter){
			var reader = new StringReader(check);
			while (reader.Peek () != -1) {
				var line = reader.ReadLine ();
				if(line.StartsWith("++READY++")){
					return true;
				}else if(line.StartsWith("++INCLUDE++")){
					filter.Includes.Add (line.Replace ("++INCLUDE++", String.Empty));
				}else if(line.StartsWith("++EXCLUDE++")){
					filter.Excludes.Add (line.Replace ("++EXCLUDE++", String.Empty));
				}
			}
			return false;
		}

        public void Run(string id, string url, string[] dlls)
        {

#if x64
            if(!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif

            Console.WriteLine("id:" + id);
            Console.WriteLine("server:" + url);
            Console.WriteLine("tests dlls:");
            foreach (var dll in dlls)
            {
                Console.WriteLine(dll);
            }
            Console.WriteLine("==========================");

            Console.Write("Connecting..");
			var connect = GetHttp (url, @"/api/connect/" + id);
			Console.WriteLine(connect);
            Console.WriteLine("Loading dlls..");

#if SILVERLIGHT
            var am = dlls.Select(Assembly.Load).ToList();
#else
            var am = dlls.Select(Assembly.LoadFile).ToList();
#endif

            Console.WriteLine("Generating Tests...");


            var runner = Runner.Create(id, am);


			TestFilter receivedFilter = new TestFilter();

			Console.WriteLine("Sending Tests...");
		
			PostHttp (url, @"/api/send_tests", runner.ToListJson ());

			while (true) {
				var check = GetHttp (url, @"/api/check_tests");
				var run = IsReadySetupFilter(check, receivedFilter);
				if (!run)
					Thread.Sleep (1000);
				else
					break;
			} 

            Console.WriteLine("Running Tests...");

            runner.RunAll(result => {
				PostHttp(url, @"/api/send_result", result.ToItemJson());
                          }, receivedFilter);

            Console.WriteLine("Quiting...");

        }


    }

}
