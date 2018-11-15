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

            ws.Post["/login"] = handleLoginPage;
            ws.Get["/logout"] = handleLogoutPage;

            ws.Get["/"] = handleMainPage;
            ws.Get["/api/session"] = handleGetSession;
            ws.Get["/api/values"] = handleGetValues;

            ws.Run();
            Console.WriteLine("A simple webserver. Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }

        private static RequestResult handleLoginPage(RequestContext context)
        {
            if(context.Session["user"] != null)
                return new RedirectRequestResult("/home.html");

            string _userName = context.FormData["userName"];
            string _password = context.FormData["password"];

            if(_userName == "admin" && _password=="admin")
            {
                User _user = new User(_userName, _password);
                context.Session["user"] = _user;

                return new RedirectRequestResult("/home.html");
            }

            return new RedirectRequestResult("/login.html");
        }

        private static RequestResult handleLogoutPage(RequestContext context)
        {
            context.Session["user"] = null;

            return new RedirectRequestResult("/login.html");
        }

        private static RequestResult handleMainPage(RequestContext context)
        {
            if (context.Session["user"] == null)
                return new RedirectRequestResult("/login.html");

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
