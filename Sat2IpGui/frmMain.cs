using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UPNPLib;
using Sat2Ip;
using Vlc.DotNet.Core;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Sat2IpGui.SatUtils;
using Interfaces;
using System.Threading.Tasks;

namespace Sat2IpGui
{
    public partial class frmMain : Form
    {
        private UPnPDevice m_device;
        private Channel m_selectedchannel;
        private ListBoxItem m_selecteditem;
        private RTSP rtsp;
        private Descrambler.Descrambler descrambler;
        private List<Channel> channels = new();
        private Process VLC;
        private Config config = new Config();
        private List<Channel> m_unfilteredchannels;
        private ChannelFilter m_cf;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public class ListBoxItem
        {
            public Object obj;
            public String textToDisplay;
            public override string ToString()
            {
                return textToDisplay;
            }
        }
        public frmMain()
        {
            InitializeComponent();
            config.load();
            m_cf = config.configitems.channelFilter;

            LoadChannels();
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            if (config.configitems.IpAddressDevice == null)
            {
                openServerConfig();
            }
            uribld.Host = config.configitems.IpAddressDevice;
            uribld.Port = int.Parse(config.configitems.PortDevice);
            rtsp = new RTSP(uribld.Uri);
            if (config.configitems.FixedTuner)
            {
                // rtsp.frontend = rtsp.getFreeTuner();
                rtsp.frontend = (int)config.configitems.TunerNumber;
            }
            VLC = new System.Diagnostics.Process();
            VLC.StartInfo.FileName = myVlcControl.VlcLibDirectory.FullName + "\\vlc.exe";
            VLC.StartInfo.Arguments = "-vvv rtp://127.0.0.1:40002";
        }
        private void loadChannelsFromTransponder(SatUtils.LNB lnb)
        {
            if (lnb == null)
                return;
            lnb.load();
            foreach (Channel c in lnb.channels)
            {
                channels.Add(c);
            }
        }
        private void LoadChannels()
        {
            channels.Clear();
            for (int i = 0; i < config.configitems.lnbs.Length; i++)
            {
                loadChannelsFromTransponder(config.configitems.lnbs[i]);
            }
            lbChannels.Items.Clear();
            channels = channels.OrderBy(c => c.Servicename).ToList();
            assignChannelNumbers();
            filterChannels();
            populateChannels();
        }
        private void populateChannels()
        {
            lbChannels.Items.Clear();
            foreach (Channel c in channels)
            {
                if (c.isDataService() && !m_cf.Data) { continue; };
                if (c.isRadioService() && !m_cf.Radio) { continue; };
                if (c.isTVService() && !m_cf.TV) { continue; };
                if ((c.free_CA_mode != 0) && m_cf.FTA) { continue; };
                if (m_cf.provider != null)
                    if (m_cf.provider != String.Empty)
                        if (!c.Providername.Equals(m_cf.provider, StringComparison.OrdinalIgnoreCase)) { continue; };
                if (m_cf.frequency != null)
                    if (m_cf.frequency.frequency != c.transponder.frequency) { continue; };
                if (m_cf.lnb != null)
                    if (m_cf.lnb.diseqcposition != c.transponder.diseqcposition) { continue; };
                if (c.Programpid == 0) { continue; };
                if (c.Servicetype == 0) { continue; };
                ListBoxItem item = new ListBoxItem();
                item.obj = c;
                item.textToDisplay = c.Servicename;
                lbChannels.Items.Add(item);
            }
        }
        private void btnPlay_Click(object sender, EventArgs e)
        {
            Play();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private async void Play()
        {
            int inx = lbChannels.SelectedIndex;
            if (inx < 0)
                return;
            if (inx < lbChannels.Items.Count)
            {
                ListBoxItem item = (ListBoxItem)lbChannels.Items[inx];
                Channel pid = (Channel)item.obj;
                if (pid.Pmtpresent == false)
                {
                    Scanner scanner = new Scanner(rtsp.Startport, rtsp.Endport, rtsp);
                    Channel channeltask = await scanner.scanChannel(pid);
                }
                String stream = pid.getPlayString() + ",17";
                log.Debug("stream: " + stream);
                try
                {
                    if (descrambler == null)
                    {
                        descrambler = new Descrambler.Descrambler(rtsp.Startport, rtsp.Startport + 2);
                        int oscamport;
                        Int32.TryParse(config.configitems.OscamPort, out oscamport);
                        descrambler.setOscam(config.configitems.OscamServer, oscamport);
                    }
                    else
                    {
                        descrambler.stop();
                        rtsp.commandTeardown("");
                        System.Threading.Thread.Sleep(1000);
                    }
                    descrambler.setChannel(pid);
                    rtsp.commandSetup(stream);
                }
                catch (Exception se)
                {
                    log.Debug("Cannot connect to OSCAM client. Is it running?" + se.Message);
                }
                var uri = new Uri(string.Format(@"rtp://{0}:{1}", "127.0.0.1", rtsp.Startport + 2));
                log.Debug("URI: " + uri.ToString());
                rtsp.commandPlay("");
                descrambler.play();
                myVlcControl.Show();
                myVlcControl.Play(uri);
                myVlcControl.Audio.Volume = 30;
                myVlcControl.VlcMediaPlayer.Audio.Volume = 30;
            }
        }

        private void OnVlcMediaPlayerLog(object sender, VlcMediaPlayerLogEventArgs e)
        {
            string message = string.Format("libVlc : {0} {1} @ {2}", e.Level, e.Message, e.Module);
            System.Diagnostics.Debug.WriteLine(message);
        }
        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        private void Stop()
        {
            myVlcControl.Stop();
            this.myVlcControl.Log -= this.OnVlcMediaPlayerLog;
            if (descrambler != null)
            {
                descrambler.stop();
                rtsp.commandTeardown("");
            }
        }
        private void ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openServerConfig();
        }
        private void openServerConfig()
        {
            frmServer server = new frmServer();
            DialogResult result = server.ShowDialog();
            if (result == DialogResult.OK)
            {
                m_device = server.SelectedDevice;
                Uri ip = new Uri(m_device.PresentationURL);
                config.configitems.IpAddressDevice = ip.Host;
                config.configitems.PortDevice = ip.Port.ToString();
                config.save();
            }

        }
        private void satelliteSetupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmConfig config = new FrmConfig();
            DialogResult result = config.ShowDialog();
            if (result == DialogResult.OK)
            {
            }
        }
        private void findChannelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmFindChannels findchannels = new FrmFindChannels();
            DialogResult result = findchannels.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadChannels();
            }
        }
        private void lbChannels_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbChannels.SelectedItem != null)
            {
                btnPlay.Enabled = true;
                m_selecteditem = (ListBoxItem)lbChannels.SelectedItem;
                m_selectedchannel = (Channel)m_selecteditem.obj;
            }
            else
            {
                btnPlay.Enabled = false;
            }
        }
        private void myVlcControl_VlcLibDirectoryNeeded(object sender, Vlc.DotNet.Forms.VlcLibDirectoryNeededEventArgs e)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            e.VlcLibDirectory = new DirectoryInfo(Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432"), @"VideoLan\VLC\"));

            if (!e.VlcLibDirectory.Exists)
            {
                var folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.Description = "Select Vlc libraries folder.";
                folderBrowserDialog.RootFolder = Environment.SpecialFolder.Desktop;
                folderBrowserDialog.ShowNewFolderButton = true;
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    e.VlcLibDirectory = new DirectoryInfo(folderBrowserDialog.SelectedPath);
                }
            }
        }
        private void lbChannels_DoubleClick(object sender, EventArgs e)
        {
            Stop();
            Play();
        }
        private void cbRadio_CheckedChanged(object sender, EventArgs e)
        {
            populateChannels();
        }
        private void cbTV_CheckedChanged(object sender, EventArgs e)
        {
            populateChannels();
        }
        private void cbData_CheckedChanged(object sender, EventArgs e)
        {
            populateChannels();
        }
        private void myVlcControl_Click(object sender, EventArgs e)
        {

        }
        private void btnVLC_Click(object sender, EventArgs e)
        {
            if (myVlcControl.IsPlaying)
            {
                myVlcControl.Stop();
            }
            VLC.Start();
        }

        private void myVlcControl_VideoOutChanged(object sender, VlcMediaPlayerVideoOutChangedEventArgs e)
        {
            myVlcControl.VlcMediaPlayer.Audio.Volume = 30;
        }
        private void assignToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmBouquet frmbouquet = new frmBouquet();
            DialogResult rslt = frmbouquet.ShowDialog();
            if (rslt == DialogResult.OK)
            {
                assignChannelNumbers();
            }
        }
        private void assignChannelNumbers()
        {
            config.load();
            ChannelNumbering cn = config.configitems.channelNumbering;
            if (cn != null)
            {
                if (cn.fastscanlocation != null)
                {
                    cn.fastscanlocation.assign(channels);
                    channels = channels.OrderBy(c => c.lcn).ToList();
                }
                else
                {
                    if (cn.DVBBouquet != null)
                    {
                        cn.DVBBouquet.assign(channels);
                        channels = channels.OrderBy(c => c.lcn).ToList();
                    }
                    else
                    {
                        channels = channels.OrderBy(c => c.Servicename).ToList();
                        foreach (Channel c in channels)
                        {
                            c.lcn = 0;
                        }
                    }
                }
            }
            populateChannels();
        }
        private void filterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (m_unfilteredchannels == null)
                m_unfilteredchannels = channels;
            frmFilter frmfilter = new frmFilter();
            DialogResult rslt = frmfilter.ShowDialog();
            if (rslt == DialogResult.OK)
            {
                filterChannels();
                populateChannels();
            }
        }
        private void filterChannels()
        {
            config.load();
            m_cf = config.configitems.channelFilter;
            if (m_cf != null)
            {
                if (m_cf.fastScanBouquet != null)
                {
                    m_cf.fastScanBouquet.assign(channels);
                    channels = channels.OrderBy(c => c.lcn).ToList();
                }
            }
        }
    }
}
