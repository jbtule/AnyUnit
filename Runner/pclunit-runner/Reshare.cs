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
using System.IO;
using System.Net;
using System.Text;

namespace pclunit_runner
{
    internal static class Reshare
    {
        public static ReshareDisposable Start(string url,string dir)
        {
            var server = new ReshareDisposable(url,dir);
            server.Start();
            return server;
        }
    }

    internal class ReshareDisposable : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _url;
        private readonly string _dir;

        public ReshareDisposable(string url, string dir)
        {
            _url = url;
            _dir = dir;
            _listener = new HttpListener();
            var prefix = _url + "/reshare/";
            _listener.Prefixes.Add(prefix);
            Console.WriteLine("Listening for assets:" + prefix);

        }

        public void Start()
        {
            _listener.Start();
            Serve();
        }


        private void Serve()
        {
            _listener.BeginGetContext(ar =>
            {
                _listener.Start();

                HttpListenerContext context;

                try
                {
                    context = _listener.EndGetContext(ar);
                }
                catch (Exception)
                {
                    return;
                }

                Serve();

                try
                {
                    var url = context.Request.RawUrl;
                    var path = url.Replace("/reshare/", "");
                    var fullpath = Path.Combine(_dir, path);
                    if (File.Exists(fullpath))
                    {

                        context.Response.ContentType = "application/octet-stream";
                        var bytes = File.ReadAllBytes(fullpath);
                        context.Response.Close(bytes, true);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.StatusDescription = "Not Found";
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = "Server Error";
                    context.Response.Close(Encoding.UTF8.GetBytes(ex.ToString()), true);
                }

            },
                 null);
        }

        public void Stop()
        {
            _listener.Stop();
        }

        public void Dispose()
        {
            Stop();
        }
    }
}