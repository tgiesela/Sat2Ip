using Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
    [Serializable]
    public class ChannelNumbering
    {
        public FastScanBouquet fastscanlocation { get; set; }
        public Bouquet DVBBouquet { get; set; }
    }
}
