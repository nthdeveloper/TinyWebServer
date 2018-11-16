using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    public class FileRequest
    {
        public string FilePath { get; set; }
        public RequestResult RequestResult { get; set; }

        public FileRequest(string filePath, RequestResult requestResult)
        {
            this.FilePath = filePath;
            this.RequestResult = requestResult;
        }
    }
}
