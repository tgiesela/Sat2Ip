using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class FastScanChannels : DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<FastScanProgramInfo> m_programs;
        public FastScanChannels()
        {
            m_programs = new List<FastScanProgramInfo>();
        }
        public List<FastScanProgramInfo> programInfos { get {return m_programs; } set {m_programs = value; } }
        protected override void processsection(Span<byte> span)
        {
            int bytesprocessed = 0;
            while (bytesprocessed < span.Length - 4)
            {
                FastScanProgramInfo pi = new FastScanProgramInfo();
                int elementlen;
                pi.network = Utils.Utils.toShort(span[bytesprocessed + 0], span[bytesprocessed + 1]);
                pi.streamid = Utils.Utils.toShort(span[bytesprocessed + 2], span[bytesprocessed + 3]);
                pi.serviceid = Utils.Utils.toShort(span[bytesprocessed + 4], span[bytesprocessed + 5]);
                pi.pmtpid[0] = Utils.Utils.toShort(span[bytesprocessed + 6], span[bytesprocessed + 7]);
                pi.pmtpid[1] = Utils.Utils.toShort(span[bytesprocessed + 8], span[bytesprocessed + 9]);
                pi.pmtpid[2] = Utils.Utils.toShort(span[bytesprocessed + 10], span[bytesprocessed + 11]);
                pi.pmtpid[3] = Utils.Utils.toShort(span[bytesprocessed + 12], span[bytesprocessed + 13]);
                pi.pmtpid[4] = Utils.Utils.toShort(span[bytesprocessed + 14], span[bytesprocessed + 15]);
                Array.Copy(span.ToArray(), bytesprocessed + 16, pi.remainingbytes, 0, 3);
                elementlen = span[bytesprocessed + 19] + 19 + 1;

                int packagenamelen = span[bytesprocessed + 21];
                pi.packagename = System.Text.Encoding.Default.GetString(span.Slice(bytesprocessed + 22, packagenamelen).ToArray());
                int channelnamelen = span[bytesprocessed + 21 + packagenamelen + 1];
                pi.channelname = System.Text.Encoding.Default.GetString(span.Slice(bytesprocessed + 22 + packagenamelen + 1, channelnamelen).ToArray());
                m_programs.Add(pi);
                bytesprocessed += elementlen;
            }
        }
    }
}
