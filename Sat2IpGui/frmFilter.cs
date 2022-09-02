using Interfaces;
using Sat2Ip;
using Sat2IpGui.SatUtils;
using Sat2ipUtils;
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
        private Sat2ipUtils.Config config = new Sat2ipUtils.Config();
        private List<Bouquet> m_bouquets = new List<Bouquet>();
        private List<Network> m_networks = new List<Network>();
        private List<Transponder> m_transponders = new List<Transponder>();
        private List<string> m_providers = new List<string>();
        private ChannelFilter m_channelFilter;

//        public FastScanBouquet fastscanlocation { get; set; }
//        public Bouquet DVBBouquet { get; set; }
//        public Transponder frequency { get; private set; }
//        public LNB lnb { get; private set; }
//        public string provider { get; private set; }
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
            m_channelFilter = config.configitems.channelFilter;
            m_bouquets.Clear();
            cmbSatellites.Items.Add(createEmtpyItem());
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
            cmbDVBBouquets.Items.Add(createEmtpyItem());
            foreach (Bouquet b in m_bouquets)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = b;
                item.textToDisplay = b.bouquet_name;
                cmbDVBBouquets.Items.Add(item);
            }
            cmbFastscanBouquets.Items.Add(createEmtpyItem());
            foreach (FastScanBouquet fsb in config.FastcanBouquets)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = fsb;
                item.textToDisplay = "(" + fsb.location.frequency / 1000 + ") - " + fsb.location.name;
                cmbFastscanBouquets.Items.Add(item);
            }
            m_providers = m_providers.OrderBy(x => x).ToList();
            cmbProviders.Items.Add(createEmtpyItem());
            foreach (string provider in m_providers)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj = provider;
                item.textToDisplay = provider;
                cmbProviders.Items.Add(item);
            }
            if (m_channelFilter.lnb != null)
            {
                cmbSatellites.Text = m_channelFilter.lnb.satellitename;
                if (m_channelFilter.frequency != null)
                {
                    if (m_channelFilter.frequency.frequency != 0)
                    {
                        Transponder tsp = m_channelFilter.frequency;
                        cmbFrequency.Text = tsp.frequency + "," + tsp.polarisation + "," + tsp.dvbsystem + "," + tsp.transportstreamid;
                    }
                }
            }
            if (m_channelFilter.fastScanBouquet != null)
            {
                string txt = "(" + m_channelFilter.fastScanBouquet.location.frequency / 1000 + ") - " + m_channelFilter.fastScanBouquet.location.name;
                cmbFastscanBouquets.Text = txt;
            }
            if (m_channelFilter.provider != null && m_channelFilter.provider != string.Empty)
            {
                cmbProviders.Text = m_channelFilter.provider;
            }
            if (m_channelFilter.DVBBouquet != null)
            {
                cmbDVBBouquets.Text = m_channelFilter.DVBBouquet.bouquet_name;
            }
            cbRadio.Checked = m_channelFilter.Radio;
            cbTV.Checked = m_channelFilter.TV;
            cbData.Checked = m_channelFilter.Data;
            cbFTA.Checked = m_channelFilter.FTA;
        }
        private ListBoxItem createEmtpyItem()
        {
            ListBoxItem item = new ListBoxItem();
            item.obj = null;
            item.textToDisplay = "[]";
            return item;
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
            if (lnb == null) return;
            m_transponders = lnb.transponders;
            cmbFrequency.Items.Clear();
            cmbFrequency.Items.Add(createEmtpyItem());
            foreach (Transponder tsp in m_transponders)
            {
                ListBoxItem item = new ListBoxItem();
                item.obj=tsp;
                item.textToDisplay = tsp.frequency + "," + tsp.polarisation + "," + tsp.dvbsystem + "," + tsp.transportstreamid;
                cmbFrequency.Items.Add(item);
            }
        }

        private void loadNetworks(LNB lnb)
        {
            if (lnb == null) return;
            m_networks.AddRange(lnb.networks);
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
                m_channelFilter.DVBBouquet = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbDVBBouquets.SelectedItem;
                m_channelFilter.DVBBouquet = (Bouquet)item.obj;
            }
            if (cmbFastscanBouquets.SelectedIndex < 0)
            {
                m_channelFilter.fastScanBouquet = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbFastscanBouquets.SelectedItem;
                m_channelFilter.fastScanBouquet = (FastScanBouquet)item.obj;
            }
            if (cmbFrequency.SelectedIndex < 0)
            {
                m_channelFilter.frequency = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbFrequency.SelectedItem;
                m_channelFilter.frequency = (Transponder)item.obj;
            }
            if (cmbSatellites.SelectedIndex < 0)
            {
                m_channelFilter.lnb = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbSatellites.SelectedItem;
                m_channelFilter.lnb = (LNB)item.obj;
            }
            if (cmbProviders.SelectedIndex < 0)
            {
                m_channelFilter.provider = null;
            }
            else
            {
                ListBoxItem item = (ListBoxItem)cmbProviders.SelectedItem;
                m_channelFilter.provider = (string)item.obj;
            }
            m_channelFilter.FTA = cbFTA.Checked;
            m_channelFilter.TV = cbTV.Checked;
            m_channelFilter.Radio = cbRadio.Checked;
            m_channelFilter.Data =  cbData.Checked;
            config.save();
        }
        public List<Channel> assign(List<Channel> channels)
        {
            List<Channel> list = new List<Channel>();
            foreach (Channel c in channels)
            {
                if (m_channelFilter.provider != null) {
                    if (!m_channelFilter.provider.Equals(c.Providername, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                if (m_channelFilter.frequency != null)
                {
                    if (m_channelFilter.frequency.frequency != c.transponder.frequency)
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
