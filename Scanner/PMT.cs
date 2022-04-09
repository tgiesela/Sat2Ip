using Sat2Ip;
using System;
using System.Collections.Generic;
using static Interfaces.DVBBase;

namespace Interfaces
{
    public class PMT : DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ushort transportstreamid { get; set; }
        private Channel m_channel;

        public PMT(Channel channel)
        {
            log.Debug("SDT created");
            m_channel = channel;
        }
        protected override void processsection(Span<byte> v)
        {
            /*
             * Information from ISO/IEC 13818-1
             * 
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
            /*
             * Here the descriptors should be processed
             */
            Channel channel = m_channel;
            if (channel == null || channel.Pmtpresent) return;
            ushort pcr_pid = Utils.Utils.toShort((byte)(v[0] & 0x1F), v[1]);
            channel.pcr_pid = pcr_pid;

            log.Debug(String.Format("PMT for PID {0} ({0:X4}), pcr_pid {1} ({1:X4})", 
                channel.service_id, pcr_pid));

            ushort program_info_length = Utils.Utils.toShort((byte)(v[2] & 0x0F), v[3]);
            int descriptorlengthprocessed = 0;
            int offset = 4;
            while (descriptorlengthprocessed < program_info_length)
            {
                descriptorlengthprocessed += processdescriptor(v.Slice(descriptorlengthprocessed + offset), channel, Channel._descriptorlevel.program, null);
            }

            offset = offset + program_info_length; /* points after descriptors */
            while ((offset + 3) < v.Length - 4)
            {
                Stream stream = new Stream();
                ushort ES_info_length = Utils.Utils.toShort((byte)(v[offset + 3] & 0x0F), v[offset + 4]);
                stream.Elementary_pid = Utils.Utils.toShort((byte)(v[offset + 1] & 0x1F), v[offset + 2]);
                stream.Stream_type = v[offset]; 
                int bytesprocessed = 0;
                offset += 5;
                while (bytesprocessed < ES_info_length)
                {
                    bytesprocessed += processdescriptor(v.Slice(bytesprocessed + offset), channel, Channel._descriptorlevel.stream, stream);
                }

                offset = offset + ES_info_length;
                channel.Pmt.Add(stream);
            }
        }
        private int processdescriptor(Span<byte> descriptor, Channel channel, Channel._descriptorlevel level, Stream stream)
        {
            int id = (int)descriptor[0];
            int length = (int)descriptor[1];

            switch (id)
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
                        capid.Cadescriptor = descriptor.Slice(0,length+2).ToArray();
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
                        capid.Cadescriptor = descriptor.Slice(0,length+2).ToArray();
                        stream.capids.Add(capid);
                    }
                    break;
                case 0x52: /* stream_identifier_descriptor */
                    if (stream != null)
                        stream.componenttag = descriptor[2];
                    break;
                case 0x0A: /* language descriptor */
                    if (stream != null)
                        stream.language = System.Text.Encoding.Default.GetString(descriptor.Slice(2, 3).ToArray()); 
                    break;
                case 0x7F: /* extension descriptor */
                    break;
                case 0x56: /* teletext_descriptor */
                    break;
                case 0x6F: /* application_signalling_descriptor (see ETSI TS 102 809 [48]) */
                    break;
                case 0x13: /* ?? */
                    break;
                case 0x66: /* data_broadcast_id_descriptor */
                    break;
                case 0x6A: /* AC-3_descriptor */
                    break;
                default:
                    log.DebugFormat("Unsupported descriptor in PMT: {0:X2}",id);
                    break;
            }
            return length + 2;
        }

    }
}
