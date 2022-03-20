using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Interfaces;
using Protocol;

namespace Sat2Ip
{
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
        private String scanquery;
        private RTSP rtsp;
        private Payloads payloads;
        private List<Transponder> nit = new();
        private List<Network> _networks = new();
        private bool[] sdtsectionprocessed;
        private bool[] patsectionprocessed;
        Dictionary<int, bool[]> pmtsections;
        struct structHeader
        {
		    public int syntaxindicator;
            public short sectionlength;
            public int streamid;
            public int programnumber;
            public int versionnr;
            public int currNextInd;
            public int sectionnr;
            public int lastsectionnr;
        }; 
        private bool patreceived;
        private bool expectNIT;
        private bool sdtreceived;
        private bool catreceived;
        private bool nitreceived;
        private Transponder _transponder;
        private bool scancomplete;
        public List<Network> networks { get { return _networks; } set { _networks = value; } }
        public Transponder Transponder { get { return _transponder; } set { _transponder = value; } }

        public int SCANTIMEOUT { get; private set; }
        public Scanner(int portdata, int portreport, RTSP rtsp)
        {
            _portdata = portdata;
            _portreport = portreport;

            reader = new RtpReader(portdata);

            this.rtsp = rtsp;
            patreceived = false;
            sdtreceived = false;
            catreceived = false;
            nitreceived = false;
            scancomplete = false;
            SCANTIMEOUT = 15000;/* Default 10 seconds timeout */
        }
        public void stop()
        {
            reader.stop();
            //reader = null;
        }

        public async Task<List<Channel>> scan(Transponder transponder)
        {
            patreceived = false;
            sdtreceived = false;
            catreceived = false;
            nitreceived = false;
            expectNIT = false;
            scancomplete = false;
            sdtsectionprocessed = null;
            patsectionprocessed = null;
            pmtsections = new Dictionary<int, bool[]>();
            pids = new List<Channel>();
            payloads = new Payloads(processpayload);

            log.DebugFormat("Scanning transponder: {0}", transponder.frequency);
            _transponder = transponder;
            scanquery = _transponder.getQuery();
            scanquery = scanquery + "&pids=0,1";
            rtsp.commandSetup(scanquery);
            rtsp.commandPlay("?addpids=0,1");

            Task scantask = ReadData();
            await scantask;
            rtsp.commandTeardown("");
            log.DebugFormat("Scanning transponder complete: {0}, channels: {1}", transponder.frequency, pids.Count);
            return pids;
        }
        protected void OnNITReceived()
        {
            log.Debug("NIT Received and processed\n");
            rtsp.commandPlay("?delpids=16");
            nitreceived = true;
            if (sdtreceived)
            {
                scancomplete = true;
            }
        }
        protected void OnCATReceived()
        {
            log.Debug("CAT Received and processed\n");
            catreceived = true;
            rtsp.commandPlay("?delpids=1");
        }
        protected void OnPATReceived(EventArgs e)
        {
            /* pids is now populated */
            patreceived = true;
            String strpids = String.Empty;
            rtsp.commandPlay("?delpids=0");

            strpids = "17";
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
            if (expectNIT)
            {
                if (nitreceived && sdtreceived)
                    scancomplete = true;
            }
            else
            {
                if (sdtreceived)
                    scancomplete = true;
            }
            rtsp.commandPlay("?delpids=" + payloadpid);
        }
        protected void OnSDTReceived()
        {
            /* pids is now populated */
            sdtreceived = true;
            //String strpids = String.Empty;
            //reader.stop();
            if (expectNIT)
            {
                if (nitreceived)
                    scancomplete = true;
            }
            else
            {
                scancomplete = true;
            }
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
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            reader.start();
            Task<RtpPacket> task = reader.readAsync();
            RtpPacket packet = await task;
            while (reader.active == true && 
                   scancomplete == false && 
                   packet != null && 
                   (stopwatch.ElapsedMilliseconds < SCANTIMEOUT))
            {
                task = reader.readAsync();
                processMpeg2Packets(packet);
                packet = await task;
            }
            reader.stop();
            if (!scancomplete)
                log.Debug("Not all expected data received, SCAN timeout occurred");

            log.Debug("ReadData completed, no more ASYNC I/O Pending");
            stopwatch.Stop();
        }

