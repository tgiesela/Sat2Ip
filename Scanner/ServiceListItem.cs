using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class ServiceListItem
    {
        int m_service_id;
        int m_service_type;
        int m_lcn;
        public ushort transportstreamid { get; set; }
        public int service_id { get { return m_service_id; } set { m_service_id = value; } }
        public int service_type { get { return m_service_type; } set { m_service_type = value;} }
        public int lcn { get { return m_lcn; } set { m_lcn = value; } }
        public int lcnleftpart { get; set; }
    }
}
