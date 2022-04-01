using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Protocol;
using static Interfaces.DVBBase;

namespace Sat2Ip
{
    /* Excerpt from TS 101 211 (https://www.etsi.org/deliver/etsi_ts/101200_101299/101211/01.12.01_60/ts_101211v011201p.pdf)
     * 
     * For terrestrial delivery systems bandwidth within a single transmitted TS is a valuable resource and in order to
     * safeguard the bandwidth allocated to the primary services receivable from the actual multiplex, the following minimum
     * repetition rates are specified in order to reflect the need to impose a limit on the amount of available bandwidth used for
     * this purpose:
     * a) all sections of the NIT shall be transmitted at least every 10 s;
     * b) all sections of the BAT shall be transmitted at least every 10 s, if present;
     * c) all sections of the SDT for the actual multiplex shall be transmitted at least every 2 s;
     * d) all sections of the SDT for other TSs shall be transmitted at least every 10 s if present;
     * e) the TDT shall be transmitted at least every 30 s;
     * f) the TOT (if present) shall be transmitted at least every 30 s;
     * g) all sections of the EIT Present/Following Table for the actual multiplex shall be transmitted at least every 2 s;
     * h) all sections of the EIT Present/Following Tables for other TSs shall be transmitted at least every 20 s if present.
     * 
     * The repetition rates for further EIT tables will depend greatly on the number of services and the quantity of related SI
     * information. The following transmission intervals should be followed if practicable but they may be increased as the use
     * of EIT tables is increased. The times are the consequence of a compromise between the acceptable provision of data to a
     * viewer and the use of multiplex bandwidth.
     * 
     * a) all sections of the EIT Schedule table for the first full day for the actual TS, should be transmitted at least every 10 s, if present;
     * b) all sections of the EIT Schedule table for the first full day for other TSs, should be transmitted at least every 60 s, if present;
     * c) all sections of the EIT Schedule table for the actual TS, should be transmitted at least every 30 s, if present;
     * d) all sections of the EIT Schedule table for other TSs, should be transmitted at least every 300 s, if present
     */
    public class Scanner
    {
        private RtpReader reader;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
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
        private int _portdata;
        private int _portreport;
        private List<Channel> pids;
        private List<Bouquet> m_bouquets = new();
        private List<Bouquet> m_sessionbouquets = new(); /* Bouquets found during this scan */
        private List<Network> m_sessionnetworks = new(); /* Networks found during this scan */
        private FastScanBouquet m_fstnetw;
        private String scanquery;
        private RTSP rtsp;
        private Payloads payloads;
        private List<Transponder> nit = new();
        private List<Network> m_networks = new();
        private bool[] sdtsectionprocessed;
        private bool[] patsectionprocessed;
        private bool[] batsectionprocessed;
        Dictionary<int, bool[]> pmtsections;
        private bool patreceived;
        private bool batreceived;
        private bool expectNIT;
        private bool sdtreceived;
        private bool catreceived;
        private bool nitreceived;
        private Transponder m_transponder;
        private Bouquet m_bouquet;
        private Stopwatch m_stopwatch;
        private bool fstnetworkreceived;
        private bool m_fastscan;

        public List<Network> networks { get { return m_networks; } set { m_networks = value; if (m_networks == null) m_networks = new(); } }
        public List<Bouquet> bouquets { get { return m_bouquets; } set { m_bouquets = value; if (m_bouquets == null) m_bouquets = new(); } }
        public Transponder Transponder { get { return m_transponder; } set { m_transponder = value; } }

        public int SCANTIMEOUT { get; private set; }
        public Scanner(int portdata, int portreport, RTSP rtsp)
        {
            _portdata = portdata;
            _portreport = portreport;

            reader = new RtpReader(portdata);

            this.rtsp = rtsp;
            patreceived = false;
            batreceived = false;
            sdtreceived = false;
            catreceived = false;
            nitreceived = false;
            fstnetworkreceived = false;
            m_fastscan = false;
            SCANTIMEOUT = 20000;/* Default 20 seconds timeout */
        }
        public void stop()
        {
            reader.stop();
            //reader = null;
        }

