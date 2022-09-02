using Sat2Ip;
using Sat2ipUtils;

namespace Interfaces
{
    [Serializable]
    public class FastScanBouquet
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public Network network { get; set; }
        public FastScanLocation location { get; set; }
        public List<FastScanProgramInfo> programInfos { get; set; }
        public void assign(List<Channel> channels)
        {
            int highestlcn=0;
            foreach (Channel c in channels)
            {
                c.lcn = 99999;
            }
            foreach (ServiceListItem sli in network.networkservices)
            {
                Channel c = channels.Find(x => x.service_id == sli.service_id && x.transponder.transportstreamid == sli.transportstreamid);
                if (c != null)
                {
                    c.lcn = sli.lcn;
                }
                if (sli.lcn > highestlcn) highestlcn = sli.lcn;
            }
            foreach (Channel c in channels)
            {
                if (c.lcn == 99999)
                    c.lcn = highestlcn++;
            }
        }
        public List<Channel> channels(LNB [] lnbs)
        {
            List<Channel> channels = new List<Channel>();

            foreach (Transponder tsp in network.transponders)
            {
                for (int i = 0; i < lnbs.Length; i++)
                {
                    if (lnbs[i] == null) continue;
                    int orbit = Utils.Utils.bcdtoint(tsp.orbit);
                    if (orbit == lnbs[i].orbit())
                    {
                        tsp.diseqcposition = lnbs[i].diseqcposition;
                        break;
                    }
                }
            }

            foreach (FastScanProgramInfo fspi in programInfos)
            {
                Transponder tsp = network.transponders.Find(x => x.transportstreamid == fspi.streamid);
                if (tsp != null)
                {
                    Channel c = new Channel(fspi.pmtpid[4], tsp);
                    if (fspi.pmtpid[0] == 0)
                    {
                        c.Servicetype = 0x02;
                    }
                    else
                    {
                        c.Servicetype = 0x01;
                    }
                    c.Servicename = fspi.channelname;
                    c.service_id = fspi.serviceid;
                    c.Providername = fspi.packagename;
                    log.DebugFormat("{0}:{1}:TV? {2}",fspi.channelname, fspi.packagename,c.isTVService());
                    Utils.Utils.DumpBytes(fspi.remainingbytes, fspi.remainingbytes.Length);
                    channels.Add(c);
                }
            }
            return channels;
        }
    }
}
