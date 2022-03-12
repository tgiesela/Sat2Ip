using Circularbuffer;
using ClientSockets;
using Protocol;
using Sat2Ip;
using System.Runtime.InteropServices;
using Utils;
using static Oscam.Definitions;

namespace Oscam
{
    public class Oscamserver
    {
        /* Constants and structs for OSCAM */
        private const uint DVBAPI_PROTOCOL_VERSION = 2;
        private const uint DVBAPI_CA_SET_PID = 0x40086f87;
        private const uint DVBAPI_CA_SET_DESCR = 0x40106f86;
        private const uint DVBAPI_CA_SET_DESCR_MODE = 0x400c6f88;
        private const uint DVBAPI_DMX_SET_FILTER = 0x403c6f2b;
        private const uint DVBAPI_DMX_STOP = 0x00006f2a;
        private const uint DVBAPI_AOT_CA = 0x9F803000;
        private const uint DVBAPI_AOT_CA_PMT = 0x9F803200;  //least significant byte is length (ignored)
        private const uint DVBAPI_AOT_CA_STOP = 0x9F803F04;
        private const uint DVBAPI_FILTER_DATA = 0xFFFF0000;
        private const uint DVBAPI_CLIENT_INFO = 0xFFFF0001;
        private const uint DVBAPI_SERVER_INFO = 0xFFFF0002;
        private const uint DVBAPI_ECM_INFO = 0xFFFF0003;
        private const uint DVBAPI_MAX_PACKET_SIZE = 262;         //maximum possible packet size
        private const uint DVBAPI_INDEX_DISABLE = 0xFFFFFFFF; // only used for ca_pid_t
        private enum ca_pmt_list_management
        {
            more = 00,
            first = 01,
            last = 02,
            only = 03,
            add = 04,
            update = 05
        }
        private enum ca_pmt_cmd_id
        {
            ok_descrambling = 01,
            ok_mmi = 02,
            query = 03,
            not_selected = 04
        }
        private class dmx_sct_filter_params
        {
            private UInt16 _pid;
            private byte[] _filterdata = new byte[16];
            private byte[] _filtermask = new byte[16];
            private byte[] _filtermode = new byte[16];
            private UInt32 _timeout;
            private UInt32 _flags;
            private int _filternr;
            public ushort Pid { get { return _pid; } set { _pid = value; } }
            public byte[] Filterdata { get { return _filterdata; } set { _filterdata = value; } }
            public byte[] Filtermask { get { return _filtermask; } set { _filtermask = value; } }
            public byte[] Filtermode { get { return _filtermode; } set { _filtermode = value; } }
            public uint Timeout { get { return _timeout; }  set { _timeout = value; } }
            public uint Flags { get { return _flags; } set { _flags = value; } }
            public int Filternr { get { return _filternr; } set { _filternr = value; } } 
        };
        private struct ca_pid_type
        {
            internal uint pid;
            internal int index;      /* -1 == disable*/
        }
        private struct transportstream
        {
            public bool scrambled;
            public DateTime lastScrambledPacket;
            public bool havecw;
        }

        /* End OSCAM Defs */

        private FFDecsa m_ffdecsa;
        private ca_pid_type m_ca_pid;
        private ca_descr_type ca_descr;
        private List<dmx_sct_filter_params> m_filter_params = new List<dmx_sct_filter_params>();
        private transportstream m_ts;

        private AsyncClientSocket m_oscamsock;
        private string m_ipaddress;
        private int m_port;
        private int m_adapterinx;
        private int m_demuxinx;
        private int m_filternr;
        private Channel? m_channel;
        private Thread? m_thread;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        private mp2packetqueue m_packetqueue;
        private Payloads m_payloads;
        private bool m_active;

