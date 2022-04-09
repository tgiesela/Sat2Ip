using Sat2Ip;
using Circularbuffer;
using Protocol;
using Oscam;

namespace Descrambler
{
    public class Descrambler
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        public struct ca_descr_type
        {
            public uint index;
            public uint parity;    /* 0 == even, 1 == odd */
            public byte[] cw;
        }
        private Payloads payloads;
        enum _payloadtype
        {
            pcmu = 0,
            val_1016 = 1,
            G721 = 2,
            GSM = 3,
            H261 = 31,
            MOV = 32,
            MP2T = 33,
            H263 = 34,
            senderreport = 200
        };

        private Channel? m_channel = null;
        private int m_port;
        private int m_outputport;
        private RtpReader reader;
        private Thread? inputthread;
        private Circularqueue m_inputpackets;
        private mp2packetqueue? m_oscampackets;
        private Thread? outputthread;
        private bool reading;
        private bool writing;
        private Oscamserver? m_oscamserver;

        public Descrambler(int port, int outputport)
        {
            m_port = port;
            m_outputport = outputport;
            log.Debug("Start reader on port: " + m_port);
            reader = new RtpReader(m_port);
            int optimalsize = 1024;
            m_oscampackets = new mp2packetqueue(optimalsize);
            m_inputpackets = new Circularqueue(optimalsize);
            payloads = new();
        }
        public void processpayload(Payload payload)
        {
            if (m_oscamserver != null)
                m_oscamserver.filterpacket(payload);

        }
        public void play()
        {
            payloads = new Payloads(processpayload);
            inputthread = new Thread(new ThreadStart(ReadData));
            inputthread.Start();
            outputthread = new Thread(new ThreadStart(WriteData));
            outputthread.Start();
        }

        public void stop()
        {
            if (!reading && !writing) return; /* Already stopped */
            reading = false;
            writing = false;
            payloads = new();
            if (m_oscamserver != null)
            {
                m_oscamserver.Stopdemux();
                m_oscamserver.Stop();
            }
        }
        private async void ReadData()
        {
            //if (m_oscamserver == null) return;
            //m_oscamserver.Start(m_channel);
            reader.start();
            Task<RtpPacket> task = reader.readAsync();
            RtpPacket packet = await task;
            reading = true;
            while (reading && packet != null)
            {
                task = reader.readAsync();
                processMpeg2Packets(packet);
                packet = await task;
            }
            reader.stop();
        }

        private void processMpeg2Packets(RtpPacket packet)
        {
            if (m_inputpackets == null) return;
            if (m_oscampackets == null) return;
            int lenprocessed = 0;
            Mpeg2Packet mp2Packet;
            byte[] rtpPayload = packet.getPayload();
            byte[] payloadpart;
            while (lenprocessed < rtpPayload.Length)
            {
                payloadpart = packet.getPayload(lenprocessed, 188);
                mp2Packet = new Mpeg2Packet(payloadpart);
                payloads.storePayload(mp2Packet);
                m_oscampackets.add(mp2Packet);
                m_inputpackets.add(mp2Packet.buffer);
                /* Payload processing is done via callback function, n/a for descrambler */
                lenprocessed += 188;
            }

        }
        private void WriteData()
        {
            byte[]? buf = new byte[188];
            byte[] rtpPacket = new byte[7*188];
            int rtsppacketcount = 0;
            int bytesprocessed = 0;
            DateTime start = DateTime.Now;
            long secondselapsed;

            RtpWriter writer = new RtpWriter(m_outputport);
            writing = true;
            while (writing)
            {
                if (m_inputpackets.getBufferedsize() < 7 * 188)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    int outputlen = 0;
                    while (outputlen < 7 * 188)
                    {
                        m_inputpackets.get(out buf);
                        if (buf != null)
                        {
                            Buffer.BlockCopy(buf, 0, rtpPacket, outputlen, 188);
                            outputlen += buf.Length;
                        }
                    }
                    try
                    {
                        rtsppacketcount++;
                        bytesprocessed += outputlen + 12;
                        secondselapsed = (DateTime.Now.Ticks - start.Ticks) / 10000000;
                        if ((rtsppacketcount % 1000) == 0)
                        {
                            log.Debug(String.Format("OUTPUT> Processed {0} bytes in {1} RTSP packets in {2} seconds: {3} Kb/s", bytesprocessed, rtsppacketcount, secondselapsed, (bytesprocessed / 1000 / secondselapsed)));
                        }
                        m_oscamserver.decryptpackets(rtpPacket);
                        writer.Write(rtpPacket);
                    }
                    catch (Exception e)
                    {
                        log.Debug("Destination socket exception: " + e.Message);
                    }
                }
            }
            log.Debug("Writing stopped");
        }

        public void setOscam(string oscamServer, int oscamPort)
        {
            if (m_oscampackets == null) throw new Exception("m_oscampackets == null");
            m_oscamserver = new Oscamserver(oscamServer, oscamPort, m_oscampackets);
            if (m_channel != null)
                m_oscamserver.Start(m_channel);
        }
        public void setChannel(Channel channel)
        {
            m_channel = channel;
            if (m_oscamserver != null)
                m_oscamserver.Start(m_channel);
        }
    }

}
