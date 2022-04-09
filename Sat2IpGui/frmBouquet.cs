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
    public partial class frmBouquet : Form
    {
        private Config config = new Config();
        private List<Bouquet> m_bouquets = new List<Bouquet>();
        public class ListBoxItem
        {
            public Object obj;
            public String textToDisplay;
            public override string ToString()
            {
                if (textToDisplay != null)
                    return textToDisplay;
                else
                    return "<noname>";
            }
        }
        public FastScanBouquet fastscanlocation { get; set; }
        public Bouquet DVBBouquet { get; set; }
        public frmBouquet()
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
                loadBouquets(config.configitems.lnbs[i]);
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
        }
        private void loadBouquets(LNB lnb)
        {
            if (lnb == null) return;
            lnb.load();
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
            if (cmbDVBBouquets.SelectedIndex >=0)
            {
                cmbFastscanBouquets.SelectedIndex = -1;
            }
        }
    }
}