        private void processMpeg2Packets(RtpPacket packet)
        {
            int lenprocessed = 0;
            Mpeg2Packet mp2Packet;
            byte[] rtpPayload = packet.getPayload();
            byte[] payloadpart;
            Payload payload;
            while (lenprocessed < rtpPayload.Length && scancomplete == false) 
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

        private void processpayload(Payload payload)
        {
            log.Debug("Process payload with length: " + payload.payloadlength);
            //Utils.Utils.DumpBytes(payload.data, payload.expectedlength);
            int payloadpid = payload.payloadpid;
            int pointer = payload.data[0];
            int tableid;
            if (pointer > payload.expectedlength)
            {
                log.Debug("Pointer points after expected length, malformed packet");
                return;
            }
            if (payloadpid == 0) /* PAT Program Association Table */
            {
                if (patreceived == true)
                    return;
                pointer = payload.data[0];
                tableid = payload.data[pointer+1];
                if (tableid == 0x00)
                {
                    processPAT(payload.getDatapart(pointer, payload.expectedlength - pointer));
                }
                else if (tableid == 0x40) /* We do not use 0x41 which is another network */
                {
                    processNIT(payload.getDatapart(pointer, payload.expectedlength - pointer));
                }
            }
            else if (payloadpid == 1) /* CAT Conditional Access Table */
            {
                if (catreceived == true)
                    return;
                pointer = payload.data[0];
                tableid = payload.data[pointer+1];
                if (tableid == 0x01)
                {
                    processCAT(payload.getDatapart(pointer, payload.expectedlength - pointer));
                }
            }
            else if (payloadpid == 0x11)   /* SDT (Service Description Table */
            {
                pointer = payload.data[0];
                tableid = payload.data[pointer+1];
                if (tableid == 0x42 ) /* We only process the SDT for the current network (0x42), other network (0x46) is ignored*/
                {
                    processSDT(payload.getDatapart(pointer, payload.expectedlength - pointer));
                }
            }
            else if (isKnownPid(payloadpid))
            {
                pointer = payload.data[0];
                tableid = payload.data[pointer+1];
                if (tableid == 0x02)
                {
                    processPMT(payload.getDatapart(pointer, payload.expectedlength - pointer),payload.payloadpid);
                }
                else
                if (tableid == 0x40) /* We do not use 0x41 which is another network */
                {
                    processNIT(payload.getDatapart(pointer, payload.expectedlength - pointer));
                }
                else
                {
                    log.DebugFormat("payload with unsupported table type: {0}", tableid);
                }
            }
        }
        private structHeader getHeader(byte[] pTable)
        {
            structHeader header = new structHeader();
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
            byte [] v = span.Slice(2).ToArray();
            int nrofsections;

            if (patreceived == true) return;

            structHeader hdr = getHeader(v);
            if (msg.Length < hdr.sectionlength)
            {
                log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", v.Length, hdr.sectionlength);
                return;
            }
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
                    channel = new Channel(network_pid, _transponder);
                    log.DebugFormat("Expect to receive NIT on PID: {0}", network_pid);
                    expectNIT = true;
                }
                else
                {
                    program_map_pid = (Utils.Utils.toShort((byte)(v[9 + (i * 4)] & 0x1F), v[10 + (i * 4)]));
                    channel = new Channel(program_map_pid, _transponder);
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
            log.DebugFormat("PMT received for PID: {0}, datalen: {1}", payloadpid, msg.Length);
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
            byte[] v = span.Slice(2).ToArray();

            structHeader hdr = getHeader(v);
            if (!pmtsections.ContainsKey(payloadpid))
            {
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
            byte[] v = span.Slice(2).ToArray();

            if (catreceived == true) return;
            structHeader hdr = getHeader(v);
            int sectionbytesprocessed = 7;
            /* Section length is length starting after length including the CRC (3 + 4 = 7) */
            while (sectionbytesprocessed < (hdr.sectionlength - 7))
            {
                int descriptorid = v[sectionbytesprocessed];
                int descriptorlength = (v[sectionbytesprocessed + 1] & 0xff);
                log.DebugFormat("CAT: desciptorid = {0}, descriptorlength={1}\n", descriptorid.ToString("X"), descriptorlength);
                sectionbytesprocessed += 2 + descriptorlength;
            }
            OnCATReceived();
        }
        private void processNIT(byte[] msg)
        {
            /*
            service_description_section()
            {
        0	table_id                        8
        1	section_syntax_indicator        1
            reserved_future_use             1
            reserved                        2
            section_length                  12
        3	transport_stream_id             16
        5	reserved                        2
            version_number                  5
            current_next_indicator          1
        6	section_number                  8
        7	last_section_number             8
        8	reserved_future_use             4
            network_descriptors_length		12
        10	for(j=0;j<N;j++)
            {
                descriptor()
            }
            reserved_future_use             4
            transport_stream_loop_length	12
            for(i=0;i<N;i++)
            {
                transport_stream_id         16
                original_network_id			16
                reserved_future_use         4
                transport_descriptors_length	12
                for(j=0;j<N;j++)
                {
                    descriptor()
                }
            }
            CRC_32                          32

            tableid 0x40, 0x41
            */
            bool currentnetwork;
            Span<byte> span = msg;
            byte[] v = span.Slice(2).ToArray();
            structHeader hdr = getHeader(v);
            int networkid = hdr.streamid;
            Network netw = _networks.Find(x => x.networkid == hdr.streamid);
            if (netw == null)
            {
                netw = new();
                netw.networkid = hdr.streamid;
                _networks.Add(netw); 
            }
            nit = netw.transponders;
            if (msg[1] == 0x40)
            {
                currentnetwork = true;  
            }
            else
            {
                currentnetwork = false;
            }

            log.Debug("NIT received");
            if (!netw.nitcomplete)
            {
                try
                {
                    short network_descriptors_length = (short)(((v[7] & 0x0F) << 8) | (v[8] & 0xff));

                    int offset = 9; /* points to network descriptors */
                    if (msg.Length < (offset + network_descriptors_length))
                    {
                        log.Debug("Message too short!");
                        return;
                    }
                    int bytesprocessed = 0;
                    while (bytesprocessed < (network_descriptors_length))
                    {
                        bytesprocessed += processnetworkdescriptor(v, offset + bytesprocessed, netw);
                    }
                    offset = offset + bytesprocessed;
                    short transport_stream_loop_length = (short)(((v[offset + 0] & 0x0F) << 8) | (v[offset + 1] & 0xff));
                    bytesprocessed = 0;
                    offset += 2;
                    if (msg.Length < (offset + transport_stream_loop_length))
                    {
                        log.Debug("Message too short!");
                        return;
                    }
                    while (bytesprocessed < transport_stream_loop_length)
                    {
                        bytesprocessed += processtransportstream(v, offset + bytesprocessed);
                    }

                    log.DebugFormat("Section {0} of NIT processed", hdr.sectionnr);
                    netw.sectionprocessed(hdr.sectionnr, hdr.lastsectionnr);
                }
                catch (Exception ex)
                {
                    log.Debug("Malformed NIT!! Exception is :" + ex.Message);
                }
            }

            if (netw.nitcomplete && currentnetwork)
            {
                log.DebugFormat("All sections of current NIT received");
                OnNITReceived();
            }

        }

        private int processtransportstream(byte[] v, int offset)
        {
            short transport_stream_id = (short)((v[offset + 0] << 8) | (v[offset + 1] & 0xff));
            short original_network_id = (short)((v[offset + 2] << 8) | (v[offset + 3] & 0xff));
            short transport_descriptors_length = (short)(((v[offset + 4] & 0x0F) << 8) | (v[offset + 5] & 0xff));
            int descriptorlengthprocessed = 0;
            offset += 6;
            log.DebugFormat("Transport stream id: {0} on network {1}", transport_stream_id, original_network_id);
            log.DebugFormat("     received length: {0}, expected length {1}", v.Length, transport_descriptors_length);
            while (descriptorlengthprocessed < transport_descriptors_length)
            {
                descriptorlengthprocessed += processtransportdescriptor(v, offset + descriptorlengthprocessed);
            }
            return descriptorlengthprocessed + 6;
        }

        private int processtransportdescriptor(byte[] v, int offset)
        {
            /* Details: see a38_dvb-si_specification */
            int id = (int)v[offset];
            int length = (int)v[offset + 1];
            if (id == 0x43)
            {
                byte[] frequency = new byte[4];
                Array.Copy(v, offset + 2, frequency, 0, 4);
                byte[] orbit_position = new byte[2];
                Array.Copy(v, offset + 6, orbit_position, 0, 2);
                int west_eastflag = (v[offset + 8] & 0x80) >> 7; /* 0: West, 1: East */
                int polarization = (v[offset + 8] & 0x60) >> 5; /* 00: H, 01: V, 10: Left, 11; Right */
                int dvbsystem = (v[offset + 8] & 0x04) >> 2; /* 1: DVB-S2, 0: DVB-S */
                int roll_off = -1;
                if (dvbsystem == 1)
                {
                    roll_off = (v[offset + 8] & 0x18) >> 3;
                }
                int modtype = (v[offset + 8] & 0x03);
                byte[] symbol_rate = new byte[4];
                Array.Copy(v, offset + 9, symbol_rate, 0, 4);
                int fec = (symbol_rate[3] & 0x0f);
                //log.DebugFormat("Transport descriptor id: {0} with length {1}", id, length);
                log.DebugFormat("Transport: Freq {0}, Orbit: {1}, Symbolrate: {2}, Fec: {3}",
                    Utils.Utils.bcdtohex(frequency),
                    Utils.Utils.bcdtohex(orbit_position),
                    Utils.Utils.bcdtohex(symbol_rate, symbol_rate.Length * 2 - 1),
                    fec
                    );
                Transponder tsp = new();
                tsp.frequency = Utils.Utils.bcdtoint(frequency) / 100;
                tsp.frequencydecimal = Decimal.Divide(Utils.Utils.bcdtoint(frequency), 100);
                switch (polarization)
                {
                    case 00: tsp.polarisation = Transponder.e_polarisation.Horizontal; break;
                    case 01: tsp.polarisation = Transponder.e_polarisation.Vertical; break;
                    case 10: tsp.polarisation = Transponder.e_polarisation.circular_left; break;
                    case 11: tsp.polarisation = Transponder.e_polarisation.circular_right; break;
                }
                switch (dvbsystem)
                {
                    case 0: tsp.dvbsystem = Transponder.e_dvbsystem.DVB_S; break;
                    case 1: tsp.dvbsystem = Transponder.e_dvbsystem.DVB_S2; break;
                }
                tsp.orbit = orbit_position;
                tsp.samplerate = Utils.Utils.bcdtoint(symbol_rate) / 100;
                tsp.diseqcposition = _transponder.diseqcposition;
                switch (fec)
                {
                    case 0: tsp.fec = Transponder.e_fec.undefined; break;
                    case 1: tsp.fec = Transponder.e_fec.fec_12; break;
                    case 2: tsp.fec = Transponder.e_fec.fec_23; break;
                    case 3: tsp.fec = Transponder.e_fec.fec_34; break;
                    case 4: tsp.fec = Transponder.e_fec.fec_56; break;
                    case 5: tsp.fec = Transponder.e_fec.fec_78; break;
                    case 6: tsp.fec = Transponder.e_fec.fec_89; break;
                    case 7: tsp.fec = Transponder.e_fec.fec_35; break;
                    case 8: tsp.fec = Transponder.e_fec.fec_45; break;
                    case 9: tsp.fec = Transponder.e_fec.fec_910; break;
                    case 15:tsp.fec = Transponder.e_fec.none; break;
                    default:tsp.fec = Transponder.e_fec.reserved; break;

                }
                switch (modtype)
                { 
                    case 0: tsp.mtype = Transponder.e_mtype.auto; break;
                    case 1: tsp.mtype = Transponder.e_mtype.qpsk; break;
                    case 2: tsp.mtype = Transponder.e_mtype.psk8; break;
                    case 3: tsp.mtype = Transponder.e_mtype.qam16; break;
                }
                Transponder existing = nit.Find(x => x.frequency == tsp.frequency && x.polarisation == tsp.polarisation);
                if (existing != null)
                {
                    nit.Remove(existing);
                }
                nit.Add(tsp);
            }
            return length + 2;
        }

        private int processnetworkdescriptor(byte[] v, int offset, Network netw)
        {
            int id = (int)v[offset];
            int length = (int)v[offset+1];
            if (id == 0x40)
            {
                int lenused = 0;
                string networkname = getStringFromDescriptor(v, offset + 1, ref lenused);
                netw.networkname = networkname;
            }
            log.DebugFormat("Network descriptor id: {0} with length {1}", id, length);
            return length + 2;
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

        private Channel findChannelByPmt(int transport_stream_id)
        {
            foreach (Channel channel in pids)
            {
                foreach (Stream stream in channel.Pmt) {
                    if (stream.Elementary_pid == transport_stream_id)
                        return channel;
                }
            }
            return null;
        }

        private bool isKnownPid(int payloadpid)
        {
            log.DebugFormat("Considering PID: {0}", payloadpid);
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
            byte[] v = span.Slice(2).ToArray();

            if (patreceived == false) return;
            if (sdtreceived == true) return;
            Utils.Utils.DumpBytes(msg, msg.Length);
            structHeader hdr = getHeader(v);
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
                    ushort service_id = Utils.Utils.toShort(v[offset], v[offset + 1]);
                    int EIT_schedule_flag = (v[offset + 2] & 0x02) >> 1;
                    int EIT_present_following_flag = (v[offset + 2] & 0x01);
                    int running_status = (v[offset + 3] & 0xE0) >> 5;
                    int free_CA_mode = (v[offset + 3] & 0x10) >> 4;
                    ushort descriptors_loop_length = Utils.Utils.toShort((byte)(v[offset + 3] & 0x0F), v[offset + 4]);
                    int bytesprocessed = 0;
                    Channel channel = findChannelByProgramnumber(service_id);
                    if (channel == null)
                        log.Debug(String.Format("NOT FOUND: SDT for service {0} ({1})", service_id, service_id.ToString("X4")));
                    while (bytesprocessed < descriptors_loop_length)
                    {
                        int descriptorid = v[offset + 5 + bytesprocessed];
                        int descriptorlength = v[offset + 6 + bytesprocessed];
                        byte[] descriptor = new byte[2 + descriptorlength];
                        System.Buffer.BlockCopy(v, offset + 5 + bytesprocessed, descriptor, 0, (2 + descriptorlength));
                        bytesprocessed = bytesprocessed + 2 + descriptorlength;
                        if (channel != null)
                        {
                            processdescriptorwithchannelinfo(descriptor, channel);
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

        private void processdescriptorwithchannelinfo(byte[] descriptor, Channel channel)
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
                case 0x48:/* service descriptor */
                    int service_type = descriptor[2];
                    channel.Servicetype = service_type;
                    int lenused = 0;
                    channel.Providername = getStringFromDescriptor(descriptor, 3, ref lenused);
                    channel.Servicename = getStringFromDescriptor(descriptor, 3+lenused+1,ref lenused);
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

        private string getStringFromDescriptor(byte[] descriptor, int offset, ref int lenused)
        {
            int strlen = descriptor[offset];
            int strencoding;
            if (strlen > 0)
            {
                if (descriptor[offset+1] < 0x20)
                {
                    strencoding = descriptor[offset + 1];
                    strlen--;
                    lenused = strlen + 1;
                    Encoding ascii = Encoding.ASCII;
                    //Encoding utf8 = Encoding.GetEncoding("utf-8");
                    Encoding iso;
                    //string asciistring = ascii.GetString(descriptor, offset + 2, strlen);
                    string asciistring = Encoding.Latin1.GetString(descriptor, offset + 2, strlen);
//                    byte[] utf8bytes = Encoding.ASCII.GetBytes(asciistring);
                    iso = Encoding.Default;
                    switch (strencoding)
                    {
                        case 0x10:
                            int subset = strencoding = descriptor[offset + 3];
                            asciistring = Encoding.Latin1.GetString(descriptor, offset + 4, strlen-2);
                            break;
                        default:
                            iso = Encoding.Default;
                            break;
                    }
                    // byte[] isoBytes = Encoding.Convert(ascii, iso, utf8bytes);
                    return asciistring; 
                }
                else
                {
                    strencoding = 0;
                    lenused = strlen;
                    return Encoding.Latin1.GetString(descriptor, offset + 1, strlen);
                }
            }
            else
            {
                return "";
            }
        }
    }
}
