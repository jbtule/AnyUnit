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
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using PclUnit.Runner;
using Runner.Shared;

namespace Runner.Shared
{
    public partial class RunTests
    {
        public RunTests(TextBox output)
        {
            _fakeConsole = new FakeConsole(output);

        }


        private FakeConsole _fakeConsole;

        public FakeConsole Console
        {
            get { return _fakeConsole; }
        }

        public class FakeConsole
        {
            private readonly TextBox _output;

            public FakeConsole(TextBox output)
            {
                _output = output;
            }

            public void WriteLine()
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => _output.Text += "\n");
            }

            public void WriteLine(string format, params object[] args)
            {

                Deployment.Current.Dispatcher.BeginInvoke(() => _output.Text += String.Format(format, args) + "\n");
            }

            public void Write(string format, params object[] args)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => _output.Text += String.Format(format, args));
            }

            internal void Write(object obj)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => _output.Text += String.Format("{0}", obj));
            }
        }

    }

}


namespace sl_50_xap
{

   

    public partial class MainPage : UserControl
    {
        private RunTests _runTest;
        private string _results = string.Empty;

        public MainPage(bool alone, string id, string url, string[] dlls)
        {
            InitializeComponent();

            _runTest = new RunTests(Output);

            var thread = new Thread(() => RunTests(alone, id, url, dlls));
            thread.Start();
        }

        private void RunTests(bool alone, string id, string url, string[] dlls)
        {

            Deployment.Current.Dispatcher.BeginInvoke(Start);
            if (alone)
                _results = _runTest.RunAlone(id, dlls).ToListJson();
            else
                _runTest.Run(id, url, dlls);
            Deployment.Current.Dispatcher.BeginInvoke(End);
            
        }

        public void Start()
        {
            System.Windows.Browser.HtmlPage.Window.Eval("window.document.title = 'START';");
        }

        public void End()
        {
            System.Windows.Browser.HtmlPage.Window.Eval(string.Format("document.getElementById('output_results').setAttribute('value','{0}');",
                EncodeString(_results)));

            System.Windows.Browser.HtmlPage.Window.Eval("window.document.title = 'DONE';");
        }

        private static string EncodeString(string data)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
        }

    }
}
