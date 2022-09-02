using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sat2IpGui.SatUtils;
using Sat2ipUtils;
using UPNPLib;
namespace Sat2IpGui
{
    public partial class frmServer : Form
    {
        private UPnP _servers;
        private Sat2ipserver _selectedServer;
        public frmServer()
        {
            InitializeComponent();
            btnOk.Enabled = false;
        }

        public Sat2ipserver SelectedDevice { get => _selectedServer; set => _selectedServer = value; }
        public List<Sat2ipserver> Sat2ipservers { get; set; }

        private void BtnFindServer_Click(object sender, EventArgs e)
        {
            lbServers.Items.Clear();
            btnOk.Enabled = false;
            _servers = new UPnP();
            foreach (Sat2ipserver server in _servers.Sat2ipServers)
            {
                lbServers.Items.Add(server.FriendlyName);
            }
            if (_servers.Sat2ipServers.Count > 0)
            {
                Sat2ipservers = _servers.Sat2ipServers;
            }
        }
        private void LbServers_Click(object sender, EventArgs e)
        {
            if (_servers == null)
            {
                return;
            }
            btnOk.Enabled = true;
            if (_servers.Sat2ipServers.Count != 0)
            {
                int inx = lbServers.SelectedIndex;
                if (inx >= 0 && inx < lbServers.Items.Count)
                {
                    SelectedDevice = _servers.Sat2ipServers[inx];
                    txtServer.Text = SelectedDevice.PresentationURL;
                    txtDescription.Text = SelectedDevice.Description;
                    txtFriendlyName.Text=  SelectedDevice.FriendlyName;
                    txtManufacturerName.Text = SelectedDevice.ManufacturerName;
                    txtManufacturerURL.Text = SelectedDevice.ManufacturerURL;
                    txtModelName.Text = SelectedDevice.ModelName;
                    txtModelNumber.Text = SelectedDevice.ModelNumber;
                    txtModelURL.Text = SelectedDevice.ModelURL;
                    txtSerialNumber.Text = SelectedDevice.SerialNumber;
                    txtUniqueDeviceName.Text = SelectedDevice.UniqueDeviceName;
                    txtUPC.Text = SelectedDevice.UPC;

                }
            }
        }
    }
}
