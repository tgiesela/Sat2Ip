using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    public class RtpPacket
    {
        private RtpHeader hdr;
        private struct RtpHeader
        {
            public int Version;
            public bool Padding;
            public bool Extension;
            public int Csrccount;
            public bool Marker;
            public int Payloadtype;
            public ushort Sequencenumber;
            public Int32 Timestamp;
            public Int32 Ssrc;
            public Int32[] CsrcList;
            public int Length;
            internal ushort ExtensionID;
            internal ushort ExtensionHeaderLength;
            internal int PayloadLength;
        }
        private byte[] buffer { get; }
        public RtpPacket(byte[] _buffer)
        {
            this.buffer = _buffer;
            extractHeader();
        }
        public int Payloadtype
        {
            get { return hdr.Payloadtype; }
        }

        private void extractHeader()
        {
            /*  Stream setup packet, met een header

                    Field           Bit Length      Description
                    Version				2			Identifies the version of RTP, which is the same in RTCP
                                                    packets as in RTP data packets.The version defined by
                                                    this specification is two (2).
                    Padding				1			If the padding bit is set, the packet contains one
                                                    or more additional padding octets at the end which are not part
                                                    of the payload.The last octet of the padding contains
                                                    a count of how many padding octets should be ignored.
                    Extension           1           If the extension bit is set, the fixed header is followed by exactly one header extension, 
                                                    with a format defined in Section 5.2.1.
                    CSRC Count          4           The CSRC count contains the number of CSRC identifiers that follow the fixed header.
                    Marker              1           The interpretation of the marker is defined by a profile. It is intended to allow 
                                                    significant events such as frame boundaries to be marked in the packet stream
                    Payload Type        7           Contains the constant 200 to identify this as an RTCP SR packet.
                    Sequence number     16          The sequence number increments by one for each RTP data packet sent, 
                                                    and may be used by the receiver to detect packet loss and to restore packet sequence
                    timestamp           32          The timestamp reflects the sampling instant of the first octet in the RTP 
                                                    data packet. The sampling instant must be derived from a clock that increments 
                                                    monotonically and linearly in time to allow synchronization and jitter calculations
                    SSRC                32          The SSRC field identifies the synchronization source. 
                    CSRC list           32 each     The CSRC list identifies the contributing sources for the payload contained in this packet. 
                                                    The number of identifiers is given by the CC field. If there are more than 15 contributing 
                                                    sources, only 15 may be identified. CSRC identifiers are inserted by mixers, using the SSRC 
                                                    identifiers of contributing sources. 
            */

            hdr = new RtpHeader();
            hdr.Version = (buffer[0] & 0xC0) >> 6;
            if (((buffer[0] & 0x20) >> 5) == 1) /* padding */
                hdr.Padding = true;
            else
                hdr.Padding = false;
            if (((buffer[0] & 0x10) >> 4) == 1) /*extension */
                hdr.Extension = true;
            else
                hdr.Extension = false;
            hdr.Csrccount = ((buffer[0] & 0x0F));
            if (((buffer[1] & 0x80) >> 7) == 1)
                hdr.Marker = true;
            else
                hdr.Marker = false;

            hdr.Payloadtype = buffer[1];
            hdr.Sequencenumber = Utils.Utils.toShort(buffer[2], buffer[3]);
            hdr.Timestamp = Utils.Utils.toInt(buffer[4], buffer[5], buffer[6], buffer[7]);
            hdr.Ssrc = Utils.Utils.toInt(buffer[8], buffer[9], buffer[10], buffer[11]);
            hdr.CsrcList = new int[hdr.Csrccount];
            for (int i = 0; i < hdr.Csrccount; i++)
            {
                if (buffer.Length >= (12 + i * 4))
                {
                    hdr.CsrcList[i] = Utils.Utils.toInt(buffer[12 + i * 4], buffer[13 + i * 4], buffer[14 + i * 4], buffer[15 + i * 4]);
                }
            }
            if (hdr.Extension)
            {
                hdr.ExtensionID = Utils.Utils.toShort(buffer[12 + hdr.Csrccount * 4], buffer[13 + hdr.Csrccount * 4]);
                hdr.ExtensionHeaderLength = Utils.Utils.toShort(buffer[14 + hdr.Csrccount * 4], buffer[15 + hdr.Csrccount * 4]);
                hdr.Length = 12 + hdr.Csrccount * 4 + hdr.ExtensionHeaderLength + 4;
            }
            else
            {
                hdr.ExtensionID = 0;
                hdr.ExtensionHeaderLength = 0;
                hdr.Length = 12 + hdr.Csrccount * 4;
            }
            hdr.PayloadLength = buffer.Length - hdr.Length;
            if (hdr.Padding)
            {
                int padcount = buffer[buffer.Length - 1];
                hdr.PayloadLength -= padcount;
            }
            if ((hdr.PayloadLength + hdr.Length) > buffer.Length)
            {
                throw new Exception("Total length exceeds length of buffer");
            }
        }

        public byte[] getPayload()
        {
            byte[] payload = new byte[hdr.PayloadLength];
            Array.Copy(buffer, hdr.Length, payload,0, hdr.PayloadLength);
            return payload;
        }
        public byte[] getPayload(int start, int length)
        {
            Span<byte> bufferspan = buffer;
            return bufferspan.Slice(start + hdr.Length, length).ToArray();
        }
    }
}
