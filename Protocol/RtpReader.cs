using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    public class RtpReader
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private UdpClient client;
        private long lastcount=0;
        private bool m_active = false;

        public int port { get; private set; }
        public bool active { get { return m_active; } }

        public RtpReader(int _port)
        {
            port = _port;
        }
        ~RtpReader()
        {
        }
        public async Task<RtpPacket> readAsync()
        {
            if (client == null)
            {
                throw new Exception("Client is null");
            }
            try
            {
                UdpReceiveResult taskresult = await client.ReceiveAsync();
                RtpPacket packet = new RtpPacket(taskresult.Buffer);
                if (packet.header.Sequencenumber != lastcount + 1)
                    if (lastcount != 0)
                        log.DebugFormat("Packets lost: {0}-{1}", lastcount, packet.header.Sequencenumber);
                lastcount = packet.header.Sequencenumber;
                return packet;
            } 
            catch (Exception ex)
            {
                return null;
            }
        }
        public RtpPacket readSync()
        {
            byte[] addr = new byte[4] { 0, 0, 0, 0 };
            IPEndPoint EP = new IPEndPoint(new IPAddress(addr), 40000);
            if (client == null)
            {
                throw new Exception("Client is null");
            }
            return new RtpPacket(client.Receive(ref EP));
        }
        public void stop()
        {
            client.Close();
            m_active = false;
        }

        public void start()
        {
            client = new UdpClient(port);
            m_active = true;
        }
    }
}
