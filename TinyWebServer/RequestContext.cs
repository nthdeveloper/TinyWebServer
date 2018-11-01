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
        public WebServer Server { get; private set; }
        public HttpListenerContext ListenerContext { get; private set; }

        public RequestContext(WebServer server, HttpListenerContext listenerContext)
        {
            this.Server = server;
            this.ListenerContext = listenerContext;
        }
    }
}
