using System;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.Conventions;
using Nancy.ModelBinding;
using AnyUnit.Run;
using System.Linq;
using System.Text;
using System.IO;
namespace anyunit_runner
{

	public class PclRunnerModule : NancyModule
	{
		public PclRunnerModule()
		{
			Get["/"] = _ => "AnyUnit Aggregating Runner";
			Get["/api/connect/{id}"] = _ => {
				string id = _.id;
				Console.WriteLine("Connecting:{0,15}",id);
				return "OK";
			};
			Post["/api/send_tests"] = _ => {
				var runner = this.Bind<Runner>();

				foreach (var test in runner.Assemblies.SelectMany(it=>it.Fixtures).SelectMany(it=>it.Tests))
				{
					PlatformResults.Instance.AddTest(test, runner.Platform);
				}
				Console.WriteLine("Receiving Test List:{0,15}", runner.Platform);
				PlatformResults.Instance.ReceivedTests(runner.Platform);
				return "OK";
			};
			Post["/api/send_result"] = _ => {
				var result = this.Bind<Result>();
				var dict = PlatformResults.Instance.AddResult(result);
				PrintResults.PrintResult(dict);
				return "OK";
			};
			Get["/api/check_tests"] = _ => {
				var results = PlatformResults.Instance;
				if(results.Go){
					var build = new StringBuilder();
					foreach(var inc in results.Includes){
						build.AppendFormat("++INCLUDE++{0}", inc);
					}
					foreach(var inc in results.Includes){
						build.AppendFormat("++EXCLUDE++{0}", inc);
					}
					build.AppendLine("++READY++");
					return build.ToString();
				}else{
					return "++NOTREADY++";
				}
			};
			Get ["/reshare/{file*}"] = _ => {
				string path = _.file;
				var fullPath = Path.GetFullPath(PlatformResults.Instance.ResharePath);
				var fullFilePath = Path.GetFullPath(Path.Combine(fullPath, path));

				if(!fullFilePath.StartsWith(fullPath)){
					return String.Format("Can't find file {0}" ,path);
				}
				var response = new Response();
				response.ContentType = "application/octet-stream";
				response.Contents = s => {
					File.OpenRead(fullFilePath).CopyTo(s);
				};
				return response;
			};
		}
	}
}
