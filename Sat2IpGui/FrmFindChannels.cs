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

namespace Sat2IpGui
{

    /*
     * git clone https://git.linuxtv.org/dtv-scan-tables
     *  See folder: /dtv-scan-tables/dvb-c/
     * 
     */
    public partial class FrmFindChannels : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private LNB m_LNB;
        private LNBInfo m_lnbinfo = new();
        private SatInfo m_satinfo = new();

        private Scanner scanner;
        private bool scanning = false;
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private Config config = new();
        private List<Transponder> m_transponders;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public FrmFindChannels()
        {
            InitializeComponent();
            config.load();

            m_transponders = new List<Transponder>();
            cmbLNB.DataSource = m_lnbinfo.datasourceLNBs();
            cmbLNB.DisplayMember = "satellitename";
            cmbLNB.ValueMember = "diseqcposition";
            cmbTransponder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLNB.DropDownStyle = ComboBoxStyle.DropDownList;

            btnScan.Enabled = false;
        }

        private void cbScanAll_CheckedChanged(object sender, EventArgs e)
        {
            cmbTransponder.Enabled = !cbScanAll.Checked;
            if (cmbLNB.SelectedIndex >= 0 || cbFastscan.Checked)
                btnScan.Enabled = true;
        }

        private void cmbLNB_SelectedValueChanged(object sender, EventArgs e)
        {
            loadTransponders(null);
        }
        private void loadTransponders(FastScanLocation location)
        {
            loadTranspondersFromIniFile(location);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void loadTranspondersFromIniFile(FastScanLocation location)
        {
            if (location != null)
            {
                m_LNB = new LNB(location.diseqcposition);
            }
            else
            {
                m_LNB = getLNBFromCombobox();
            }
            m_LNB.load();
            cmbTransponder.DataSource = m_satinfo.datasourceTransponders(m_LNB);
            cmbTransponder.DisplayMember = "displayName";
            cmbTransponder.ValueMember = "frequency";
        }
        private void loadTranspondersNIT()
        {
            m_LNB = getLNBFromCombobox();
            foreach (Network netw in m_LNB.networks)
            {
                if (netw.satellitenetwork) /* There are also terrestrial and cable networks, currently not supported */
                {
                    foreach (Transponder t in netw.transponders)
                    {
                        if (m_transponders.Find(x => x.frequency == t.frequency && x.diseqcposition == t.diseqcposition) == null)
                            m_transponders.Add(t);
                        else
                            log.Debug("Duplicate transponder skipped: " + t.frequency);
                    }
                }
            }
        }
        private LNB getLNBFromCombobox()
        {
            if (cmbLNB.SelectedIndex == -1)
                throw new Exception("LNB/satellite Not selected");
            LNB lnb = cmbLNB.SelectedItem as LNB;
            return lnb;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private Transponder getTransponderForFastscan(FastScanLocation location)
        {
            LNB lnb;
            for (int i = 0; i < config.configitems.lnbs.Length; i++)
            {
                if (config.configitems.lnbs[i] != null)
                {
                    lnb = config.configitems.lnbs[i];
                    if (lnb.orbit() == location.position)
                    {
                        lnb.load();
                        loadTransponders(location);
                        foreach (Transponder t in m_transponders)
                        {
                            if (t.frequency == location.frequency / 1000)
                            {
                                location.diseqcposition = lnb.diseqcposition;
                                t.diseqcposition = lnb.diseqcposition;
                                return t;
                            }
                        }
                    }
                }
            }
            return null;
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
            scanner = new Scanner(rtsp.Startport, rtsp.Endport, rtsp);
            if (m_LNB != null)
            {
                scanner.networks = m_LNB.networks;
                scanner.bouquets = m_LNB.bouquets;
            }
            btnStop.Enabled = true;
            scanning = true;
            if (cbFastscan.Checked)
            {
                foreach (FastScanLocation fsl in config.Fastscanlocations)
                {
                    Transponder tsp = getTransponderForFastscan(fsl);
                    if (tsp != null)
                    {
                        FastScanBouquet fsb = await fastscanChannelsAsync(scanner, fsl, tsp);
                        FastScanBouquet exists = config.FastcanBouquets.Find(x => x.location.name == fsb.location.name && x.location.frequency == fsb.location.frequency);
                        if (exists != null)
                            config.FastcanBouquets.Remove(exists);
                        config.FastcanBouquets.Add(fsb);
                        if (!scanning)
                            break;
                    }
                    else
                        MessageBox.Show(string.Format("Cannot find Transponder {0}, fastscan {1} not possible", fsl.frequency, fsl.name));
                }
                config.save();
            }

            if (cbScanAll.Checked)
            {
                if (cmbLNB.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select LNB/Satellite");
                    btnScan.Enabled = true;
                    return;
                }
                int channelcount = 0;
                for (int i = 0; i < cmbTransponder.Items.Count && scanning; i++)
                {
                    Transponder tsp = cmbTransponder.Items[i] as Transponder;
                    txtTransponder.Text = tsp.frequency.ToString();
                    List<Channel> lChannels = new List<Channel>();
                    await scanChannelsAsync(scanner, lChannels, tsp);
                    channelcount += lChannels.Count;
                    txtChannels.Text = channelcount.ToString();
                    this.Update();
                    if (lChannels.Count > 0)
                    {
                        m_LNB.networks = scanner.networks;
                        m_LNB.bouquets = scanner.bouquets;
                        m_satinfo.addTranspondersFromNit(scanner.networks, txtNetworkID.Text);
                        cmbTransponder.DataSource = m_satinfo.updateDatasourceTransponders();
                        cmbTransponder.DisplayMember = "displayName";
                        cmbTransponder.ValueMember = "frequency";
                        cmbTransponder.SelectedItem = tsp;
                        m_LNB.transponders = m_satinfo.m_transponders;
                        m_LNB.setTransponder(tsp, lChannels);
                    }
                }
            }
            else
            {
                if (cmbLNB.SelectedIndex >= 0 && cmbTransponder.SelectedIndex >= 0)
                {
                    List<Channel> lChannels = new List<Channel>();
                    Transponder tsp = cmbTransponder.SelectedItem as Transponder;
                    await scanChannelsAsync(scanner, lChannels, tsp);
    
                    if (lChannels.Count > 0)
                    {
                        m_LNB.networks = scanner.networks;
                        m_LNB.bouquets = scanner.bouquets;
                        m_satinfo.addTranspondersFromNit(scanner.networks, txtNetworkID.Text);
                        cmbTransponder.DataSource = m_satinfo.updateDatasourceTransponders();
                        cmbTransponder.DisplayMember = "displayName";
                        cmbTransponder.ValueMember = "frequency";
                        cmbTransponder.SelectedItem = tsp;
                        m_LNB.transponders = m_satinfo.m_transponders;
                        m_LNB.setTransponder(tsp, lChannels);
                    }
                }
                else
                {
                    if (cbFastscan.Checked)
                    {

                    }
                    else
                    {
                        MessageBox.Show("Please select LNB/Satellite and frequency");
                        return;
                    }
                }
            }

//            List<Network> networks = scanner.networks;
//            List<Bouquet> bouquet = scanner.bouquets;
//            List<Channel> channels = m_LNB.channels;
//            List<Transponder> transponders = getTranspondersFromNetwork(networks);
//            foreach (Network n in networks)
//            {
//                List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
//                log.DebugFormat("Network: id {0}, name {1}", n.networkid, n.networkname);
//                log.Debug("  Bouquets");
//                foreach (Linkage l in n.bouquetlinkages)
//                {
//                    printlinkagedetails(l);
//                }
//                log.Debug("  EPG Info");
//                foreach (Linkage l in n.epglinkages)
//                {
//                    printlinkagedetails(l);
//                }
//                log.Debug("  Service Info");
//                foreach (Linkage l in n.silinkages)
//                {
//                    printlinkagedetails(l);
//                }
//                foreach (Transponder t in nit)
//                {  
//                    if (t.orbit != null)
//                        log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: {3}, id: {4}", t.frequency, t.polarisation, t.samplerate,Utils.Utils.bcdtohex(t.orbit), t.transportstreamid);
//                    else
//                        log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: ??, id: {3}", t.frequency, t.polarisation, t.samplerate, t.transportstreamid);
//                }
//            }
//            foreach (Bouquet b in bouquet)
//            {
//                log.DebugFormat("Bouquet id: {0} ({1}), found on transponder {2}, LNB {3}", b.bouquet_id, b.bouquet_name, b.transponder.frequency, b.transponder.diseqcposition);
//                if (b.bouquetlinkage != null)
//                {
//                    printlinkagedetails(b.bouquetlinkage);
//                }
//                foreach (BatStream bs in b.streams)
//                {
//                    log.DebugFormat("    Stream id: {0} ({0:X}) network: {1})", bs.streamid, bs.original_networkid);
//                    foreach (ServiceListItem sli in bs.services)
//                    {
//                        Channel c = channels.Find(x => x.service_id == sli.service_id);
//                        if (c != null)
//                            log.DebugFormat("        Service id: {0} ({0:X}), type: {1}, name: {2}, provider: {3}", sli.service_id, sli.service_type, c.Servicename, c.Providername);
//                        else
//                            log.DebugFormat("        Service id: {0} ({0:X}), type: {1}", sli.service_id, sli.service_type);
//                    }
//                }
//            }
            m_LNB.save();
            MessageBox.Show("Scan complete, channels saved");
            scanner.stop();
            btnScan.Enabled = true;
            btnClose.Enabled = true;
            btnStop.Enabled = false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void printlinkagedetails(Linkage l)
        {
            List<Network> networks = scanner.networks;
            List<Transponder> transponders = getTranspondersFromNetwork(networks);
            List<Channel> channels = m_LNB.channels;

            log.DebugFormat("    Detailed info on: {0} service: {1}", l.transportstreamid, l.serviceid);
            Transponder tsp = transponders.Find(x => x.transportstreamid == l.transportstreamid);
            Channel c = channels.Find(x => x.service_id == l.serviceid);
            if (tsp != null)
            {
                log.DebugFormat("        on Transponder: {0}", tsp.frequencydecimal);
            }
            if (c != null)
            {
                log.DebugFormat("        service: {0}", c.Servicename);
            }
        }
        private List<Transponder> getTranspondersFromNetwork(List<Network> networks)
        {
            List<Transponder> _tsps = new List<Transponder>();
            foreach (Network n in networks)
            {
                List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
                foreach (Transponder t in nit)
                {
                    if (_tsps.Find(x => x.frequency == t.frequency && x.diseqcposition == t.diseqcposition) == null)
                        _tsps.Add(t);

                }
            }
            return _tsps;
        }

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
                    for (int i = 0; i < config.configitems.lnbs.Length; i++)
                    {
                        if (config.configitems.lnbs[i] != null)
                        {
                            LNB lnb = new LNB(config.configitems.lnbs[i].diseqcposition);
                            lnb.load();
                            allchannels.AddRange(lnb.channels);
                        }
                    }
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
        private void dumpInfo(Transponder tsp, List<Channel> channels)
        {
            foreach (Channel channel in channels)
            {
                log.DebugFormat("Provider: {0}\tService: {1} ({1:X})\tProgram PID: {2} ({2:X})\tService type: {3}",
                    channel.Providername, channel.Servicename, channel.Programpid, channel.Servicetype);
                //foreach (Sat2Ip.Stream pmt in channel.Pmt)
                //{
                //    System.Console.WriteLine("Elmentary PID: " + pmt.Elementary_pid.ToString("X2") + ", Type: " +
                //        pmt.Stream_type.ToString());
                //}
            }
        }

        private void cbUseNit_CheckedChanged(object sender, EventArgs e)
        {
            loadTransponders(null);
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

        private void cbFastscan_CheckedChanged(object sender, EventArgs e)
        {
            if (cbFastscan.Checked)
                btnScan.Enabled = true;
        }

        private void cmbTransponder_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbLNB.SelectedIndex < 0)
                return;
            else
                btnScan.Enabled=true;
        }
    }
}
