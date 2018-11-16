using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    public abstract class RequestResult
    {
        public static RequestResult None { get; private set; } = new NoResult();
        public static RequestResult NoContent { get; private set; } = new NoContentResult();
        public static RequestResult NotFound { get; private set; } = new NotFoundResult();
        public static RequestResult BadRequest { get; private set; } = new BadRequestResult();

        internal abstract void WriteResult(RequestContext context);

        class NoResult : RequestResult
        {
            internal override void WriteResult(RequestContext context)
            {
            }
        }

        class NoContentResult : RequestResult
        {
            internal override void WriteResult(RequestContext context)
            {
                context.ListenerContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
        }

        class NotFoundResult : RequestResult
        {
            internal override void WriteResult(RequestContext context)
            {
                context.ListenerContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }

        class BadRequestResult : RequestResult
        {
            internal override void WriteResult(RequestContext context)
            {
                context.ListenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            }
        }
    }    

    public class StaticFileResult : RequestResult
    {
        readonly string m_FilePath;

        public StaticFileResult(string filePath)
        {
            m_FilePath = filePath;
        }

        internal override void WriteResult(RequestContext context)
        {
            if (String.IsNullOrEmpty(m_FilePath))
            {
                RequestResult.NotFound.WriteResult(context);
                return;
            }

            string _fullPath = context.Server.GetFileLocalPath(m_FilePath);

            if (String.IsNullOrEmpty(_fullPath) || !File.Exists(_fullPath))
            {
                RequestResult.NotFound.WriteResult(context);
                return;
            }

            var _contentType = WebServer.GetContentType(m_FilePath);
            context.ListenerContext.Response.ContentType = _contentType.MimeType;
            
            byte[] _fileData = File.ReadAllBytes(_fullPath);
            context.ListenerContext.Response.ContentLength64 = _fileData.Length;
            context.ListenerContext.Response.OutputStream.Write(_fileData, 0, _fileData.Length);
        }
    }

    public class TextResult : RequestResult
    {
        readonly string m_TextData;
        readonly string m_ContentType;
        readonly Encoding m_Encoding = Encoding.UTF8;

        public TextResult(string textData, string contentType = System.Net.Mime.MediaTypeNames.Text.Plain)
        {
            m_TextData = textData;
            m_ContentType = contentType;
        }

        public TextResult(string textData, Encoding encoding, string contentType = System.Net.Mime.MediaTypeNames.Text.Plain)
        {
            m_TextData = textData;
            m_Encoding = encoding;
            m_ContentType = contentType;
        }

        internal override void WriteResult(RequestContext context)
        {
            context.ListenerContext.Response.ContentType = m_ContentType;
            context.ListenerContext.Response.Headers.Add("Access-Control-Allow-Origin: *");

            byte[] _data = m_Encoding.GetBytes(m_TextData);
            context.ListenerContext.Response.ContentLength64 = _data.Length;
            context.ListenerContext.Response.OutputStream.Write(_data, 0, _data.Length);
        }
    }  
    
    public class RedirectResult : RequestResult
    {
        readonly string m_RedirectLocation;

        public RedirectResult(string location)
        {
            m_RedirectLocation = location;
        }

        internal override void WriteResult(RequestContext context)
        {
            context.ListenerContext.Response.StatusCode = (int)HttpStatusCode.Redirect;
            context.ListenerContext.Response.Redirect(m_RedirectLocation);
        }
    }
}
