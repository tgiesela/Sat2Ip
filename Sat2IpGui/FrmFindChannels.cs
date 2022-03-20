using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sat2Ip;
using Microsoft.Extensions.Logging;
using Interfaces;


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
        public FrmFindChannels()
        {

            InitializeComponent();
            cmbTransponder.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbLNB.DropDownStyle = ComboBoxStyle.DropDownList;
            if (Properties.App.Default.LNB1 != "")
                cmbLNB.Items.Add(Properties.App.Default.LNB1);
            if (Properties.App.Default.LNB2 != "")
                cmbLNB.Items.Add(Properties.App.Default.LNB2);
            if (Properties.App.Default.LNB3 != "")
                cmbLNB.Items.Add(Properties.App.Default.LNB3);
            if (Properties.App.Default.LNB4 != "")
                cmbLNB.Items.Add(Properties.App.Default.LNB4);

            reader = new SatUtils.SatelliteReader();
            m_listinfo = reader.read(@"satellites.csv");
            btnScan.Enabled = false;
        }

        private void cbScanAll_CheckedChanged(object sender, EventArgs e)
        {
            cmbTransponder.Enabled = !cbScanAll.Checked;
        }

        private void cmbLNB_SelectedValueChanged(object sender, EventArgs e)
        {
            if (cbUseNit.Checked)
                populateTranspondersNIT();
            else
                populateTransponders();
        }

        private void populateTransponders()
        {
            List<Transponder> transponders = new List<Transponder>();
            info = reader.findSatelliteName(cmbLNB.SelectedItem.ToString());
            lnb = cmbLNB.SelectedIndex + 1;
            m_LNB = new SatUtils.LNB(lnb);
            iniFilename = reader.getTransponderIniFilename(info);
            inifile = new SatUtils.IniFile(System.IO.Path.GetFullPath(iniFilename));
            string[] sections = inifile.ReadSections();
            cmbTransponder.Items.Clear();
            if (sections.Contains<string>("DVB") && sections.Contains<string>("SATTYPE"))
            {
                string nroftransponders = inifile.ReadValue("DVB", "0");
                cmbTransponder.Items.Clear();
                for (int i = 1; i <= int.Parse(nroftransponders); i++)
                {
                    string line = inifile.ReadValue("DVB", i.ToString());
                    cmbTransponder.Items.Add(line);
                    Transponder tsp = extractInfoFromTransponder(line);
                    transponders.Add(tsp);
                }
                btnScan.Enabled = true;
            }
            else
            {
                MessageBox.Show("Ini file for satellite not valid: " + iniFilename);
                inifile = null;
                btnScan.Enabled = false;
            }
            m_LNB.transponders = transponders;
            m_LNB.save();
        }

        private void populateTranspondersNIT()
        {
            List<Transponder> transponders = new List<Transponder>();
            lnb = cmbLNB.SelectedIndex + 1;
            m_LNB = new SatUtils.LNB(lnb);
            cmbTransponder.Items.Clear();
            foreach (Network netw in m_LNB.networks)
            {
                foreach (Transponder t in netw.transponders)
                {
                    if (transponders.Find(x => x.frequency == t.frequency && x.diseqcposition == t.diseqcposition) == null)
                        transponders.Add(t);
                    else
                        log.Debug("Duplicate transponder skipped: " + t.frequency);
                }
            }
            transponders = transponders.OrderBy(x => x.frequency).ToList();
            foreach (Transponder t in transponders)
            {
                string freq = t.frequencydecimal.Value.ToString(System.Globalization.CultureInfo.CreateSpecificCulture("en-us"));
                string line = String.Format("{0},{1},{2},{3},{4},{5}", freq, (char)t.polarisation, t.samplerate, (int)t.fec, t.dvbsystem, t.mtype);
                cmbTransponder.Items.Add(line);
            }

            btnScan.Enabled = true;
            m_LNB.transponders = transponders;
            m_LNB.save();
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
            tsp.diseqcposition = lnb;
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
            uribld.Host = Sat2IpGui.Properties.App.Default.IpAddressDevice;
            uribld.Port = int.Parse(Sat2IpGui.Properties.App.Default.PortDevice);
            RTSP rtsp = new RTSP(uribld.Uri);
            scanner = new Scanner(rtsp.Startport, rtsp.Endport, rtsp);
            btnStop.Enabled = true;
            scanning = true;
            if (cbScanAll.Checked)
            {
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
                    }
                }

            }
            else
            {
                List<Channel> lChannels = new List<Channel>();
                string transponder = cmbTransponder.SelectedItem.ToString();
                Transponder tsp = extractInfoFromTransponder(transponder);
                await scanChannelsAsync(scanner, lChannels, tsp);

                if (lChannels.Count > 0)
                {
                    m_LNB.setTransponder(tsp, lChannels);
                    m_LNB.networks = scanner.networks;
                }
            }
            m_LNB.save();
            MessageBox.Show("Scan complete, channels saved");

            List<Network> networks = scanner.networks;
            foreach (Network n in networks)
            {
                List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
                log.DebugFormat("Network: id {0}, name {1}", n.networkid, n.networkname);
                foreach (Transponder t in nit)
                {  
                    log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: {3}", t.frequency, t.polarisation, t.samplerate,Utils.Utils.bcdtohex(t.orbit));
                }
                m_LNB.save();
            }
            scanner.stop();
            btnScan.Enabled = true;
            btnClose.Enabled = true;
            btnStop.Enabled = false;
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
            if (cbUseNit.Checked)
            {
                populateTranspondersNIT();
            }
            else
            {
                populateTransponders();
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
    }
}
