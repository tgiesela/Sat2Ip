
using Circularbuffer;
using log4net;
using log4net.Config;
using Protocol;
using Sat2Ip;
using System.Diagnostics;
using System.Reflection;
using Sat2IpGui.SatUtils;

namespace test
{
    class Program
    {
        private RTSP? rtsp;
        private bool reading;
        private bool writing;
        private List<Channel> channels = new();
        private Circularqueue? m_inputpackets;
        private mp2packetqueue ? m_oscampackets;
        private Thread? outputthread;
        private Oscam.Oscamserver m_oscamserver; 
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        void start(string stream)
        {
            UriBuilder uribld = new UriBuilder();
            uribld.Scheme = "rtp";
            uribld.Host = "10.56.57.55";
            uribld.Port = 554;
            rtsp = new RTSP(uribld.Uri);
            rtsp.commandSetup(stream);
            rtsp.commandPlay("");
            int optimalsize = 1000;
            outputthread = new Thread(new ThreadStart(WriteData));
            outputthread.Start();
            m_oscampackets = new mp2packetqueue(optimalsize);
            m_inputpackets = new Circularqueue(optimalsize);
            m_oscamserver = new Oscam.Oscamserver("10.56.57.155", 9000, m_oscampackets);
        }
        void stop()
        {
            writing = false;
            if (rtsp != null)
                rtsp.commandTeardown("");
        }
        private void loadChannelsFromTransponder(LNB lnb)
        {
            if (lnb == null)
                return;
            //List<House> houseOnes = houses.FindAll(house => house.Name == "House 1");
            //List<House> houseOnes = houses.Where(house => house.Name == "House 1").ToList();
            foreach (Transponder trs in lnb.getTransponders())
            {
                foreach (Channel c in lnb.getChannelsOnTransponder(trs.frequency))
                {
                    channels.Add(c);
                }
            }
        }
        async Task read(long timeout, Channel channel)
        {
            if (rtsp == null) return;
            if (channel == null)
                m_oscamserver.Start(new Channel(0,new Transponder()));
            else
                m_oscamserver.Start(channel);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            RtpReader reader = new RtpReader(rtsp.Startport);
            reader.start();
            Task<RtpPacket> task = reader.readAsync();
            RtpPacket packet = await task;
            reading = true;
            log.Debug("Start reading");
            while (reading && packet != null && stopwatch.ElapsedMilliseconds < timeout)
            {
                task = reader.readAsync();
                processMpeg2Packets(packet);
                packet = await task;
            }
            log.Debug("Finished reading");
            m_oscamserver.Stop();
            reader.stop();
            stopwatch.Stop();
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
                m_oscampackets.add(mp2Packet);
                
                m_inputpackets.add(mp2Packet.buffer);
                //m_inputpackets.add(mp2Packet.buffer);
                /* Payload processing is done via callback function, n/a for descrambler */
                lenprocessed += 188;
            }
        }
        private void WriteData()
        {
            /* 
             * Currently writing from inputpackets. 
             * This should be changed to outputpackets when decoding is implemented 
             */
            byte[]? buf = new byte[188];
            byte[] rtpPacket = new byte[7 * 188];
            int rtsppacketcount = 0;
            int bytesprocessed = 0;
            DateTime start = DateTime.Now;
            long secondselapsed;

            RtpWriter writer = new RtpWriter(rtsp.Startport+2);
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
                        if ((rtsppacketcount % 10000) == 0)
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
        static async Task Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new System.IO.FileInfo(@"log4.config"));

            Program p = new Program();
            p.loadChannelsFromTransponder(new LNB(1));
            p.loadChannelsFromTransponder(new LNB(2));
            string streamZDFHD = "?src=1&tuner=1&freq=11361&sr=22000&pol=h&msys=dvbs2&plts=off&ro=0.35&fec=23&mtype=8psk&pids=0,6100,6110,6120,6121,6122,6123,6130,6131,6132,6170";
            string streamNPO1HD = "?src=2&tuner=1&freq=12187&sr=29900&pol=h&msys=dvbs2&plts=off&ro=0.35&fec=23&mtype=8psk&pids=0,1,2729,1809,1829,1849,4109,519,3129,99,119,40,50";
            
 //           p.start(streamZDFHD);
 //           await p.read(30000,null);
 //           p.stop();
            Channel npo1hd = p.findchannels("NPO1 HD");
            p.start(streamNPO1HD);
            await p.read(30000, npo1hd);
            p.stop();

        }

        private Channel findchannels(string servicename)
        {
            return channels.Find(x => x.Servicename == servicename);

        }
    }
}