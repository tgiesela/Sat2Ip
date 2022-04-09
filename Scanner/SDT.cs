using Sat2Ip;
using System;
using System.Collections.Generic;
using static Interfaces.DVBBase;

namespace Interfaces
{
    public class SDT : DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ushort transportstreamid { get; set; }
        public List<Channel> pids { get; set; }

        private Transponder m_transponder;

        public SDT(Transponder tsp)
        {
            log.Debug("SDT created");
            pids = new List<Channel>();
            m_transponder = tsp;
        }
        protected override void processsection(Span<byte> section)
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

            ushort original_network_id = Utils.Utils.toShort(section[0], section[1]);
            int offset = 3; /* points to table */
            try
            {
                while (offset < section.Length - 4)
                {
                    ushort service_id = Utils.Utils.toShort(section[offset], section[offset + 1]); 
                    Channel channel = pids.Find(x => x.service_id == service_id);
                    if (channel == null)
                    {
                        channel = new Channel(0, m_transponder);
                        PAT.PATEntry pe = m_transponder.pids.Find(x => x.serviceid == service_id);
                        if (pe != null)
                        {
                            channel.Programpid = pe.programpid;
                        }
                        else
                        {
                            log.DebugFormat("Service ID {0} ({0:X}) in SDT NOT FOUND in PAT", service_id);
                        }
                        channel.service_id = service_id;
                        pids.Add(channel);
                    }
                    channel.EIT_schedule_flag = (section[offset + 2] & 0x02) >> 1;
                    channel.EIT_present_following_flag = (section[offset + 2] & 0x01);
                    channel.running_status = (section[offset + 3] & 0xE0) >> 5;
                    channel.free_CA_mode = (section[offset + 3] & 0x10) >> 4;
                    ushort descriptors_loop_length = Utils.Utils.toShort((byte)(section[offset + 3] & 0x0F), section[offset + 4]);
                    int descriptorlengthprocessed = 0;
                    offset += 5;
                    while (descriptorlengthprocessed < descriptors_loop_length)
                    {
                        descriptorlengthprocessed += processsdtdescriptor(section.Slice(descriptorlengthprocessed + offset), channel);
                        log.Debug(String.Format("SDT for service {0} ({1}), pid {2}", service_id, service_id.ToString("X4"), channel.Programpid));
                    }
                    offset += descriptors_loop_length;
                }
            }
            catch (Exception ex)
            {
                log.Debug("Malformed SDT!! Exception is :" + ex.Message);
            }

        }
        private int processsdtdescriptor(Span<byte> descriptor, Channel channel)
        {
            /*
             * Documentation from DVB BlueBook A038 
             *      Digital Video Broadcasting (DVB);
             *   Specification for Service Information (SI)
             *              in DVB systems)
             *            DVB Document A038
             *              July 2014
             */
            int id = (int)descriptor[0];
            int length = (int)descriptor[1];
            int lenused = 0;
            switch (descriptor[0])
            {
                case 0x42:/* stuffing descriptor*/
                case 0x48:/* service descriptor */
                    int service_type = descriptor[2];
                    channel.Servicetype = service_type;
                    channel.Providername = DVBBase.getStringFromDescriptor(descriptor.ToArray(), 3, ref lenused);
                    channel.Servicename = DVBBase.getStringFromDescriptor(descriptor.ToArray(), 3 + lenused + 1, ref lenused);
                    if (channel.Servicename.Length == 0)
                    {
                        channel.Servicename = channel.transponder.frequency.ToString() + "-" + channel.Programpid.ToString();
                    }
                    break;
                case 0x49: break; /* country availability descriptor*/
                case 0x4A: break; /* linkage descriptor */
                case 0x4B: break; /* NVOD_reference descriptor */
                case 0x4C: break; /* time shifted service descriptor */
                case 0x50:        /* component descriptor */
                    ComponentDescriptor descr = new ComponentDescriptor();
                    descr.stream_content_ext = (short)((descriptor[2] & 0xF0) >> 4);
                    descr.stream_content = (short)((descriptor[2] & 0x0F));
                    descr.component_type = descriptor[3];
                    descr.component_tag = descriptor[4];
                    descr.language = System.Text.Encoding.Default.GetString(descriptor.Slice(5,3).ToArray());
                    descr.description = DVBBase.getStringFromDescriptor(descriptor.Slice(8, length-6).ToArray(), 0, ref lenused);
                    ComponentDescriptor existing = channel.componentdescriptors.Find(x => x.component_tag == descr.component_tag);
                    if (existing != null)
                    {
                        channel.componentdescriptors.Remove(existing);
                    }
                    channel.componentdescriptors.Add(descr);
                    break; 
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
            return length + 2;
        }
    }
}