        public async Task<List<Channel>> scan(Transponder transponder)
        {
            patreceived = false;
            batreceived = false;
            sdtreceived = false;
            catreceived = false;
            nitreceived = false;
            expectNIT = false;
            sdtsectionprocessed = null;
            patsectionprocessed = null;
            pmtsections = new Dictionary<int, bool[]>();
            m_sessionbouquets = new();
            m_sessionnetworks = new();
            pids = new List<Channel>();
            payloads = new Payloads(processpayload);

            log.DebugFormat("Scanning transponder: {0}", transponder.frequency);
            Task scantask = ReadData();
            m_transponder = transponder;
            scanquery = m_transponder.getQuery();
            scanquery = scanquery + "&pids=0,1,17";
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=0,1,17");

            await scantask;
            rtsp.commandTeardown("");
            log.DebugFormat("Scanning transponder complete: {0}, channels: {1}", transponder.frequency, pids.Count);
            return pids;
        }
        public async Task<FastScanBouquet> scanfast(FastScanLocation location, Transponder tsp)
        {
            fstnetworkreceived = false;
            m_fstnetw = new FastScanBouquet();
            m_fstnetw.network = new Network();
            m_fstnetw.location = location;
            m_fastscan = true;
            log.DebugFormat("Fast Scan transponder: {0} - {1}", location.frequency, location.name);

            Channel channel = new Channel(location.pid, tsp);
            channel.Programnumber = location.pid;
            log.DebugFormat("Add channel for fastscan {0}. Program number: {1}", location.name, location.pid);
            pids = new List<Channel>();
            pids.Add(channel);
            payloads = new Payloads(processpayload);

            Task scantask = ReadData();
            scanquery = tsp.getQuery();
            scanquery = scanquery + "&pids=" + location.pid;
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=" + location.pid);

            await scantask;
            rtsp.commandTeardown("");
            log.DebugFormat("Scanning transponder complete: {0}, channels: {1}", tsp.frequency, pids.Count);
            return m_fstnetw;
        }

