using System;
using System.Collections.Generic;
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
        public int Startport
        {
            get
            {
                return _startport;
            }

            set
            {
                _startport = value;
            }
        }
        public int Endport
        {
            get
            {
                return _endport;
            }

            set
            {
                _endport = value;
            }
        }
        public string Destination
        {
            get
            {
                return _destination;
            }

            set
            {
                _destination = value;
            }
        }
        public RTSP(Uri uri):base(uri)
        {
            Destination = uri.Host;
            commandOptions();
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
                        switch (keyword)
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
            Console.WriteLine("Timer started... ");
        }
        public void commandPlay(String play)
        {
            if (!_playsupported)
                throw new Exception("PLAY command not supported");
            this.Method = "PLAY";
            this.Command = String.Format("stream={0}",_streamid) + play;
            Console.WriteLine("PLAY " + this.Command.ToString());
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
            _timer.Stop();
            _session = String.Empty;

        }
        private void HandleTimer(object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer expired");
            if (this.busy) return; /* Still connected */
            commandOptions();
        }
    }
}

