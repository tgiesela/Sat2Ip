using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    [Serializable]
    public class Stream
    {
        int _stream_type;
        int _elementary_pid;
        private List<capid> _capids;
        public List<capid> capids { get { return _capids; } set { _capids = value; } }
        public int Stream_type { get { return _stream_type; } set { _stream_type = value; } }
        public int Elementary_pid { get { return _elementary_pid; } set { _elementary_pid = value; } }
        public String Stream_type_description
        {
            get
            {
                switch (_stream_type)
                {
                    case 0x00: return "ITU-T | ISO/IEC Reserved"; 
                    case 0x01: return "ISO/IEC 11172 Video"; 
                    case 0x02: return "ITU - T Rec.H.262 | ISO / IEC 13818-2 Video or ISO/IEC 11172-2 constrained parameter video stream"; 
                    case 0x03: return "ISO/IEC 11172 Audio";
                    case 0x04: return "ISO/IEC 13818-3 Audio";
                    case 0x05: return "ITU - T Rec.H.222.0 | ISO / IEC 13818-1 private_sections";
                    case 0x06: return "ITU - T Rec.H.222.0 | ISO / IEC 13818-1 PES packets containing private data";
                    case 0x07: return "ISO/IEC 13522 MHEG";
                    case 0x08: return "ITU-T Rec.H.222.0 | ISO/IEC 13818-1 Annex A DSM CC";
                    case 0x09: return "ITU-T Rec. H.222.1";
                    case 0x0A: return "ISO/IEC 13818-6 type A";
                    case 0x0B: return "ISO/IEC 13818-6 type B";
                    case 0x0C: return "ISO/IEC 13818-6 type C";
                    case 0x0D: return "ISO/IEC 13818-6 type D";
                    case 0x0E: return "ISO/IEC 13818-1 auxiliary";

                    case 0x0F: return "ISO/IEC 13818-7 Audio with ADTS transport syntax";
                    case 0x10: return "ISO/IEC 14496-2 Visual";
                    case 0x11: return "ISO/IEC 14496-3 Audio with the LATM transport syntax as defined in ISO/IEC 14496-3";
                    case 0x12: return "ISO/IEC 14496-1 SL - packetized stream or FlexMux stream carried in PES packets";
                    case 0x13: return "ISO/IEC 14496-1 SL - packetized stream or FlexMux stream carried in ISO/IEC 14496_sections";
                    case 0x14: return "ISO/IEC 13818-6 Synchronized Download Protocol";
                    case 0x15: return "Metadata carried in PES packets";
                    case 0x16: return "Metadata carried in metadata_sections";
                    case 0x17: return "Metadata carried in ISO/IEC 13818-6 Data Carousel";
                    case 0x18: return "Metadata carried in ISO/IEC 13818-6 Object Carousel";
                    case 0x19: return "Metadata carried in ISO/IEC 13818-6 Synchronized Download Protocol";
                    case 0x1A: return "IPMP stream(defined in ISO/IEC 13818-11, MPEG-2 IPMP)";
                    case 0x1B: return "AVC video stream conforming to one or more profiles defined in Annex A of Rec. ITU-T H.264 | ISO/IEC 14496-10";
                    case 0x1C: return "ISO/IEC 14496-3 Audio, without using any additional transport syntax, such as DST, ALS and SLS";
                    case 0x1D: return "ISO/IEC 14496-17 Text";
                    case 0x1E: return "Auxiliary video stream as defined in ISO/IEC 23002-3";
                    case 0x1F: return "SVC video sub - bitstream of an AVC video stream conforming to one or more profiles defined in Annex G of Rec. ITU-T H.264 | ISO/IEC 14496-10";
                    case 0x20: return "MVC video sub - bitstream of an AVC video stream conforming to one or more profiles defined in Annex H of Rec. ITU-T H.264 | ISO/IEC 14496-10";
                    case 0x21: return "Video stream conforming to one or more profiles as defined in Rec.ITU-T T.800 | ISO/IEC 15444-1";
                    case 0x22: return "Additional view Rec.ITU-T H.262 | ISO/IEC 13818-2 video stream for service - compatible stereoscopic 3D services";
                    case 0x23: return "Additional view Rec.ITU-T H.264 | ISO/IEC 14496-10 video stream conforming to one or more profiles defined in Annex A for service - compatible stereoscopic 3D services";
                    case 0x24: return "Rec.ITU-T H.265 | ISO/IEC 23008-2 video stream or an HEVC temporal video sub - bitstream";
                    case 0x25: return "HEVC temporal video subset of an HEVC video stream conforming to one or more profiles defined in Annex A of Rec.ITU-T H.265 | ISO/IEC 23008-2";
                    case 0x26: return "MVCD video sub - bitstream of an AVC video stream conforming to one or more profiles defined in Annex I of Rec.ITU-T H.264 | ISO/IEC 14496-10";
                    case 0x27: return "Timeline and External Media Information Stream";
                    case 0x28: return "HEVC enhancement sub - Annex G of Rec. ITU-T H.265 | ISO/IEC 23008-2";
                    case 0x29: return "HEVC temporal enhancement sub - Annex G of Rec.ITU-T H.265 | ISO/IEC 23008-2";
                    case 0x2A: return "HEVC enhancement sub - Annex H of Rec.ITU - T H.265 | ISO/IEC 23008-2";
                    case 0x2B: return "HEVC temporal enhancement sub - Annex H of Rec.ITU - T H.265 | ISO/IEC 23008-2";
                    case 0x2C: return "Green access units carried in MPEG-2 sections";
                    case 0x2D: return "ISO/IEC 23008-3 Audio with MHAS transport syntax – main stream";
                    case 0x2E: return "ISO/IEC 23008-3 Audio with MHAS transport syntax – auxiliary stream";
                    case 0x2F: return "Quality access units carried in sections";
     
                    default: 
                        if (Stream_type >= 0x30 && Stream_type <= 0x7F)
                            return "ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Reserved";
                        else
                            return "User Private";
                }
            }
        }

        public string language { get; set; }
        public byte componenttag { get; set; }

        public Stream()
        {
            _capids = new List<capid>();
        }
    }
}
