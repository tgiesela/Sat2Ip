using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    [Serializable]
    public class ConfigItems
    {
        private LNB[] m_lnb = new LNB[4];
        public LNB[] lnbs { get { return m_lnb; } set { m_lnb = value; } }

        public string OscamServer { get; set; }
        public string OscamPort { get; set; }
        public string IpAddressDevice { get; set; }
        public string PortDevice { get; set; }
        public ConfigItems()
        {
        }

    }
}
