using Interfaces;
using Sat2Ip;
using Sat2IpGui.SatUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sat2IpGui
{
    public partial class frmFilter : Form
    {
        private Config config = new Config();
        private List<Bouquet> m_bouquets = new List<Bouquet>();
        private List<Network> m_networks = new List<Network>();
        private List<Transponder> m_transponders = new List<Transponder>();
        private List<string> m_providers = new List<string>();
        public FastScanBouquet fastscanlocation { get; set; }
        public Bouquet DVBBouquet { get; set; }
        public Transponder frequency { get; private set; }
        public LNB lnb { get; private set; }
        public string provider { get; private set; }
        public class ListBoxItem
        {
            public Object obj;
            public String textToDisplay;
            public override string ToString()
            {
                if (textToDisplay != null && textToDisplay != "")
                    return textToDisplay;
                else
                    return "<noname>";
            }
        }
        public frmFilter()
        {
            InitializeComponent();
            populateComboboxes();
        }
        private void populateComboboxes()
        {
            config.load();
            m_bouquets.Clear();
            for (int i = 0; i < config.configitems.lnbs.Length; i++)
            {
                LNB lnb = config.configitems.lnbs[i];
                if (lnb != null)
                {
                    lnb.load();
                    ListBoxItem item = new ListBoxItem();
                    item.obj = lnb;
                    item.textToDisplay = lnb.satellitename;
                    cmbSatellites.Items.Add(item);
                    loadBouquets(lnb);
                    loadNetworks(lnb);
                    loadProviders(lnb);
                }
            }
            foreach (Bouquet b in m_bouquets)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = b;
                item.textToDisplay = b.bouquet_name;
                cmbDVBBouquets.Items.Add(item);
            }
            foreach (FastScanBouquet fsb in config.FastcanBouquets)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = fsb;
                item.textToDisplay = "(" + fsb.location.frequency / 1000 + ") - " + fsb.location.name;
                cmbFastscanBouquets.Items.Add(item);
            }
            m_providers = m_providers.OrderBy(x => x).ToList();
            foreach (string provider in m_providers)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = provider;
                item.textToDisplay = provider;
                cmbProviders.Items.Add(item);
            }
        }

        private void loadProviders(LNB lnb)
        {
            foreach (Channel c in lnb.channels)
            {
                if (m_providers.Find(x => x.Equals(c.Providername, StringComparison.OrdinalIgnoreCase)) == null)
                    m_providers.Add(c.Providername);
            }
        }

        private void loadTransponders(LNB lnb)
        {
            m_transponders = lnb.transponders;
            cmbFrequency.Items.Clear();
            foreach (Transponder tsp in m_transponders)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj=tsp;
                item.textToDisplay = tsp.frequency + "," + tsp.polarisation + "," + tsp.dvbsystem + "," + tsp.transportstreamid;
                cmbFrequency.Items.Add(item);
            }
        }

        private void loadNetworks(LNB lNB)
        {
            m_networks.AddRange(lNB.networks);
        }

        private void loadBouquets(LNB lnb)
        {
            List<Bouquet> bouquets = lnb.bouquets;
            if (lnb.bouquets != null)
            {
                bouquets = bouquets.OrderBy(x => x.bouquet_name).ToList();
                foreach (Bouquet bouquet in bouquets)
                {
                    m_bouquets.Add(bouquet);
                }
            }
        }

        private void cmbSatellites_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBoxItem lbi = cmbSatellites.SelectedItem as ListBoxItem;
            LNB lnb = (LNB)lbi.obj;
            loadTransponders(lnb);
        }

        private void cmbFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmbProviders_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cmbFastscanBouquets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFastscanBouquets.SelectedIndex >= 0)
            {
                cmbDVBBouquets.SelectedIndex = -1;
            }
        }

        private void cmbDVBBouquets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDVBBouquets.SelectedIndex >= 0)
            {
                cmbFastscanBouquets.SelectedIndex = -1;
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (cmbDVBBouquets.SelectedIndex < 0)
            {
                DVBBouquet = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbDVBBouquets.SelectedItem;
                DVBBouquet = (Bouquet)item.obj;
            }
            if (cmbFastscanBouquets.SelectedIndex < 0)
            {
                fastscanlocation = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbFastscanBouquets.SelectedItem;
                fastscanlocation = (FastScanBouquet)item.obj;
            }
            if (cmbFrequency.SelectedIndex < 0)
            {
                frequency = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbFrequency.SelectedItem;
                frequency = (Transponder)item.obj;
            }
            if (cmbSatellites.SelectedIndex < 0)
            {
                lnb = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbSatellites.SelectedItem;
                lnb = (LNB)item.obj;
            }
            if (cmbProviders.SelectedIndex < 0)
            {
                provider = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbSatellites.SelectedItem;
                provider = (string)item.obj;
            }
        }
        public List<Channel> assign(List<Channel> channels)
        {
            List<Channel> list = new List<Channel>();
            foreach (Channel c in channels)
            {
                if (provider != null) {
                    if (!provider.Equals(c.Providername, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                if (frequency != null)
                {
                    if (frequency.frequency != c.transponder.frequency)
                        continue;
                }
                if (cbData.Checked && !c.isDataService()) continue;
                if (cbRadio.Checked && !c.isRadioService()) continue;
                if (cbTV.Checked && !c.isTVService()) continue;
                if (cbFTA.Checked && c.free_CA_mode != 0) continue;
                list.Add(c);
            }
            return list;
        }

    }
}
