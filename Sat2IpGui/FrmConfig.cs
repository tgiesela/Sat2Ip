﻿using Sat2IpGui.SatUtils;
using Sat2ipUtils;
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
        private Config config;
        private CheckBox[] checkboxes;
        private ComboBox[] comboboxes;
        private SatInfo m_satinfo = new();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public FrmConfig()
        {
            InitializeComponent();
            checkboxes = new CheckBox[] { cbLNB1, cbLNB2, cbLNB3, cbLNB4 };
            comboboxes = new ComboBox[] { cmbSatellites1, cmbSatellites2, cmbSatellites3, cmbSatellites4 };

            config = new Config();
            config.load();
            if (config.configitems.dvbtype == "DVBC")
            {
                rbDVBC.Checked = true;
                rbDVBS.Checked = false;
            }
            else
            {
                rbDVBC.Checked = false;
                rbDVBS.Checked = true;
            }

            for (int i = 0; i < comboboxes.Length; i++)
            {
                LoadSatellites(comboboxes[i]);
            }
            if (config.configitems != null)
            {
                for (int i = 0; i < config.configitems.lnbs.Length; i++)
                {
                    if (config.configitems.lnbs[i] != null)
                    {
                        checkboxes[i].Checked = true;
                        comboboxes[i].Enabled = true;
                        comboboxes[i].SelectedItem = m_satinfo.findSatelliteName(config.configitems.lnbs[i].satellitename);
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
                txtIpAddressDevice.Text = config.configitems.IpAddressDevice;
            }
            cbFixedTuner.Checked = config.configitems.FixedTuner;
            cbFixedTuner_CheckedChanged(null, null);
            if (config.configitems.TunerNumber > 0)
                numTuner.Value = config.configitems.TunerNumber;
        }

        private void LoadSatellites(ComboBox cmbSatellites)
        {
            cmbSatellites.DataSource = m_satinfo.datasourceSatellites();
            cmbSatellites.DisplayMember = "displayname";
            cmbSatellites.ValueMember = "satellitename";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void BtnOK_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < checkboxes.Length; i++)
            {
                if (checkboxes[i].Checked)
                {
                    if (config.configitems.lnbs[i] == null)
                    {
                        config.configitems.lnbs[i] = new LNB(i+1);
                    }
                    config.configitems.lnbs[i].satellitename = comboboxes[i].Text;
                    config.configitems.lnbs[i].diseqcposition = i + 1;
                }
                else
                {
                    config.configitems.lnbs[i] = null;
                }
            }

            config.configitems.IpAddressDevice = txtIpAddressDevice.Text;
            config.configitems.OscamServer = txtOscamserver.Text;
            config.configitems.OscamPort = txtOscamport.Text;
            config.configitems.FixedTuner = cbFixedTuner.Checked;
            config.configitems.TunerNumber = numTuner.Value;
            if (rbDVBC.Checked)
                config.configitems.dvbtype = "DVBC";
            else
                config.configitems.dvbtype = "DVBS";
            config.save();
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
        private void cbFixedTuner_CheckedChanged(object sender, EventArgs e)
        {
            if (cbFixedTuner.Checked)
            {
                numTuner.Enabled = true;
            }
            else
            {
                numTuner.Enabled = false;
            }
        }

        private void rbDVBS_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDVBS.Checked)
            {
                gbLNB.Enabled = true;
            }
            else
            {
                gbLNB.Enabled = false;
            }
        }
    }
}
