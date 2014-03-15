using System;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.Conventions;
using Nancy.ModelBinding;
using PclUnit.Run;
using System.Linq;
using System.Text;

namespace pclunit_runner
{

	public class PclRunnerModule : NancyModule
	{
		public PclRunnerModule()
		{
			Get["/"] = _ => "PclUnit Aggregating Runner";
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
		}
	}

	public class CustomBootstrapper : DefaultNancyBootstrapper
	{
		string resharePath;

		public CustomBootstrapper(string resharePath){
			this.resharePath = resharePath;

		}

		protected override void ConfigureConventions(NancyConventions conventions)
		{
			base.ConfigureConventions(conventions);

			conventions.StaticContentsConventions.Add(
				StaticContentConventionBuilder.AddDirectory("reshare", resharePath)
			);

		}
	}
}
