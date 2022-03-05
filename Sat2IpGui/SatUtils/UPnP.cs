using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPNPLib;

namespace Sat2IpGui.SatUtils
{
    class UPnP
    {
        private IList<UPnPDevice> _devices = new List<UPnPDevice>();
        private string _typeuri = "urn:ses-com:device:SatIPServer:1";
        public UPnP()
        {
            UPnPDeviceFinder finder = new UPnPDeviceFinder();
            UPnPDevices devices = finder.FindByType(_typeuri, 0);
            foreach (UPnPDevice device in devices)
            {
                Devices.Add(device);
                if (device.Description != null)
                {
                    Console.WriteLine(device.Description);
                }
                Console.WriteLine(device.ManufacturerName);
                Console.WriteLine(device.ManufacturerURL);
                Console.WriteLine(device.ModelName);
                Console.WriteLine(device.PresentationURL);
            }
            if (Devices.Count == 0)
            {
                throw new Exception("SAT>IP server not found");
            }
        }
        public IList<UPnPDevice> Devices
        {
            get
            {
                return _devices;
            }

            set
            {
                _devices = value;
            }
        }
    }
}
