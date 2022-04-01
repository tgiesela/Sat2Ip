using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class Bouquet:DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private int m_bouquet_id;
        private List<BatStream> m_streams;
        private Transponder m_transponder;
        public int bouquet_id { get { return m_bouquet_id; } set { m_bouquet_id = value; } }
        public List<BatStream> streams { get { return m_streams; } set { m_streams = value; } }
        public string bouquet_name { get; set; }
        public Transponder transponder { get { return m_transponder; } set { m_transponder = value; } }
        public Linkage bouquetlinkage { get; set; }
        public Linkage epglinkage { get; set; }
        public Linkage silinkage { get; set; }
        public Bouquet()
        {
            m_transponder = null;
            m_streams = new List<BatStream>();
        }

        private int processtransportstream(Span<byte> v)
        {
            int offset = 0;
            BatStream batstream = new();
            ushort transport_stream_id = Utils.Utils.toShort(v[0], v[1]);
            ushort original_network_id = Utils.Utils.toShort(v[2], v[3]);
            short transport_descriptors_length = (short)Utils.Utils.toShort((byte)(v[4] & 0x0F), v[5]);

            batstream.streamid = (short)transport_stream_id;
            batstream.original_networkid = (short)original_network_id;
            if (streams.Find(x => x.streamid == transport_stream_id) == null)
            {
                log.DebugFormat("Add transportstream to Bouquet {0}: {1} (0x{1:X})", bouquet_id, transport_stream_id);
                streams.Add(batstream);
            }

            int descriptorlengthprocessed = 0;
            offset += 6;
            //log.DebugFormat("Transport stream id: {0} on network {1}", transport_stream_id, original_network_id);
            while (descriptorlengthprocessed < transport_descriptors_length)
            {
                descriptorlengthprocessed += processdescriptor(v.Slice(offset + descriptorlengthprocessed), batstream);
            }
            return descriptorlengthprocessed + 6;
        }

        public int processdescriptor(Span<byte> v, BatStream bs)
        {
            /* Details: see a38_dvb-si_specification */
            int id = (int)v[0];
            int length = (int)v[1];
            switch (id)
            {
                case 0x41: /* service list descriptor */
                    List<ServiceListItem> servicelist = getservicelist(v.Slice(2, length));
                    if (bs == null)
                    {
                        log.Debug("Service list descriptor received without Transport stream??!!");
                        break;
                    }
                    bs.services.AddRange(servicelist);
                    break;
                case 0x42: /* stuffing descriptor */
                    break;
                case 0x47: /* bouquet name descriptor */
                    int lenused = 0;
                    bouquet_name = getStringFromDescriptor(v.ToArray(), 1, ref lenused);
                    break;
                case 0x49: /* country availablitity descriptor */
                    break;
                case 0x4A: /* linkage descriptor */
                    processLinkageDescriptor(v.Slice(2, length));
                    break;
                case 0x53: /* CA-identifier descriptor */
                    break;
                case 0x5C: /* multilingual bouquet name descriptor */
                    break;
                case 0x5F: /* private data specifier descriptor */
                    break;
                case 0x73: /* default auhthority descriptor (TS 102 323 [13] */
                    break;
                case 0x7D: /* XAIT location descriptor */
                    break;
                case 0x7E: /* FTA content management descriptor */
                    break;
                case 0x7F: /* extenstion descriptor */
                    break;
            }
            return length + 2;
        }

        private void processLinkageDescriptor(Span<byte> span)
        {
            short transport_stream_id = (short)((span[0] << 8) | (span[1] & 0xff));
            short original_network_id = (short)((span[2] << 8) | (span[3] & 0xff));
            short service_id = (short)((span[4] << 8) | (span[5] & 0xff)); ;
            byte linkagetype = span[6];
            if (linkagetype == 0x01)
            {
                bouquetlinkage = new Linkage();
                bouquetlinkage.transportstreamid = transport_stream_id;
                bouquetlinkage.networkid = original_network_id;
                bouquetlinkage.serviceid = service_id;
            }
            if (linkagetype == 0x02)
            {
                epglinkage = new Linkage();
                epglinkage.transportstreamid = transport_stream_id;
                epglinkage.networkid = original_network_id;
                epglinkage.serviceid = service_id;
            }
            if (linkagetype == 0x04)
            {
                silinkage = new Linkage();
                silinkage.transportstreamid = transport_stream_id;
                silinkage.networkid = original_network_id;
                silinkage.serviceid = service_id;
            }
        }

        private List<ServiceListItem> getservicelist(Span<byte> buf)
        {
            List<ServiceListItem> items = new();
            int lenprocessed = 0;
            while (lenprocessed < buf.Length)
            {
                ServiceListItem item = new ServiceListItem();
                item.service_id = Utils.Utils.toShort(buf[lenprocessed], buf[lenprocessed + 1]);
                item.service_type = buf[lenprocessed + 2];
                items.Add(item);
                lenprocessed = lenprocessed + 3;
            }
            return items;
        }

        protected override void processsection(Span<byte> section)
        {   /*
            bouquet_association_section(){
                table_id 8 uimsbf
                section_syntax_indicator 1 bslbf
                reserved_future_use 1 bslbf
                reserved 2 bslbf
                section_length 12 uimsbf
                bouquet_id 16 uimsbf
                reserved 2 bslbf
                version_number 5 uimsbf
                current_next_indicator 1 bslbf
                section_number 8 uimsbf
                last_section_number 8 uimsbf
                reserved_future_use 4 bslbf
                bouquet_descriptors_length 12 uimsbf
                for (i = 0; i < N; i++)
                {
                    descriptor()
                }
                reserved_future_use 4 bslbf
                transport_stream_loop_length 12 uimsbf
                for (i = 0; i < N; i++)
                {
                    transport_stream_id 16 uimsbf
                    original_network_id 16 uimsbf
                    reserved_future_use 4 bslbf
                    transport_descriptors_length 12 uimsbf
                    for (j = 0; j < N; j++)
                    {
                        descriptor()
                    }
                }
                CRC_32 32 rpchof
            */

            int offset = 0;
            ushort bouquet_descriptors_length = Utils.Utils.toShort((byte)(section[offset] & 0x0F), section[offset + 1]);
            int bytesprocessed = 0;
            int lenprocessed = 0;
            offset += 2;
            while (bytesprocessed < bouquet_descriptors_length)
            {
                lenprocessed = processdescriptor(section.Slice(offset), null);
                bytesprocessed += lenprocessed;
                offset += lenprocessed;
            }
            ushort transport_stream_loop_length = Utils.Utils.toShort((byte)(section[offset] & 0x0F), section[offset + 1]);
            offset += 2;
            bytesprocessed = 0;
            while (bytesprocessed < transport_stream_loop_length)
            {
                lenprocessed = processtransportstream(section.Slice(offset));
                bytesprocessed += lenprocessed;
                offset += lenprocessed;
            }
        }
    }
}
