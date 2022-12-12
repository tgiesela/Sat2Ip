using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sat2Ip;
using Microsoft.Extensions.Logging;
using Interfaces;
using Sat2IpGui.SatUtils;
using Sat2ipUtils;
using System.IO;
using System.Text.RegularExpressions;

namespace Sat2IpGui
{

    /*
     * git clone https://git.linuxtv.org/dtv-scan-tables
     *  See folder: /dtv-scan-tables/dvb-c/
     * 
     */
    public partial class FrmFindChannelsDVBC : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private LNB m_LNB;
        private SatelliteInfo info;
        private string iniFilename;
        private IniFile inifile;
        private Scanner scanner;
        private bool scanning = false;
        private Config config = new();
        private CableInfo m_cableinfo = new();
        int channelcount = 0;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public FrmFindChannelsDVBC()
        {
            InitializeComponent();
            config.load();

            cmbProvider.DataSource = m_cableinfo.datasourceProviders();
            cmbProvider.ValueMember = "name";
            cmbProvider.DisplayMember = "name";
            cmbProvider.DropDownStyle = ComboBoxStyle.DropDownList;

            cmbFrequency.DropDownStyle = ComboBoxStyle.DropDownList;

            btnScan.Enabled = false;
            m_LNB = new LNB(1);
            m_LNB.load();

            txtNetworkID.Text = config.configitems.networkid.ToString();
        }

        private void cbScanAll_CheckedChanged(object sender, EventArgs e)
        {
            cmbFrequency.Enabled = !cbScanAll.Checked;
            if (cmbProvider.SelectedIndex >= 0)
                btnScan.Enabled = true;
        }

        private void cmbProvider_SelectedValueChanged(object sender, EventArgs e)
        {
            loadTransponders();
        }
        private void loadTransponders()
        {
            m_cableinfo.currentProvider = getSelectedProvider();
            cmbFrequency.DataSource = m_cableinfo.datasourceTransponders();
            cmbFrequency.DisplayMember = "displayName";
            cmbFrequency.ValueMember = "frequency";
        }

