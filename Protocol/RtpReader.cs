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
        private UdpClient client;

        public int port { get; private set; }

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
            UdpReceiveResult taskresult = await client.ReceiveAsync();
            return new RtpPacket(taskresult.Buffer);
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
        }

        public void start()
        {
            client = new UdpClient(port);
        }
    }
}
