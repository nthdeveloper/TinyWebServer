using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    class ContentType
    {
        public string Extension { get; private set; }
        public string MimeType { get; private set; }

        public ContentType(string extension, string mimeType)
        {
            this.Extension = extension;
            this.MimeType = mimeType;
        }
    }
}
