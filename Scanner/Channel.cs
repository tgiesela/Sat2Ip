using Interfaces;
using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Interfaces.PAT;

namespace Sat2Ip
{
    [Serializable]
    public class Channel
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _programpid = -1;
        private List<Sat2Ip.Stream> _pmt;
        private bool _pmtpresent = false;
        private string m_providername = string.Empty;
        private string m_servicename = string.Empty;
        private Transponder _transponder;
        private List<capid> _capids;
        public enum _descriptorlevel
        {
            program,
            stream
        };
        private _descriptorlevel _CAlevel;
        public int EIT_schedule_flag { get; set; }
        public int EIT_present_following_flag { get; set; }
        public ushort service_id { get; set; }
        public int running_status { get; set; }
        public int free_CA_mode { get; set; }
        public int Programpid { get { return _programpid; } set { _programpid = value; } }
        public List<Sat2Ip.Stream> Pmt { get { return _pmt; } set { _pmt = value; } }
        public bool Pmtpresent { get { return _pmtpresent; } set { _pmtpresent = value; } }
        public string Providername { get { return m_providername; } set { m_providername = value; } }
        public string Servicename { get {return (lcn != 0)?lcn + ". " + m_servicename:m_servicename; } set { m_servicename = value; } }
        public int Servicetype {   get; set; }
        public _descriptorlevel CAlevel { get { return _CAlevel; } set { _CAlevel = value; } }
        public List<capid> Capids { get { return _capids; } set { _capids = value; } }
        public Transponder transponder { get { return _transponder; } set { _transponder = value; } }
        public List<ComponentDescriptor> componentdescriptors { get; set; }
        public ushort pcr_pid { get; set; }
        public int lcn { get; set; }
        public bool isRadioService()
        {
            if (Servicetype == 0x02 || Servicetype == 0x07 || Servicetype == 0x0A)
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
            componentdescriptors = new List<ComponentDescriptor>();
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
        public string getPidString()
        {
            string pids = "0";
            if (Capids.Count > 0)
                pids = pids + ",1";
            foreach (int p in this.getPids())
            {
                pids = pids + "," + p.ToString();
            }
            return pids;
        }
        public string getPlayString()
        {
            return _transponder.getQuery() + "&pids="+  getPidString();
        }

    }
}
