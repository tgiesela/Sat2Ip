using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPNPLib;

namespace Sat2ipUtils
{
    [Serializable]
    public class ConfigItems
    {
        private LNB[] m_lnb = new LNB[4];
        private Sat2ipserver[] m_sat2ipservers;
        public LNB[] lnbs { get { return m_lnb; } set { m_lnb = value; } }
        public string OscamServer { get; set; }
        public string OscamPort { get; set; }
        public string IpAddressDevice { get; set; }
        public Sat2ipserver[] sat2ipdevices { get { return m_sat2ipservers; } set { m_sat2ipservers = value; } }
        public ChannelFilter channelFilter { get; set; }
        public ChannelNumbering channelNumbering { get; set; }
        public string PortDevice { get; set; }
        public bool FixedTuner { get; set; }
        public decimal TunerNumber { get; set; }
        public string dvbtype { get; set; }
        public int networkid { get; set; }

        public ConfigItems()
        {
            channelNumbering = new ChannelNumbering();
            channelFilter = new ChannelFilter();
        }

    }
}
