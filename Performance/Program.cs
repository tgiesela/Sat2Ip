
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
            Payloads payloads = new Payloads(printpayload);

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

            //p.testFastScan();

            Channel npo1hd = p.findchannels("Hustler TV");
            //p.start(streamNPO1HD);
            string playstring = "?src=1&freq=12515&sr=22000&pol=h&msys=dvbs&plts=off&ro=0.35&fec=56&mtype=qpsk&pids=901&fe=4";
            p.start(playstring);
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
        private void testFastScan()
        {
            string [] payloads = new string[10];

            payloads[0] =
            "bcf3ce006acf0009f0024000f3bf00650001f01f430b012225000130a1027500" +
            "0341060064010154018308006482560154825703ea0001f02d430b0112290001" +
            "92a602200002410c138901139301139d0113a7018310138980521393802d139d" +
            "805813a7805a03eb0001f01f430b0112440001928102200004410633a80133a9" +
            "01830833a880ce33a980d103ed0001f042430b01127300019286022000024115" +
            "33ff01340001340101340201340301340401340501831c33ff80d2340080a134" +
            "0180a63402809d3403808d340480a8340580a403ee0001f01f430b0112880001" +
            "92ae0220000241061086011090018308108682b2109080ea03ef0001f018430b" +
            "0113030001928602200002410313320183041332808f03f20001f026430b0113" +
            "47000192a60220000241092b8e012b98012ba201830c2b8e80712b98807a2ba2" +
            "807f03f30001f01f430b0113620001928e0220000241062b66012b7a0183082b" +
            "6680702b7a807d03f40001f026430b011376000192a602200002410918f00118" +
            "f10118f501830c18f080da18f180d818f580e503fb0001f02d430b0114940001" +
            "928602200002410c283d01283e01283f012840018310283d806f283e8080283f" +
            "80872840808b03fc0001f01f430b011508000192a10220000441061b60011b64" +
            "0183081b6080d91b6480e903fd0001f065430b01152300019286022000024124" +
            "6eac016ead016eae016eaf016eb0016eb1016eb2016eb3016eb4016eb5016eb6" +
            "016ed20183306eac80866ead80ff6eae81006eaf80fe6eb081016eb181026eb2" +
            "81036eb381046eb4811b6eb5811c6eb6811a6ed2811d03fe0001f026430b0115" +
            "38000192a10220000441091afa011b03011b0601830c1afa80571b0380c21b06" +
            "80e203ff0001f03b430b0115520001928602200002411216a90116aa0116ab01" +
            "16af0116b00116b101831816a980af16aa80ae16ab80b216af820916b0820416" +
            "b180b104010001f026430b01158200019286022000024109285501285701285b" +
            "01830c2855808928578085285b807b04020001f02d430b011597000192a10220" +
            "0004410c275301276001276201276301831027538055276080cb2762827c2763" +
            "827b04030001f018430b0116120001928102200004410315e101830415e18120" +
            "04040001f018430b011626000192a10220000441031146018304114680500406" +
            "0001f018430b011656000192a60220000241031194018304119483e504070001" +
            "f02d430b0116710001928602200002410c14be0114bf0114c00114c201831014" +
            "be808214bf809a14c080ad14c283e204090001f018430b0109640001928e0220" +
            "0002410327750183042775809ee8092756";

            payloads[1] =
            "bcf3df006acf0109f0024000f3d0040b0001f01f430b0109930001928e022000" +
            "0441060001010002018308000182aa000282ab040f0001f168430b0110530001" +
            "9286022000024193288701288801288901288a01288b0128a00228a10228a202" +
            "28a30228a40228a50228a60228a70228a80228ac0228ad0228ae0228af0228b0" +
            "0228b10228b20228b30228b40228b50228b60228ba0228bb0228bc0228bd0228" +
            "c00228c10228c20228c80228c90228ca0228cb0228cc0228cd0228ce0228cf02" +
            "28d30228d40228d50228d60228d70228d80228d90228da0228db0283c4288780" +
            "842888809028898093288a8096288b80b428a082e628a182e728a282e828a382" +
            "e928a482ea28a5833928a682eb28a782ec28a882ed28ac82ff28ad830128ae83" +
            "0228af830428b0830028b182fc28b282fd28b382fe28b4830328b5832728b683" +
            "2628ba830528bb830728bc830628bd833528c0830e28c1830f28c2831028c883" +
            "1128c9831228ca831328cb831428cc831528cd831628ce831828cf831728d383" +
            "1928d4831b28d5831d28d6831e28d7831f28d8832128d9831a28da831c28db83" +
            "2004100001f01f430b011067000192a10220000441067a4b017a4c0183087a4b" +
            "80c17a4c811904160001f018430b011156000192a10220000441037729018304" +
            "772980d7041b0001f01f430b0107430001928102200004410670320170350183" +
            "087032829470358290041d0001f018430b01077300019286022000034103526c" +
            "018304526c808e04250001f0b2430b01089100019286022000024145286f0128" +
            "710128730128e10228e20228e30228e40228e50228e60228ea0228eb0228ec02" +
            "28ed0228ee0228ef0228f00228f10228f40228f50228f60228f70228f80228f9" +
            "02835c286f80882871808a2873808c28e182ee28e282ef28e382f028e482f128" +
            "e582f428e682f228ea82f528eb82f628ec82f728ed82f828ee82fa28ef82fb28" +
            "f082f928f1833a28f4830828f5830928f6830b28f7830c28f8830a28f9830d04" +
            "2c0001f01f430b011778000192a50295000941061644011645018308164480a2" +
            "1645803904310001f018430b012109000192810275000341036e400183046e40" +
            "829504340001f01f430b011934000192ae02970002410622c60122d401830822" +
            "c680c322d4810f04360001f03b430b011973000192a10275000341126ff0016f" +
            "f1016ff70170040170060170080183186ff080606ff1805f6ff780297004809b" +
            "700680e07008809504370001f050430b0119530001928102750003411b6d6601" +
            "6d67016d68016d6b016d6c026d6d026d6e016d6f026d710283246d66828c6d67" +
            "828d6d68828f6d6b82936d6c83236d6d83246d6e82916d6f83316d7183256122" +
            "f922";

            payloads[2] =
            "bcf3c0006acf0209f0024000f3b1043a0001f02d430b012051000192a1027500" +
            "03410c4e22014e24014e25014e280183104e2280b94e2480ba4e2580b84e2882" +
            "0704410001f05e430b012187000192810275000341212ee3012ee4012ee5012e" +
            "e6012ef4012f08012f1c012f1d012f30012f3a012f3b02832c2ee380722ee482" +
            "052ee582062ee6820a2ef480762f0880782f1c80752f1d80792f3080a72f3a80" +
            "832f3b833b04420001f026430b012207000192ae029700024109233101233601" +
            "233701830c233180c0233680c9233780c804430001f049430b01222600019281" +
            "027500034118708001708501708a01708f0170940179e00179f40179fe018320" +
            "708080b5708580bb708a80b6708f80b77094809179e0801c79f4805c79fe807e" +
            "04460001f0ea430b012285000192b602970002415d23950223b40223b60223b7" +
            "0223c10223c20223c30223c40223c50223c60223c70223cb0223cc0223cd0223" +
            "ce0223d00223d70223d80223d90223da0223db0223dc0223dd0223de0223df02" +
            "23e00223e10223e20223e30223ea0223ee02837c2395836c23b4836223b682e4" +
            "23b7836823c1834c23c2834b23c3834823c4834623c5834723c6834a23c78349" +
            "23cb834d23cc836423cd834423ce834e23d0836123d7836523d8835923d9836f" +
            "23da835f23db836023dc835823dd836323de835423df835a23e0834f23e18357" +
            "23e2834523e3835523ea839323ee836a04490001f018430b0123440001928603" +
            "000002410307da01830407da82ac044c0001f01f430b012402000192ae029700" +
            "02410622040122050183082204810a22058109044d0001f02d430b0118360001" +
            "928102750003410c6dca016dcb016dcf016dd10183106dca828b6dcb82986dcf" +
            "82966dd1829704520001f026430b012522000192a60220000241091a91011a92" +
            "011a9301830c1a9180591a9281141a93811504530001f034430b012544000192" +
            "8102200004410f445c01445d01445e01445f014463018314445c8073445d8074" +
            "445e8077445f807c4463820804540001f018430b012551000192a10220000441" +
            "032f5a0183042f5a812304550001f018430b0125740001928602200002410315" +
            "180183041518829d04570001f02d430b0126040001928102200004410c1c7902" +
            "1c7a011c7c021efb0283101c79833c1c7a80561c7c833d1efb832904590001f0" +
            "5e430b01263300019281022000044121313801313901313c0131460131590131" +
            "6e02316f02317002317402317502317602832c313880d0313980cf313c80a531" +
            "4680be315980d3316e832d316f8337317083943174832c3175832e3176832f91" +
            "7bed4b";

            payloads[3] =
            "bcf3c8006acf0309f0024000f3b9045b0001f08f430b01266200019281022000" +
            "044136332f013330013336013339013341023342023343023344023345023346" +
            "02334702334802334902334a02334b02334d02334e023354028348332f80ca33" +
            "3080a93336829c333980b0334183843342833f33438385334483863345838733" +
            "468388334783893348838a3349838b334a838c334b838d334d838e334e838f33" +
            "548391045d0001f02d430b0126920001928102200004410c32d50232d60132da" +
            "0132db01831032d5833432d6809232da80a332db80f003f00002f018430b0113" +
            "18000192a10220000441037473018304747380d607d70002f026430b01183600" +
            "02828102750004410916f001178f01179101830c16f0815d178f81931791815b" +
            "07db0002f018430b01191400028281027500044103146d018304146d81f807dc" +
            "0002f018430b011934000282a102750004410318100183041810819407e30002" +
            "f026430b0120700002828102750004410916780116cc01190401830c16788159" +
            "16cc81911904817107e60002f049430b012129000282a1027500044118196901" +
            "196a01197801197a01621601621901621a01621c0183201969815e196a815719" +
            "788168197a819e621681696219819f621a815a621c81a007e70002f01f430b01" +
            "214800028281027500044106159a01159c018308159a8190159c81fc07e80002" +
            "f018430b012168000282a602750002410313e001830413e0817407f30002f03b" +
            "430b012382000282810275000441120b5e010b60010b64011843011846011847" +
            "0183180b5e81660b6081670b64816b18438165184681641847816307f60002f0" +
            "18430b012441000282a50295000641031cea0183041cea82b907f90002f02643" +
            "0b01071400028281022000044109240401240e01241901830c240481a6240e81" +
            "472419814907fd0002f026430b01077300028281022000044109189d01189e01" +
            "18f601830c189d8134189e813518f681e107fe0002f034430b010788000282a1" +
            "02200004410f284301288c02288d02288f0228900283142843816f288c83a528" +
            "8d83a6288f83a9289083aa07ff0002f042430b01080300028281022000044115" +
            "190102191701193202193402194002194202194602831c190183a71917813619" +
            "3283a2193483a4194083a8194283a3194683ab08020002f02d430b0108470002" +
            "82a602300003410c1b1c011b1d011b27011b280183101b1c812e1b1d812d1b27" +
            "81431b2881b708050002f03b430b01089100028281022000044112278a0127ab" +
            "0127b10127b30127b40127b5018318278a813027ab813a27b181b927b3813d27" +
            "b4813f27b5813c58fc4d7c";

            payloads[4] =
            "bcf380006acf0409f0024000f37108060002f02d430b010906000282a1022000" +
            "04410c27da0127f901280501281501831027da813b27f9814128058140281581" +
            "3e08080002f034430b010936000282a102200004410f206c0120770120940120" +
            "950120a8018314206c814a2077814420948146209581a720a8814808090002f0" +
            "49430b010964000282810220000441181e1e011e1f011e23011e24011e25011e" +
            "28011e46011e500183201e1e81521e1f81531e2381551e2481501e2581561e28" +
            "814d1e4681321e508196080d0002f02d430b0110240002828e02300003410c22" +
            "d90122dc0122e30122e401831022d9817022dc813822e3813322e481b8080e00" +
            "02f026430b011038000282a1022000044109ccc401ccd801cce201830cccc481" +
            "86ccd88178cce2817b08100002f018430b011068000282a6023000034103514a" +
            "018304514a814e08110002f02d430b0110810002828102200004410cc48201c4" +
            "9b01c4ae01c4bd018310c48281d6c49b81dec4ae8158c4bd818a08120002f018" +
            "430b011097000282a602300003410352170183045217813908130002f02d430b" +
            "0111120002828102200004410cc4e001c4e501c4f401c51c018310c4e08183c4" +
            "e581fac4f481cac51c81f208140002f01f430b011127000282a1022000044106" +
            "52d001530201830852d081315302814b08290002f01f430b0114640002828102" +
            "2000044106d3d001d3d5018308d3d08175d3d581f5082a0002f02d430b011479" +
            "000282a102200004410cc6c101c6c501c6c801c6cf018310c6c18181c6c58200" +
            "c6c881ccc6cf8182082b0002f018430b01149400028281022000044103572001" +
            "8304572081ea082c0002f018430b011508000282a6023000024103d7e7018304" +
            "d7e78176082e0002f02d430b011538000282a102200004410c58560158570158" +
            "6701587b02831058568188585781f358678189587b83bb082f0002f073430b01" +
            "15530002828102200004412a254d02255702255802255902256202256702d742" +
            "01d74901d74a01d75c02d75f02d76102d76202d76f018338254d83b8255783ba" +
            "255883ac255983af256283ae256783add742817fd74981dcd74a81d7d75c83b9" +
            "d75f83b0d76183b5d76283a1d76f81f708300002f049430b011568000282a102" +
            "2000044118d36101d36d01d37c01d38101d38601d39b01d39f01d3a9018320d3" +
            "6180ccd36d8184d37c8177d38181fed38681ebd39b81cdd39f81ecd3a981f90e" +
            "a3705f";

            payloads[5] =
            "bcf3e5006acf0509f0024000f3d608310002f065430b01158200028281022000" +
            "044124cb8601cb8801cb8b01cb8d01cba701cbab01cbdc02cbde02cbe402cbe5" +
            "02cbe602cbe8028330cb86816ccb888185cb8b81d5cb8d81ffcba781afcbab81" +
            "c9cbdc83b6cbde83b7cbe483b3cbe583b2cbe683b1cbe883b408350002f01843" +
            "0b011641000282860230000241031bbc0183041bbc817a08370002f018430b01" +
            "167200028286023000024103c472018304c472817208380002f018430b011686" +
            "000282a6023000024103d860018304d8608173083a0002f01f430b0112250002" +
            "82a6023000024106cf9b01cf9e018308cf9b817ecf9e81f4083b0002f034430b" +
            "0112650002828102750002410f1c27011c28011c29011c2b011c2c0183141c27" +
            "81e81c2881c81c2981e71c2b81da1c2c81f6083c0002f018430b011265000282" +
            "a1027500024103cb97018304cb97817c083e0002f07a430b011307000282a102" +
            "750004412dd03e01d04301d04801d04d01d05201d05c01d06101d06601d06b01" +
            "d07001d07501d07a01d07f01d08401d09301833cd03e81abd04381add04881a9" +
            "d04d81aad05281c0d05c81bcd06181c1d06681bbd06b81aed07081bdd07581ac" +
            "d07a81bed07f81a4d08481a5d093817d083f0002f026430b0113440002828102" +
            "7500044109cfe501cfe901cfef01830ccfe5815fcfe98160cfef816108400002" +
            "f057430b011345000282a102750004411ec6d701d7a201d7a701d7a901d7aa01" +
            "d7ad01d7ae01d7b101d7b201d7ff018328c6d7819dd7a281b0d7a781d4d7a981" +
            "9cd7aa81d1d7ad816dd7ae819ad7b1819bd7b281b1d7ff81c308440002f02d43" +
            "0b011426000282a502950006410ccd5501cd5f01cd6401cd6e018310cd558137" +
            "cd5f8187cd648162cd6e816a0c820003f08f430b011739000235b60299000241" +
            "36177a01177f01178e0117af0117b10117b30117b40117c00117ca0117cc0117" +
            "ce0117d10117e00117ea0117f00217f10217f2021816018348177a8066177f80" +
            "36178e800b17af802e17b1802717b3803717b4802017c0800217ca800617cc80" +
            "1d17ce800e17d1801317e0802c17ea801b17f082c917f182cc17f282ca181680" +
            "350c880003f050430b011856000235b602990002411b1b6c011b6f011b73011b" +
            "7c011b92011bfc011bfd011bff011c020183241b6c80041b6f80051b7380071b" +
            "7c800a1b92800d1bfc801e1bfd80331bff80321c02803b0c8b0003f018430b01" +
            "1914000235960300000341034e830183044e8380f70c8c0003f049430b011934" +
            "000235b602750003411836e30136e60136e70136f40136f60136f80136f90136" +
            "fa01832036e3804136e6803136e7804536f4803c36f6804336f8801136f98014" +
            "36fa802868cb0d48";

            payloads[6] =
            "bcf3c0006acf0609f0024000f3b10c8e0003f02d430b011973000235b6029900" +
            "02410c37270137290137470237480283103727810637298107374782cd374882" +
            "d40c900003f01f430b012012000235a60299000241063c38013c3a0183083c38" +
            "80f43c3a805d0c910003f026430b0120320002358502750009410917d50117d6" +
            "0117d701830c17d5803417d6805e17d780250c930003f018430b012070000235" +
            "81027500034103335a018304335a83de0c940003f034430b012090000235b602" +
            "990002410f030601030d01031201031601031b0183140306802f030d80610312" +
            "80f303168108031b80300c950003f01f430b012109000235960299000241063c" +
            "fc013cfd0183083cfc810d3cfd80f10c960003f088430b012129000235b60299" +
            "0002413351ae0151b30151b80151bd0151c20151d10151d50151da0151db0151" +
            "e00151e50152780152840152da0252db0252dc0252eb02834451ae801851b380" +
            "1951b8801751bd802251c2802351d1800c51d5806451da83e451db801651e080" +
            "2151e58015527880425284810e52da82dc52db82dd52dc82de52eb82c40c9800" +
            "03f042430b012168000235a1027500034115145101145201145a011469011478" +
            "01147d02147e02831c145180ed145280f6145a80f2146980bf147880c4147d83" +
            "40147e83410c990003f0c7430b0121870002359602990002414e4ed102521c01" +
            "522101522501522601522b01523001523f01524d01524f015250015252015253" +
            "02525802525902525a02525b02525c02525d02525e02525f0252600252620252" +
            "650152680152b00183684ed182d2521c80095221800f5225800152268010522b" +
            "800852308003523f8040524d804f524f804c5250804b5252804d525382d05258" +
            "82bd525982be525a82bf525b82c0525c82c1525d82c8525e82d5525f82d75260" +
            "82c7526282cf5265804e5268804752b080120ca10003f034430b012344000235" +
            "8602990002410f1f43011f4d011f73021f7a021f7d0283141f4380ec1f4d83dd" +
            "1f73837e1f7a837c1f7d837f0ca20003f02d430b012363000235b60295000341" +
            "0c132e01134c02134d021351028310132e80ee134c837b134d837a1351839204" +
            "510035f049430b012515000192810220000441180fe0010fe201100601100b01" +
            "101301101a02101b02101d0183200fe0810c0fe2803f100683e0100b810b1013" +
            "83e3101a82da101b82db101d83e1005e0055f02d430b01121900013086029900" +
            "07410c03e80104740104c401055001831003e882a2047482a904c482a1055082" +
            "a400600055f018430b0108530001308602990007410309c401830409c482a658" +
            "eb0ded";

            payloads[7] =
            "bcf39a006acf0709f0024000f38b00050085f049430b01246000019281027500" +
            "034118003001003e0100ad0200b10202fc010304010308010701018320003080" +
            "ac003e80cd00ad833000b1832202fc829b030480980308809907018094000700" +
            "85f03b430b01214800019281027500034112003d0100410100a00200a90200aa" +
            "020306018318003d80bd0041809c00a0832800a9832b00aa832a030680aa0008" +
            "0085f018430b01207100019285027500094103007d018304007d8054000f0085" +
            "f01f430b010921000192810220000541060023010026018308002380df002680" +
            "ab00210085f034430b012480000192a102750003410f002f01003301003f0103" +
            "81010384018314002f80b300338081003f8097038180a00384809f2e1800b0f0" +
            "2d430b010873000130a602750003410c000101000301000401000a0183100001" +
            "82670003826300048266000a826517700110f01f430b011919000130a1029900" +
            "044106006602006a028308006683bc006a83790001013ef01f430b0123030001" +
            "30a60275000341060321010324018308032181220324829e02bc013ef01f430b" +
            "0113340001308102750003410600110103e9018308001182b303e982a8038401" +
            "3ef034430b0113730001308602750003410f01f60101f70101f80101fa0101fc" +
            "01831401f6822e01f7825e01f8826001fa825c01fc824f1388013ef034430b01" +
            "1727000130a602990003410f332d01332e01333001334a02334f028314332d81" +
            "2a332e826233308233334a83c1334f835113ef013ef02d430b01174700013086" +
            "02750003410c252301252601252901252c018310252382712526827825298279" +
            "252c82611450013ef073430b011766000130a602990003412a0d49010d4a010d" +
            "4b010d71020d72020d73020d74020d75020d76020d77020d78020d79020d7b02" +
            "0d7d0283380d49820d0d4a820e0d4b820f0d7183c30d7283c40d7383c50d7483" +
            "c00d7583bd0d7683be0d7783810d7883800d7983820d7b83bf0d7d83831b5801" +
            "3ef03b430b012111000130a102750003411202c10102c20102c90102ca0202cb" +
            "0202d301831802c1826d02c2826c02c9823702ca837702cb837802d381251c20" +
            "013ef049430b012149000130a10275000341180e30020e3a020e43020e44021c" +
            "22011c380129d10129ee0283200e3083c90e3a83c60e4383c70e4483c81c2282" +
            "7a1c38827e29d1823129ee837d1c84013ef026430b0121690001308602750003" +
            "4109016101016d02018001830c01618253016d83d20180823ba94afe06";

            payloads[8] =
            "bcf3d6006acf0809f0024000f3c71f40013ef050430b012303000130a6027500" +
            "03411b0001010dad010dae010dfd020dfe020dff020e00020e01020e02028324" +
            "0001829f0dad82870dae82880dfd83710dfe83750dff83720e0083740e018376" +
            "0e0283731fa4013ef01f430b0123220001308102750003410639d10139d80183" +
            "0839d1824e39d8827d2134013ef026430b012399000130860297000241090389" +
            "01038a01038e01830c03898274038a8275038e826822c4013ef034430b012475" +
            "0001308602990003410f07d20107d40107d50107e101083802831407d2823207" +
            "d4827707d5826407e18281083883c22328013ef026430b012520000130a50275" +
            "00044109233101233501233701830c23318128233581292337822c238c013ef0" +
            "2d430b0125390001308603000004410c044d01044e01044f010450018310044d" +
            "8213044e82ae044f80650450806e2454013ef042430b01257700013086027500" +
            "034115064101064201064601064c01064d01064e01065301831c0641826e0642" +
            "82270646824c064c827f064d8212064e82380653822024b8013ef018430b0125" +
            "97000130a10275000341032065018304206582342af8013ef018430b01071900" +
            "0130a60275000341031139018304113982492b5c013ef018430b010727000130" +
            "8603000003410311fa01830411fa82af2bc0013ef02d430b010758000130a602" +
            "750003410c427f01428d01428e01428f018310427f8241428d8240428e824342" +
            "8f823e2c24013ef018430b0107750001308602990007410305fc01830405fc82" +
            "3a2c88013ef018430b010796000130a60275000341033d580183043d5882462d" +
            "50013ef01f430b010834000130a60275000341060db5010db70183080db58244" +
            "0db782472e7c013ef034430b0108920001308602750003410f12c50112c60112" +
            "c70112c90112ca01831412c582a012c6823f12c7824d12c9824512ca82482f44" +
            "013ef042430b01093000013086030000024115420801421c01422d0142300142" +
            "3201423301423401831c42088280421c82a7422d822f42308211423282a54233" +
            "82a342348269300c013ef096430b010971000130860297000241394336014345" +
            "02434602434702434802434902434a02434b02434c02434d02434e02434f0243" +
            "5002435102435202435302435402435502435702834c43368236434583d34346" +
            "83d4434783d5434883d7434983d8434a83dc434b83cd434c83ce434d83cf434e" +
            "83d0434f83ca435083cb435183cc435283d9435383da435483db435583d64357" +
            "83d13070013ef02d430b010992000130a102750002410c214601214701214801" +
            "21490183102146821b214782292148822a2149821c4d9c4b03";

            payloads[9] =
            "bcf2d4006acf0909f0024000f2c530d4013ef02d430b01101300013086029900" +
            "03410c452f014530014532014536018310452f82214530821d4532821f453682" +
            "283138013ef034430b011034000130a102750003410f06a90106ac0106ae0106" +
            "b10106b601831406a9828406ac825006ae825206b1811f06b6811e3200013ef0" +
            "2d430b011075000130a603000003410c0e81010e83010e85010e860183100e81" +
            "82390e83824a0e85820b0e86820c332c013ef01f430b01113700013081027500" +
            "0341061ca5011cbb0183081ca5824b1cbb825433f4013ef026430b0111790001" +
            "3086027500034109132a01132e01136101830c132a823c132e81261361825a3b" +
            "c4013ef02d430b0115660001308602990003410c031f01032101032701032801" +
            "8310031f827603218255032780c70328825d3c28013ef026430b011585000130" +
            "a6027500044109036701036901036a01830c036781170369825f036a81163d54" +
            "013ef057430b0116420001308602750003411e051b01051c01051d01051e0105" +
            "1f01052001052401052601052901052b018328051b8216051c8214051d825905" +
            "1e8230051f82170520822605248286052681240529821a052b82193db8013ef0" +
            "57430b011662000130b602750003411e423b0142410142420142430142460142" +
            "4c014250014251014252014254018328423b82424241823d4242821542438223" +
            "42468235424c825b425082184251822b425282254254821e23f0013ff018430b" +
            "012558000130a602750004410304b501830404b5826a26ac013ff018430b0126" +
            "9200013096027500034103019f018304019f80c63e1c013ff018430b01168100" +
            "01308e02750003410301330183040133822d04b001b7f018430b011432000130" +
            "a60299000341030080018304008082221a2cfbfff018430b0120540001308502" +
            "99000441031c4d0183041c4d82581af4fbfff01f430b01209200013086029900" +
            "03410610060134de0183081006822434de82106c71c737";

            Network netw = new();
            foreach (string s in payloads) {
                byte[] payload = BigInteger.Parse(s, System.Globalization.NumberStyles.HexNumber).ToByteArray().Reverse().ToArray();
                Span<byte> span = payload;
                byte[] v = span.Slice(1).ToArray();
                tableHeader hdr = getHeader(v);
                netw.addsection(hdr, span.Slice(8));
            }

        }

        private Channel findchannels(string servicename)
        {
            
            return channels.Find(x => x.Servicename == servicename);

        }
    }
}