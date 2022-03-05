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
using UPNPLib;
namespace Sat2IpGui
{
    public partial class frmServer : Form
    {
        private UPnP _devices;
        private UPnPDevice _selectedDevice;
        public frmServer()
        {
            InitializeComponent();
        }

        public UPnPDevice SelectedDevice { get => _selectedDevice; set => _selectedDevice = value; }

        private void BtnFindServer_Click(object sender, EventArgs e)
        {
            lbServers.Items.Clear();
            _devices = new UPnP();
            foreach (UPnPDevice device in _devices.Devices)
            {
                lbServers.Items.Add(device.FriendlyName);
            }
        }
        private void LbServers_Click(object sender, EventArgs e)
        {
            if (_devices == null)
            {
                return;
            }
            if (_devices.Devices.Count != 0)
            {
                int inx = lbServers.SelectedIndex;
                if (inx >= 0 && inx < lbServers.Items.Count)
                {
                    SelectedDevice = _devices.Devices[inx];
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
