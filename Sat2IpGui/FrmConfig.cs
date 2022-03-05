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
        public FrmConfig()
        {
            InitializeComponent();
            cbLNB1.Checked = !Properties.App.Default.LNB1.Equals("");
            cbLNB2.Checked = !Properties.App.Default.LNB2.Equals("");
            cbLNB3.Checked = !Properties.App.Default.LNB3.Equals("");
            cbLNB4.Checked = !Properties.App.Default.LNB4.Equals("");

            cmbSatellites1.Enabled = false;
            cmbSatellites2.Enabled = false;
            cmbSatellites3.Enabled = false;
            cmbSatellites4.Enabled = false;
            satreader = new SatUtils.SatelliteReader();
            listinfo = satreader.read(@"satellites.csv");
            LoadSatellites(cmbSatellites1);
            LoadSatellites(cmbSatellites2);
            LoadSatellites(cmbSatellites3);
            LoadSatellites(cmbSatellites4);
            if (cbLNB1.Checked)
            {
                cmbSatellites1.Enabled = true;
                Debug.WriteLine(Properties.App.Default.LNB1);
                cmbSatellites1.SelectedItem = Properties.App.Default.LNB1;
            }
            if (cbLNB2.Checked)
            {
                cmbSatellites2.Enabled = true;
                cmbSatellites2.SelectedItem = Properties.App.Default.LNB2;
            }
            if (cbLNB3.Checked)
            {
                cmbSatellites3.Enabled = true;
                cmbSatellites3.SelectedItem = Properties.App.Default.LNB3;
            }
            if (cbLNB4.Checked)
            {
                cmbSatellites4.Enabled = true;
                cmbSatellites4.SelectedItem = Properties.App.Default.LNB4;
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
            Properties.App.Default.LNB1 = "";
            Properties.App.Default.LNB2 = "";
            Properties.App.Default.LNB3 = "";
            Properties.App.Default.LNB4 = "";
            if (cbLNB1.Checked)
            {
                Properties.App.Default.LNB1 = cmbSatellites1.Text;
                task = DownloadFrequenciesAsync(cmbSatellites1.SelectedIndex);
                await task;
            }
            if (cbLNB2.Checked)
            { 
                Properties.App.Default.LNB2 = cmbSatellites2.Text;
                task = DownloadFrequenciesAsync(cmbSatellites2.SelectedIndex);
                await task;
            }
            if (cbLNB3.Checked)
            {
                Properties.App.Default.LNB3 = cmbSatellites3.Text;
                task = DownloadFrequenciesAsync(cmbSatellites3.SelectedIndex);
                await task;
            }
            if (cbLNB4.Checked)
            {
                Properties.App.Default.LNB4 = cmbSatellites4.Text;
                task = DownloadFrequenciesAsync(cmbSatellites4.SelectedIndex);
                await task;
            }
            Properties.App.Default.Save();
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
