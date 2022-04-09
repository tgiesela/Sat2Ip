
using Circularbuffer;
using log4net;
using log4net.Config;
using Protocol;
using Sat2Ip;
using System.Diagnostics;
using System.Reflection;
using Sat2IpGui.SatUtils;
using System.Numerics;
using static Interfaces.DVBBase;
using Interfaces;
using System.Buffers.Binary;

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
        private Payloads payloads;
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
            lnb.load();
            foreach (Transponder trs in lnb.transponders)
            {
                foreach (Channel c in lnb.getChannelsOnTransponder(trs.frequency, trs.diseqcposition))
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

            payloads = new Payloads(printpayload);

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
                payloads.storePayload(mp2Packet);
                m_inputpackets.add(mp2Packet.buffer);
                //m_inputpackets.add(mp2Packet.buffer);
                /* Payload processing is done via callback function, n/a for descrambler */
                lenprocessed += 188;
            }
        }
        int seen_bc=0;
        int seen_bd=0;
        bool bc_complete;
        bool bd_complete;
        Network netw = new Network();
        private void printpayload(Payload payload)
        {
            if (payload.payloadpid == 901)
            {
                log.DebugFormat("Payload {2}: {0} van {1}", payload.data[6],payload.data[7],payload.data[0]);
                //Utils.Utils.DumpBytes(payload.data,payload.expectedlength);
                if (payload.data[0] == 0xbc) { seen_bc++; if (seen_bc > payload.data[7]) bc_complete = true; }
                if (payload.data[0] == 0xbd) { seen_bd++; if (seen_bd > payload.data[7]) bd_complete = true; }

                if (payload.data[0] == 0xbd)
                    processFastScan(payload);
                if (payload.data[0] == 0xbc)
                    processChannelInfo(payload);
                if (bd_complete)
                {
                    foreach (programinfo pi in programs)
                    {
                        log.DebugFormat("{0}:\t{1} stream {2} ({2:X}), network {3},", pi.packagename, pi.channelname, pi.streamid, pi.network);
                    }
                }
                if (bd_complete)
                {
                    foreach (Transponder t in netw.transponders)
                    {
                        log.DebugFormat("{2} {0} {1:X} ", t.frequency, t.services[0].service_id, Utils.Utils.bcdtohex(t.orbit));
                    }
                }
                if (bc_complete && bd_complete)
                    Environment.Exit(0);
            }
        }

        private void processChannelInfo(Payload payload)
        {
            Span<byte> buffer = payload.data;
            tableHeader hdr = getHeader(buffer.Slice(1).ToArray());
            netw.addsection(hdr, buffer.Slice(8, payload.expectedlength-8));
        }

        private void processFastScan(Payload payload)
        {
            Span<byte> buffer = payload.data;
            tableHeader hdr = getHeader(buffer.Slice(1).ToArray());
            processsection(hdr, buffer.Slice(8, payload.expectedlength-8));
        }

        private class programinfo
        {
            public ushort [] pmtpid { get; set; } = new ushort[5];
            public byte[] remainingbytes { get; set; } = new byte[3];
            public ushort network { get; set; }
            public ushort streamid { get; set; }
            public ushort programid { get; set; }
            public string packagename { get; set; }
            public string channelname { get; set; }

        }
        private List<programinfo> programs = new List<programinfo>();
        private void processsection(tableHeader hdr, Span<byte> span)
        {
            int bytesprocessed = 0;
            while (bytesprocessed < span.Length - 4)
            {
                programinfo pi = new programinfo();
                int elementlen=0;
                ushort[] pmtpid = new ushort[5];
                byte[] remainingbytes = new byte[3];
                pi.network = Utils.Utils.toShort(span[bytesprocessed+0], span[bytesprocessed + 1]);
                pi.streamid = Utils.Utils.toShort(span[bytesprocessed + 2], span[bytesprocessed + 3]);
                pi.programid = Utils.Utils.toShort(span[bytesprocessed + 4], span[bytesprocessed + 5]);
                pi.pmtpid[0] = Utils.Utils.toShort(span[bytesprocessed + 6], span[bytesprocessed + 7]);
                pi.pmtpid[1] = Utils.Utils.toShort(span[bytesprocessed + 8], span[bytesprocessed + 9]);
                pi.pmtpid[2] = Utils.Utils.toShort(span[bytesprocessed + 10], span[bytesprocessed + 11]);
                pi.pmtpid[3] = Utils.Utils.toShort(span[bytesprocessed + 12], span[bytesprocessed + 13]);
                pi.pmtpid[4] = Utils.Utils.toShort(span[bytesprocessed + 14], span[bytesprocessed + 15]);
                Array.Copy(span.ToArray(), bytesprocessed+16, pi.remainingbytes, 0, 3);
                elementlen = span[bytesprocessed + 19] + 19 + 1;

                int packagenamelen = span[bytesprocessed + 21];
                pi.packagename = System.Text.Encoding.Default.GetString(span.Slice(bytesprocessed + 22, packagenamelen).ToArray());
                int channelnamelen = span[bytesprocessed + 21 + packagenamelen + 1];
                pi.channelname = System.Text.Encoding.Default.GetString(span.Slice(bytesprocessed + 22 + packagenamelen + 1, channelnamelen).ToArray());
                programs.Add(pi);
                bytesprocessed += elementlen;
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
        public static uint crc32bmpeg2(ReadOnlySpan<byte> message, int l)
        {
            /*
             * As noted in the crcalc web page, crc32/mpeg2 uses a left shifting (not reflected) 
             * CRC along with the CRC polynomial 0x104C11DB7 and initial CRC value of 0xFFFFFFFF,
             * and not post complemented:
             */
            int i, j;
            uint crc, msb;

            crc = 0xFFFFFFFF;
            for (i = 0; i < l; i++)
            {
                // xor next byte to upper bits of crc
                crc ^= (((uint)message[i])<< 24);
                for (j = 0; j < 8; j++)
                {    // Do eight times.
                    msb = crc >> 31;
                    crc <<= 1;
                    crc ^= (0 - msb) & 0x04C11DB7;
                }
            }
            return crc;         // don't complement crc on output
        }
        public static uint CalculateCrc32(ReadOnlySpan<byte> data, int? bound = null)
        {
            uint crc = 0xffffffff;
            int bytes = bound ?? data.Length;
            int p = 0;
            while (true)
            {
                uint dw = BinaryPrimitives.ReadUInt32LittleEndian(
                    data.Slice(p * sizeof(uint), sizeof(uint)));
                crc ^= dw;
                p++;
                for (int i = 0; i < 32; ++i)
                {
                    if ((crc & 0x80000000) != 0)
                    {
                        crc = (crc << 1) ^ 0x04C11DB7;
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
                if (bytes <= sizeof(uint)) return crc;
                bytes -= sizeof(uint);
            }
        }
        static async Task Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new System.IO.FileInfo(@"log4.config"));

            byte[] buffer = { 
                0x00, 0xb0, 0x5d, 0x07, 0xfe, 0xc1, 0x00, 0x00, 0x00, 0x00, 0xe0, 0x10 , 0x28, 0x47 , 0xe1 , 0x07,
                0x28, 0x48, 0xe1, 0x08, 0x28, 0x43, 0xe1, 0x02, 0x28, 0x3d, 0xe1, 0x00 , 0x28, 0x3f , 0xe1 , 0x09,
                0x28, 0x41, 0xe1, 0x04, 0x28, 0x42, 0xe1, 0x05, 0x28, 0x8c, 0xe1, 0x03 , 0x28, 0x8d , 0xe1 , 0x13,
                0x28, 0x8e, 0xe1, 0x14, 0x28, 0x92, 0xe1, 0x18, 0x28, 0x94, 0xe1, 0x1a , 0x28, 0x96 , 0xe1 , 0x1c,
                0x28, 0x51, 0xe1, 0x06, 0x28, 0x91, 0xe1, 0x17, 0x28, 0x95, 0xe1, 0x1b , 0x28, 0x97 , 0xe1 , 0x01,
                0x28, 0x8f, 0xe1, 0x15, 0x28, 0x90, 0xe1, 0x16, 0x28, 0x93, 0xe1, 0x19 ,
                };
            byte[] crc32 = { 0xb7, 0x4b, 0xf9, 0x8f };
            uint crc32calculated = crc32bmpeg2(buffer, buffer.Length);

            byte[] buffer2 = {
                0x00, 0xb0 ,0x21 ,0x04 ,0x1b ,0xcd ,0x00 ,0x00 ,0x00 ,0x00 ,0xe0 ,0x10 ,0x70 ,0x31 ,0xe0 ,0x64,
                0x70, 0x32 ,0xe0 ,0xc8 ,0x70 ,0x34 ,0xe1 ,0x90 ,0x70 ,0x35 ,0xe1 ,0xf4 ,0x70 ,0x36 ,0xe2 ,0x58,
                };
            crc32calculated = crc32bmpeg2(buffer, buffer.Length);

            Program p = new Program();
            LNB lnb1 = new LNB(1);
            LNB lnb2 = new LNB(2);
            p.loadChannelsFromTransponder(lnb1);
            p.loadChannelsFromTransponder(lnb2);
            string streamZDFHD = "?src=1&&freq=11361&sr=22000&pol=h&msys=dvbs2&plts=off&ro=0.35&fec=23&mtype=8psk&pids=0,6100,6110,6120,6121,6122,6123,6130,6131,6132,6170&fe=4";
            string streamNPO1HDfull = "?src=2&&freq=12187&sr=29900&pol=h&msys=dvbs2&plts=off&ro=0.35&fec=23&mtype=8psk&pids=0,1,2729,1809,1829,1849,4109,519,3129,99,119,40,50&fe=4";
            string streamNPO1HDfast = "?src=2&freq=12187&sr=29900&pol=h&msys=dvbs2&plts=off&ro=0.35&fec=23&mtype=8psk&fe=4&pids=0,1,519,90";

            //           p.start(streamZDFHD);
            //           await p.read(30000,null);
            //           p.stop();


            Transponder npo1tsp = lnb2.getTransponder(12187);
            
            //p.start(streamNPO1HD);
            Channel npo1hd = new Channel(2729, npo1tsp);
            Sat2Ip.Stream pmt1 = new Sat2Ip.Stream();
            pmt1.Elementary_pid = 519;
            Sat2Ip.Stream pmt2 = new Sat2Ip.Stream();
            pmt2.Elementary_pid = 99;
            Sat2Ip.Stream pmt3 = new Sat2Ip.Stream();
            pmt3.Elementary_pid = 40;
            Sat2Ip.Stream pmt4 = new Sat2Ip.Stream();
            pmt4.Elementary_pid = 119;
            Sat2Ip.Stream pmt5 = new Sat2Ip.Stream();
            pmt5.Elementary_pid = 50;
            List<Sat2Ip.Stream> list = new List<Sat2Ip.Stream>();
            list.Add(pmt1);
            list.Add(pmt2);
            list.Add(pmt3);
            list.Add(pmt4);
            list.Add(pmt5);

            npo1hd.Pmt = list;
            npo1hd.Pmtpresent = true;
            log.Debug("PLAYSTRING=" + npo1hd.getPlayString());
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            //npo1hd = p.channels.Find(x => x.Programpid == 2729);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            //string playstring = "?src=1&freq=12515&sr=22000&pol=h&msys=dvbs&plts=off&ro=0.35&fec=56&mtype=qpsk&pids=901&fe=4";
            p.start(streamNPO1HDfast);
            await p.read(30000, npo1hd);
            p.stop();
        }
        private tableHeader getHeader(byte[] pTable)
        {
            tableHeader header = new tableHeader();
            if (pTable.Length < 7)
            {
                log.Debug("Short header received");
                return header;
            }
            header.syntaxindicator = (pTable[0] & 0x80) >> 7;
            header.sectionlength = (short)(((pTable[0] & 0x0F) << 8) | (pTable[1] & 0x00ff));
            header.streamid = ((pTable[2] & 0xff) << 8) | (pTable[3] & 0xff);
            header.programnumber = ((pTable[2] & 0xff) << 8) | (pTable[3] & 0xff);
            header.versionnr = (pTable[4] & 0x3E) >> 1;
            header.currNextInd = pTable[4] & 0x01;
            header.sectionnr = pTable[5];
            header.lastsectionnr = pTable[6];
            return header;
        }

    }
}