        private CableProvider getSelectedProvider()
        {
            if (cmbProvider.SelectedIndex == -1)
                return null;
            else
            {
                CableProvider cp = cmbProvider.SelectedItem as CableProvider;
                return cp;
            }
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private async void btnScan_ClickAsync(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            btnClose.Enabled = false;
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            uribld.Host = config.configitems.IpAddressDevice;
            uribld.Port = int.Parse(config.configitems.PortDevice);
            RTSP rtsp = new RTSP(uribld.Uri);
            if (config.configitems.FixedTuner)
            {
                // rtsp.frontend = rtsp.getFreeTuner();
                rtsp.frontend = (int)config.configitems.TunerNumber;
            }
            scanner = new Scanner(rtsp);
            if (m_LNB != null)
            {
                scanner.networks = m_LNB.networks;
                scanner.bouquets = m_LNB.bouquets;
            }
            btnStop.Enabled = true;
            scanning = true;

            if (cmbProvider.SelectedIndex < 0)
            {
                MessageBox.Show("Please select Provider/Frequency");
                btnScan.Enabled = false;
                return;
            }

            try { config.configitems.networkid = int.Parse(txtNetworkID.Text.Trim()); } 
            catch (Exception) { config.configitems.networkid = -1; };
            channelcount = 0;
            if (cbScanAll.Checked)
            {
                /* We do not use the combobox, but we use the available transponders */
                List<Transponder> scanned = new();
                List<Transponder> alltransponders = new(); 
                scanned.AddRange(m_cableinfo.Transponders);
                foreach (Transponder tsp in scanned)
                {
                    if (scanning)
                        await scantransponder(tsp);
                }
                alltransponders.AddRange(m_cableinfo.Transponders);
                foreach (Transponder tsp in alltransponders)
                {
                    if (scanned.Contains(tsp))
                        continue;
                    else
                    {
                        if (scanning)
                            await scantransponder(tsp);
                    }
                }
            }
            else
            {
                if (cmbFrequency.SelectedIndex >= 0)
                {
                    Transponder tsp = cmbFrequency.SelectedItem as Transponder;
                    await scantransponder(tsp);
                }
                else
                {
                    MessageBox.Show("Please select LNB/Satellite and frequency");
                    return;
                }
            }
            config.save();
            m_LNB.save();
            MessageBox.Show("Scan complete, channels saved");
            scanner.stop();
            btnScan.Enabled = true;
            btnClose.Enabled = true;
            btnStop.Enabled = false;
        }

        private async Task scantransponder(Transponder tsp)
        {
            txtTransponder.Text = tsp.frequency.ToString();
            List<Channel> lChannels = new();
            await scanChannelsAsync(scanner, lChannels, tsp);
            channelcount += lChannels.Count;
            txtChannels.Text = channelcount.ToString();
            this.Update();
            if (lChannels.Count > 0)
            {
                m_LNB.networks = scanner.networks;
                m_LNB.bouquets = scanner.bouquets;
                m_cableinfo.addTranspondersFromNit(scanner.networks, txtNetworkID.Text);
                cmbFrequency.DataSource = m_cableinfo.updateDatasourceTransponders();
                cmbFrequency.DisplayMember = "displayName";
                cmbFrequency.ValueMember = "frequency";
                cmbFrequency.SelectedItem = tsp;
                m_LNB.transponders = m_cableinfo.Transponders;
                m_LNB.setTransponder(tsp, lChannels);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private async Task<int> scanChannelsAsync(Scanner scanner, List<Channel> lChannels, Transponder tsp)
        {
            int nrOfChannelsInTransponder = 0;
            List<Channel> scantask = await scanner.scan(tsp);
            if (scantask != null)
            {
                foreach (Channel c in scantask)
                {
                    c.transponder = tsp;
                    lChannels.Add(c);
                }
                nrOfChannelsInTransponder = lChannels.Count;
                dumpInfo(tsp, scantask);
            }
            return nrOfChannelsInTransponder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private async Task<FastScanBouquet> fastscanChannelsAsync(Scanner scanner, FastScanLocation location, Transponder tsp)
        {
            FastScanBouquet scantask = await scanner.scanfast(location, tsp);
            if (scantask != null)
            {
                if (scantask.network != null)
                {
                    List<ServiceListItem> services = scantask.network.networkservices;
                    services = services.OrderBy(x => x.lcn).ToList();
                    List<Channel> allchannels = new();
                    LNB lnb = new LNB(location.diseqcposition);
                    lnb.load();
                    allchannels.AddRange(lnb.channels);
                    log.DebugFormat("\nFastscan results for {0}\n", scantask.location.name);
                    foreach (ServiceListItem service in services)
                    {
                        List<Channel> cs = allchannels.FindAll(x => x.service_id == service.service_id && x.transponder.transportstreamid == service.transportstreamid);
                        if (cs != null && cs.Count > 0)
                        {
                            foreach (Channel c in cs)
                                log.DebugFormat("{0}\t {1} Freq: {2} netw:{3}", service.lcn, c.Servicename, c.transponder.frequency, c.transponder.network_id);
                        }
                        else
                        {
                            log.DebugFormat("{0}\t {1}", service.lcn, "?");

                        }
                    }
                    if (scantask.programInfos != null)
                    {
                        foreach (FastScanProgramInfo pi in scantask.programInfos)
                        {
                            log.DebugFormat("{0}:\t{1} Stream: {2}({2:X}), Network:{3})", pi.packagename, pi.channelname, pi.streamid, pi.network);
                            log.DebugFormat("\tPIDS: {0}({0:X})\t{1}({1:X})\t{2}({2:X})\t{3}({3:X})\t{4}({4:X})", pi.pmtpid[0], pi.pmtpid[1], pi.pmtpid[2], pi.pmtpid[3], pi.pmtpid[4]);
                        }
                    }
                }
            }
            return scantask;
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void dumpInfo(Transponder tsp, List<Channel> channels)
        {
            foreach (Channel channel in channels)
            {
                log.DebugFormat("Provider: {0}\tService: {1} ({1:X})\tProgram PID: {2} ({2:X})\tService type: {3}",
                    channel.Providername, channel.Servicename, channel.Programpid, channel.Servicetype);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (scanning)
            {
                scanning = false;
                if (scanner != null)
                {
                    scanner.stop();
                }
            }
        }

        private void cmbFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFrequency.SelectedIndex < 0)
                return;
            else
            {
                Transponder x = cmbFrequency.SelectedItem as Transponder;
                btnScan.Enabled = true;
            }
        }

    }
}
