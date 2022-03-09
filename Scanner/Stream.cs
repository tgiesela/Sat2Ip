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
                    case 0x02: return "ITU - T Rec.H.262 | ISO / IEC 13818 - 2 Video or ISO / IEC 11172 - 2 constrained parameter video stream"; 
                    case 0x03: return "ISO / IEC 11172 Audio";
                    case 0x04: return "ISO / IEC 13818 - 3 Audio";
                    case 0x05: return "ITU - T Rec.H.222.0 | ISO / IEC 13818 - 1 private_sections";
                    case 0x06: return "ITU - T Rec.H.222.0 | ISO / IEC 13818 - 1 PES packets containing private data";
                    case 0x07: return "ISO/IEC 13522 MHEG";
                    case 0x08: return "ITU-T Rec.H.222.0 | ISO/IEC 13818-1 Annex A DSM CC";
                    case 0x09: return "ITU-T Rec. H.222.1";
                    case 0x0A: return "ISO/IEC 13818-6 type A";
                    case 0x0B: return "ISO/IEC 13818-6 type B";
                    case 0x0C: return "ISO/IEC 13818-6 type C";
                    case 0x0D: return "ISO/IEC 13818-6 type D";
                    case 0x0E: return "ISO/IEC 13818-1 auxiliary";
                    default: if (Stream_type >= 0x0F && Stream_type <= 0x7F)
                            return "ITU-T Rec. H.222.0 | ISO/IEC 13818-1 Reserved";
                        else
                            return "User Private";
                }
            }
        }

        public Stream()
        {
            _capids = new List<capid>();
        }
    }
}
