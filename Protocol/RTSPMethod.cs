using ClientSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Sat2Ip
{
    public class RTSPMethod
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Regex RegexStatusLine = new Regex(@"RTSP/(\d+)\.(\d+)\s+(\d+)\s+([^.]+?)\r\n(.*)", RegexOptions.Singleline);
        private SyncClientSocket _socket;
        private String _method;
        private String _body = String.Empty;
        private String _command = String.Empty;
        private String _responsebody = String.Empty;
        private IDictionary<string, string> _requestheaders = new Dictionary<string, string>();
        private IDictionary<string, string> _replyheaders;
        private int _cseq;
        private int _majorversion;
        private int _minorversion;
        private int _majorversionServer;
        private int _minorversionServer;
        private int _statuscode;
        private String _statusphrase;
        internal bool busy;

        public string Method
        {
            get
            {
                return _method;
            }

            set
            {
                _method = value;
            }
        }

        public string Body
        {
            get
            {
                return _body;
            }

            set
            {
                _body = value;
            }
        }

        public IDictionary<string, string> Requestheaders
        {
            get
            {
                return _requestheaders;
            }

            set
            {
                _requestheaders = value;
            }
        }

        public IDictionary<string, string> Replyheaders
        {
            get
            {
                return _replyheaders;
            }

            set
            {
                _replyheaders = value;
            }
        }

        public int Cseq
        {
            get
            {
                return _cseq;
            }

            set
            {
                _cseq = value;
            }
        }

        public string Responsebody
        {
            get
            {
                return _responsebody;
            }

            set
            {
                _responsebody = value;
            }
        }

        public string Command
        {
            get
            {
                return _command;
            }

            set
            {
                _command = value;
            }
        }

        public RTSPMethod(Uri uri)
        {
           _socket = new SyncClientSocket(System.Net.Dns.GetHostAddresses(uri.Host)[0].ToString(), 554);
            _cseq = 0;
            _majorversion = 1;
            _minorversion = 0;
        }
        public void connect()
        {
            this.busy = true;
            _socket.Connect();
        }
        public void disconnect()
        {
            _socket.Disconnect();
            this.busy = false;
        }
        public void Invoke()
        {
            connect();
            StringBuilder request = new StringBuilder();
            request.AppendFormat("{0} rtsp://{1}/{4} RTSP/{2}.{3}\r\n", _method, _socket.IpAddress, _majorversion, _minorversion, _command);
            foreach (var header in _requestheaders)
            {
                request.AppendFormat("{0}: {1}\r\n", header.Key, header.Value);
            }
            request.AppendFormat("\r\n{0}", _body);
            //Encoding.UTF8.GetBytes(request.ToString());
            _socket.SendData(Encoding.UTF8.GetBytes(request.ToString()));
            String responseString = _socket.ReceiveData();
            if (responseString == null)
            {
                throw new Exception("No data received");
            }
            //var responseString = Encoding.UTF8.GetString(responseBytes, 0, responseByteCount);
            var m = RegexStatusLine.Match(responseString);
            if (m.Success)
            {
                _majorversionServer = int.Parse(m.Groups[1].Captures[0].Value);
                _minorversionServer = int.Parse(m.Groups[2].Captures[0].Value);
                _statuscode = int.Parse(m.Groups[3].Captures[0].Value);
                _statusphrase = m.Groups[4].Captures[0].Value;
                responseString = m.Groups[5].Captures[0].Value;
            }

            var sections = responseString.Split(new[] { "\r\n\r\n" }, StringSplitOptions.None);
            _responsebody = sections[1];
            var headers = sections[0].Split(new[] { "\r\n" }, StringSplitOptions.None);
            _replyheaders = new Dictionary<string, string>();
            foreach (var headerInfo in headers.Select(header => header.Split(':')))
            {
                if (headerInfo[0].Equals("CSeq"))
                {
                    if (int.Parse(headerInfo[1].Trim()) != Cseq)
                    {
                        log.Debug(String.Format("CSeq out of sequence: send{0}, received{1}", Cseq, int.Parse(headerInfo[1].Trim())));
                    }
                }
                _replyheaders.Add(headerInfo[0], headerInfo[1].Trim());
            }
            disconnect();
        }
    }
}
