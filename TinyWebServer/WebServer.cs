using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web;

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
        private readonly TimeSpan m_SessionTimeout;
        private System.Threading.Timer m_SessionCleanTimer;

        private readonly Dictionary<string, RequestHandler> m_GetRouteHandlers = new Dictionary<string, RequestHandler>();
        private readonly Dictionary<string, RequestHandler> m_PostRouteHandlers = new Dictionary<string, RequestHandler>();
        private readonly SessionCollection m_Sessions = new SessionCollection();

        public string RootDirectory { get { return m_RootDirectory; } }

        public Dictionary<string, RequestHandler> Get { get { return m_GetRouteHandlers; } }
        public Dictionary<string, RequestHandler> Post { get { return m_PostRouteHandlers; } }

        public bool IsListening { get { return m_Listener.IsListening; } }

        public WebServer(string rootDirectory, string prefix, bool enableCookieBasedSession=false, int sessionTimeoutSeconds=60000)
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException("Windows XP SP2 or Server 2003 is required.");

            // URI prefixes are required,
            // for example "http://contoso.com:8080/index/".
            if (prefix == null || String.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("prefixes");

            if(sessionTimeoutSeconds < 1)
                throw new ArgumentException("sessionTimeoutSeconds must be greater than zero");

            m_RootDirectory = rootDirectory;
            m_CookieBasedSessionsEnabled = enableCookieBasedSession;
            m_SessionTimeout = TimeSpan.FromSeconds(sessionTimeoutSeconds);            
            m_Listener.Prefixes.Add(prefix);
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

            //Start periodic session cleaner
            if(m_CookieBasedSessionsEnabled)
            {
                if (m_SessionCleanTimer == null)
                    m_SessionCleanTimer = new Timer(cleanExpiredSessions);

                m_SessionCleanTimer.Change(m_SessionTimeout, m_SessionTimeout);
            }
        }

        public void Stop()
        {
            if (m_Listener.IsListening)
            {
                m_Listener.Stop();
                m_Listener.Close();
            }

            if (m_SessionCleanTimer != null)
                m_SessionCleanTimer.Change(Timeout.Infinite, Timeout.Infinite);

            m_Sessions.Clear();
        }

        private void cleanExpiredSessions(object arg)
        {
            Session[] _allSessions = m_Sessions.ToArray();

            DateTime _referenceTime = DateTime.Now;
            foreach(Session session in _allSessions)
            {
                if (session.Expires < _referenceTime)
                    m_Sessions.Remove(session);

                if (!m_Listener.IsListening)
                    return;
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

        private void processRequest(HttpListenerContext listenerContext)
        {
            Session _session = null;
            if (m_CookieBasedSessionsEnabled)
                _session = getRequestSession(listenerContext);

            //Set session expiration time
            _session.Expires = DateTime.Now.Add(m_SessionTimeout);

            RequestContext _requestContext = new RequestContext(this, listenerContext, _session);
            readFormData(_requestContext);

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
            var _sessionCookie = listenerContext.Request.Cookies[SessionCookieName];
            Session _session = null;

            if (_sessionCookie != null)
                _session = m_Sessions[_sessionCookie.Value];

            if (_session != null)
                return _session;

            _sessionCookie = new Cookie(SessionCookieName, Guid.NewGuid().ToString());
            listenerContext.Response.Cookies.Add(_sessionCookie);

            _session = new Session(_sessionCookie.Value);
            m_Sessions.Add(_session);

            return _session;
        }

        private void readFormData(RequestContext requestContext)
        {
            var _request = requestContext.ListenerContext.Request;

            if (_request.HasEntityBody && _request.ContentType == "application/x-www-form-urlencoded")
            {
                Encoding _encoding = _request.ContentEncoding;
                using (var reader = new StreamReader(_request.InputStream, _encoding))
                {
                    string formData = reader.ReadToEnd();
                    string[] lines = formData.Split('&');
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] nameValue = line.Split('=');
                            requestContext.FormData.Add(HttpUtility.UrlDecode(nameValue[0]), HttpUtility.UrlDecode(nameValue[1]));
                        }
                    }
                }
            }
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