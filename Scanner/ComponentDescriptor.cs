using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class ComponentDescriptor
    {
        public short stream_content_ext { get; set; }
        public short stream_content { get; set; }
        public byte component_tag { get; set; }
        public byte component_type { get; set; }
        public string description { get; set; }
        public string language { get; set; }
        public string defaultdescription 
        { get
            {
                switch (stream_content)
                {
                    case 0x00: return "reserved for future use";
                    case 0x01: /* video */
                        switch (component_type)
                        {
                            case 0x01: return "MPEG-2 video, 4:3 aspect ratio, 25 Hz";
                            case 0x02: return "MPEG-2 video, 16:9 aspect ratio with pan vectors,25 Hz";
                            case 0x03: return "MPEG - 2 video, 16:9 aspect ratio without panvectors, 25 Hz";
                            case 0x04: return "MPEG - 2 video, > 16:9 aspect ratio, 25 Hz(see  note 2)";
                            case 0x05: return "MPEG - 2 video, 4:3 aspect ratio, 30 Hz(see note  2)";
                            case 0x06: return "MPEG - 2 video, 16:9 aspect ratio with pan vectors,30 Hz(see note 2)";
                            case 0x07: return "MPEG - 2 video, 16:9 aspect ratio without panvectors, 30 Hz(see note 2)";
                            case 0x08: return "MPEG - 2 video, > 16:9 aspect ratio, 30 Hz(see  note 2)";
                            case 0x09: return "MPEG - 2 high definition video, 4:3 aspect ratio, 25Hz(see note 2)";
                            case 0x0A: return "MPEG - 2 high definition video, 16:9 aspect ratiowith pan vectors, 25 Hz(see note 2)";
                            case 0x0B: return "MPEG - 2 high definition video, 16:9 aspect ratiowithout pan vectors, 25 Hz(see note 2)";
                            case 0x0C: return "MPEG - 2 high definition video, > 16:9 aspect ratio,25 Hz(see note 2)";
                            case 0x0D: return "MPEG - 2 high definition video, 4:3 aspect ratio, 30Hz(see note 2)";
                            case 0x0E: return "MPEG - 2 high definition video, 16:9 aspect ratio with pan vectors, 30 Hz(see note 2)";
                            case 0x0F: return "MPEG - 2 high definition video, 16:9 aspect ratio without pan vectors, 30 Hz(see note 2)";
                            case 0x10: return "MPEG - 2 high definition video, > 16:9 aspect ratio, 30 Hz(see note 2)";
                            default: return "reserved for future use";
                        }
                    case 0x02: /* audio */
                        switch (component_type)
                        {
                            case 0x01: return "MPEG - 1 Layer 2 audio, single mono channel";
                            case 0x02: return "MPEG - 1 Layer 2 audio, dual mono channel";
                            case 0x03: return "MPEG - 1 Layer 2 audio, stereo(2 channel)";
                            case 0x04: return "MPEG - 1 Layer 2 audio, multi - lingual, multi - channel";
                            case 0x05: return "MPEG - 1 Layer 2 audio, surround sound";
                            case 0x40: return "MPEG - 1 Layer 2 audio description for the visually impaired(see note 5)";
                            case 0x41: return "MPEG - 1 Layer 2 audio for the hard of hearing";
                            case 0x42: return "receiver - mix supplementary audio as per annex E of ETSI TS 101 154[9]";
                            case 0x47: return "MPEG - 1 Layer 2 audio, receiver - mix audio description";
                            case 0x48: return "MPEG - 1 Layer 2 audio, broadcast - mix audio description";
                            default: return "reserved for future use";
                        }
                    case 0x03: /* teletext subtitles */
                        switch (component_type)
                        {
                            case 0x00: return "reserved for future use";
                            case 0x01: return "EBU Teletext subtitles";
                            case 0x02: return "associated EBU Teletext";
                            case 0x03: return "VBI data";
                            case 0x10: return "DVB subtitles defined in ETSI EN 300 743[54](normal) with no monitor aspect ratio criticality";
                            case 0x11: return "DVB subtitles defined in ETSI EN 300 743[54](normal) for display on 4:3 aspect ratio monitor";
                            case 0x12: return "DVB subtitles defined in ETSI EN 300 743[54](normal) for display on 16:9 aspect ratio monitor";
                            case 0x13: return "DVB subtitles defined in ETSI EN 300 743[54](normal) for display on 2.21:1 aspect ratio monitor";
                            case 0x14: return "DVB subtitles defined in ETSI EN 300 743[54](normal) for display on a high definition monitor";
                            case 0x15: return "DVB subtitles defined in ETSI EN 300 743[54](normal) with plano - stereoscopic disparity for display on a high definition monitor";
                            case 0x20: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) with no monitor aspect ratio criticality";
                            case 0x21: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) for display on 4:3 aspect ratio monitor";
                            case 0x22: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) for display on 16:9 aspect ratio monitor";
                            case 0x23: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) for display on 2.21:1 aspect ratio monitor";
                            case 0x24: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) for display on a high definition monitor";
                            case 0x25: return "DVB subtitles defined in ETSI EN 300 743[54](for the hard of hearing) with plano-stereoscopic disparity for display on a high definition monitor";
                            case 0x30: return "open(in -vision) sign language interpretation for the deaf ";
                            case 0x31: return "closed sign language interpretation for the deaf";
                            case 0x40: return "video spatial resolution has been upscaled from lower resolution source material";
                            case 0x41: return "Video is standard dynamic range(SDR)";
                            case 0x42: return "Video is high dynamic range(HDR) remapped from standard dynamic range(SDR) source material";
                            case 0x43: return "Video is high dynamic range(HDR) up - converted from standard dynamic range(SDR) source material";
                            case 0x44: return "Video is standard frame rate, less than or equal to 60 Hz";
                            case 0x45: return "High frame rate video generated from lower frame rate source material";
                            case 0x80: return "dependent SAOC - DE data stream";
                            default: return "reserved for future use";
                        }
                    case 0x04: /* AC3 audio mode */
                        if (component_type <= 0x7F)
                            return "reserved for AC-3 audio modes";
                        else
                            return "reserved for enhanced AC-3 audio modes";
                    case 0x05: /* HD video */
                        switch (component_type)
                        {
                            case 0x00: return "reserved for future use";
                            case 0x01: return "H.264 / AVC standard definition video, 4:3 aspect ratio, 25 Hz";
                            case 0x02: return "reserved for future use";
                            case 0x03: return "H.264 / AVC standard definition video, 16:9 aspect ratio, 25 Hz";
                            case 0x04: return "H.264 / AVC standard definition video, > 16:9 aspect ratio, 25 Hz";
                            case 0x05: return "H.264 / AVC standard definition video, 4:3 aspect ratio, 30 Hz";
                            case 0x06: return "reserved for future use";
                            case 0x07: return "H.264 / AVC standard definition video, 16:9 aspect ratio, 30 Hz";
                            case 0x08: return "H.264 / AVC standard definition video, > 16:9 aspect ratio, 30 Hz";
                            case 0x0B: return "H.264 / AVC high definition video, 16:9 aspect ratio,25 Hz";
                            case 0x0C: return "H.264 / AVC high definition video, > 16:9 aspect ratio, 25 Hz";
                            case 0x0F: return "H.264 / AVC high definition video, 16:9 aspect ratio,30 Hz";
                            case 0x10: return "H.264 / AVC high definition video, > 16:9 aspect ratio, 30 Hz";
                            case 0x80: return "H.264 / AVC plano - stereoscopic frame compatible high definition video, 16:9 aspect ratio, 25 Hz, Side - by - Side";
                            case 0x81: return "H.264 / AVC plano - stereoscopic frame compatible high definition video, 16:9 aspect ratio, 25 Hz,Top - and - Bottom";
                            case 0x82: return "H.264 / AVC plano - stereoscopic frame compatible high definition video, 16:9 aspect ratio, 30 Hz,Side - by - Side";
                            case 0x83: return "H.264 / AVC stereoscopic frame compatible high definition video, 16:9 aspect ratio, 30 Hz, Top - and - Bottom";
                            case 0x84: return "H.264 / MVC dependent view, plano - stereoscopic service compatible video";
                            default: return "reserved for future use";
                        }
                    case 0x06: /* HE AAC audio */
                        switch (component_type)
                        {
                            case 0x00: return "reserved for future use";
                            case 0x01: return "HE AAC audio, single mono channel";
                            case 0x02: return "reserved for future use";
                            case 0x03: return "HE AAC audio, stereo";
                            case 0x04: return "reserved for future use";
                            case 0x05: return "HE AAC audio, surround sound";
                            case 0x40: return "HE AAC audio description for the visually impaired";
                            case 0x41: return "HE AAC audio for the hard of hearing";
                            case 0x42: return "HE AAC receiver - mix supplementary audio as per annex E of ETSI TS 101 154[9]";
                            case 0x43: return "HE AAC v2 audio, stereo";
                            case 0x44: return "HE AAC v2 audio description for the visually impaired";
                            case 0x45: return "HE AAC v2 audio for the hard of hearing";
                            case 0x46: return "HE AAC v2 receiver - mix supplementary audio as per annex E of ETSI TS 101 154[9]";
                            case 0x47: return "HE AAC receiver - mix audio description for the visually impaired";
                            case 0x48: return "HE AAC broadcast - mix audio description for the visually impaired";
                            case 0x49: return "HE AAC v2 receiver - mix audio description for the visually impaired";
                            case 0x4A: return "HE AAC v2 broadcast - mix audio description for the visually impaired";
                            case 0xA0: return "HE AAC, or HE AAC v2 with SAOC - DE ancillary data";
                            default: return "reserved for future use";
                        }
                    case 0x07: /* DTS/DTS-HD audio mode */
                        if (component_type <= 0x7F)
                            return "reserved for DTS and DTS-HD audio modes";
                        else
                            return "reserved for future use";
                    case 0x08: /* ?? */
                        switch (component_type)
                        {
                            case 0x00: return "reserved for future use";
                            case 0x01: return "DVB SRM data defined in ETSI TS 102 770 [41]";
                            default: return "reserved for DVB CPCM modes defined in ETSI TS / TR 102 825[39] and[i.3]";
                        }
                    case 0x09: /* HEVC video */
                        switch (stream_content_ext)
                        {
                            case 0x00:
                                switch (component_type)
                                {
                                    case 0x00: return "HEVC Main Profile high definition video, 50 Hz";
                                    case 0x01: return "HEVC Main 10 Profile high definition video, 50 Hz";
                                    case 0x02: return "HEVC Main Profile high definition video, 60 Hz";
                                    case 0x03: return "HEVC Main 10 Profile high definition video, 60 Hz";
                                    case 0x04: return "HEVC ultra high definition video";
                                    case 0x05: return "HEVC ultra high definition video with PQ10 HDR with a frame rate lower than or equal to 60 Hz";
                                    case 0x06: return "HEVC ultra high definition video, frame rate of 100 Hz, 120 000 / 1 001 Hz, or 120 Hz without a half frame rate HEVC temporal video sub-bit - stream";
                                    case 0x07: return "HEVC ultra high definition video with PQ10 HDR, frame rate of 100 Hz, 120 000 / 1 001 Hz, or 120 Hz without a half frame rate HEVC temporal video sub - bit - stream";
                                    default: return "reserved for future use";
                                }
                            case 0x01:
                                switch (component_type)
                                {
                                    case 0x00: return "AC - 4 main audio, mono ";
                                    case 0x01: return "AC - 4 main audio, mono, dialogue enhancement enabled";
                                    case 0x02: return "AC - 4 main audio, stereo";
                                    case 0x03: return "AC - 4 main audio, stereo, dialogue enhancement enabled";
                                    case 0x04: return "AC - 4 main audio, multichannel ";
                                    case 0x05: return "AC - 4 main audio, multichannel, dialogue enhancement enabled";
                                    case 0x06: return "AC - 4 broadcast - mix audio description, mono, for the visually impaired";
                                    case 0x07: return "AC - 4 broadcast - mix audio description, mono, for the visually impaired, dialogue enhancement enabled";
                                    case 0x08: return "AC - 4 broadcast - mix audio description, stereo, for the visually impaired";
                                    case 0x09: return "AC - 4 broadcast - mix audio description, stereo, for the visually impaired, dialogue enhancement enabled";
                                    case 0x0A: return "AC - 4 broadcast - mix audio description, multichannel, for the visually impaired";
                                    case 0x0B: return "AC - 4 broadcast - mix audio description, multichannel, for the visually impaired, dialogue enhancement enabled";
                                    case 0x0C: return "AC - 4 receiver - mix audio description, mono, for the visually impaired";
                                    case 0x0D: return "AC - 4 receiver - mix audio description, stereo, for the visually impaired";
                                    case 0x0E: return "AC - 4 Part - 2";
                                    case 0x0F: return "MPEG - H Audio LC Profile";
                                    default: return "reserved for future use";
                                }
                            case 0x02:
                                return "TTML subtitles defined in ETSI EN 303 560 [55]";
                            default: return "reserved for future use";
                        }
                    case 0x0A:
                        return "reserved for future use";
                    case 0x0B:
                        switch (stream_content_ext)
                        {
                            case 0x0E: return "NGA component type feature flags according to table 27";
                            case 0x0F:
                                switch (component_type)
                                {
                                    case 0x00: return "less than 16:9 aspect ratio";
                                    case 0x01: return "16:9 aspect ratio";
                                    case 0x02: return "greater than 16:9 aspect ratio";
                                    case 0x03: return "plano - stereoscopic top and bottom(TaB) framepacking";
                                    case 0x04: return "HLG10 HDR(see notes 7, 11, and 12)";
                                    case 0x05: return "HEVC temporal video subset for a frame rate of 100 Hz, 120 000 / 1 001 Hz, or 120 Hz";
                                    default: return "reserved for future use";
                                }
                            default: return "reserved for future use";
                        }
                    default: return "?";
                }
            }
        }
    }
}
