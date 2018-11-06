using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace TinyWebServer
{
    public delegate RequestResult RequestHandler(RequestContext context);

    public class WebServer
    {
        private static Dictionary<string, ContentType> ContentTypes = new Dictionary<string, ContentType>()
        {
            {".html", new ContentType(".html", "text/html") },
            {".htm", new ContentType(".htm", "text/html") },
            {".xml", new ContentType(".css", "text/xml") },
            {".css", new ContentType(".css", "text/css") },
            {".js", new ContentType(".js", "application/javascript") },
            {".png", new ContentType(".png", "image/png") },
            {".jpg", new ContentType(".jpg", "image/jpeg") },
            {".gif", new ContentType(".gif", "image/gif") },
            {".ico", new ContentType(".gif", "image/x-icon") }
        };

        private const string DefaultFileName = "home.html";
        private const string SessionCookieName = "_TWS";

        private readonly HttpListener m_Listener = new HttpListener();
        private readonly string m_RootDirectory;
        private readonly bool m_CookieBasedSessionsEnabled;

        private readonly Dictionary<string, RequestHandler> m_GetRouteHandlers = new Dictionary<string, RequestHandler>();
        private readonly Dictionary<string, RequestHandler> m_PostRouteHandlers = new Dictionary<string, RequestHandler>();
        private readonly Dictionary<string, Session> m_Sessions = new Dictionary<string, Session>();

        public string RootDirectory { get { return m_RootDirectory; } }

        public Dictionary<string, RequestHandler> Get { get { return m_GetRouteHandlers; } }
        public Dictionary<string, RequestHandler> Post { get { return m_PostRouteHandlers; } }

        public bool IsListening { get { return m_Listener.IsListening; } }

        public WebServer(string rootDirectory, bool enableCookieBasedSession, params string[] prefixes)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required.");

            m_RootDirectory = rootDirectory;
            m_CookieBasedSessionsEnabled = enableCookieBasedSession;

            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            foreach (string s in prefixes)
                m_Listener.Prefixes.Add(s);
        }

        public void Run()
        {
            if (!m_Listener.IsListening)
            {
                m_Listener.Start();

                Thread _thr = new Thread(listenerThread);
                _thr.Name = "WebListenerThread";
                _thr.Start();
            }
        }

        private void listenerThread()
        {
            try
            {
                while (m_Listener.IsListening)
                {
                    var _listenerContext = m_Listener.GetContext();

                    ThreadPool.QueueUserWorkItem((c) =>
                    {
                        var ctx = c as HttpListenerContext;
                        try
                        {
                            processRequest(ctx);
                        }
                        catch { } // suppress any exceptions
                        finally
                        {
                            // always close the stream
                            ctx.Response.OutputStream.Close();
                        }
                    }, _listenerContext);
                }
            }
            catch { } // suppress any exceptions
        }

        public void Stop()
        {
            if (m_Listener.IsListening)
            {
                m_Listener.Stop();
                m_Listener.Close();
            }
        }

        private void processRequest(HttpListenerContext listenerContext)
        {
            Session _session = null;
            if (m_CookieBasedSessionsEnabled)
                _session = getRequestSession(listenerContext);

            RequestContext _requestContext = new RequestContext(this, listenerContext, _session);
            RequestResult _result = null;

            if (listenerContext.Request.HttpMethod == HttpMethod.Get.Method)
            {
                _result = processGetRequest(_requestContext);
            }
            else if (listenerContext.Request.HttpMethod == HttpMethod.Post.Method)
            {
                _result = processPostRequest(_requestContext);
            }
            else if (listenerContext.Request.HttpMethod == HttpMethod.Options.Method)
            {
                _result = processOptionsRequest(_requestContext);
            }
            else
            {
                _result = RequestResult.BadRequest;
            }

            if (_result != null)
                _result.WriteResult(_requestContext);
        }

        private Session getRequestSession(HttpListenerContext listenerContext)
        {
            var _sesionCookie = listenerContext.Request.Cookies[SessionCookieName];

            if (_sesionCookie == null)
            {
                _sesionCookie = new Cookie(SessionCookieName, Guid.NewGuid().ToString());
                listenerContext.Response.Cookies.Add(_sesionCookie);
            }

            if (m_Sessions.ContainsKey(_sesionCookie.Name))
                return m_Sessions[_sesionCookie.Name];

            Session _session = new Session(_sesionCookie.Name);
            m_Sessions.Add(_session.Id, _session);

            return _session;
        }

        private RequestResult processOptionsRequest(RequestContext requestContext)
        {
            requestContext.ListenerContext.Response.Headers.Add("Access-Control-Allow-Origin: *");
            requestContext.ListenerContext.Response.Headers.Add("Access-Control-Allow-Methods: POST, GET, OPTIONS");
            requestContext.ListenerContext.Response.Headers.Add("Access-Control-Allow-Headers: Content-Type");

            return RequestResult.None;
        }

        private RequestResult processGetRequest(RequestContext requestContext)
        {
            if (IsStaticFileRequest(requestContext.ListenerContext.Request))
            {
                return new StaticFileResult(GetStaticFilePath(requestContext.ListenerContext.Request));
            }

            string _url = requestContext.ListenerContext.Request.Url.AbsolutePath;

            if (m_GetRouteHandlers.ContainsKey(_url))
            {
                RequestResult _result = m_GetRouteHandlers[_url].Invoke(requestContext);
                if (_result != null)
                    return _result;
            }

            return RequestResult.None;
        }

        private RequestResult processPostRequest(RequestContext requestContext)
        {
            string _url = requestContext.ListenerContext.Request.Url.AbsolutePath;

            if (m_PostRouteHandlers.ContainsKey(_url))
            {
                RequestResult _result = m_PostRouteHandlers[_url].Invoke(requestContext);
                if (_result != null)
                    return _result;
            }

            return RequestResult.None;
        }

        private static bool IsStaticFileRequest(HttpListenerRequest request)
        {
            string _filePath = request.Url.AbsolutePath.Replace('/', '\\');

            if (_filePath.StartsWith("\\"))
                _filePath = _filePath.Remove(0, 1);

            if (_filePath.Length < 3)
                return false;

            if (!Path.HasExtension(_filePath))
                return false;

            return true;
        }

        internal string GetStaticFilePath(HttpListenerRequest request)
        {
            string _filePath = request.Url.AbsolutePath.Replace('/', '\\');

            if (_filePath.StartsWith("\\"))
                _filePath = _filePath.Remove(0, 1);

            return _filePath;
        }

        internal string GetStaticFileLocalPath(string relativePath)
        {
            string _filePath = relativePath.Replace('/', '\\');

            if (_filePath.StartsWith("\\"))
                _filePath = _filePath.Remove(0, 1);

            if (!Path.HasExtension(_filePath))
            {
                _filePath = Path.Combine(_filePath, DefaultFileName);
            }

            _filePath = Path.Combine(m_RootDirectory, _filePath);

            return _filePath;
        }

        internal static ContentType GetContentType(string filePath)
        {
            string _extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (ContentTypes.ContainsKey(_extension))
                return ContentTypes[_extension];

            return ContentTypes[".html"];
        }
    }
}