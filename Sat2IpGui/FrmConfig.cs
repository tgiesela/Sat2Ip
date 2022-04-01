using Sat2IpGui.SatUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sat2IpGui
{
    public partial class FrmConfig : Form
    {
        List<SatUtils.SatelliteInfo> listinfo;
        private SatUtils.SatelliteReader satreader;
        private Config config;
        private CheckBox[] checkboxes;
        private ComboBox[] comboboxes;
        public FrmConfig()
        {
            InitializeComponent();
            checkboxes = new CheckBox[] { cbLNB1, cbLNB2, cbLNB3, cbLNB4 };
            comboboxes = new ComboBox[] { cmbSatellites1, cmbSatellites2, cmbSatellites3, cmbSatellites4 };

            satreader = new SatUtils.SatelliteReader();
            listinfo = satreader.read(@"satellites.csv");
            config = new Config();
            config.load();

            for (int i = 0; i < comboboxes.Length; i++)
            {
                LoadSatellites(comboboxes[i]);
            }
            if (config.configitems != null)
            {
                for (int i = 0; i < config.configitems.lnb.Length; i++)
                {
                    if (config.configitems.lnb[i] != null)
                    {
                        checkboxes[i].Checked = true;
                        comboboxes[i].Enabled = true;
                        comboboxes[i].SelectedItem = config.configitems.lnb[i].satellitename;
                    }
                    else
                    {
                        checkboxes[i].Checked = false;
                        comboboxes[i].Enabled = false;
                        comboboxes[i].SelectedItem = -1;
                    }
                }
                txtOscamserver.Text = config.configitems.OscamServer;
                txtOscamport.Text = config.configitems.OscamPort;
            }
        }

        private void LoadSatellites(ComboBox cmbSatellites)
        {
            foreach (SatUtils.SatelliteInfo info in listinfo)
            {
                cmbSatellites.Items.Add(satreader.getSatelliteName(info));
            }
        }

        private async void BtnOK_Click(object sender, EventArgs e)
        {
            Task task;
            for (int i = 0; i < checkboxes.Length; i++)
            {
                if (checkboxes[i].Checked)
                {
                    if (config.configitems.lnb[i] == null)
                    {
                        config.configitems.lnb[i] = new LNB(i+1);
                    }
                    config.configitems.lnb[i].satellitename = comboboxes[i].Text;
                    config.configitems.lnb[i].diseqcposition = i + 1;
                    task = DownloadFrequenciesAsync(cmbSatellites1.SelectedIndex);
                    await task;
                }
                else
                {
                    config.configitems.lnb[i] = null;
                }
            }

            config.configitems.OscamServer = txtOscamserver.Text;
            config.configitems.OscamPort = txtOscamport.Text;
            config.save();
        }

        private async Task DownloadFrequenciesAsync(int selectedIndex)
        {
            SatUtils.SatelliteInfo info = listinfo[selectedIndex];
            string filename = satreader.getTransponderIniFilename(info);

            var client = new HttpClient();
            var response = await client.GetAsync(info.DownloadLink);

            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                var fileInfo = new FileInfo(filename);
                using (var fileStream = fileInfo.OpenWrite())
                {
                    await stream.CopyToAsync(fileStream);
                }
            }
        }

        private void cbLNB1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLNB1.Checked)
            {
                cmbSatellites1.Enabled = true;
            }
            else
            {
                cmbSatellites1.Enabled = false;
            }
        }

        private void cbLNB2_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLNB2.Checked)
            {
                cmbSatellites2.Enabled = true;
            }
            else
            {
                cmbSatellites2.Enabled = false;
            }
        }

        private void cbLNB3_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLNB3.Checked)
            {
                cmbSatellites3.Enabled = true;
            }
            else
            {
                cmbSatellites3.Enabled = false;
            }
        }

        private void cbLNB4_CheckedChanged(object sender, EventArgs e)
        {
            if (cbLNB4.Checked)
            {
                cmbSatellites4.Enabled = true;
            }
            else
            {
                cmbSatellites4.Enabled = false;
            }
        }
    }
}
