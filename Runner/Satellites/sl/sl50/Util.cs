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
using PclUnit.Runner;

namespace sl_runner
{
    public static class Util
    {
        public static bool started = false;





        public static Thread LaunchBrowserRunner(bool ishidden, string temphtml, IDictionary<string,string> outputs=null, string cleanupdir =null)
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
                    if (browser.DocumentTitle.Contains(
                            "START"))
                    {
                        started = true;
                    }
                    if (browser.DocumentTitle.Contains(
                            "DONE"))
                    {
                        if (outputs != null && outputs.Any())
                        {
                            var encresults = browser.Document.GetElementById("output_results").GetAttribute("value");
                            var resultsjson =Encoding.UTF8.GetString(Convert.FromBase64String(encresults));
                            var file =Newtonsoft.Json.JsonConvert.DeserializeObject<ResultsFile>(resultsjson);
                            WriteResults.ToFiles(file, outputs);
                            Environment.ExitCode = file.HasError ? 1 : 0;
                        }

                        Application.Exit();
                    }

                };

                form.Controls.Add(browser);

                Application.Run(form);

                if (!String.IsNullOrWhiteSpace(cleanupdir))
                {
                    try
                    {
                        //Try Delete Temp shared path
                        Directory.Delete(cleanupdir);
                    }
                    catch { }
                }

                
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return thread;
        }

        public static string EncodeString(string data)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

        public static string CreateNewHtml(string shared, string tempPath, string url, string id,
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
                html = html.Replace("$$testalone$$", String.IsNullOrWhiteSpace(url) ? "true" : "false");
                html = html.Replace("$$testurl$$", url ?? "http://127.0.0.1");
                html = html.Replace("$$testplatform$$", id);
                html = html.Replace("$$testdlls$$", EncodeString(String.Join("|",
                                         dlls.Select(Assembly.ReflectionOnlyLoadFrom).Select(d =>d.FullName))));

                File.WriteAllText(tempPath2, html);
            }
            return htmlname;
        }

        public static string CompileNewXAP(HashSet<string> fullsetOfDlls, string shared)
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
                    zip.AddEntry("AppManifest.xaml", new byte[] { });
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

        public static HashSet<string> GenerateFullListOfDlls(IEnumerable<string> dlls)
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
