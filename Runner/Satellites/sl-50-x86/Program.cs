using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using Ionic.Zip;
using Ionic.Zlib;

namespace sl_50_x86
{
    class Program
    {
        static void Main(string[] args)
        {

            var id = args.First();
            var url = args.Skip(1).First();
            var shared = args.Skip(2).First();
            var dlls = args.Skip(3);

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

            var tempPath = Path.Combine(shared, Guid.NewGuid() + ".xap");
            using (
                var stream = Assembly.GetExecutingAssembly()
                                     .GetManifestResourceStream("sl_50_x86.xap.sl-50-x86-xap.xap"))
                using (var baseZip = ZipFile.Read(stream))
                {
                    var set = new HashSet<string>();
                    var item = baseZip["AppManifest.xaml"];
                    var xml = XDocument.Parse(new StreamReader(item.OpenReader()).ReadToEnd());
                    using (var zip = new ZipFile(){ CompressionLevel = CompressionLevel.Default, CompressionMethod = CompressionMethod.Deflate})
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
                            zip.AddFile(dll,"");
                            parts.Add(new XElement(XName.Get("AssemblyPart", "http://schemas.microsoft.com/client/2007/deployment"), 
                                new XAttribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml"), Path.GetFileNameWithoutExtension(dll)),
                                new XAttribute("Source", Path.GetFileName(dll))));

                        }
                        using (var memstream = new MemoryStream())
                        {
                            xml.Save(memstream,SaveOptions.OmitDuplicateNamespaces);
                            zip.UpdateEntry("AppManifest.xaml", memstream.ToArray());
                        }
                        zip.Save(tempPath);
                    }
            }

            Console.WriteLine(tempPath);

            var stream2 = Assembly.GetExecutingAssembly()
                                  .GetManifestResourceStream("sl_50_x86.TestPage.html");
            var htmlname = Guid.NewGuid() + ".html";
            var tempPath2 = Path.Combine(shared, htmlname);


            using (var streamReader = new StreamReader(stream2))
            {
                var html =streamReader.ReadToEnd();
                html = html.Replace("$$xappath$$", Path.GetFileName(tempPath));
                html = html.Replace("$$testurl$$", url);
                html = html.Replace("$$testplatform$$", id);
                html = html.Replace("$$testdlls$$", Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Join("|", 
                    dlls.Select(Assembly.ReflectionOnlyLoadFrom).Select(d=>d.FullName)))));

                File.WriteAllText(tempPath2,html);
            }
            var temphtml = string.Format("{0}/reshare/{1}", url, htmlname);
            Console.WriteLine(temphtml);

            var thread = new Thread(() =>
            {
                var form = new Form
                {
                    Height = 240,
                    Width = 320,
                    WindowState = FormWindowState.Normal,
                    ShowInTaskbar = false,
                    Text = "sl50 - Browser Host"
                };

                var browser = new WebBrowser
                {
                    Url = new Uri(temphtml),
                    Dock = DockStyle.Fill
                };

                browser.DocumentTitleChanged += (sender, e) =>
                                                    {
                                                        if(browser.DocumentTitle.Contains("DONE"))
                                                            Application.Exit();
                                                    };

                form.Controls.Add(browser);

                Application.Run(form);

            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

       
    }
}
