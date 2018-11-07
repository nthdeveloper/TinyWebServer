﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using TinyWebServer;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string _appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            WebServer ws = new WebServer(_appDirectory, true, "http://localhost:8080/");

            ws.Get["/"] = handleMainPage;
            ws.Get["/api/session"] = handleGetSession;
            ws.Get["/api/values"] = handleGetValues;

            ws.Run();
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }

        private static RequestResult handleMainPage(RequestContext context)
        {
            return new RedirectRequestResult("/home.html");
        }

        private static RequestResult handleGetSession(RequestContext context)
        {
            return new TextResult($"{{\"sessionId\":\"{context.Session.Id}\"}}", "application/json");
        }

        private static RequestResult handleGetValues(RequestContext context)
        {
            return new TextResult("[1,2,3,4]", "application/json");
        }       
    }
}
