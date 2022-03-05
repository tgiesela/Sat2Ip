using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    public class RtpWriter
    {
        private short rtp_seqnum = 0;
        private long rtp_ssrc = 0;
        private long rtime;
        private DateTime start = DateTime.Now;
        private UdpClient dest;
        private byte[] rtpPacket = new byte[1328];

        public RtpWriter(int outputport)
        {
            dest = new UdpClient("127.0.0.1", outputport);
            //IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), outputport);
        }
        public void Write(byte[] buf)
        {
            rtp_seqnum++;
            rtime = DateTime.Now.Ticks * 9 / 100;
            rtpPacket[0] = 0x80;
            rtpPacket[1] = 33; // MPEG TS rtp payload type
            rtpPacket[2] = (byte)(rtp_seqnum >> 8);
            rtpPacket[3] = (byte)(rtp_seqnum & 0xff);
            rtpPacket[4] = (byte)((rtime >> 24) & 0xff);
            rtpPacket[5] = (byte)((rtime >> 16) & 0xff);
            rtpPacket[6] = (byte)((rtime >> 8) & 0xff);
            rtpPacket[7] = (byte)(rtime & 0xff);

            rtpPacket[8] = (byte)((rtp_ssrc >> 24) & 0xff);
            rtpPacket[9] = (byte)((rtp_ssrc >> 16) & 0xff);
            rtpPacket[10] = (byte)((rtp_ssrc >> 8) & 0xff);
            rtpPacket[11] = (byte)(rtp_ssrc & 0xff);
            Buffer.BlockCopy(buf, 0, rtpPacket, 12, buf.Length);
            dest.Send(rtpPacket, 12 + buf.Length);
        }
    }
}