        protected void OnNITReceived()
        {
            log.Debug("NIT Received and processed");
            nitreceived = true;
            if (m_stopwatch.Elapsed.TotalSeconds > 10) /* No more NIT expected */
            {
                rtsp.commandPlay("?delpids=16");
            }
        }
        protected void OnCATReceived()
        {
            log.Debug("CAT Received and processed");
            catreceived = true;
            rtsp.commandPlay("?delpids=1");
        }
        protected void OnBATReceived()
        {
            log.DebugFormat("BAT received and processed: id {0} ({0:X}), name: {1}",m_bouquet.bouquet_id, m_bouquet.bouquet_name);
            if (m_stopwatch.Elapsed.TotalSeconds > 10) /* No more BAT expected */
            {
                batreceived = true;
                if (sdtreceived)
                    rtsp.commandPlay("?delpids=17");
            }
        }
        protected void OnPATReceived(EventArgs e)
        {
            patreceived = true;
            rtsp.commandPlay("?delpids=0");

            /* pids is now populated, add pid=17 for BAT and SDT */
            string strpids = "17";
            foreach (Channel channel in pids)
            {
                if (channel.Programpid == 0)
                    continue; /* Currently we do not processes this one because it is a network PID*/
                else
                {
                    strpids = strpids + "," + channel.Programpid.ToString();
                }
            }
            rtsp.commandPlay("?addpids=" + strpids);
        }
        protected void OnPMTReceived(int payloadpid)
        {
            rtsp.commandPlay("?delpids=" + payloadpid);
        }
        protected void OnSDTReceived()
        {
            /* pids is now populated */
            sdtreceived = true;
            if (batreceived)
                rtsp.commandPlay("?delpids=17"); 
        }
        public class PATReceivedArgs : EventArgs
        {
            public List<Channel> pids { get; set; }
            public PATReceivedArgs(List<Channel> pids)
            {
                this.pids = pids;
            }
        }
        public class SDTReceivedArgs : EventArgs
        {
            public List<Channel> pids { get; set; }
            public SDTReceivedArgs(List<Channel> pids)
            {
                this.pids = pids;
            }
        }
        public class ScanresultArgs : EventArgs
        {
            public List<Channel> pids { get; set; }
            public ScanresultArgs(List<Channel> pids)
            {
                this.pids = pids;
            }
        }
        private async Task ReadData()
        {
            m_stopwatch = new Stopwatch();
            m_stopwatch.Start();

            reader.start();
            Task<RtpPacket> task = reader.readAsync();
            RtpPacket packet = await task;
            while (reader.active == true && 
                   isComplete() == false && 
                   packet != null && 
                   (m_stopwatch.ElapsedMilliseconds < SCANTIMEOUT))
            {
                task = reader.readAsync();
                processMpeg2Packets(packet);
                packet = await task;
            }
            reader.stop();
            if (!isComplete())
                log.Debug("Not all expected data received, SCAN timeout occurred");

            foreach (Bouquet b in m_bouquets.ToArray())
            {
                if (!b.complete)
                {
                    m_bouquets.Remove(b);
                }
            }
            foreach (Network n in m_networks.ToArray())
            {
                if (!n.complete)
                {
                    m_networks.Remove(n);
                }
            }
            log.Debug("ReadData completed, no more ASYNC I/O Pending");
            m_stopwatch.Stop();
        }
        private bool isComplete()
        {
            if (m_fastscan)
                if (fstnetworkreceived)
                    return true;
            if (!batreceived && m_stopwatch.Elapsed.TotalSeconds > 10)
                batreceived = true; /* BAT is optional, so if not received within the transmission interval, assume it was there */
            if (sdtreceived && patreceived && batreceived)
            {
                if (expectNIT)
                {
                    if (nitreceived)
                        return true;
                }
                else
                    return true;
            }
            return false;
        }
        private void processMpeg2Packets(RtpPacket packet)
        {
            int lenprocessed = 0;
            Mpeg2Packet mp2Packet;
            byte[] rtpPayload = packet.getPayload();
            byte[] payloadpart;
            Payload payload;
            while (lenprocessed < rtpPayload.Length && isComplete() == false) 
            {
                payloadpart = packet.getPayload(lenprocessed, 188);
                mp2Packet = new Mpeg2Packet(payloadpart);
                payload = payloads.storePayload(mp2Packet);
                /*
                if (payload != null && payload.isComplete())
                {
                    if (payload.payloadlength > 0)
                    {
                        processpayload(payload);
                    }
                    payload.clear();
                }
                */
                lenprocessed += 188;
            }

        }
        private void processBAT(byte[] msg)
        {
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();

            tableHeader hdr = getHeader(v);
            m_bouquet = m_sessionbouquets.Find(x => x.bouquet_id == hdr.streamid);
            if (m_bouquet == null)
            {
                m_bouquet = new();
                m_bouquet.bouquet_id = hdr.streamid;
                m_bouquet.transponder = this.m_transponder;
                m_sessionbouquets.Add(m_bouquet);
            }

            //m_bouquet = findBouquet(hdr.streamid);
            if (m_bouquet.complete) return;

            try
            {
                if (msg.Length < hdr.sectionlength)
                {
                    log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", v.Length, hdr.sectionlength);
                    return;
                }
                m_bouquet.addsection(hdr, span.Slice(8));
                if (m_bouquet.complete)
                {
                    Bouquet existing_bouquet = m_bouquets.Find(x => x.bouquet_id == m_bouquet.bouquet_id);
                    if (existing_bouquet == null)
                        m_bouquets.Add(m_bouquet);
                    else
                    {
                        m_bouquets.Remove(existing_bouquet);
                        m_bouquets.Add(m_bouquet);
                    }
                    OnBATReceived();
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed BAT!! Exception is :" + ex.Message);
            }
        }
        private void processpayload(Payload payload)
        {
            //log.DebugFormat("Process payload for PID {0} with length: {1}", payload.payloadpid, payload.payloadlength);
            //Utils.Utils.DumpBytes(payload.data, payload.expectedlength);
            int payloadpid = payload.payloadpid;
            int tableid;
            if (payloadpid == 0) /* PAT Program Association Table */
            {
                if (patreceived == true)
                    return;
                tableid = payload.data[0];
                if (tableid == 0x00)
                {
                    processPAT(payload.getDatapart(0, payload.expectedlength));
                }
                else if (tableid == 0x40) /* We do not use 0x41 which is another network */
                {
                    processNIT(payload.getDatapart(0, payload.expectedlength));
                }
            }
            else if (payloadpid == 1) /* CAT Conditional Access Table */
            {
                if (catreceived == true)
                    return;
                tableid = payload.data[0];
                if (tableid == 0x01)
                {
                    processCAT(payload.getDatapart(0, payload.expectedlength));
                }
            }
            else if (payloadpid == 0x11)   /* SDT (Service Description Table) or BAT (Bouquet Association Table) */
            {
                tableid = payload.data[0];
                //log.DebugFormat("Processing table-id: {0} (0x{0:X})", tableid);
                switch (tableid)
                {
                    case 0x42: processSDT(payload.getDatapart(0, payload.expectedlength)); break;
                    case 0x4A: processBAT(payload.getDatapart(0, payload.expectedlength)); break;
                    case 0x46: /* We only process the SDT for the current network (0x42), other network (0x46) is ignored*/ break;
                    default: log.DebugFormat("payload with unsupported table type: {0}", tableid); break;
                }
            }
            else if (isKnownPid(payloadpid))
            {
                //pointer = payload.data[0];
                tableid = payload.data[0];
                switch (tableid)
                {
                    case 0x02: processPMT(payload.getDatapart(0, payload.expectedlength), payload.payloadpid); break;
                    case 0x40: processNIT(payload.getDatapart(0, payload.expectedlength)); break;
                    case 0x41: /* We do not use 0x41 which is NIT of another network */ break;
                    case 0x72: /* Stuffing table */ break;
                    case 0xbc: processFSTNetwork(payload.getDatapart(0, payload.expectedlength)); break;
                    case 0xbd: processFSTLCN(payload.getDatapart(0, payload.expectedlength)); break;
                    default: log.DebugFormat("payload with unsupported table type: {0}", tableid); break; 
                }
            }
        }

        private void processFSTNetwork(byte[] msg)
        {
            /* Similar to NIT, except everything is considered to be one network */

            log.Debug("FST Network received");
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();
            tableHeader hdr = getHeader(v);

            try
            {
                if (msg.Length < hdr.sectionlength)
                {
                    log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", v.Length, hdr.sectionlength);
                    return;
                }
                int networkid = hdr.streamid;
                m_fstnetw.network.addsection(hdr, span.Slice(8));
                log.DebugFormat("Section {0} of FST Network processed", hdr.sectionnr);
                if (m_fstnetw.network.complete)
                {
                    log.DebugFormat("All sections of current FST Network received");
                    fstnetworkreceived = true;
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed FST Network section!! Exception is :" + ex.Message);
            }

        }

        private void processFSTLCN(byte[] vs)
        {
            //throw new NotImplementedException();
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
        private void processPAT(byte[] msg)
        {
            log.Debug("PAT received");
            /*
            Program Association Table(PAT) section 
            syntax syntax bit index	# of bits	mnemonic
            table_id                    0   8   uimsbf
            section_syntax_indicator    8   1   bslbf
            '0'                         9   1   bslbf
            reserved                    10  2   bslbf
            section_length              12  12  uimsbf
            transport_stream_id         24  16  uimsbf
            reserved                    40  2   bslbf
            version_number              42  5   uimsbf
            current_next_indicator      47  1   bslbf
            section_number              48  8   bslbf
            last_section_number         56  8   bslbf
            for i = 0 to N
              program_number            56 + (i * 4)    16  uimsbf
              reserved                  72 + (i * 4)    3   bslbf
              if program_number = 0
                    network_PID         75 + (i * 4)    13  uimsbf
              else
                    program_map_pid     75 + (i * 4)    13  uimsbf
              end if
            next
            CRC_32                      88 + (i * 4)    32  rpchof
            Table section legend
            */
            Span<byte> span = msg;
            byte [] v = span.Slice(1).ToArray();
            int nrofsections;

            if (patreceived == true) return;

            tableHeader hdr = getHeader(v);
            if (msg.Length < hdr.sectionlength)
            {
                log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", v.Length, hdr.sectionlength);
                return;
            }
            m_transponder.transportstreamid = (ushort)hdr.streamid;
            if (patsectionprocessed == null || patsectionprocessed.Length != (hdr.lastsectionnr + 1))
            {
                patsectionprocessed = new bool[hdr.lastsectionnr + 1];
            }

            expectNIT = false;

            pids = new List<Channel>();
            nrofsections = (hdr.sectionlength - 4 - 5) / 4;
            for (int i = 0; i < nrofsections; i++)
            {
                ushort program_number = Utils.Utils.toShort(v[7 + (i * 4)], v[8 + (i * 4)]);
                ushort network_pid = 0;
                ushort program_map_pid = 0;
                Channel channel;
                if (program_number == 0)
                {
                    network_pid = (Utils.Utils.toShort((byte)(v[9 + (i * 4)] & 0x1F), v[10 + (i * 4)]));
                    channel = new Channel(network_pid, m_transponder);
                    log.DebugFormat("Expect to receive NIT on PID: {0}", network_pid);
                    expectNIT = true;
                }
                else
                {
                    program_map_pid = (Utils.Utils.toShort((byte)(v[9 + (i * 4)] & 0x1F), v[10 + (i * 4)]));
                    channel = new Channel(program_map_pid, m_transponder);
                }
                channel.Programnumber = program_number;
                log.DebugFormat("Add channel. Program number: {0}, network_pid: {1}, program_map_pid: {2}", program_number, network_pid, program_map_pid);
                pids.Add(channel);
            }
            patsectionprocessed[hdr.sectionnr] = true;
            foreach (bool processed in patsectionprocessed)
                if (!processed)
                {
                    log.Debug("Not all sections of PAT processed yet");
                    return;
                };
            patreceived = true;
            OnPATReceived(new PATReceivedArgs(pids));
        }
        private void processPMT(byte[] msg, int payloadpid)
        {
            /*
             * TS_program_map_section( ) {
                table_id                    8
                section_syntax_indicator    1
                ‘ 0 ’                       1
                reserved                    2
                section_length              12
                program_number              16
                reserved                    2
                version_number              5
                current_next_indicator      1
                section_number              8
                last_sectionnumber          8
                reserved                    3
                PCR_PID                     13
                reserved                    4
                program_info_length         12
                for  (i=0; i < N; i++)  {
                    descriptor()
                }
                for  ( i=0; i<N1;i ++)  {
                    stream_type             8
                    reserved                3
                    elementary_PID          13
                    reserved                4
                    ES_info_length          12
                    for   (j=0; j<N2; j++)  {
                        descriptor()
                    }
                }
                CRC_32                      32

                A descriptor starts with a 1 byte code identifying the descriptor, followed
                by 1 byte length.
                0x0A:   ISO_639_language_descriptor
                0x52:   Stream identifier descriptor
             }
             */
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();

            tableHeader hdr = getHeader(v);
            if (!pmtsections.ContainsKey(payloadpid))
            {
                log.DebugFormat("PMT received for PID: {0}, datalen: {1}", payloadpid, msg.Length);
                pmtsections.Add(payloadpid, new bool[hdr.lastsectionnr + 1]);
            }
            bool[] pmtsectionprocessed = pmtsections[payloadpid];

            ushort pcr_pid = Utils.Utils.toShort((byte)(v[7] & 0x1F), v[8]);
            ushort program_info_length = Utils.Utils.toShort((byte)(v[9] & 0x0F), v[10]);
            /*
             * Here the descriptors should be processed
             */
            Channel channel = findChannel(payloadpid);
            if (channel == null || channel.Pmtpresent) return;
            log.Debug(String.Format("PMT for PID {0} ({1}), pcr_pid {2} ({3}, program_number {4} ({5}))", payloadpid, payloadpid.ToString("X4"), pcr_pid, pcr_pid.ToString("X4"), hdr.programnumber, hdr.programnumber.ToString("X4")));
            int bytesprocessed = 0;
            int offset = 0;
            while (bytesprocessed < program_info_length)
            {
                int descriptorid = v[offset + 11 + bytesprocessed];
                int descriptorlength = v[offset + 12 + bytesprocessed];
                byte[] descriptor = new byte[2 + descriptorlength];
                System.Buffer.BlockCopy(v, offset + 11 + bytesprocessed, descriptor,0, (2 + descriptorlength));
                processdescriptor(descriptor, channel, Channel._descriptorlevel.program, null);
                bytesprocessed = bytesprocessed + 2 + descriptorlength;
            }
            offset = offset + 11 + program_info_length; /* points after descriptors */
            while ((offset+3) < hdr.sectionlength)
            {
                Stream stream = new Sat2Ip.Stream();
                ushort stream_type = v[offset];
                ushort elementary_pid = Utils.Utils.toShort((byte)(v[offset + 1] & 0x1F), v[offset + 2]); 
                ushort ES_info_length = Utils.Utils.toShort((byte)(v[offset + 3] & 0x0F), v[offset + 4]);
                stream.Elementary_pid = elementary_pid;
                stream.Stream_type = stream_type;
                bytesprocessed = 0;
                while (bytesprocessed < ES_info_length)
                {
                    int descriptorid = v[offset + 5 + bytesprocessed];
                    int descriptorlength = v[offset + 6 + bytesprocessed];
                    byte[] descriptor = new byte[2 + descriptorlength];
                    System.Buffer.BlockCopy(v, offset + 5 + bytesprocessed, descriptor, 0, (2 + descriptorlength));
                    processdescriptor(descriptor, channel, Channel._descriptorlevel.stream, stream);
                    bytesprocessed = bytesprocessed + 2 + descriptorlength;
                }

                offset = offset + 5 + ES_info_length;
                channel.Pmt.Add(stream);
            }
            pmtsectionprocessed[hdr.sectionnr] = true;
            foreach (bool processed in pmtsectionprocessed)
                if (!processed)
                {
                    log.DebugFormat("Not all sections of PMT for pid {0} processed yet", payloadpid);
                    return;
                };

            channel.Pmtpresent = true;
            OnPMTReceived(payloadpid);
        }
        private void processCAT(byte[] msg)
        {
            /*
                Conditional Access Table(CAT) section
                syntax syntax bit index	# of bits	mnemonic
                table_id                    0   8   uimsbf
                section_syntax_indicator    8   1   bslbf
                '0'                         9   1   bslbf
                reserved                    10  2   bslbf
                section_length              12  12  uimsbf
                transport_stream_id         24  16  uimsbf
                reserved                    40  2   bslbf
                version_number              42  5   uimsbf
                current_next_indicator      47  1   bslbf
                section_number              48  8   bslbf
                last_section_number         56  8   bslbf
                for i = 0 to N
                	descriptor()
                next
                CRC_32                      88 + (i * 4)    32  rpchof
                Table section legend
            */
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();

            if (catreceived == true) return;
            tableHeader hdr = getHeader(v);
            int sectionbytesprocessed = 7;
            /* Section length is length starting after length including the CRC (3 + 4 = 7) */
            while (sectionbytesprocessed < (hdr.sectionlength - 7))
            {
                int descriptorid = v[sectionbytesprocessed];
                int descriptorlength = (v[sectionbytesprocessed + 1] & 0xff);
                //log.DebugFormat("CAT: desciptorid = {0}, descriptorlength={1}", descriptorid.ToString("X"), descriptorlength);
                sectionbytesprocessed += 2 + descriptorlength;
            }
            OnCATReceived();
        }
        private void processNIT(byte[] msg)
        {
            /*
            tableid 0x40, 0x41
            */
            bool currentnetwork;
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();
            tableHeader hdr = getHeader(v);
            int networkid = hdr.streamid;
            Network netw = m_sessionnetworks.Find(x => x.networkid == hdr.streamid);
            if (netw == null)
            {
                netw = new(m_transponder.diseqcposition);
                netw.networkid = hdr.streamid;
                m_sessionnetworks.Add(netw);
            }
            else
            {
                if (netw.complete)
                    return;
            }
            nit = netw.transponders;
            if (msg[0] == 0x40)
            {
                currentnetwork = true;  
            }
            else
            {
                currentnetwork = false;
            }
            try
            {
                if (msg.Length < hdr.sectionlength)
                {
                    log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", v.Length, hdr.sectionlength);
                    return;
                }

                log.Debug("NIT received");
                netw.addsection(hdr, span.Slice(8));
                if (netw.complete && currentnetwork)
                {
                    log.DebugFormat("All sections of current NIT received");
                    Network existing_network = m_networks.Find(x => x.networkid == netw.networkid);
                    if (existing_network == null)
                        m_networks.Add(netw);
                    else
                    {
                        m_networks.Remove(existing_network);
                        m_networks.Add(netw);
                    }
                    OnNITReceived();
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed NIT section!! Exception is :" + ex.Message);
            }
        }
        private void processdescriptor(byte[] descriptor, Channel channel, Channel._descriptorlevel level, Stream stream)
        {
            switch (descriptor[0])
            {
                case 0x09:/* CA descriptor */
                    ushort CA_system_ID = Utils.Utils.toShort(descriptor[2],descriptor[3]);
                    ushort CA_PID = Utils.Utils.toShort((byte)(descriptor[4] & 0x1F), descriptor[5]);
                    channel.CAlevel = level;

                    if (level == Channel._descriptorlevel.program)
                    {
                        log.Debug(String.Format("CA descriptor at program level. PID {0}, Systemid {1}",CA_PID ,CA_system_ID ));
                        capid capid = new capid();
                        capid.CA_PID = CA_PID;
                        capid.CA_System_ID = CA_system_ID;
                        capid.Cadescriptor = descriptor;
                        channel.Capids.Add(capid);
                    }
                    else
                    {
                        if (stream == null)
                            throw new Exception("CA level is stream, but stream is absent");
                        log.Debug(String.Format("CA descriptor at stream level. PID {0}, Systemid {1}", CA_PID, CA_system_ID));
                        capid capid = new capid();
                        capid.CA_PID = CA_PID;
                        capid.CA_System_ID = CA_system_ID;
                        capid.Cadescriptor = descriptor;
                        stream.capids.Add(capid);
                    }
                    break;
            }
        }
        private Channel findChannel(int payloadpid)
        {
            foreach (Channel channel in pids)
            {
                if (channel.Programpid == payloadpid)
                    return channel;
            }
            return null;
        }
        private Channel findChannelByProgramnumber(int programnumber)
        {
            foreach (Channel channel in pids)
            {
                if (channel.Programnumber == programnumber)
                    return channel;
            }
            return null;
        }
        private Bouquet findBouquet(int bouquetid)
        {
            Bouquet bouquet = m_bouquets.Find(x => x.bouquet_id == bouquetid);
            if (bouquet == null) {
                bouquet = new Bouquet();
                bouquet.bouquet_id = bouquetid;
                bouquet.transponder = m_transponder;
                m_bouquets.Add(bouquet);
            }
            if (m_sessionbouquets.Find(x => x.bouquet_id == bouquetid) == null)
                m_sessionbouquets.Add(bouquet);
            return bouquet;
        }
        private bool isKnownPid(int payloadpid)
        {
            //log.DebugFormat("Considering PID: {0}", payloadpid);
            if (pids != null)
            {
                foreach (Channel channel in pids)
                {
                    if (channel.Programpid == payloadpid)
                        return true;
                }
            }
            log.DebugFormat("PID not found");
            return false;
        }
        private void processSDT(byte[] msg)
        {
            /*
            service_description_section()
            {
            table_id                        8
            section_syntax_indicator        1
            reserved_future_use             1
            reserved                        2
            section_length                  12
            transport_stream_id             16
            reserved                        2
            version_number                  5
            current_next_indicator          1
            section_number                  8
            last_section_number             8
            original_network_id             16
            reserved_future_use             8
            for(i=0;i<N;i++)
            {
                service_id                  16
                reserved_future_use         6
                EIT_schedule_flag           1
                EIT_present_following_flag  1
                running_status              3
                free_CA_mode                1
                descriptors_loop_length     12
                for(j=0;j<N;j++)
                {
                    descriptor()
                }
            }
            CRC_32                          32
            
            tableid 0x42, 0x46 
            */
            Span<byte> span = msg;
            byte[] v = span.Slice(1).ToArray();

            if (patreceived == false) return;
            if (sdtreceived == true) return;
            //Utils.Utils.DumpBytes(msg, msg.Length);
            tableHeader hdr = getHeader(v);
            log.DebugFormat("Processing SDT section {0} of {1}",hdr.sectionnr, hdr.lastsectionnr+1);
            if (sdtsectionprocessed == null || sdtsectionprocessed.Length != (hdr.lastsectionnr+1))
            {
                sdtsectionprocessed = new bool[hdr.lastsectionnr+1];
            }

            if (msg.Length < hdr.sectionlength)
            {
                log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}",v.Length, hdr.sectionlength);
                return;
            }
            ushort original_network_id = Utils.Utils.toShort(v[7], v[8]);
            int offset = 10; /* points to table */
            try
            {
                while (offset < hdr.sectionlength - 7)
                {
                    int service_id = Utils.Utils.toShort(v[offset], v[offset + 1]); ;
                    Channel channel = findChannelByProgramnumber(service_id);
                    if (channel == null)
                        log.Debug(String.Format("NOT FOUND: SDT for service {0} ({1})", service_id, service_id.ToString("X4")));
                    else
                    {
                        channel.EIT_schedule_flag = (v[offset + 2] & 0x02) >> 1;
                        channel.EIT_present_following_flag = (v[offset + 2] & 0x01);
                        channel.running_status = (v[offset + 3] & 0xE0) >> 5;
                        channel.free_CA_mode = (v[offset + 3] & 0x10) >> 4;
                    }
                    ushort descriptors_loop_length = Utils.Utils.toShort((byte)(v[offset + 3] & 0x0F), v[offset + 4]);
                    int bytesprocessed = 0;
                    while (bytesprocessed < descriptors_loop_length)
                    {
                        int descriptorid = v[offset + 5 + bytesprocessed];
                        int descriptorlength = v[offset + 6 + bytesprocessed];
                        byte[] descriptor = new byte[2 + descriptorlength];
                        System.Buffer.BlockCopy(v, offset + 5 + bytesprocessed, descriptor, 0, (2 + descriptorlength));
                        bytesprocessed = bytesprocessed + 2 + descriptorlength;
                        if (channel != null)
                        {
                            processsdtdescriptor(descriptor, channel);
                            log.Debug(String.Format("SDT for service {0} ({1}), pid {2}", service_id, service_id.ToString("X4"), channel.Programpid));
                        }

                    }
                    offset = offset + 5 + descriptors_loop_length;
                }
                sdtsectionprocessed[hdr.sectionnr] = true;
                foreach (bool processed in sdtsectionprocessed)
                    if (!processed) {
                        log.Debug("Not all sections of SDT processed yet");
                        return;
                    };
                OnSDTReceived();
            }
            catch (Exception ex)
            {
                log.Debug("Malformed SDT!! Exception is :" + ex.Message);
            }
        }
        private void processsdtdescriptor(byte[] descriptor, Channel channel)
        {
            /*
             * Documentation from DVB BlueBook A038 
             *      Digital Video Broadcasting (DVB);
             *   Specification for Service Information (SI)
             *              in DVB systems)
             *            DVB Document A038
             *              July 2014
             */
            switch (descriptor[0])
            {
                case 0x42:/* stuffing descriptor*/
                case 0x48:/* service descriptor */
                    int service_type = descriptor[2];
                    channel.Servicetype = service_type;
                    int lenused = 0;
                    channel.Providername = DVBBase.getStringFromDescriptor(descriptor, 3, ref lenused);
                    channel.Servicename = DVBBase.getStringFromDescriptor(descriptor, 3+lenused+1,ref lenused);
                    if (channel.Servicename.Length == 0)
                    {
                        channel.Servicename = channel.transponder.frequency.ToString() + "-" + channel.Programpid.ToString();
                    }
                    break; 
                case 0x49: break; /* country availability descriptor*/
                case 0x4A: break; /* linkage descriptor */
                case 0x4B: break; /* NVOD_reference descriptor */
                case 0x4C: break; /* time shifted service descriptor */
                case 0x50: break; /* component descriptor */
                case 0x51: break; /* mosaic descriptor */
                case 0x53: break; /* CA identifier descriptor */
                case 0x57: break; /* telephone descriptor */
                case 0x5D: break; /* multilingual descriptor */
                case 0x5F: break; /* private data descriptor */
                case 0x64: break; /* data_broadcast descriptor */
                case 0x6E: break; /* announcement support descriptor */
                case 0x71: break; /* service identifier descriptor */
                case 0x72: break; /* service availability descriptor */
                case 0x73: break; /* default authority descriptor */
                case 0x7D: break; /* XAIT location descriptor */
                case 0x7E: break; /* FTA_content_management descriptor */
                case 0x7F: break; /* extension descriptor */

            }
        }

    }
}
