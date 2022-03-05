using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace ClientSockets
{
    public class SyncClientSocket
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port;
        private String _hostaddress;
        private Socket _sender = null;
        private IPHostEntry _ipHostInfo;
        private IPAddress _ipAddress;
        private IPEndPoint _remoteEP;
        private byte[] _receivebuffer = new byte[4096];

        public IPAddress IpAddress
        {
            get
            {
                return _ipAddress;
            }

            set
            {
                _ipAddress = value;
            }
        }

        public SyncClientSocket(String hostaddress, int port)
        {
            _hostaddress = hostaddress;
            _port = port;
            if (!IPAddress.TryParse(hostaddress, out _ipAddress))
            {
                _ipHostInfo = Dns.GetHostEntry(_hostaddress);
                _ipAddress = _ipHostInfo.AddressList[0];
            }
            _remoteEP = new IPEndPoint(_ipAddress, _port);

        }
        public void Connect()
        {
            // Create a TCP/IP  socket.
            try
            {
                _sender = new Socket(AddressFamily.InterNetwork,
                                     SocketType.Stream, ProtocolType.Tcp);
                _sender.DontFragment = true;
                _sender.SendBufferSize = 4096;
                log.Debug(String.Format("Socket created for {0}:{1} - DontFragment {2}", _hostaddress, _port,_sender.DontFragment.ToString()));
            }
            catch (SocketException e)
            {
                log.Debug(String.Format("Socket error {0}", e.Message));
                _sender = null;
            }
            if (_sender != null)
            {
                try
                {
                    _sender.Connect(_remoteEP);
                    log.Debug(String.Format("Socket connected to {0} - DontFragment: {1}", _sender.RemoteEndPoint.ToString(),_sender.DontFragment.ToString()));
                }
                catch (SocketException e)
                {
                    log.Debug(String.Format("Socket error {0}", e.Message));
                    _sender = null;
                    throw e;
                }
            }
        }
        public void SendData(String data)
        {
            byte[] bytedata = Encoding.ASCII.GetBytes(data);
            SendData(bytedata);
        }
        public void SendData(byte[] bytedata)
        {
            try
            {
                if (bytedata.Length > _receivebuffer.Length)
                {
                    log.Debug(String.Format("Buffer too long {0} > {1}", bytedata.Length, _receivebuffer.Length));
                    throw new Exception("Buffer overflow");
                }
                _sender.Send(bytedata,bytedata.Length,SocketFlags.None);
            }
            catch (SocketException e)
            {
                log.Debug(String.Format("Socket error {0}", e.Message));
                _sender.Shutdown(SocketShutdown.Both);
                _sender.Close();
            }
        }
        public void SendData(byte[] bytedata, int bytestosend)
        {
            byte[] senddata = new byte[bytestosend];
            Array.Copy(bytedata, senddata, bytestosend);
            SendData(senddata);
        }
        public String ReceiveData()
        {
            try
            {
                int x = _sender.Receive(_receivebuffer);
                if (x == 0)
                {
                    return null;
                }
                return Encoding.ASCII.GetString(_receivebuffer);
            }
            catch (SocketException e)
            {
                log.Debug(String.Format("Socket error {0}", e.Message));
                return null;
            }
        }
        public byte[] ReceiveByteData()
        {
            try
            {
                int receivedlen = _sender.Receive(_receivebuffer);
                if (receivedlen == 0)
                {
                    return null;
                }
                byte[] received = new byte[receivedlen];
                Array.Copy(_receivebuffer, received, receivedlen);
                return received;
            }
            catch (SocketException e)
            {
                log.Debug(String.Format("Socket error {0}", e.Message));
                return null;
            }
        }
        public void Disconnect()
        {
            log.DebugFormat("Socket closed for {0}:{1}", _hostaddress, _port);
            _sender.Shutdown(SocketShutdown.Both);
            _sender.Close();
            _sender.Dispose();
            _sender = null;
        }
    }
}
