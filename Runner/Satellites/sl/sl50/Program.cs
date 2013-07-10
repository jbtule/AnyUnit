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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Ionic.Zip;
using Ionic.Zlib;

namespace sl_50_x86
{
    class Program
    {

        public static bool started = false;

        
        static void Main(string[] args)
        {
#if x64
            if (!Environment.Is64BitProcess)
                throw new Exception("This runner is expected to run 64bit");
#endif
            var hidden = args.First();
            var id = args.Skip(1).First();
            var url = args.Skip(2).First();
            var shared = args.Skip(3).First();
            var dlls = args.Skip(4);

            var ishidden = hidden.Contains("hidden");
            var fullsetOfDlls = GenerateFullListOfDlls(dlls);

            var tempPath = CompileNewXAP(fullsetOfDlls, shared);
            Console.WriteLine(tempPath);

            var htmlname = CreateNewHtml(shared, tempPath, url, id, dlls);

            var temphtml = string.Format("{0}/reshare/{1}", url, htmlname);

            Console.WriteLine(temphtml);

            LaunchBrowserRunner(ishidden, temphtml);

            //If silverlight doesn't start in 2 minutes
            //kill self
            Thread.Sleep(new TimeSpan(0,0,2,0));
            if (!started)
            {
                Application.Exit();
            }
            
        }

        private static void LaunchBrowserRunner(bool ishidden, string temphtml)
        {
            var thread = new Thread(() =>
                                        {
                                            var form = new Form
                                                           {
                                                               Height = 300,
                                                               Width = 400,
                                                               WindowState =
                                                                   ishidden ? FormWindowState.Minimized : FormWindowState.Normal,
                                                               ShowInTaskbar = !ishidden,
                                                               Text = "sl50 - Browser Host"
                                                           };

                                            var browser = new WebBrowser
                                                              {
                                                                  Url = new Uri(temphtml),
                                                                  Dock = DockStyle.Fill
                                                              };

                                            browser.DocumentTitleChanged += (sender, e) =>
                                                                                {
                                                                                    if (browser.DocumentTitle.Contains("START"))
                                                                                        started = true;
                                                                                    if (browser.DocumentTitle.Contains("DONE"))
                                                                                        Application.Exit();
                                                                                };

                                            form.Controls.Add(browser);

                                            Application.Run(form);
                                        });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private static string CreateNewHtml(string shared, string tempPath, string url, string id,
                                          IEnumerable<string> dlls)
        {

            var htmlname = string.Format("{0}.html", Guid.NewGuid());

            var stream2 = Assembly.GetExecutingAssembly()
                                  .GetManifestResourceStream("sl_runner.TestPage.html");

            var tempPath2 = Path.Combine(shared, htmlname);


            using (var streamReader = new StreamReader(stream2))
            {
                var html = streamReader.ReadToEnd();
                html = html.Replace("$$xappath$$", Path.GetFileName(tempPath));
                html = html.Replace("$$testurl$$", url);
                html = html.Replace("$$testplatform$$", id);
                html = html.Replace("$$testdlls$$", Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Join("|",
                                                                                                              dlls.Select(
                                                                                                                  Assembly
                                                                                                                      .ReflectionOnlyLoadFrom)
                                                                                                                  .Select(
                                                                                                                      d =>
                                                                                                                      d.FullName)))));

                File.WriteAllText(tempPath2, html);
            }
            return htmlname;
        }

        private static string CompileNewXAP(HashSet<string> fullsetOfDlls, string shared)
        {
            var tempPath = Path.Combine(shared, Guid.NewGuid() + ".xap");
            using (
                var stream = Assembly.GetExecutingAssembly()
                                     .GetManifestResourceStream("sl_runner.xap.sl-50-xap.xap"))
            using (var baseZip = ZipFile.Read(stream))
            {
                var set = new HashSet<string>();
                var item = baseZip["AppManifest.xaml"];
                var xml = XDocument.Parse(new StreamReader(item.OpenReader()).ReadToEnd());
                using (
                    var zip = new ZipFile()
                                  {
                                      CompressionLevel = CompressionLevel.Default,
                                      CompressionMethod = CompressionMethod.Deflate
                                  })
                {
                    zip.AddEntry("AppManifest.xaml", new byte[] {});
                    foreach (var entry in baseZip.Entries)
                    {
                        if (entry.FileName.Contains(".dll"))
                        {
                            using (var memstream = new MemoryStream())
                            {
                                entry.OpenReader().CopyTo(memstream);
                                memstream.Seek(0, SeekOrigin.Begin);
                                zip.AddEntry(entry.FileName, memstream.ToArray());
                                set.Add(Path.GetFileName(entry.FileName));
                            }
                        }
                    }
                    var desc = xml.DescendantNodes().OfType<XElement>();

                    var parts = desc.Single(it => it.Name.LocalName.Contains("Deployment.Parts"));

                    foreach (var dll in fullsetOfDlls)
                    {
                        if (set.Contains(Path.GetFileName(dll)))
                            continue;
                        set.Add(Path.GetFileName(dll));
                        zip.AddFile(dll, "");
                        parts.Add(new XElement(XName.Get("AssemblyPart", "http://schemas.microsoft.com/client/2007/deployment"),
                                               new XAttribute(
                                                   XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml"),
                                                   Path.GetFileNameWithoutExtension(dll)),
                                               new XAttribute("Source", Path.GetFileName(dll))));
                    }
                    using (var memstream = new MemoryStream())
                    {
                        xml.Save(memstream, SaveOptions.OmitDuplicateNamespaces);
                        zip.UpdateEntry("AppManifest.xaml", memstream.ToArray());
                    }
                    zip.Save(tempPath);
                }
            }
            return tempPath;
        }

        private static HashSet<string> GenerateFullListOfDlls(IEnumerable<string> dlls)
        {
            var fullsetOfDlls = new HashSet<string>();
            foreach (var dll in dlls)
            {
                fullsetOfDlls.Add(dll);
                var dir = Path.GetDirectoryName(dll);
                foreach (var moreDll in Directory.GetFiles(dir, "*.dll"))
                {
                    fullsetOfDlls.Add(Path.Combine(dir, moreDll));
                }
            }
            return fullsetOfDlls;
        }
    }
}
