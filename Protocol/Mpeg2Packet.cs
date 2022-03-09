﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace Sat2Ip
{
    public class Mpeg2Packet
    {
        public int payloadlength { get; set; }
        public byte[] buffer { get; }
        public int transporterror { get; private set; }
        public int payloadstartindicator { get; internal set; }
        public int priority { get; private set; }
        public ushort pid { get; private set; }
        public int scramblingcontrol { get; private set; }
        public int adaptation { get; private set; }
        public int continuitycounter { get; private set; }
        public int headerlen { get; private set; }
        public byte[] payload { get; internal set; }

        public Mpeg2Packet(byte[] _buffer)
        {
            this.buffer = _buffer;
            this.payload = new byte[188];
            extractHeader();
        }
        private void extractHeader()
        {
            /*
                http://www.abdn.ac.uk/erg/research/future-net/digital-video/mpeg2-trans.html
            
            The header has the following fields:

            The header starts with a well-known Synchronisation Byte (8 bits). This has the bit pattern 0x47(0100 0111).
            A set of three flag bits are used to indicate how the payload should be processed.
                The first flag indicates a transport error.
                The second flag indicates the start of a payload(payload_unit_start_indicator)
                The third flag indicates transport priority bit.
            The flags are followed by a 13 bit Packet Identifier(PID).This is used to uniquely identify the stream to which 
            the packet belongs (e.g.PES packets corresponding to an ES) generated by the multiplexer. The PID allows the 
            receiver to differentiate the stream to which each received packet belongs.Some PID values are predefined and 
            are used to indicate various streams of control information.A packet with an unknown PID, or one with a PID 
            which is not required by the receiver, is silently discarded.The particular PID value of 0x1FFF is reserved 
            to indicate that the packet is a null packet(and is to be ignored by the receiver).
            The two scrambling control bits are used by conditional access procedures to encrypted the payload of some TS packets.
            Two adaption field control bits which may take four values:
                01 – no adaptation field, payload only
                10 – adaptation field only, no payload
                11 – adaptation field followed by payload
                00 - RESERVED for future use
            Finally there is a half byte Continuity Counter(4 bits)
            */
            int offset = 0;
            if (buffer[offset] == 0x47)
            {
                transporterror = (buffer[offset + 1] & 0x80) >> 7;
                payloadstartindicator = (buffer[offset + 1] & 0x40) >> 6;
                priority = (buffer[offset + 1] & 0x20) >> 5;
                pid = Utils.Utils.toShort((byte)(buffer[offset + 1] & 0x1F), (buffer[offset + 2]));
                scramblingcontrol = (buffer[offset + 3] & 0xC0) >> 6;
                adaptation = (buffer[offset + 3] & 0x30) >> 4;
                continuitycounter = (buffer[offset + 3] & 0x0F);
                if (adaptation == 0x02 || adaptation == 0x03)
                {
                    headerlen = 4 + 1 + processAdaptation(buffer, offset + 4);
                }
                else
                {
                    headerlen = 4;
                }
                offset += headerlen;
                Array.Copy(buffer, offset, payload,0, (188 - offset));
                payloadlength = 188 - offset;
            }
        }
        private int processAdaptation(byte[] v, int offset)
        {
            int adaptation_field_length = v[offset];
            if (adaptation_field_length > 0)
            {
                int discontinuity_indicator = v[offset + 1] & 0x80 >> 7;
                int random_access_indicator = v[offset + 1] & 0x40 >> 6;
                int elementary_stream_priority_indicator = v[offset + 1] & 0x20 >> 5;
                int PCR_flag = v[offset + 1] & 0x10 >> 4;
                int OPCR_flag = v[offset + 1] & 0x08 >> 3;
                int splicing_point_flag = v[offset + 1] & 0x04 >> 2;
                int transport_private_data_flag = v[offset + 1] & 0x02 >> 1;
                int adaptation_field_extension_flag = v[offset + 1] & 0x01;
                offset = offset + 2;
                if (PCR_flag == 1)
                {
                    long program_clock_reference_base = Utils.Utils.toLong(0, 0, 0, v[offset + 2], v[offset + 3], v[offset + 4], v[offset + 5], v[offset + 6]);
                    program_clock_reference_base = program_clock_reference_base >> 7;
                    int reserved = (v[offset + 6] & 0x7E) >> 1;
                    int program_clock_reference_extension = (v[offset + 6] & 0x01) << 8 + v[offset + 7];
                    offset = offset + 6;
                }
                if (OPCR_flag == 1)
                {
                    long original_program_clock_reference_base = Utils.Utils.toLong(0, 0, 0, v[offset], v[offset + 1], v[offset + 2], v[offset + 3], v[offset + 4]);
                    int reserved2 = (v[offset + 4] & 0x7E) >> 1;
                    int original_program_clock_reference_extension = (v[offset + 4] & 0x01) << 8 + v[offset + 5];
                    offset = offset + 6;
                }
                if (splicing_point_flag == 1)
                {
                    int splice_countdown = v[offset];
                    offset = offset + 1;
                }
                if (transport_private_data_flag == 1)
                {
                    int transport_private_data_length = v[offset];
                    byte[] privatedata = new byte[transport_private_data_length];
                    for (int i = 0; i < transport_private_data_length; i++)
                    {
                        privatedata[i] = v[offset + 1 + i];
                    }
                    offset = offset + transport_private_data_length + 1;
                }
                if (adaptation_field_extension_flag == 1)
                {
                    int adaptation_field_extension_length = v[offset];
                    int ltw_flag = (v[offset] & 0x80) >> 7;
                    int piecewise_rate_flag = (v[offset] & 0x40) >> 6;
                    int seamless_splice_flag = (v[offset] & 0x20) >> 5;
                    int reserved3 = (v[offset] & 0x1F0);
                    offset = offset + 1;
                    if (ltw_flag == 1)
                    {
                        int ltw_valid_flag = v[offset] & 0x80 >> 7;
                        ushort ltw_offset = Utils.Utils.toShort((byte)(v[offset] & 0x7F), v[offset + 1]);
                        offset = offset + 2;
                    }
                    if (piecewise_rate_flag == 1)
                    {
                        int reserved4 = v[offset] & 0xC0 >> 6;
                        int piecewise_rate = Utils.Utils.toInt(0, (byte)(v[offset] & 0x3F), v[offset + 1], v[offset + 2]);
                        offset = offset + 3;
                    }
                    if (seamless_splice_flag == '1')
                    {
                        int splice_type = (v[offset] & 0xF0) >> 4;
                        int DTS_next_AU = (v[offset] & 0x0E) << 29;
                        DTS_next_AU = DTS_next_AU + (v[offset + 1] << 21) + ((v[offset + 2] & 0xFE) << 14) +
                                                    (v[offset + 3] << 6) + (v[offset + 4]) >> 1;
                        offset = offset + 5;
                    }
                }
            }
            return adaptation_field_length;
        }
    }

}
