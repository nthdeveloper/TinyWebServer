using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    public class RequestContext
    {
        private readonly WebServer m_Server;
        private readonly HttpListenerContext m_ListenerContext;
        private readonly Session m_Session;

        public WebServer Server => m_Server;
        public HttpListenerContext ListenerContext => m_ListenerContext;
        public Session Session => m_Session;

        public RequestContext(WebServer server, HttpListenerContext listenerContext, Session session)
        {
            m_Server = server;
            m_ListenerContext = listenerContext;
            m_Session = session;
        }
    }
}
