using Interfaces;
using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    [Serializable]
    public class ChannelFilter
    {
        public LNB lnb { get; set; }
        public Transponder frequency { get; set; }
        public string provider { get; set; }
        public FastScanBouquet fastScanBouquet { get; set; }
        public Bouquet DVBBouquet { get; set; }
        public bool TV { get; set; }
        public bool Radio { get; set; }
        public bool Data { get; set; }
        public bool FTA { get; set; }
    }
}
