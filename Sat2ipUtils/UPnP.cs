using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UPNPLib;

namespace Sat2ipUtils
{
    [Serializable]
    public class Sat2ipserver
    {
        public string ManufacturerName { get; set; }
        public string ManufacturerURL { get; set; }
        public string ModelName { get; set; }
        public string PresentationURL { get; set; }
        public string Description { get; set; }
        public string FriendlyName { get; set; }
        public string ModelNumber { get; set; }
        public string ModelURL { get; set; }
        public string SerialNumber { get; set; }
        public string UniqueDeviceName { get; set; }
        public string UPC { get; set; }
    }
    public class UPnP
    {
        private List<Sat2ipserver> _sat2ipservers = new List<Sat2ipserver>();
        private readonly string _typeuri = "urn:ses-com:device:SatIPServer:1";
        public UPnP()
        {
            UPnPDeviceFinder finder = new();
            UPnPDevices devices = finder.FindByType(_typeuri, 0);
            foreach (UPnPDevice device in devices)
            {
                Sat2ipserver server = new()
                {
                    ManufacturerName = device.ManufacturerName,
                    ManufacturerURL = device.ManufacturerURL,
                    ModelName = device.ModelName,
                    PresentationURL = device.PresentationURL,
                    Description = device.Description,
                    FriendlyName = device.FriendlyName,
                    ModelNumber = device.ModelNumber,
                    ModelURL = device.ModelURL,
                    SerialNumber = device.SerialNumber,
                    UniqueDeviceName = device.UniqueDeviceName,
                    UPC = device.UPC
                };
                _sat2ipservers.Add(server);
            }
            if (_sat2ipservers.Count == 0)
            {
                throw new Exception("SAT>IP server not found");
            }
        }
        public List<Sat2ipserver> Sat2ipServers
        {
            get
            {
                return _sat2ipservers;
            }

            set
            {
                _sat2ipservers = value;
            }
        }
    }
}
