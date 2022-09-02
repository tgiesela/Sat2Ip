using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Sat2Ip
{
    public class RTSP:RTSPMethod
    {
        private bool _setupsupported = false;
        private bool _playsupported = false;
        private bool _optionssupported = true;
        private bool _teardownsupported = false;
        private bool _describesupported = false;
        private String _session = String.Empty;
        private String _destination = String.Empty;
        private int _timeout = 60;
        private String _streamid = String.Empty;
        private Timer _timer;
        private int _startport = 40000;
        private int _endport = 40001;
        private String _rtpinfo = String.Empty;
        private int _frontend = -1;
        private int _nroftuners;
        struct videosection
        {
            public int streamid;
            public int version;
            public int diseqcposition;
            public int tuner;
            internal int level;
            internal int locked;
            internal decimal frequency;
            internal int quality;
            internal string polarisation;
            internal string system;
            internal string roll_off;
            internal string pilots;
            internal string symbolrate;
            internal string fec;
            internal string stype;
        }
        videosection[] vs;
        private int _nroftunersinuse;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public int frontend { get { return _frontend; } set { _frontend = value; } }
        public int Startport { get  { return _startport; }  set { _startport = value; } }
        public int Endport { get { return _endport; } set { _endport = value; } } 
        public string Destination { get { return _destination; } set { _destination = value; } }
        public int nroftuners {  get { return _nroftuners; } set { _nroftuners = value; } }
        public RTSP(Uri uri):base(uri)
        {
            Destination = uri.Host;
            commandOptions();
            if (_describesupported)
                commandDescribe();
        }
        public int getFreeTuner()
        {
            for (int j = 1; j <= nroftuners; j++)
            {
                bool isused = false; ;
                for (int i = 0; i < _nroftunersinuse; i++)
                {
                    if (vs[i].tuner == j)
                        isused = true;
                }
                if (!isused)
                    return j;
            }
            return -1;
        }
        public void commandOptions()
        {
            this.Method = "OPTIONS";
            this.Requestheaders.Clear();
            this.Cseq = this.Cseq;
            this.Requestheaders.Add("CSeq",this.Cseq.ToString());
            if (_session != String.Empty)
            {
                this.Requestheaders.Add("Session",_session);
            }
            this.Invoke();
            foreach(var section in this.Replyheaders)
            {
                if (section.Key.Equals("Public"))
                {
                    String options = section.Value;
                    String[] keywords = options.Split(',');
                    foreach (String keyword in keywords)
                    {
                        switch (keyword.Trim())
                        {
                            case "SETUP":
                                _setupsupported = true;
                                break;
                            case "DESCRIBE":
                                _describesupported = true;
                                break;
                            case "TEARDOWN":
                                _teardownsupported = true;
                                break;
                            case "PLAY":
                                _playsupported = true;
                                break;
                            case "OPTIONS":
                                _optionssupported = true;
                                break;
                            default:
                                break;
                        }
                    }
                }
                
            }
        }
        public void commandSetup(String setup)
        {
            if (!_setupsupported)
                throw new Exception("SETUP command not supported");
            /* Find to consecutive ports to use for udp communication */
            var activeTcpConnections = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
            var usedPorts = new HashSet<int>();
            foreach (var connection in activeTcpConnections)
            {
                usedPorts.Add(connection.LocalEndPoint.Port);
            }
            for (var port = 40000; port <= 65534; port += 2)
            {
                if (!usedPorts.Contains(port) && !usedPorts.Contains(port + 1))
                {

                    _startport = port;
                    _endport = port + 1;
                    break;
                }
            }

            this.Method = "SETUP";
            if (_frontend != -1)
                this.Command = setup + "&fe=" + _frontend;
            else
                this.Command = setup;
            
            this.Requestheaders.Clear();
            this.Cseq = this.Cseq + 1;
            this.Requestheaders.Add("CSeq", this.Cseq.ToString());
            String transportheader = String.Format("RTP/AVP;unicast;client_port={0}-{1}", _startport, _endport);
            this.Requestheaders.Add("Transport", transportheader.ToString());
            if (_session!= String.Empty)
            {
                this.Requestheaders.Add("Session", _session);
            }
            this.Invoke();
            if (this.Status != 200)
            {
                throw new Exception("Setup command rejected with status: " + this.Status);
            }
            foreach (var section in this.Replyheaders)
            {
                if (section.Key.Equals("Session"))
                {
                    String[] parts = section.Value.Split(';');
                    _session = parts[0];
                    if (parts.Length > 1)
                    {
                        if (parts[1].StartsWith("timeout="))
                        {
                            _timeout = int.Parse(parts[1].Substring(8));
                        }
                    }
                }
                if (section.Key.Equals("com.ses.streamID"))
                {
                    _streamid = section.Value;
                }
                if (section.Key.Equals("Transport"))
                {
                    String[] parts = section.Value.Split(';');
                    for (int inx = 0; inx < parts.Length; inx++)
                    {
                        if (parts[inx].StartsWith("destination="))
                        {
                            _destination=parts[inx].Substring(12);
                        }
                    }
                }
            }
            _timer = new Timer((_timeout/2)*1000);
            _timer.Elapsed += new ElapsedEventHandler(HandleTimer);
            _timer.Start();
            log.Debug("Timer started... ");
        }
        public void commandPlay(String play)
        {
            if (!_playsupported)
                throw new Exception("PLAY command not supported");
            this.Method = "PLAY";
            this.Command = String.Format("stream={0}",_streamid) + play;
            log.Debug("PLAY " + this.Command.ToString());
            this.Requestheaders.Clear();
            this.Cseq = this.Cseq + 1;
            this.Requestheaders.Add("CSeq", this.Cseq.ToString());
            if (_session != String.Empty)
            {
                this.Requestheaders.Add("Session", _session);
            }
            this.Invoke();
            foreach (var section in this.Replyheaders)
            {
                if (section.Key.Equals("Session"))
                {
                    if (!section.Value.Equals(_session))
                        throw new Exception(String.Format("response from other session received: {0} <> {1}",_session, section.Value));
                }
                if (section.Key.Equals("RTP-Info"))
                {
                    _rtpinfo = section.Value;
                }
            }
        }
        public void commandDescribe()
        {
            if (!_describesupported)
                throw new Exception("DESCRIBE command not supported");
            this.Method = "DESCRIBE";
            this.Command = "";
            this.Requestheaders.Clear();
            this.Cseq = this.Cseq + 1;
            this.Requestheaders.Add("CSeq", this.Cseq.ToString());
            if (_session != String.Empty)
            {
                this.Requestheaders.Add("Session", _session);
            }
            this.Requestheaders.Add("Accept", "application/sdp");
            this.Invoke();
            foreach (var section in this.Replyheaders)
            {
                if (section.Key.Equals("Session"))
                {
                    if (!section.Value.Equals(_session))
                        throw new Exception(String.Format("response from other session received: {0} <> {1}", _session, section.Value));
                }
            }
            StringReader reader = new StringReader(this.Responsebody);
            string line;
            int videosect = 0;
            string[] parts;
            while ((line = reader.ReadLine()) != null)
            {
                string type = line.Substring(0, 2);
                switch (type)
                {
                    case "v=": break;
                    case "s=":
                        parts = line.Substring(2).Split(' ');
                        if (parts[0] == "SatIPServer:1")
                        {
                            string[] tunernrs = parts[1].Split(',');
                            decimal dvbstuners = Convert.ToDecimal(tunernrs[0]);
                            decimal dvbttuners = 0;
                            decimal dvbctuners = 0;
                            if (tunernrs.Length > 1)
                                dvbttuners = Convert.ToDecimal(tunernrs[1]);
                            if (tunernrs.Length > 2)
                                dvbctuners = Convert.ToDecimal(tunernrs[2]);
                            _nroftuners = (int)(dvbstuners+dvbttuners+dvbctuners);
                            vs = new videosection[_nroftuners];
                        }
                        break;
                    case "t=": break;
                    case "m=": videosect++; break;
                    case "a=": 
                        string aline = line.Substring(2).Trim();
                        if (aline.StartsWith("control"))
                        {
                            parts = aline.Split('=');
                            vs[videosect-1].streamid = int.Parse(parts[1]);
                        }
                        else
                        if (aline.StartsWith("fmtp:"))
                        {
                            string[] segments = aline.Substring(8).Split(';');
                            foreach (string segment in segments)
                            {
                                if (segment.StartsWith("ver="))
                                    vs[videosect-1].version = (int)decimal.Parse(segment.Substring(4));
                                if (segment.StartsWith("tuner="))
                                {
                                    string[] streamparts = segment.Substring(6).Split(',');
                                    vs[videosect-1].tuner = int.Parse(streamparts[0]);
                                    vs[videosect-1].level = int.Parse(streamparts[1]);
                                    vs[videosect-1].locked = int.Parse(streamparts[2]);
                                    vs[videosect-1].quality = int.Parse(streamparts[3]);
                                    vs[videosect-1].frequency = decimal.Parse(streamparts[4]);
                                    vs[videosect-1].polarisation = streamparts[5];
                                    vs[videosect-1].system = streamparts[6];
                                    vs[videosect-1].stype = streamparts[7];
                                    vs[videosect-1].pilots = streamparts[8];
                                    vs[videosect-1].roll_off = streamparts[9];
                                    vs[videosect-1].symbolrate = streamparts[10];
                                    vs[videosect-1].fec = streamparts[11];
                                }
                                if (segment.StartsWith("src="))
                                    vs[videosect-1].diseqcposition = int.Parse(segment.Substring(4));

                            }
                        }
                        break;
                    case "c=": break;

                }
                log.Debug(line);
            }
            log.DebugFormat("Server has {0} tuners, of which currently {1} are playing", _nroftuners, videosect);
            _nroftunersinuse = videosect;
        }
        public void commandTeardown(String play)
        {
            if (!_teardownsupported)
                throw new Exception("TEARDOWN command not supported");
            this.Method = "TEARDOWN";
            this.Command = String.Format("stream={0}", _streamid) + play;
            this.Requestheaders.Clear();
            this.Cseq = this.Cseq + 1;
            this.Requestheaders.Add("CSeq", this.Cseq.ToString());
            if (_session != String.Empty)
            {
                this.Requestheaders.Add("Session", _session);
            }
            this.Invoke();
            foreach (var section in this.Replyheaders)
            {
                if (section.Key.Equals("Session"))
                {
                    if (!section.Value.Equals(_session))
                        throw new Exception(String.Format("response from other session received: {0} <> {1}", _session, section.Value));
                }
            }
            if (_timer != null)
                _timer.Stop();
            _session = String.Empty;

        }
        private void HandleTimer(object source, ElapsedEventArgs e)
        {
            log.Debug("Timer expired");
            if (this.busy) return; /* Still connected */
            commandOptions();
        }
    }
}