        public Oscamserver(string ipaddress, int port, mp2packetqueue queue)
        {
            m_ipaddress = ipaddress;
            m_port = port;
            m_oscamsock = new(ipaddress, port);
            m_oscamsock.ReceiveComplete += new EventHandler<DatareceivedArgs>(DataReceived);
            m_ffdecsa = new();
            m_packetqueue = queue;
            m_payloads = new(processoscampayload);
        }
        private int processData(Span<byte> data)
        {
            int lenprocessed = 0;
            if (data.Length > 0)
            {
                byte[] b_uint32 = data.Slice(0,4).ToArray();
                byte[] b_uint16 = data.Slice(0,2).ToArray(); 
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(b_uint32);
                uint opcode = (uint)BitConverter.ToInt32(b_uint32, 0);
                switch (opcode)
                {
                    case DVBAPI_SERVER_INFO:
                        {
                            byte[] oscamversion = data.Slice(7,data[6]).ToArray();
                            log.Debug("DVBAPI_SERVER_INFO -> OSCAM version: " + System.Text.Encoding.Default.GetString(oscamversion));
                            lenprocessed = 7 + data[6];
                            break;
                        }

                    case DVBAPI_DMX_SET_FILTER:
                        {
                            if (data.Length < 65) 
                            {
                                log.Debug("Insufficient data for DVBAPI_DMX_SET_FILTER");
                                throw new Exception("Insufficient data");
                            }
                            lock (m_filter_params)
                            {
                                m_adapterinx = data[4];
                                m_demuxinx = data[5];
                                m_filternr = data[6];
                                dmx_sct_filter_params? filter = null;
                                foreach (dmx_sct_filter_params flt in m_filter_params)
                                {
                                    if (flt.Filternr == m_filternr)
                                    {
                                        m_filter_params.Remove(flt);
                                        break;
                                    }
                                }
                                filter = new dmx_sct_filter_params();
                                filter.Filternr = m_filternr;
                                m_filter_params.Add(filter);
                                log.Debug("FILTER ADDED");

                                filter.Pid = Utils.Utils.toShort(data[7], data[8]);
                                filter.Filterdata = data.Slice(9, 16).ToArray();
                                filter.Filtermask = data.Slice(25, 16).ToArray();
                                filter.Filtermode = data.Slice(41, 16).ToArray();
                                filter.Timeout = (UInt32)Utils.Utils.toInt(data[57], data[58], data[59], data[60]);
                                filter.Flags = (UInt32)Utils.Utils.toInt(data[61], data[62], data[63], data[64]);

                                log.Debug(String.Format("DVBAPI_DMX_SET_FILTER -> filternr = {0}, pid = {1}({1:X})", filter.Filternr, filter.Pid));
                                log.Debug(String.Format("Filter: {0:X} {1:X} {2:X} {3:X} {4:X} {5:X} {6:X} {7:X} {8:X} {9:X} {10:X} {11:X} {12:X} {13:X} {14:X} {15:X} "
                                    , filter.Filterdata[0], filter.Filterdata[1], filter.Filterdata[2], filter.Filterdata[3], filter.Filterdata[4], filter.Filterdata[5]
                                    , filter.Filterdata[6], filter.Filterdata[7], filter.Filterdata[8], filter.Filterdata[9], filter.Filterdata[10], filter.Filterdata[11]
                                    , filter.Filterdata[12], filter.Filterdata[13], filter.Filterdata[14], filter.Filterdata[15]));
                                log.Debug(String.Format("Mask: {0:X} {1:X} {2:X} {3:X} {4:X} {5:X} {6:X} {7:X} {8:X} {9:X} {10:X} {11:X} {12:X} {13:X} {14:X} {15:X} "
                                    , filter.Filtermask[0], filter.Filtermask[1], filter.Filtermask[2], filter.Filtermask[3], filter.Filtermask[4], filter.Filtermask[5]
                                    , filter.Filtermask[6], filter.Filtermask[7], filter.Filtermask[8], filter.Filtermask[9], filter.Filtermask[10], filter.Filtermask[11]
                                    , filter.Filtermask[12], filter.Filtermask[13], filter.Filtermask[14], filter.Filtermask[15]));
                                lenprocessed = 65;
                            }
                            break;
                        }
                    case DVBAPI_DMX_STOP:
                        {
                            if (data.Length < 9)
                            {
                                log.Debug("Insufficient data for DVBAPI_DMX_STOP");
                                throw new Exception("Insufficient data");
                            }
                            lock (m_filter_params)
                            {
                                m_adapterinx = data[4];
                                m_demuxinx = data[5];
                                m_filternr = data[6];
                                ushort pid = Utils.Utils.toShort(data[7], data[8]);
                                dmx_sct_filter_params? filter = null;
                                lenprocessed = 9;
                                foreach (dmx_sct_filter_params flt in m_filter_params)
                                {
                                    if (flt.Filternr == m_filternr && flt.Pid == pid)
                                    {
                                        filter = flt;
                                        log.Debug(String.Format("DVBAPI_DMX_STOP -> filternr = {0}, pid = {1}", filter.Filternr, filter.Pid));
                                        break;
                                    }
                                }
                                if (filter == null)
                                {
                                    /* filter not found */
                                    log.Debug(String.Format("DVBAPI_DMX_STOP -> Filter not found: filternr = {0}, pid = {1}", m_filternr, pid));
                                    break;
                                }
                                log.Debug(String.Format("DVBAPI_DMX_STOP -> Removed filter with filternr = {0}, pid = {1}", filter.Filternr, filter.Pid));
                                m_filter_params.Remove(filter);
                            }
                            break;
                        }
                    case DVBAPI_CA_SET_PID:
                        {
                            if (data.Length < 13)
                            {
                                log.Debug("Insufficient data for DVBAPI_CA_SET_PID");
                                throw new Exception("Insufficient data");
                            }

                            m_adapterinx = data[4];
                            m_ca_pid.pid = (uint)Utils.Utils.toInt(data[5], data[6], data[7], data[8]);
                            m_ca_pid.index = Utils.Utils.toInt(data[9], data[10], data[11], data[12]);
                            log.Debug(String.Format("DVBAPI_CA_SET_PID -> pid = {0}({0:X}), index = {1}", m_ca_pid.pid, m_ca_pid.index));
                            lenprocessed = 13;
                            if (m_ca_pid.index == -1)
                            {
                                /* disable */
                                break;
                            }
                            break;
                        }
                    case DVBAPI_CA_SET_DESCR:
                        {
                            if (data.Length < 21)
                            {
                                log.Debug("Insufficient data for DVBAPI_CA_SET_DESCR");
                                throw new Exception("Insufficient data");
                            }
                            m_adapterinx = data[4];
                            ca_descr.index = (uint)Utils.Utils.toInt(data[5], data[6], data[7],data[8]);
                            ca_descr.parity = (uint)Utils.Utils.toInt(data[9], data[10], data[11], data[12]);
                            ca_descr.cw = data.Slice(13, 8).ToArray();
                            m_ffdecsa.SetDescr(ca_descr);
                            log.Debug(String.Format("DVBAPI_CA_SET_DESCR -> index = {0}, parity = {1}", ca_descr.index, ca_descr.parity));
                            m_ts.havecw = true;
                            lenprocessed = 21;
                            break;
                        }
                    case DVBAPI_ECM_INFO:
                        {
                            ushort serviceid;
                            ushort caid;
                            ushort pid;
                            uint providerid;
                            uint ecmtime;
                            byte cardsystemnamelen;
                            byte[] cardsystemname = new byte[255];
                            byte readernamelen;
                            byte[] readername = new byte[255];
                            byte fromsourcenamelen;
                            byte[] fromsourcename = new byte[255];
                            byte protocolnamelen;
                            byte[] protocolname = new byte[255];
                            byte hops;
                            int offset;

                            m_adapterinx = data[4];
                            serviceid = Utils.Utils.toShort(data[5], data[6]);
                            caid = Utils.Utils.toShort(data[7], data[8]);
                            pid = Utils.Utils.toShort(data[9], data[10]);
                            providerid = (uint)Utils.Utils.toInt(data[11], data[12], data[13], data[14]);
                            ecmtime = (uint)Utils.Utils.toInt(data[15], data[16], data[17], data[18]);
                            offset = 19;
                            cardsystemnamelen = data[offset];
                            cardsystemname = data.Slice(offset + 1, cardsystemnamelen).ToArray();

                            offset = offset + 1 + cardsystemnamelen;
                            readernamelen = data[offset];
                            readername = data.Slice(offset + 1, readernamelen).ToArray();

                            offset = offset + 1 + readernamelen;
                            fromsourcenamelen = data[offset];
                            fromsourcename = data.Slice(offset + 1, fromsourcenamelen).ToArray();

                            offset = offset + 1 + fromsourcenamelen;
                            protocolnamelen = data[offset];
                            protocolname = data.Slice(offset + 1, protocolnamelen).ToArray();

                            offset = offset + 1 + protocolnamelen;
                            hops = data[offset];
                            log.Debug(String.Format("DVBAPI_ECM_INFO -> adapter = {0}, service = {1}, caid = {2}, pid = {3}, providerid = {4}\n" +
                                                    "Cardsystem {5}, Reader {6}, source {7}, protocol {8}, ", m_adapterinx, serviceid, caid, pid, providerid
                                                    , System.Text.ASCIIEncoding.ASCII.GetString(cardsystemname, 0, cardsystemnamelen)
                                                    , System.Text.ASCIIEncoding.ASCII.GetString(readername, 0, readernamelen)
                                                    , System.Text.ASCIIEncoding.ASCII.GetString(fromsourcename, 0, fromsourcenamelen)
                                                    , System.Text.ASCIIEncoding.ASCII.GetString(protocolname, 0, protocolnamelen)));
                            lenprocessed = offset+1;
                            break;
                        }
                    default:
                        log.Debug(String.Format("Received unimplemented opcode {0}", opcode));
                        lenprocessed = 4;
                        break;
                }

            }
            return lenprocessed;
        }
        private void DataReceived(object? sender, DatareceivedArgs e)
        {
            Span<byte> msgspan = e.buffer;

            log.Debug("Data received");

            int lenprocessed = 0;
            while (lenprocessed < e.length)
            {
                lenprocessed += processData(msgspan.Slice(lenprocessed));
            }
            m_oscamsock.Receive();
        }
        /// <summary>
        /// Connects to OSCAM
        /// </summary>
        public void Start(Channel channel)
        {
            m_channel = channel;
            m_thread = new Thread(new ThreadStart(oscamthread));
            m_thread.Start();
        }
        void processoscampayload(Payload payload)
        {
            filterpacket(payload);
        }
        private void oscamthread()
        {
            byte[] decryptbuf = new byte[7*188];
            m_oscamsock.Start();
            m_oscamsock.Receive();
            sendgreeting();
            sendCA_PMT();
            m_active = true;
            while (m_active)
            {
                if (m_packetqueue.getBufferedsize() < 1)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    Mpeg2Packet? buf;
                    m_packetqueue.get(out buf);
                    m_payloads.storePayload(buf);
                }
            }
            log.Debug("Writing stopped");
        }
        /// <summary>
        /// Disconnects OSCAM
        /// </summary>
        public void Stop()
        {
            m_active=false;
            m_oscamsock.Disconnect();
        }

