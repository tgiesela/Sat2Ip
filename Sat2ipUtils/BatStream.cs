using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class BatStream
    {
        private List<ServiceListItem> m_services = new List<ServiceListItem>();
        private short m_streamid;
        private short m_original_networkid;
        public List<ServiceListItem> services { get { return m_services; } set { m_services = value; } }
        public short streamid { get { return m_streamid; } set { m_streamid = value; } }
        public short original_networkid { get { return m_original_networkid; } set { m_original_networkid = value; } }
    }
}
