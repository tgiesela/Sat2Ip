using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using UPNPLib;
using Sat2Ip;
using Vlc.DotNet.Core;
using System.Reflection;
using System.IO;
using Descrambler;

namespace Sat2IpGui
{
    public partial class frmMain : Form
    {
        private UPnPDevice m_device;
        private SatUtils.LNB m_LNB1;
        private SatUtils.LNB m_LNB2;
        private SatUtils.LNB m_LNB3;
        private SatUtils.LNB m_LNB4;
        private Channel m_selectedchannel;
        private ListBoxItem m_selecteditem;
        private String _currentpids = String.Empty;
        private RTSP rtsp;
        private DescramblerNew descrambler;
        List<Channel> channels = new();
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
            cbTV.Checked = true;
            LoadChannels();
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            uribld.Host = Sat2IpGui.Properties.App.Default.IpAddressDevice;
            uribld.Port = int.Parse(Sat2IpGui.Properties.App.Default.PortDevice);
            rtsp = new RTSP(uribld.Uri);
        }
        private void loadChannelsFromTransponder(SatUtils.LNB lnb)
        {
            if (lnb == null)
                return;
            //List<House> houseOnes = houses.FindAll(house => house.Name == "House 1");
            //List<House> houseOnes = houses.Where(house => house.Name == "House 1").ToList();
            foreach (Transponder trs in lnb.getTransponders())
            {
                foreach (Channel c in lnb.getChannelsOnTransponder(trs.frequency))
                {
                    if (c.isDataService() & !cbData.Checked) { continue; };
                    if (c.isRadioService() & !cbRadio.Checked) { continue; };
                    if (c.isTVService() & !cbTV.Checked) { continue; };
                    if (c.Programnumber == 0) { continue;  };
                    if (c.Servicetype == 0) { continue; };
                    channels.Add(c);
                }
            }
        }
        private void LoadChannels()
        {
            channels.Clear();
            lbChannels.Items.Clear();
            if (Properties.App.Default.LNB1 != "")
            {
                m_LNB1 = new SatUtils.LNB(0);
                loadChannelsFromTransponder(m_LNB1);
            }
            if (Properties.App.Default.LNB2 != "")
            {
                m_LNB2 = new SatUtils.LNB(1);
                loadChannelsFromTransponder(m_LNB2);
            }
            if (Properties.App.Default.LNB3 != "")
            {
                m_LNB3 = new SatUtils.LNB(2);
                loadChannelsFromTransponder(m_LNB3);
            }
            if (Properties.App.Default.LNB4 != "")
            {
                m_LNB4 = new SatUtils.LNB(3); ;
                loadChannelsFromTransponder(m_LNB4);
            }
            channels = channels.OrderBy(c => c.Servicename).ToList();
            lbChannels.Items.Clear();
            foreach (Channel c in channels)
            {
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
        private void Play()
        {
            int inx = lbChannels.SelectedIndex;
            if (inx < 0)
                return;
            if (inx < lbChannels.Items.Count)
            {
                ListBoxItem item = (ListBoxItem)lbChannels.Items[inx];
                Channel pid = (Channel)item.obj;
                String stream = pid.getPlayString();
                log.Debug("stream: " + stream);
                _currentpids = pid.getPidString();
                try
                {
                    if (descrambler == null)
                    {
                        descrambler = new Descrambler.DescramblerNew(rtsp.Startport, pid, rtsp.Startport + 2);
                    }
                    else
                    {
                        descrambler.stop();
                        rtsp.commandTeardown("");
                        System.Threading.Thread.Sleep(1000);
                    }
                    rtsp.commandSetup(stream);
                }
                catch (Exception se)
                {
                    log.Debug("Cannot connect to OSCAM client. Is it running?" + se.Message);
                }
                //var uri = new Uri(string.Format(@"rtp://{0}:{1}", rtsp.Destination, rtsp.Startport + 2));
                var uri = new Uri(string.Format(@"rtp://{0}:{1}", "127.0.0.1", rtsp.Startport + 2));
                log.Debug("URI: " + uri.ToString());
                rtsp.commandPlay("");
                descrambler.play();
                myVlcControl.Play(uri);
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
            descrambler.stop();
            rtsp.commandTeardown("");
            //cDescramblerWrapper_StopDescrambler(pcDescramblerWrapper);
        }

        private void ServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frmServer server = new frmServer();
            DialogResult result = server.ShowDialog();
            if (result == DialogResult.OK)
            {
                m_device = server.SelectedDevice;
                Uri ip = new Uri(m_device.PresentationURL);
                Properties.App.Default.IpAddressDevice = ip.Host;
                Properties.App.Default.Save();
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
            LoadChannels();
        }

        private void cbTV_CheckedChanged(object sender, EventArgs e)
        {
            LoadChannels();
        }

        private void cbData_CheckedChanged(object sender, EventArgs e)
        {
            LoadChannels();
        }
    }
}
