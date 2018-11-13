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

        internal DateTime Expires { get; set; }

        public Session(string id)
        {
            m_Id = id;
        }
    }

    public class SessionCollection
    {
        private readonly Dictionary<string, Session> m_Sessions = new Dictionary<string, Session>();
        private readonly object m_SyncObj = new object();

        //public bool ContainsSession(string id)
        //{
        //    lock(m_SyncObj)
        //    {
        //        return m_Sessions.ContainsKey(id);
        //    }
        //}

        public Session this[string id]
        {
            get
            {
                lock (m_SyncObj)
                {
                    if (m_Sessions.ContainsKey(id))
                        return m_Sessions[id];

                    return null;
                }
            }
            set
            {
                lock (m_SyncObj)
                {
                    m_Sessions[id] = value;
                }
            }
        }

        public void Add(Session session)
        {
            lock (m_SyncObj)
            {
                m_Sessions[session.Id] = session;
            }
        }

        public void Remove(Session session)
        {
            lock (m_SyncObj)
            {
                if (m_Sessions.ContainsKey(session.Id))
                    m_Sessions.Remove(session.Id);
            }
        }

        public void Clear()
        {
            lock(m_SyncObj)
            {
                m_Sessions.Clear();
            }
        }

        public Session[] ToArray()
        {
            lock (m_SyncObj)
            {
                return m_Sessions.Values.ToArray();
            }
        }        
    }
}
