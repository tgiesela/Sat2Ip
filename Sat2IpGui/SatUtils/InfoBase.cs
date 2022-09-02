using Interfaces;
using Sat2Ip;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using System;
using System.ComponentModel;

namespace Sat2IpGui.SatUtils
{
    public class InfoBase
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        internal List<Transponder> m_transponders = new();
        public virtual BindingSource datasourceTransponders()
        {
            BindingSource bs = new();
            bs.DataSource = m_transponders;
            return bs;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public void addTranspondersFromNit(List<Network> networks)
        {
            addTranspondersFromNit(networks, "");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public void addTranspondersFromNit(List<Network> networks, string networkID)
        {
            int netwid = 0;
            //List<Transponder> _tsps = new List<Transponder>();
            if (networkID.Equals(""))
                netwid = -1;
            else
            {
                try { netwid = int.Parse(networkID); } 
                catch (Exception){ netwid = -1; }
            }
            
            foreach (Network n in networks)
            {
                if ((n.networkid == netwid) || (netwid==-1 && n.currentnetwork))
                {
                    List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
                    foreach (Transponder t in nit)
                    {
                        if (m_transponders.Find(x => x.frequency == t.frequency && x.diseqcposition == t.diseqcposition) == null)
                            m_transponders.Add(t);
                    }
                }
            }
        }

    }
}