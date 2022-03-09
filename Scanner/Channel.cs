using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    [Serializable]
    public class Channel
    {
        /* Structure to hold information of a CA-pid */
        private int _programpid = -1;
        private int _programnumber = -1;
        private List<Sat2Ip.Stream> _pmt;
        private bool _pmtpresent = false;
        private String _providername = String.Empty;
        private String _channelname = String.Empty;
        private Transponder _transponder;
        private List<capid> _capids;
        public enum _descriptorlevel
        {
            program,
            stream
        };
        private _descriptorlevel _CAlevel;

        public int Programpid { get { return _programpid; } set { _programpid = value; } }
        public List<Sat2Ip.Stream> Pmt { get { return _pmt; } set { _pmt = value; } }
        public int Programnumber { get { return _programnumber; } set { _programnumber = value; } }
        public bool Pmtpresent { get { return _pmtpresent; } set { _pmtpresent = value; } }
        public string Providername { get { return _providername; } set { _providername = value; } }
        public string Servicename { get; set; }
        public int Servicetype {   get; set; }
        public _descriptorlevel CAlevel { get { return _CAlevel; } set { _CAlevel = value; } }
        public List<capid> Capids { get { return _capids; } set { _capids = value; } }
        public Transponder transponder { get { return _transponder; } set { _transponder = value; } }

        public bool isRadioService()
        {
            if (Servicetype == 0x02 || Servicetype == 0x07)
                return true;
            else
                return false;
        }
        public bool isDataService()
        {
            if (Servicetype == 0x0C)
                return true;
            else
                return false;
        }
        public bool isTVService()
        {
            return (!isDataService() && !isRadioService());
        }
        public Channel(int programpid, Transponder transponder)
        {
            _programpid = programpid;
            _transponder = transponder;
            _pmt = new List<Sat2Ip.Stream>();
            _capids = new List<capid>();
            Servicename = "";
        }
        public List<int> getPids()
        {
            List<int> pids = new List<int>();
            pids.Add(this._programpid);
            foreach (capid p in this._capids)
            {
                if (!pids.Contains(p.CA_PID))
                    pids.Add(p.CA_PID);
            }
            foreach (Sat2Ip.Stream stream in _pmt)
            {
                pids.Add(stream.Elementary_pid);
                foreach (capid p in stream.capids)
                {
                    if (!pids.Contains(p.CA_PID))
                        pids.Add(p.CA_PID);
                }
            }
            return pids;
        }
        public String getPidString()
        {
            String pids = String.Empty;
            pids = "0";
            if (Capids.Count > 0)
                pids = pids + ",1";
            foreach (int p in this.getPids())
            {
                pids = pids + "," + p.ToString();
            }
            return pids;
        }
        public String getPlayString()
        {
            return _transponder.getQuery() + "&pids="+  getPidString();
        }
    }
}
