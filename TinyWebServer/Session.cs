using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinyWebServer
{
    public class Session
    {
        Dictionary<string, object> m_Data = new Dictionary<string, object>();

        public object this[string key]
        {
            get
            {
                if (m_Data.ContainsKey(key))
                    return m_Data[key];

                return null;
            }
            set
            {
                m_Data[key] = value;
            }
        }

        readonly string m_Id;

        public string Id => m_Id;

        public Session(string id)
        {
            m_Id = id;
        }
    }
}
