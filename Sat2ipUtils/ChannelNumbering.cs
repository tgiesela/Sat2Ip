using Interfaces;

namespace Sat2ipUtils
{
    [Serializable]
    public class ChannelNumbering
    {
        public FastScanBouquet fastscanlocation { get; set; }
        public Bouquet DVBBouquet { get; set; }
    }
}