        /// <summary>
        /// Sends a stop command to OSCAM to end demux
        /// </summary>
        public void Stopdemux()
        {
            byte[] stopdata = new byte[8];
            byte[] b_uint32 = new byte[4];
            b_uint32 = BitConverter.GetBytes(DVBAPI_AOT_CA_STOP);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b_uint32);
            Buffer.BlockCopy(b_uint32, 0, stopdata, 0, b_uint32.Length);
            stopdata[4] = 0x83;
            stopdata[5] = 0x02;
            stopdata[6] = 0x00;
            stopdata[7] = (byte)m_demuxinx;
            m_oscamsock.SendWait(stopdata, 8);
        }
        private void sendgreeting()
        {
            byte[] b_uint32 = new byte[4];
            byte[] b_uint16 = new byte[2];
            byte[] data = new byte[256+8];
            String clientversion = "TGI - version 0.0001";

            b_uint32 = BitConverter.GetBytes(DVBAPI_CLIENT_INFO);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b_uint32);
            b_uint16 = BitConverter.GetBytes(DVBAPI_PROTOCOL_VERSION);
            Buffer.BlockCopy(b_uint32, 0, data, 0, 4);
            Buffer.BlockCopy(b_uint16, 0, data, 4, 2);
            data[6] = (byte)clientversion.Length;
            Buffer.BlockCopy(System.Text.ASCIIEncoding.ASCII.GetBytes(clientversion), 0, data, 7, clientversion.Length);

            m_oscamsock.SendWait(data, 7 + clientversion.Length);
        }
        private void sendFilterdata(byte[] payload, int offset, int length, int filternr)
        {
            byte[] filterdata = new byte[4096];

            /* Now copy message id to first 4 bytes of message */
            byte[] b_uint32 = new byte[4];
            b_uint32 = BitConverter.GetBytes(DVBAPI_FILTER_DATA);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b_uint32);
            Buffer.BlockCopy(b_uint32, 0, filterdata, 0, b_uint32.Length);
            filterdata[4] = (byte)m_demuxinx;
            filterdata[5] = (byte)filternr;

            Buffer.BlockCopy(payload, offset, filterdata, 6, length);
            log.Debug("Send DVBAPI_FILTER_DATA with length " + (length+6));
            Utils.Utils.DumpBytes(filterdata, length + 6);
            m_oscamsock.Send(filterdata, length + 6);
        }

        private Boolean testlikeoscam(byte[] payload, int offset, int length, dmx_sct_filter_params filter)
        {
            int i, k, match;
            byte flt, mask;
            match = 1;
            for (i = 0, k = offset; i < (16); i++, k++)
            {
                mask = filter.Filtermask[i];
                if (k == offset + 1) //skip len bytes
                {
                    k += 2;
                }
                if (mask == 0x00)
                {
                    continue;
                }
                flt = (byte)(filter.Filterdata[i] & mask);
                //cs_log_dbg(D_DVBAPI,"Demuxer %d filter%d[%d] = %02X, filter mask[%d] = %02X, flt&mask = %02X , buffer[%d] = %02X, buffer[%d] & mask = %02X", demux_id, filter_num+1, i,
                //	demux[demux_id].demux_fd[filter_num].filter[i], i, mask, flt&mask, k, buffer[k], k, buffer[k] & mask); 
                if (k <= length)
                {
                    if (flt == (payload[k] & mask)) match = 1; else match = 0;
                }
                else
                {
                    match = 0;
                }
                if (match == 0)
                    break;
            }
            return (match == 1 && i == 16);
        }
        public void decryptpacket(byte[] packet)
        {
            if (m_ts.havecw)
            {
                m_ffdecsa.DecryptPackets(packet, ca_descr.index);
            }
        }
        public void decryptpackets(byte[] packets)
        {
            if (m_ts.havecw)
            {
                m_ffdecsa.DecryptMultiple(packets, ca_descr.index);
            }
        }
        public bool filterpacket(Payload packet)
        {
            if (packet.payloadpid > 0)
            {
                lock (m_filter_params)
                {
                    foreach (dmx_sct_filter_params flt in m_filter_params)
                    {
                        if (packet.payloadpid == flt.Pid)
                        {
                            
                            //int i = 0;
                            int offset = 1;
                            int tablelen = packet.expectedlength;
                            //if ((packet.data[offset + i] & flt.Filtermask[i]) == flt.Filterdata[i])
                            //{
                            //    for (i = 1; i < flt.Filterdata.Length; i++)
                            //    {
                            //        if ((packet.data[offset + i + 2] & flt.Filtermask[i]) != (flt.Filterdata[i] & flt.Filtermask[i]))
                            //        {
                            //            break;
                            //        }
                            //    }
                            //}
                            //if (i == flt.Filtermask.Length)
                            //{
                            if (testlikeoscam(packet.data, offset, tablelen + 1, flt)) { 
                                log.Debug(String.Format("Filter matched, starting from offset: {0}, tablelength: {1}, pid: {2}, filternr: {3}", offset, tablelen+2, packet.payloadpid, flt.Filternr));
                                sendFilterdata(packet.data, offset, tablelen + 3 , flt.Filternr);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private int getCAPMTProgramInfo(Channel channel, Span<byte> pData, bool addOscamDescriptor)
        {
            /* Build list with all CA-pids */
            List<capid> capids = new();
            if (channel.Capids.Count > 0)
                foreach (capid c in channel.Capids)
                {
                    capids.Add(c);
                }
            if (channel.Pmt.Count > 0)
                foreach (Sat2Ip.Stream stream in channel.Pmt)
                {
                    foreach (capid c in stream.capids)
                    {
                        capids.Add(c);
                    }
                }

            int length = 0;
            pData[0] = (byte)ca_pmt_cmd_id.ok_descrambling;
            if (addOscamDescriptor)
            {
                pData[1] = (byte)0x82;
                pData[2] = (byte)0x02;
                pData[3] = 0;
                pData[4] = 0;
                length = 5;
            }
            else
            {
                length = 1;
            }
            pData[length] = 0x84;
            pData[length + 1] = 0x02;
            byte[] bshort = new byte[2];
            bshort = Utils.Utils.fromShort((ushort)channel.Programpid);
            pData[length + 2] = (byte)bshort[0];
            pData[length + 3] = (byte)bshort[1];
            length += 4;
            if (capids.Count > 0)
            {
                foreach (capid pid in capids)
                {
                    var source = new ReadOnlySpan<byte>(pid.Cadescriptor);
                    var target = pData.Slice(length);
                    source.CopyTo(target);
                    //Array.Copy(pid.Cadescriptor, 0, barr, length, pid.Cadescriptor.Length);
                    length += pid.Cadescriptor.Length;
                }
            }
            return length;
        }
        private int getCAPMTStreamInfo(Channel channel, Span<byte> pData)
        {
            int offset = 0;
            int length = 0;

            foreach (Sat2Ip.Stream s in channel.Pmt)
            {
                pData[offset] = (byte)s.Stream_type;
                byte[] bshort = new byte[2];
                bshort = Utils.Utils.fromShort((ushort)s.Elementary_pid);
                pData[offset + 1] = (byte)bshort[0];
                pData[offset + 2] = (byte)bshort[1];
                pData[offset + 3] = 0;
                pData[offset + 4] = 0;
                offset += 5;
            }
            return offset + length;
        }
        private void sendCA_PMT()
        {
            /*
            ca_pmt () {
                ca_pmt_tag                  24 uimsbf
                length_field()
                ca_pmt_list_management       8 uimsbf
                program_number              16 uimsbf
                reserved                     2 bslbf
                version_number               5 uimsbf
                current_next_indicator       1 bslbf
                reserved                     4 bslbf
                program_info_length         12 uimsbf
                if (program_info_length != 0) {
                    ca_pmt_cmd_id                  8 uimsbf     -- at program level
                    for (i = 0; i < n; i++)
                    {
                        CA_descriptor() -- CA descriptor at programme level 
                    }
                }
                for (i=0; i<n; i++) {
                    stream_type              8 uimsbf
                    reserved                 3 bslbf
                    elementary_PID          13 uimsbf           --elementary stream PID
                    reserved                 4 bslbf
                    ES_info_length          12 uimsbf
                    if (ES_info_length != 0) {
                        ca_pmt_cmd_id        8 uimsbf           -- at ES level 
                        for (i=0; i<n; i++) {
                            CA_descriptor() -- CA descriptor at elementary stream level --
                        }
                    }
                }
            } 
            */
            if (m_channel == null)
            {
                log.Debug("Channel not set!");
                return;
            }
            int program_info_length = 0;
            int stream_info_length = 0;

            byte[] capmt = new byte[512];

            /* Now copy message id to first 4 bytes of message */
            byte[] b_uint32 = new byte[4];
            b_uint32 = BitConverter.GetBytes(DVBAPI_AOT_CA_PMT);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(b_uint32);
            Buffer.BlockCopy(b_uint32, 0, capmt, 0, b_uint32.Length - 1); /* Only the fist 3 bytes*/
            /* Now set the length of the message in the following 3 bytes, actual length will be set later on */
            capmt[3] = 0x82;

            capmt[6] = (byte)ca_pmt_list_management.only;/* only */
            Buffer.BlockCopy(Utils.Utils.fromShort((ushort)m_channel.Programnumber), 0, capmt, 7, 2);
            capmt[9] = (0x00 << 6) + (0x01 << 1);
            capmt[10] = 0x00; /* placeholder for program_info_length */
            capmt[11] = 0x00; /* placeholder for program_info_length */
            Span<byte> spancapmt = capmt;

            /* Invoegen program pid descriptor als tag 0x84 met lengte 4 en de pid : 840209D8 (RTL4 HD) */

            program_info_length = getCAPMTProgramInfo(m_channel, spancapmt.Slice(12), true);
            //            if (m_channel.Capids.Count > 0)
            //            {
            //                program_info_length = 0; /* because demuxoptions are also inserted */
            //                foreach (capid p in m_channel.Capids)
            //                {
            //                    Buffer.BlockCopy(p.Cadescriptor, 0, capmt, 17 + program_info_length, p.Cadescriptor.Length);
            //                    program_info_length = program_info_length + p.Cadescriptor.Length;
            //                }
            //                program_info_length = program_info_length + 4 + 1; /* Add 4 bytes for demuxoptions and 1 for ca_pmt_cmd_id*/
            //                Buffer.BlockCopy(Utils.Utils.fromShort((ushort)program_info_length), 0, capmt, 10, 2);
            //            }
            Buffer.BlockCopy(Utils.Utils.fromShort((ushort)program_info_length), 0, capmt, 10, 2);

            int offset = program_info_length + 12;
            stream_info_length = getCAPMTStreamInfo(m_channel, spancapmt.Slice(offset));
            //            foreach (Sat2Ip.Stream s in m_channel.Pmt)
            //            {
            //                if (s.Capids.Count > 0)
            //                {
            //                    capmt[offset] = (byte)s.Stream_type;
            //                    Buffer.BlockCopy(Utils.Utils.fromShort((ushort)s.Elementary_pid), 0, capmt, offset + 1, 2);
            //                    capmt[offset + 3] = 0x00;  /* Placeholder of 2 bytes for ES_info_length */
            //                    capmt[offset + 4] = 0x00;
            //                    capmt[offset + 5] = (byte)ca_pmt_cmd_id.ok_descrambling;
            //                    foreach (capid p in s.Capids)
            //                    {
            //                        Buffer.BlockCopy(p.Cadescriptor, 0, capmt, offset + 6 + stream_info_length, p.Cadescriptor.Length);
            //                        stream_info_length = stream_info_length + p.Cadescriptor.Length;
            //                    }
            //                    stream_info_length = stream_info_length + 1; /* 1 byte extra for ca_pmt_cmd_id */
            //                    Buffer.BlockCopy(Utils.Utils.fromShort((ushort)stream_info_length), 0, capmt, offset + 3, 2);
            //                    offset = offset + 5 + stream_info_length;
            //                    stream_info_length = 0;
            //                }
            //            }
            int totallength = stream_info_length + program_info_length + 12;
            capmt[4] = (byte)((totallength - 6) >> 8);
            capmt[5] = (byte)((totallength - 6) & 0xff);
            log.Debug("Send CA_PMT");
            Utils.Utils.DumpBytes(capmt, totallength);
            m_oscamsock.SendWait(capmt, totallength);
        }

    }
}