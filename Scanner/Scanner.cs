using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Protocol;
using static Interfaces.DVBBase;
using static Interfaces.PAT;

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
        private List<Channel> m_pids;
        private List<Bouquet> m_bouquets = new();
        private List<Bouquet> m_sessionbouquets = new(); /* Bouquets found during this scan */
        private List<Network> m_sessionnetworks = new(); /* Networks found during this scan */
        private FastScanBouquet m_fstbouquet;
        private FastScanChannels m_fstchannels;
        private String scanquery;
        private RTSP rtsp;
        private Payloads payloads;
        private List<Transponder> nit = new();
        private List<Network> m_networks = new();
        private bool patreceived;
        private bool batreceived;
        private bool expectNIT;
        private bool sdtreceived;
        private bool catreceived;
        private bool nitreceived;
        private bool pmtreceived;
        private Transponder m_transponder;
        private Bouquet m_bouquet;
        private Stopwatch m_stopwatch;
        private bool fstnetworkreceived;
        private bool fstlcnreceived;
        private bool m_fastscan;
        private bool m_channelscan;
        private PAT m_pat;
        private SDT m_sdt;

        public List<Network> networks { get { return m_networks; } set { m_networks = value; if (m_networks == null) m_networks = new(); } }
        public List<Bouquet> bouquets { get { return m_bouquets; } set { m_bouquets = value; if (m_bouquets == null) m_bouquets = new(); } }
        public Transponder Transponder { get { return m_transponder; } set { m_transponder = value; } }

        public int SCANTIMEOUT { get; private set; }
        public Scanner(RTSP rtsp)
        {
            _portdata = rtsp.Startport;
            _portreport = rtsp.Startport+2;

            reader = new RtpReader(_portdata);

            this.rtsp = rtsp;
            patreceived = false;
            batreceived = false;
            sdtreceived = false;
            pmtreceived = false;
            catreceived = false;
            nitreceived = false;
            fstnetworkreceived = false;
            fstlcnreceived = false;
            m_fastscan = false;
            m_channelscan = false;
            SCANTIMEOUT = 20000;/* Default 20 seconds timeout */
        }
        public void stop()
        {
            reader.stop();
            //reader = null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public async Task<List<Channel>> scan(Transponder transponder)
        {
            patreceived = false;
            batreceived = false;
            sdtreceived = false;
            pmtreceived = false;
            catreceived = false;
            nitreceived = false;
            expectNIT = false;
            m_sessionbouquets = new();
            m_sessionnetworks = new();
            payloads = new Payloads(processpayload);
            m_pids = new List<Channel>();
            m_pat = new PAT();
            m_sdt = new SDT(transponder);

            log.DebugFormat("Scanning transponder: {0}", transponder.frequency);
            Task scantask = ReadData();
            m_transponder = transponder;
            scanquery = m_transponder.getQuery();
            scanquery = scanquery + "&pids=0,1,17";
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=0,1,17");

            await scantask;
            rtsp.commandTeardown("");
            log.DebugFormat("Scanning transponder complete: {0}, channels: {1}", transponder.frequency, m_pids.Count);
            return m_pids;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public async Task<FastScanBouquet> scanfast(FastScanLocation location, Transponder tsp)
        {
            fstnetworkreceived = false;
            fstlcnreceived = false;
            m_fstbouquet = new FastScanBouquet();
            m_fstbouquet.network = new Network();
            m_fstchannels = new FastScanChannels();
            m_fstbouquet.location = location;
            m_fastscan = true;
            log.DebugFormat("Fast Scan transponder: {0} - {1}", location.frequency, location.name);

            m_transponder = tsp;
            PATEntry pe = new PATEntry();
            pe.programpid = (ushort)location.pid;
            pe.serviceid = -1;
            m_transponder.pids = new List<PATEntry>();
            m_transponder.pids.Add(pe);
            Channel channel = new Channel(location.pid, tsp);
            log.DebugFormat("Add channel for fastscan {0}. Program number: {1}", location.name, location.pid);
            m_pids = new List<Channel>();
            m_pids.Add(channel);
            payloads = new Payloads(processpayload);

            Task scantask = ReadData();
            scanquery = tsp.getQuery();
            scanquery = scanquery + "&pids=" + location.pid;
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=" + location.pid);

            await scantask;
            rtsp.commandTeardown("");
            log.DebugFormat("Scanning transponder complete: {0}, channels: {1}", tsp.frequency, m_pids.Count);
            return m_fstbouquet;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public async Task<Channel> scanChannel(Channel channel)
        {
            patreceived = false;
            batreceived = true;
            sdtreceived = true;
            catreceived = true;
            pmtreceived = false;
            channel.Pmtpresent = false;
            channel.Pmt.Clear();
            nitreceived = true;
            expectNIT = false;
            m_channelscan = true;
            m_sessionbouquets = new();
            m_sessionnetworks = new();
            payloads = new Payloads(processpayload);
            m_pat = new PAT();
            m_pids = new();
            m_pids.Add(channel);

            log.DebugFormat("Scanning transponder for PAT: {0}", channel.transponder.frequency);
            Task scantask = ReadData();
            m_transponder = channel.transponder;
            scanquery = m_transponder.getQuery();
            scanquery = scanquery + "&pids=0";
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=" + channel.Programpid.ToString());

            await scantask;

            m_pids = new List<Channel>();
            foreach (PATEntry entry in m_transponder.pids)
            {
                if (entry.serviceid == channel.service_id)
                {
                    m_pids.Add(channel);
                    channel.Programpid = entry.programpid;
                    break;
                }
            }
            rtsp.commandPlay("?addpids=" + channel.Programpid);
            scantask = ReadData();
            await scantask;

            rtsp.commandTeardown("");
            log.DebugFormat("Scanning channel complete: {0}, channels: {1}", channel.transponder.frequency, channel.service_id);
            return channel;
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
            if (!m_channelscan)
                expectNIT = m_pat.expectNIT;
            patreceived = true;
            m_transponder.pids = m_pat.pids;

            rtsp.commandPlay("?delpids=0");

            /* pids is now populated, add pid=17 for BAT and SDT */
            string strpids = "";
            if (expectNIT)
            {
                strpids = "16";
                rtsp.commandPlay("?addpids=" + strpids);
            }
        }
        protected void OnPMTReceived(int payloadpid)
        {
            bool allpmtsreceived = true;
            foreach (Channel c in m_pids)
            {
                if (c.Pmtpresent == false)
                    allpmtsreceived = false;

            }
            if (allpmtsreceived)
                pmtreceived = true;
            rtsp.commandPlay("?delpids=" + payloadpid);
        }
        protected void OnSDTReceived()
        {
            /* pids is now populated */
            sdtreceived = true;
            string strpids = "17";
            foreach (Channel channel in m_pids)
            {
                strpids = strpids + "," + channel.Programpid.ToString();
            }
            rtsp.commandPlay("?addpids=" + strpids);

            if (batreceived)
                rtsp.commandPlay("?delpids=17"); 
        }
        public class PATReceivedArgs : EventArgs
        {
            public List<Channel> pids { get; set; }
            public PATReceivedArgs(List<PATEntry> pids)
            {
                //this.pids = pids;
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
                if (fstnetworkreceived && fstlcnreceived)
                    return true;
            if (!batreceived && m_stopwatch.Elapsed.TotalSeconds > 10)
                batreceived = true; /* BAT is optional, so if not received within the transmission interval, assume it was there */
            if (sdtreceived && patreceived && batreceived && pmtreceived)
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

            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
                m_bouquet = m_sessionbouquets.Find(x => x.bouquet_id == hdr.streamid);
                if (m_bouquet == null)
                {
                    m_bouquet = new();
                    m_bouquet.bouquet_id = hdr.streamid;
                    m_bouquet.transponder = this.m_transponder;
                    m_sessionbouquets.Add(m_bouquet);
                }

                if (m_bouquet.complete) return;
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
                else if (tableid == 0x40 || tableid == 0x41) /* We do not use 0x41 which is another network */
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
                    case 0x41: processNIT(payload.getDatapart(0, payload.expectedlength)); break;
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

            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
                int networkid = hdr.streamid;
                m_fstbouquet.network.addsection(hdr, span.Slice(8));
                log.DebugFormat("Section {0} of FST Network processed", hdr.sectionnr);
                if (m_fstbouquet.network.complete)
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
        private void processFSTLCN(byte[] msg)
        {
            log.Debug("FST LCN received");
            Span<byte> span = msg;

            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
                int networkid = hdr.streamid;
                m_fstchannels.addsection(hdr, span.Slice(8));
                log.DebugFormat("Section {0} of FST LCN received", hdr.sectionnr);
                if (m_fstchannels.complete)
                {
                    log.DebugFormat("All sections of current FST LCN received");
                    m_fstbouquet.programInfos = m_fstchannels.programInfos;
                    fstlcnreceived = true;
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed FST LCN section!! Exception is :" + ex.Message);
            }
        }
        private tableHeader getHeader(Span<byte> pTable)
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
            Span<byte> span = msg;
            if (patreceived == true) return;
            log.Debug("PAT received");

            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
                m_pat.addsection(hdr, span.Slice(8));
                m_transponder.transportstreamid = (ushort)hdr.streamid;
                if (m_pat.complete)
                {
                    OnPATReceived(new PATReceivedArgs(m_pat.pids));
                }
            }
            catch (Exception e)
            {
                log.Debug("Malformed PAT received!" + e.Message);
            }
        }
        private void processPMT(byte[] msg, int payloadpid)
        {
            Span<byte> span = msg;
            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
                Channel channel = findChannel(payloadpid);
                if (channel == null || channel.Pmtpresent) return;
                PMT pmt = new PMT(channel);
                pmt.addsection(hdr, span.Slice(8));
                if (pmt.complete)
                {
                    channel.Pmtpresent = true;
                    OnPMTReceived(payloadpid);
                }

            }
            catch (Exception e)
            {
                log.Debug("Malformed PMT received!" + e.Message);
            }
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
            tableHeader hdr = getHeader(span.Slice(1));
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
            try
            {
                tableHeader hdr = getHeader(span.Slice(1));
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
                netw.currentnetwork = currentnetwork;
                log.Debug("NIT section received");
                netw.addsection(hdr, span.Slice(8));
                if (netw.complete )
                {
                    if (currentnetwork)
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
                    }
                    else 
                    {
                        log.DebugFormat("All sections of other NIT received");
                        Network existing_network = m_networks.Find(x => x.networkid == netw.networkid);
                        if (existing_network == null)
                            m_networks.Add(netw);
                        else
                        {
                            m_networks.Remove(existing_network);
                            m_networks.Add(netw);
                        }
                    }
                    OnNITReceived();
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed NIT section!! Exception is :" + ex.Message);
            }
        }
        private Channel findChannel(int payloadpid)
        {
            foreach (Channel channel in m_pids)
            {
                if (channel.Programpid == payloadpid)
                    return channel;
            }
            return null;
        }
        private bool isKnownPid(int payloadpid)
        {
            if (m_transponder.pids != null)
                foreach (PATEntry pid in m_transponder.pids)
                {
                    if (pid.programpid == payloadpid)
                        return true;
                }
            log.DebugFormat("PID {0} not found", payloadpid);
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

            try {
                tableHeader hdr = getHeader(span.Slice(1));
                m_sdt.addsection(hdr, span.Slice(8));
                log.DebugFormat("Processing SDT section {0} of {1}",hdr.sectionnr, hdr.lastsectionnr+1);
                if (m_sdt.complete)
                {
                    m_pids = m_sdt.pids;
                    OnSDTReceived();
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed SDT!! Exception is :" + ex.Message);
            }
        }
    }
}
