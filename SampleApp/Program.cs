using System;
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

            WebServer ws = new WebServer(_appDirectory, "http://localhost:8080/", true, 1000);

            ws.Get["/login"] = handleGetLoginPage;
            ws.Post["/login"] = handlePostLoginAction;
            ws.Get["/logout"] = handleLogoutPage;

            ws.Get["/"] = (c) => new RedirectResult("/home");
            ws.Get["/home"] = handleHomePage;

            ws.Get["/api/session"] = handleGetSession;
            ws.Get["/api/values"] = handleGetValues;

            ws.PreprocessFileRequest += ws_PreprocessFileRequest;

            ws.Run();
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }

        private static void ws_PreprocessFileRequest(RequestContext context, FileRequest request)
        {
            //Do not allow retrieving HTML files directly
            string _fileExtension = Path.GetExtension(request.FilePath).ToLowerInvariant();
            if(_fileExtension == ".html")
            {
                request.RequestResult = new RedirectResult("/");
            }
        }

        private static RequestResult handleGetLoginPage(RequestContext context)
        {
            if (context.Session["user"] == null)
                return new StaticFileResult("/login.html");

            return new RedirectResult("/home");
        }

        private static RequestResult handlePostLoginAction(RequestContext context)
        {
            if (context.Session["user"] != null)
                return new RedirectResult("/home");

            string _userName = context.FormData["userName"];
            string _password = context.FormData["password"];

            if (_userName == "admin" && _password == "admin")
            {
                User _user = new User(_userName, _password);
                context.Session["user"] = _user;

                return new RedirectResult("/home");
            }

            return new RedirectResult("/login");
        }

        private static RequestResult handleLogoutPage(RequestContext context)
        {
            context.Session["user"] = null;

            return new RedirectResult("/login");
        }

        private static RequestResult handleHomePage(RequestContext context)
        {
            if (context.Session["user"] == null)
                return new RedirectResult("/login");

            return new StaticFileResult("/home.html");
        }

        private static RequestResult handleGetSession(RequestContext context)
        {
            return new TextResult($"{{\"sessionId\":\"{context.Session.Id}\"}}", "application/json");
        }

        private static RequestResult handleGetValues(RequestContext context)
        {
            if (context.Session["user"] == null)
                return new RedirectResult("/login");

            return new TextResult("[1,2,3,4]", "application/json");
        }
    }
}
