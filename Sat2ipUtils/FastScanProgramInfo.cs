using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class FastScanProgramInfo
    {
        public ushort[] pmtpid { get; set; } = new ushort[5];
        public byte[] remainingbytes { get; set; } = new byte[3];
        public ushort network { get; set; }
        public ushort streamid { get; set; }
        public ushort serviceid { get; set; }
        public string packagename { get; set; }
        public string channelname { get; set; }
    }
}
