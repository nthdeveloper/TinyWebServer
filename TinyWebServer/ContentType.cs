using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    class ContentType
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

        public string Extension { get; private set; }
        public string MimeType { get; private set; }

        private ContentType(string extension, string mimeType)
        {
            this.Extension = extension;
            this.MimeType = mimeType;
        }

        public static ContentType GetContentTypeForFile(string filePath)
        {
            string _extension = Path.GetExtension(filePath).ToLowerInvariant();

            if (ContentTypes.ContainsKey(_extension))
                return ContentTypes[_extension];

            return ContentTypes[".html"];
        }
    }
}
