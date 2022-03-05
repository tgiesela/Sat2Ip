using log4net;
using log4net.Config;
using Sat2Ip;
using System;
using System.Reflection;
using System.Text;

namespace Test // Note: actual namespace depends on the project name.
{
    public class Program
    {
        private List<Channel> pids = new List<Channel>();
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        private Transponder _transponder = new();
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
            public int nrofsections;
        };
        bool patreceived = false;
        bool sdtreceived = false;
        private void processdescriptor(byte[] descriptor, Channel channel, Channel._descriptorlevel level, Sat2Ip.Stream? stream)
        {
            switch (descriptor[0])
            {
                case 0x09:/* CA descriptor */
                    ushort CA_system_ID = Utils.Utils.toShort(descriptor[2], descriptor[3]);
                    ushort CA_PID = Utils.Utils.toShort((byte)(descriptor[4] & 0x1F), descriptor[5]);
                    channel.CAlevel = level;

                    if (level == Channel._descriptorlevel.program)
                    {
                        log.Debug(String.Format("CA descriptor at program level. PID {0}, Systemid {1}", CA_PID, CA_system_ID));
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
                        stream.Capids.Add(capid);
                    }
                    break;
            }
        }
        private Channel? findChannel(int payloadpid)
        {
            foreach (Channel channel in pids)
            {
                if (channel.Programpid == payloadpid)
                    return channel;
            }
            return null;
        }
        private string getStringFromDescriptor(byte[] descriptor, int offset, ref int lenused)
        {
            int strlen = descriptor[offset];
            int strencoding;
            if (strlen > 0)
            {
                if (descriptor[offset+1] < 0x20) /* First character or Encoding type */
                {
                    strencoding = descriptor[offset + 1];
                    strlen--;
                    lenused = strlen + 1;
                    System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
                    System.Text.Encoding iso;
                    string utf8string = utf8.GetString(descriptor, offset + 2, strlen);
                    byte[] utf8bytes = System.Text.Encoding.ASCII.GetBytes(utf8string);
                    iso = Encoding.Default;
                    switch (strencoding)
                    {
                        case 0x10:
                            int subset = strencoding = descriptor[offset + 3];
                            utf8string = utf8.GetString(descriptor, offset + 4, strlen);
                            break;
                        default:
                            iso = System.Text.Encoding.Default;
                            break;
                    }
                    byte[] isoBytes = System.Text.Encoding.Convert(utf8, iso, utf8bytes);
                    return iso.GetString(isoBytes);
                }
                else
                {
                    strencoding = 0;
                    lenused = strlen;
                    return System.Text.Encoding.UTF8.GetString(descriptor, offset + 1, strlen);
                }
            }
            else
            {
                return "";
            }
        }
        private Channel? findChannelByProgramnumber(int programnumber)
        {
            foreach (Channel channel in pids)
            {
                if (channel.Programnumber == programnumber)
                    return channel;
            }
            return null;
        }
        private structHeader getHeader(byte[] pTable)
        {
            structHeader header = new structHeader();
            header.syntaxindicator = (pTable[0] & 0x80) >> 7;
            header.sectionlength = (short)(((pTable[0] & 0x0F) << 8) | (pTable[1] & 0x00ff));
            header.streamid = ((pTable[2] & 0xff) << 8) | (pTable[3] & 0xff);
            header.programnumber = ((pTable[2] & 0xff) << 8) | (pTable[3] & 0xff);
            header.versionnr = (pTable[4] & 0x3E) >> 1;
            header.currNextInd = pTable[4] & 0x01;
            header.sectionnr = pTable[5];
            header.lastsectionnr = pTable[6];
            header.nrofsections = (header.sectionlength - 4 - 5) / 4;
            return header;
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
                    channel.Servicename = getStringFromDescriptor(descriptor, 3 + lenused + 1, ref lenused);
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
            log.Debug("Processing SDT");
            structHeader hdr = getHeader(v);
            if (v.Length < hdr.sectionlength)
            {
                log.Debug("Short payload!!!!");
                return;
            }
            ushort original_network_id = Utils.Utils.toShort(v[7], v[8]);
            int offset = 10; /* points to table */
            while (offset < hdr.sectionlength - 7)
            {
                ushort service_id = Utils.Utils.toShort(v[offset], v[offset + 1]);
                int EIT_schedule_flag = (v[offset + 2] & 0x02) >> 1;
                int EIT_present_following_flag = (v[offset + 2] & 0x01);
                int running_status = (v[offset + 3] & 0xE0) >> 5;
                int free_CA_mode = (v[offset + 3] & 0x10) >> 4;
                ushort descriptors_loop_length = Utils.Utils.toShort((byte)(v[offset + 3] & 0x0F), v[offset + 4]);
                int bytesprocessed = 0;
                Channel? channel = findChannelByProgramnumber(service_id);
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
            sdtreceived = true;
//            OnSDTReceived(new SDTReceivedArgs(pids));

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

            ushort pcr_pid = Utils.Utils.toShort((byte)(v[7] & 0x1F), v[8]);
            ushort program_info_length = Utils.Utils.toShort((byte)(v[9] & 0x0F), v[10]);
            /*
             * Here the descriptors should be processed
             */
            Channel? channel = findChannel(payloadpid);
            if (channel == null || channel.Pmtpresent) channel = new(payloadpid,_transponder);
            log.Debug(String.Format("PMT for PID {0} ({1}), pcr_pid {2} ({3}, program_number {4} ({5}))", payloadpid, payloadpid.ToString("X4"), pcr_pid, pcr_pid.ToString("X4"), hdr.programnumber, hdr.programnumber.ToString("X4")));
            int bytesprocessed = 0;
            int offset = 0;
            while (bytesprocessed < program_info_length)
            {
                int descriptorid = v[offset + 11 + bytesprocessed];
                int descriptorlength = v[offset + 12 + bytesprocessed];
                byte[] descriptor = new byte[2 + descriptorlength];
                System.Buffer.BlockCopy(v, offset + 11 + bytesprocessed, descriptor, 0, (2 + descriptorlength));
                processdescriptor(descriptor, channel, Channel._descriptorlevel.program, null);
                bytesprocessed = bytesprocessed + 2 + descriptorlength;
            }
            offset = offset + 11 + program_info_length; /* points after descriptors */
            while ((offset + 3) < hdr.sectionlength)
            {
                Sat2Ip.Stream stream = new Sat2Ip.Stream();
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
            channel.Pmtpresent = true;
        }

        byte[] getPart(int start, int length)
        {
            byte[] buf = new byte[1500];
            for (int i = 0; i< 1500;i++) buf[i] = (byte)(i % 255);
            Span<byte> span = buf;
            return span.Slice(start, length).ToArray();  
        }
        static void Main(string[] args)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new System.IO.FileInfo(@"log4.config"));

            byte[] bufsdt = {
                0x42, 0xf1, 0x56, 0x04, 0x0b, 0xc5, 0x00, 0x00, 0x00, 0x01, 0xff, 0x00, 0x01, 0xff, 0x80, 0x24,
                0x48, 0x1a, 0x1f, 0x03, 0x53, 0x45, 0x53, 0x14, 0x53, 0x45, 0x53, 0x20, 0x55, 0x48, 0x44, 0x20,
                0x44, 0x65, 0x6d, 0x6f, 0x20, 0x43, 0x68, 0x61, 0x6e, 0x6e, 0x65, 0x6c, 0x50, 0x06, 0x09, 0x04,
                0x00, 0x30, 0x00, 0x00, 0x00, 0x02, 0xff, 0x80, 0x2b, 0x48, 0x21, 0x1f, 0x0a, 0x05, 0x53, 0x45,
                0x53, 0x20, 0x41, 0x53, 0x54, 0x52, 0x41, 0x14, 0x05, 0x55, 0x48, 0x44, 0x31, 0x20, 0x62, 0x79,
                0x20, 0x41, 0x53, 0x54, 0x52, 0x41, 0x20, 0x2f, 0x20, 0x48, 0x44, 0x2b, 0x50, 0x06, 0x09, 0x04,
                0x00, 0x30, 0x00, 0x00, 0x18, 0x3d, 0xff, 0x90, 0x38, 0x48, 0x20, 0x1f, 0x0f, 0x05, 0x50, 0x72,
                0x6f, 0x53, 0x69, 0x65, 0x62, 0x65, 0x6e, 0x53, 0x61, 0x74, 0x2e, 0x31, 0x0e, 0x05, 0x50, 0x72,
                0x6f, 0x37, 0x53, 0x61, 0x74, 0x2e, 0x31, 0x20, 0x55, 0x48, 0x44, 0x53, 0x0c, 0x18, 0x30, 0x18,
                0x42, 0x18, 0x43, 0x18, 0x60, 0x18, 0x6a, 0x18, 0x6d, 0x50, 0x06, 0x09, 0x04, 0x00, 0x30, 0x00,
                0x00, 0x00, 0x07, 0xff, 0x80, 0x1f, 0x48, 0x1d, 0x1f, 0x03, 0x53, 0x45, 0x53, 0x17, 0x53, 0x45,
                0x53, 0x20, 0x55, 0x48, 0x44, 0x20, 0x44, 0x65, 0x6d, 0x6f, 0x20, 0x43, 0x68, 0x61, 0x6e, 0x6e,
                0x65, 0x6c, 0x20, 0x30, 0x36, 0x00, 0x06, 0xff, 0x80, 0x1f, 0x48, 0x1d, 0x1f, 0x03, 0x53, 0x45,
                0x53, 0x17, 0x53, 0x45, 0x53, 0x20, 0x55, 0x48, 0x44, 0x20, 0x44, 0x65, 0x6d, 0x6f, 0x20, 0x43,
                0x68, 0x61, 0x6e, 0x6e, 0x65, 0x6c, 0x20, 0x30, 0x35, 0x00, 0x05, 0xff, 0x80, 0x1f, 0x48, 0x1d,
                0x1f, 0x03, 0x53, 0x45, 0x53, 0x17, 0x53, 0x45, 0x53, 0x20, 0x55, 0x48, 0x44, 0x20, 0x44, 0x65,
                0x6d, 0x6f, 0x20, 0x43, 0x68, 0x61, 0x6e, 0x6e, 0x65, 0x6c, 0x20, 0x30, 0x34, 0x00, 0x04, 0xff,
                0x80, 0x1f, 0x48, 0x1d, 0x1f, 0x03, 0x53, 0x45, 0x53, 0x17, 0x53, 0x45, 0x53, 0x20, 0x55, 0x48,
                0x44, 0x20, 0x44, 0x65, 0x6d, 0x6f, 0x20, 0x43, 0x68, 0x61, 0x6e, 0x6e, 0x65, 0x6c, 0x20, 0x30,
                0x33, 0x00, 0x03, 0xff, 0x80, 0x1f, 0x48, 0x1d, 0x1f, 0x03, 0x53, 0x45, 0x53, 0x17, 0x53, 0x45,
                0x53, 0x20, 0x55, 0x48, 0x44, 0x20, 0x44, 0x65, 0x6d, 0x6f, 0x20, 0x43, 0x68, 0x61, 0x6e, 0x6e,
                0x65, 0x6c, 0x20, 0x30, 0x32, 0x36, 0x49, 0xc3, 0xe5 };
            Program p = new Program();
            p.processSDT(bufsdt);
            byte[] bufpmt = {  
 0x00,0x02,0xb1,0x6e,0x74,0xa1,0xc1,0x00,0x00,0xf4,0x30,0xf0,0x00,0x02,0xe0,0xa8,
 0xf0,0x1c,0x52,0x01,0xa8,0x09,0x11,0x01,0x00,0xe5,0x06,0x41,0x06,0xff,0xff,0xff,
 0xff,0xff,0xff,0xff,0xff,0xff,0x2f,0x25,0x09,0x04,0x18,0x10,0xe8,0xee,0x04,0xe0,
 0x70,0xf0,0x22,0x52,0x01,0x70,0x0a,0x04,0x73,0x70,0x61,0x01,0x09,0x11,0x01,0x00,
 0xe5,0x06,0x41,0x06,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0x2f,0x25,0x09,
 0x04,0x18,0x10,0xe8,0xee,0x04,0xe0,0x71,0xf0,0x1f,0x0a,0x04,0x64,0x6f,0x73,0x01,
 0x09,0x11,0x01,0x00,0xe5,0x06,0x41,0x06,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
 0xff,0x2f,0x25,0x09,0x04,0x18,0x10,0xe8,0xee,0xc0,0xe0,0xd0,0xf0,0x69,0xc6,0x05,
 0x01,0x00,0x05,0x06,0xff,0xc2,0x60,0x43,0x53,0x45,0x5f,0x41,0x55,0x44,0x49,0x43,
 0x53,0x45,0x5f,0x43,0x48,0x41,0x4e,0x43,0x53,0x45,0x5f,0x45,0x50,0x47,0x31,0x43,
 0x53,0x45,0x5f,0x45,0x50,0x47,0x32,0x43,0x53,0x45,0x5f,0x46,0x55,0x54,0x31,0x43,
 0x53,0x45,0x5f,0x46,0x55,0x54,0x32,0x43,0xe0,0xd0,0xf0,0x59,0xc6,0x05,0x01,0x00,
 0x05,0x06,0xff,0xc2,0x50,0x43,0x53,0x45,0x5f,0x41,0x55,0x44,0x49,0x43,0x53,0x45,
 0x5f,0x43,0x48,0x41,0x4e,0x43,0x53,0x45,0x5f,0x45,0x50,0x47,0x31,0x43,0x53,0x45,
 0x5f,0x45,0x50,0x47,0x32,0x43,0x53,0x45,0x5f,0x46,0x55,0x54,0x31,0x43,0x53,0x45,
 0x5f,0x46,0x55,0x54,0x32,0x43,0x53,0x45,0x5f,0x4d,0x53,0x44,0x31,0x43,0x53,0x45,
 0x5f,0x4d,0x53,0x44,0x32,0x43,0x53,0x45,0x5f,0x50,0x49,0x4c,0x31,0x43,0x53,0x45,
 0x5f,0x50,0x49,0x4c,0x32,0xc0,0xe0,0xde,0xf0,0x0a,0xc2,0x08,0x43,0x4f,0x4d,0x55,
 0x4e,0x00,0x00,0x00,0xc0,0xe1,0x75,0xf0,0x0a,0xc2,0x08,0x42,0x41,0x4e,0x4e,0x45,
 0x52,0x20,0x50,0xc1,0xe0,0xd5,0xf0,0x0a,0xc2,0x08,0x50,0x49,0x4c,0x4f,0x54,0x45,
 0x00,0x00,0xc1,0xe0,0xfd,0xf0,0x0a,0xc2,0x08,0x44,0x43,0x4f,0x4d,0x55,0x4e,0x00,
 0x00,0xc1,0xe1,0x33,0xf0,0x0a,0xc2,0x08,0x4c,0x41,0x4e,0x5a,0x00,0x00,0x00,0x00,
 0xc1,0xe1,0x64,0xf0,0x0a,0xc2,0x08,0x4c,0x4f,0x52,0x44,0x00,0x00,0x00,0x00,0xc1};
            p.processPMT(bufpmt,0);
            byte[] part = p.getPart(0, 5);
            part = p.getPart(10, 50);
            log.Debug("Done");
            byte[] barr = new byte[4] { 0x01, 0x12, 0x14, 0x25 };
            Console.WriteLine("BCD-String: " + Utils.Utils.bcdtohex(barr, 8));
            Console.WriteLine("BCD-String: " + Utils.Utils.bcdtohex(barr, 7));
        }
    }
}
