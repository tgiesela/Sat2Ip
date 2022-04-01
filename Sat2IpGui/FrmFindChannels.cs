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

namespace Sat2IpGui
{

    public partial class FrmFindChannels : Form
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int lnb;
        private SatUtils.LNB m_LNB;
        private SatUtils.SatelliteReader reader;
        private List<SatUtils.SatelliteInfo> m_listinfo;
        private SatUtils.SatelliteInfo info;
        private string iniFilename;
        private SatUtils.IniFile inifile;
        private Scanner scanner;
        private bool scanning = false;
        private Config config = new Config();
        private List<Transponder> m_transponders;
        private FastScanLocation m_fastscanlocation;

        public FrmFindChannels()
        {
            InitializeComponent();
            config.load();
            for (int i = 0; i < config.configitems.lnb.Length; i++)
            {
                if (config.configitems.lnb[i] != null)
                    cmbLNB.Items.Add(config.configitems.lnb[i].satellitename);
            }

            cmbTransponder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLNB.DropDownStyle = ComboBoxStyle.DropDownList;

            reader = new SatUtils.SatelliteReader();
            m_listinfo = reader.read(@"satellites.csv");
            btnScan.Enabled = false;
            m_transponders = new List<Transponder>();
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
            populateCmbTransponders();
        }
        private void loadTransponders(FastScanLocation location)
        {
            m_transponders.Clear();
            if (cbUseNit.Checked)
                loadTranspondersNIT();
            else
                loadTranspondersFromIniFile(location);
        }
        private void loadTranspondersFromIniFile(FastScanLocation location)
        {
            if (location != null)
            {
                info = reader.findSatelliteOrbit(location.position);
                m_LNB = new LNB(location.diseqcposition);
                m_LNB.load();
            }
            else
            {
                m_LNB = getLNBFromCombobox();
                info = reader.findSatelliteName(cmbLNB.SelectedItem.ToString());
            }
            iniFilename = reader.getTransponderIniFilename(info);
            inifile = new SatUtils.IniFile(System.IO.Path.GetFullPath(iniFilename));
            string[] sections = inifile.ReadSections();
            m_transponders.Clear();
            if (sections.Contains<string>("DVB") && sections.Contains<string>("SATTYPE"))
            {
                string nroftransponders = inifile.ReadValue("DVB", "0");
                for (int i = 1; i <= int.Parse(nroftransponders); i++)
                {
                    string line = inifile.ReadValue("DVB", i.ToString());
                    Transponder tsp = extractInfoFromTransponder(line);
                    m_transponders.Add(tsp);
                }
            }
            else
            {
                MessageBox.Show("Ini file for satellite not valid: " + iniFilename);
                inifile = null;
            }
        }
        private void loadTranspondersNIT()
        {
            lnb = cmbLNB.SelectedIndex + 1;
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
        private void populateCmbTransponders()
        {
            cmbTransponder.Items.Clear();
            m_transponders = m_transponders.OrderBy(x => x.frequency).ToList();
            foreach (Transponder t in m_transponders)
            {
                string freq = t.frequencydecimal.Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                string line = string.Format("{0},{1},{2},{3},{4},{5}", freq, (char)t.polarisation, t.samplerate, (int)t.fec, t.dvbsystem, t.mtype);
                cmbTransponder.Items.Add(line);
            }
        }
        private LNB getLNBFromCombobox()
        {
            LNB lnb = null;
            for (int i = 0; i < config.configitems.lnb.Length; i++)
            {
                if (config.configitems.lnb[i].satellitename == cmbLNB.SelectedItem.ToString())
                {
                    lnb = config.configitems.lnb[i];
                    lnb.load();
                    break;
                }
            }
            if (lnb == null)
                MessageBox.Show("Could not locate LNB from combobox");
            return lnb;
        }
        private Transponder getTransponderForFastscan(FastScanLocation location)
        {
            LNB lnb;
            for (int i = 0; i < config.configitems.lnb.Length; i++)
            {
                if (config.configitems.lnb[i] != null)
                {
                    lnb = config.configitems.lnb[i];
                    if (lnb.orbit() == location.position)
                    {
                        lnb.load();
                        if (cbUseNit.Checked)
                        {
                            foreach (Transponder t in lnb.transponders)
                            {
                                if (t.frequency == location.frequency)
                                {
                                    return t;
                                }
                            }
                        }
                        else
                        {
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
            }
            return null;
        }
        private Transponder extractInfoFromTransponder(string transponder)
        {
            Transponder tsp = new Transponder();
            char[] delimiterChars = { ' ', ',' };
            string[] parts = transponder.Split(delimiterChars);
            decimal frequencydecimal = decimal.Parse(parts[0], System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
            int frequency = (int)frequencydecimal;
            string polarisation = parts[1];
            int samplerate = int.Parse(parts[2]);
            string errorcorrections = parts[3];
            string dvbtype = parts[4];
            string mtype = parts[5];
            tsp.diseqcposition = m_LNB.diseqcposition;
            tsp.frequency = (int)frequencydecimal;
            tsp.frequencydecimal = frequencydecimal;
            tsp.samplerate = samplerate;
            tsp.polarisationFromString(polarisation);
            tsp.dvbsystemFromString(dvbtype);
            tsp.fecFromString(errorcorrections);
            tsp.mtypeFromString(mtype);
            return tsp;
        }

        private async void btnScan_ClickAsync(object sender, EventArgs e)
        {
            btnScan.Enabled = false;
            btnClose.Enabled = false;
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            uribld.Host = config.configitems.IpAddressDevice;
            uribld.Port = int.Parse(config.configitems.PortDevice);
            RTSP rtsp = new RTSP(uribld.Uri);
            rtsp.frontend = rtsp.getFreeTuner();
            rtsp.frontend = 4;
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
                    Transponder tsp = extractInfoFromTransponder(cmbTransponder.Items[i].ToString());
                    txtTransponder.Text = tsp.frequency.ToString();
                    List<Channel> lChannels = new List<Channel>();
                    await scanChannelsAsync(scanner, lChannels, tsp);
                    channelcount += lChannels.Count;
                    txtChannels.Text = channelcount.ToString();
                    this.Update();
                    if (lChannels.Count > 0)
                    {
                        m_LNB.setTransponder(tsp, lChannels);
                        m_LNB.networks = scanner.networks;
                        m_LNB.bouquets = scanner.bouquets;
                        m_LNB.transponders = getTranspondersFromNetwork(m_LNB.networks);
                    }
                }
            }
            else
            {
                if (cmbLNB.SelectedIndex >= 0 && cmbTransponder.SelectedIndex >= 0)
                {
                    List<Channel> lChannels = new List<Channel>();
                    string transponder = cmbTransponder.SelectedItem.ToString();
                    Transponder tsp = extractInfoFromTransponder(transponder);
                    await scanChannelsAsync(scanner, lChannels, tsp);
    
                    if (lChannels.Count > 0)
                    {
                        m_LNB.setTransponder(tsp, lChannels);
                        m_LNB.networks = scanner.networks;
                        m_LNB.bouquets = scanner.bouquets;
                        m_LNB.transponders = getTranspondersFromNetwork(m_LNB.networks);
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

            List<Network> networks = scanner.networks;
            List<Bouquet> bouquet = scanner.bouquets;
            List<Channel> channels = m_LNB.channels;
            List<Transponder> transponders = getTranspondersFromNetwork(networks);
            foreach (Network n in networks)
            {
                List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
                log.DebugFormat("Network: id {0}, name {1}", n.networkid, n.networkname);
                log.Debug("  Bouquets");
                foreach (Linkage l in n.bouquetlinkages)
                {
                    printlinkagedetails(l);
                }
                log.Debug("  EPG Info");
                foreach (Linkage l in n.epglinkages)
                {
                    printlinkagedetails(l);
                }
                log.Debug("  Service Info");
                foreach (Linkage l in n.silinkages)
                {
                    printlinkagedetails(l);
                }
                foreach (Transponder t in nit)
                {  
                    if (t.orbit != null)
                        log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: {3}, id: {4}", t.frequency, t.polarisation, t.samplerate,Utils.Utils.bcdtohex(t.orbit), t.transportstreamid);
                    else
                        log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: ??, id: {3}", t.frequency, t.polarisation, t.samplerate, t.transportstreamid);
                }
            }
            foreach (Bouquet b in bouquet)
            {
                log.DebugFormat("Bouquet id: {0} ({1}), found on transponder {2}, LNB {3}", b.bouquet_id, b.bouquet_name, b.transponder.frequency, b.transponder.diseqcposition);
                if (b.bouquetlinkage != null)
                {
                    printlinkagedetails(b.bouquetlinkage);
                }
                foreach (BatStream bs in b.streams)
                {
                    log.DebugFormat("    Stream id: {0} ({0:X}) network: {1})", bs.streamid, bs.original_networkid);
                    foreach (ServiceListItem sli in bs.services)
                    {
                        Channel c = channels.Find(x => x.Programnumber == sli.service_id);
                        if (c != null)
                            log.DebugFormat("        Service id: {0} ({0:X}), type: {1}, name: {2}, provider: {3}", sli.service_id, sli.service_type, c.Servicename, c.Providername);
                        else
                            log.DebugFormat("        Service id: {0} ({0:X}), type: {1}", sli.service_id, sli.service_type);
                    }
                }
            }
            m_LNB.save();
            MessageBox.Show("Scan complete, channels saved");
            scanner.stop();
            btnScan.Enabled = true;
            btnClose.Enabled = true;
            btnStop.Enabled = false;
        }

        private void printlinkagedetails(Linkage l)
        {
            List<Network> networks = scanner.networks;
            List<Transponder> transponders = getTranspondersFromNetwork(networks);
            List<Channel> channels = m_LNB.channels;

            log.DebugFormat("    Detailed info on: {0} service: {1}", l.transportstreamid, l.serviceid);
            Transponder tsp = transponders.Find(x => x.transportstreamid == l.transportstreamid);
            Channel c = channels.Find(x => x.Programnumber == l.serviceid);
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
                    for (int i = 0; i < config.configitems.lnb.Length; i++)
                    {
                        if (config.configitems.lnb[i] != null)
                        {
                            LNB lnb = new LNB(config.configitems.lnb[i].diseqcposition);
                            lnb.load();
                            allchannels.AddRange(lnb.channels);
                        }
                    }
                    log.DebugFormat("\nFastscan results for {0}\n", scantask.location.name);
                    foreach (ServiceListItem service in services)
                    {
                        List<Channel> cs = allchannels.FindAll(x => x.Programnumber == service.service_id && x.transponder.transportstreamid == service.transportstreamid);
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
                }
            }
            return scantask;
        }
        private void dumpInfo(Transponder tsp, List<Channel> channels)
        {
            foreach (Channel channel in channels)
            {
                log.Debug("Provider: " + channel.Providername +
                    "\tService: " + channel.Servicename + 
                    "\tProgram number: " + channel.Programnumber.ToString("X2") + 
                    "\tProgram PID: " + channel.Programpid.ToString("X2") +
                    "\tService type: " + channel.Servicetype.ToString());
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
            populateCmbTransponders();
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
