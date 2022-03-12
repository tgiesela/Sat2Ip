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
        private int frequency;
        private string polarisation;
        private int samplerate;
        private string errorcorrections;
        private string dvbtype;
        private string mtype;
        private SatUtils.SatelliteReader reader;
        private List<SatUtils.SatelliteInfo> m_listinfo;
        private SatUtils.SatelliteInfo info;
        private string iniFilename;
        private SatUtils.IniFile inifile;
        public FrmFindChannels()
        {

//            log4net.ILog log4 = log4net.LogManager.GetLogger
//            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
            List<Transponder> transponders = new List<Transponder>();
            info = reader.findSatelliteName(cmbLNB.SelectedItem.ToString());
            lnb = cmbLNB.SelectedIndex + 1;
            m_LNB = new SatUtils.LNB(lnb);
            iniFilename = reader.getTransponderIniFilename(info);
            inifile = new SatUtils.IniFile(System.IO.Path.GetFullPath(iniFilename));
            string[] sections = inifile.ReadSections();
            if (sections.Contains<string>("DVB") && sections.Contains<string>("SATTYPE")) {
                string nroftransponders = inifile.ReadValue("DVB", "0");
                cmbTransponder.Items.Clear();
                for (int i = 1; i <= int.Parse(nroftransponders);i++)
                {
                    string line = inifile.ReadValue("DVB", i.ToString());
                    cmbTransponder.Items.Add(line);
                    Transponder tsp = new Transponder();
                    extractInfoFromTransponder(line);
                    tsp.diseqcposition = lnb;
                    tsp.frequency = frequency;
                    tsp.samplerate = samplerate;
                    tsp.polarisationFromString(polarisation);
                    tsp.dvbsystemFromString(dvbtype);
                    tsp.fecFromString(errorcorrections);
                    tsp.mtypeFromString(mtype);
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

        private void cmbTransponder_SelectedIndexChanged(object sender, EventArgs e)
        {
            string transponder = cmbTransponder.SelectedItem.ToString();
            extractInfoFromTransponder(transponder);
        }

        private void extractInfoFromTransponder(string transponder)
        {
            char[] delimiterChars = { ' ', ',' };
            string[] parts = transponder.Split(delimiterChars);
            frequency = int.Parse(parts[0]);
            polarisation = parts[1];
            samplerate = int.Parse(parts[2]);
            errorcorrections = parts[3];
            dvbtype = parts[4];
            mtype = parts[5];
        }

        private async void btnScan_ClickAsync(object sender, EventArgs e)
        {
            //IntPtr pcScannerWrapper = cScannerWrapper_Create();
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            uribld.Host = Sat2IpGui.Properties.App.Default.IpAddressDevice;
            uribld.Port = int.Parse(Sat2IpGui.Properties.App.Default.PortDevice);
            RTSP rtsp = new RTSP(uribld.Uri);
            Scanner scanner = new Scanner(rtsp.Startport, rtsp.Endport, rtsp);
            if (cbScanAll.Checked)
            {
                string nroftransponders = inifile.ReadValue("DVB", "0");
                cmbTransponder.Items.Clear();
                int channelcount = 0;
                for (int i = 1; i <= int.Parse(nroftransponders); i++)
                {
                    extractInfoFromTransponder(inifile.ReadValue("DVB", i.ToString()));
                    txtTransponder.Text = frequency.ToString();

                    List<Channel> lChannels = new List<Channel>();
                    Transponder tsp = new Transponder();
                    await scanChannelsAsync(scanner, lChannels, tsp);
                    channelcount += lChannels.Count;
                    txtChannels.Text = channelcount.ToString();
                    this.Update();

                    m_LNB.setTransponder(tsp, lChannels);
                }
                m_LNB.save();
                MessageBox.Show("Scan complete, channels saved");
            }
            else
            {
                List<Channel> lChannels = new List<Channel>();
                Transponder tsp = new Transponder();
                await scanChannelsAsync(scanner, lChannels, tsp);

                m_LNB.setTransponder(tsp,lChannels);
                m_LNB.save();
                MessageBox.Show("Scan complete, channels saved");
            }
            List<Network> networks = scanner.networks;
            foreach (Network n in networks)
            {
                List<Transponder> nit = n.transponders.OrderBy(x => x.frequency).ToList();
                log.DebugFormat("Network: id {0}, name {1}", n.networkid, n.networkname);
                foreach (Transponder t in nit)
                {  
                    log.DebugFormat("Freq: {0}, polarization: {1}, symbolrate: {2}, orbit: {3}", t.frequency, t.polarisation, t.samplerate,Utils.Utils.bcdtohex(t.orbit));
                }

            }
            scanner.stop();
        }

        private async Task<int> scanChannelsAsync(Scanner scanner, List<Channel> lChannels, Transponder tsp)
        {
            tsp.diseqcposition = lnb;
            tsp.frequency = frequency;
            tsp.samplerate = samplerate;
            tsp.polarisationFromString(polarisation);
            tsp.dvbsystemFromString(dvbtype);
            tsp.fecFromString(errorcorrections);
            tsp.mtypeFromString(mtype);

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

        private void button1_Click(object sender, EventArgs e)
        {

        }
    }
}
