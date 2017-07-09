﻿//
//  Copyright 2013 AnyUnit Contributors
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

namespace sl_50_xap
{
    public partial class App : Application
    {

        public App()
        {
            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var alone = e.InitParams["testalone"].Contains("t");
            var id =e.InitParams["testplatform"];
            var url =e.InitParams["testurl"];
            var bytes = Convert.FromBase64String(e.InitParams["testdlls"]);
            var dlls =Encoding.UTF8.GetString(bytes,0,bytes.Length)
                                .Split(new[] {"|"},
                                        StringSplitOptions.RemoveEmptyEntries);

            this.RootVisual = new MainPage(alone,id,url, dlls);

        }



        private void Application_Exit(object sender, EventArgs e)
        {

        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled.
                // For production applications this error handling should be replaced with something that will
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        private static string ErrorMessage(string acc,Exception e)
        {
            acc += Environment.NewLine;
            acc += e.GetType().ToString()+ Environment.NewLine;
            acc += e.Message + Environment.NewLine;
            acc += e.StackTrace + Environment.NewLine;
            var agg = e as AggregateException;
            if (agg != null)
            {
                int i = 1;
                foreach (var e2 in agg.InnerExceptions)
                {
                    acc += (i++).ToString()+ Environment.NewLine;
                    acc = ErrorMessage(acc,e2);
                }
            }else
              if (e.InnerException != null)
              {
                  return ErrorMessage(acc, e.InnerException);
              }
            return acc;
        }

        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                System.Windows.Browser.HtmlPage.Window.Eval("window.document.title = 'ERROR';");

                string errorMsg = ErrorMessage(String.Empty,e.ExceptionObject);
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");
                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application: " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}
