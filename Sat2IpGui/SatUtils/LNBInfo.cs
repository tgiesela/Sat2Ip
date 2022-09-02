using Sat2ipUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sat2IpGui.SatUtils
{
    public class LNBInfo: InfoBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private List<LNB> m_lnbs = new();
        private Config config = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public LNBInfo()
        {
            config.load();
            for (int i = 0; i < config.configitems.lnbs.Length; i++)
            {
                if (config.configitems.lnbs[i] != null)
                {
                    LNB lnb = new(config.configitems.lnbs[i].diseqcposition);
                    lnb.satellitename = config.configitems.lnbs[i].satellitename;
                    m_lnbs.Add(lnb);
                }
            }
        }
        public BindingSource datasourceLNBs()
        {
            BindingSource bs = new();
            bs.DataSource = m_lnbs;
            return bs;
        }

    }
}
