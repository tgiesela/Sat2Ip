using Sat2Ip;
using System;
using System.Collections.Generic;
using static Interfaces.DVBBase;

namespace Interfaces
{
    public class Network:DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public int networkid { get; set; }
        public string networkname { get; set; }
        public List<Transponder> transponders { get; set; }
        private bool terresrtialorcable;
        private bool satellite;
        public int diseqcposition { get; set; }
        public List<Linkage> bouquetlinkages { get; set; }
        public List<Linkage> epglinkages { get; set; }
        public List<Linkage> silinkages { get; set; }
        public List<ServiceListItem> networkservices { get; set; }
        public bool satellitenetwork { get { return satellite; } set { satellite = value; } }
        public bool terresrtialorcablenetwork { get { return terresrtialorcable; } set { terresrtialorcable = value; } }
        public bool currentnetwork { get; set; }

        public Network(int _diseqcposition) : this()
        {
            diseqcposition = _diseqcposition;
        }
        public Network()
        {
            transponders = new List<Transponder>();
            bouquetlinkages = new List<Linkage>();
            epglinkages = new List<Linkage>(); 
            silinkages = new List<Linkage>();
            networkservices = new List<ServiceListItem>();
            terresrtialorcable = false;
            satellite = false;
        }

        protected override void processsection(Span<byte> section)
        {
            /*
                service_description_section()
                {
                0	table_id                        8
                1	section_syntax_indicator        1
                    reserved_future_use             1
                    reserved                        2
                    section_length                  12
                3	transport_stream_id             16
                5	reserved                        2
                    version_number                  5
                    current_next_indicator          1
                6	section_number                  8
                7	last_section_number             8
                8	reserved_future_use             4
                    network_descriptors_length		12
                10	for(j=0;j<N;j++)
                    {
                        descriptor()
                    }
                    reserved_future_use             4
                    transport_stream_loop_length	12
                    for(i=0;i<N;i++)
                    {
                        transport_stream_id         16
                        original_network_id			16
                        reserved_future_use         4
                        transport_descriptors_length	12
                        for(j=0;j<N;j++)
                        {
                            descriptor()
                        }
                    }
                    CRC_32                          32
                */
            //Utils.Utils.DumpBytes(section.ToArray(), section.Length);
            int offset = 0;
            ushort network_descriptors_length = Utils.Utils.toShort((byte)(section[0] & 0x0F), section[1]);

            offset += 2; /* points to network descriptors */
            int bytesprocessed = 0;
            while (bytesprocessed < network_descriptors_length)
            {
                bytesprocessed += processdescriptor(section.Slice(offset + bytesprocessed), null);
            }
            offset = offset + bytesprocessed;
            short transport_stream_loop_length = (short)(((section[offset + 0] & 0x0F) << 8) | (section[offset + 1] & 0xff));
            bytesprocessed = 0;
            offset += 2;
            if (section.Length < (offset + transport_stream_loop_length))
            {
                log.Debug("Message too short!");
                return;
            }
            while (bytesprocessed < transport_stream_loop_length)
            {
                bytesprocessed += processtransportstream(section.Slice(offset + bytesprocessed));
            }
        }

        private int processtransportstream(Span<byte> v)
        {
            int offset = 0;
            ushort transport_stream_id = Utils.Utils.toShort(v[0], v[1]);
            ushort original_network_id = Utils.Utils.toShort(v[2], v[3]);
            short transport_descriptors_length = (short)Utils.Utils.toShort((byte)(v[4] & 0x0F), v[5]);

            int descriptorlengthprocessed = 0;
            offset += 6;
            log.DebugFormat("Transport stream id: {0} on network {1}", transport_stream_id, original_network_id);
            Transponder tsp = new();
            tsp.transportstreamid = transport_stream_id;
            tsp.network_id = original_network_id;
            while (descriptorlengthprocessed < transport_descriptors_length)
            {
                descriptorlengthprocessed += processdescriptor(v.Slice(offset + descriptorlengthprocessed), tsp);
            }
            Transponder? existing = transponders.Find(x => x.transportstreamid == tsp.transportstreamid && x.network_id == tsp.network_id);
            if (existing != null)
            {
                transponders.Remove(existing);
            }
            if (tsp.frequency > 0 && (satellitenetwork || terresrtialorcablenetwork))
                transponders.Add(tsp);

            return descriptorlengthprocessed + 6;
        }

        private int processdescriptor(Span<byte> v, Transponder? tsp)
        {
            int id = (int)v[0];
            int length = (int)v[1];
            switch (id)
            {
                case 0x40: /* network name descriptor */
                    int lenused = 0;
                    networkname = DVBBase.getStringFromDescriptor(v.ToArray(), 1, ref lenused);
                    break;
                case 0x41: /* service list descriptor */
                    Span<byte> span = v;
                    List<ServiceListItem> servicelist = getservicelist(v.Slice(2, length));
                    foreach (ServiceListItem item in servicelist)
                    {
                        log.DebugFormat("   id: {0} ({0:X}), type: {1} ({1:X})", item.service_id, item.service_type);
                    }
                    tsp.services = servicelist;
                    break;
                case 0x42: /* stuffing descriptor */
                    break;
                case 0x43: /* satellite delivery system descriptor */
                    if (tsp == null)
                        log.Debug("Satellite delivery descriptor in first loop !!");
                    else
                    {
                        processsatellitedescriptor(v.Slice(2, length), tsp);
                        satellite = true;
                    }
                    break;
                case 0x44: /* cable delivery system descriptor */
                    processcabledescriptor(v.Slice(2, length), tsp);
                    terresrtialorcable = true;
                    break;
                case 0x4A: /* linkage descriptor */
                    processLinkageDescriptor(v.Slice(2, length));
                    break;
                case 0x5A: /* terrestrial_delivery_system_descriptor */
                    processterrestrialdescriptor(v.Slice(2, length), tsp);
                    terresrtialorcable = true;
                    break;
                case 0x5B: /* multilingual_network_name_descriptor */
                    break;
                case 0x5F: /* private_data_specifier_descriptor */
                    break;
                case 0x62: /* frequency_list_descriptor */
                    break;
                case 0x6C: /* cell_list_descriptor */
                    break;
                case 0x6D: /* cell_frequency_link_descriptor */
                    break;
                case 0x73: /* default_authority_descriptor (TS 102 323 [13]) */
                    break;
                case 0x77: /* time_slice_fec_identifier_descriptor (EN 301 192 [4]) (see note 3) */
                    break;
                case 0x79: /* S2_satellite_delivery_system_descriptor */
                    break;
                case 0x7D: /* XAIT location descriptor */
                    break;
                case 0x7E: /* FTA_content_management_descriptor */
                    break;
                case 0x7F: /* extension descriptor */
                    break;
                case 0x83: /* Fast scan logical channel numbers */
                    int loop_length = v[1];
                    int bytesprocessed = 0;
                    while (bytesprocessed < loop_length)
                    {
                        ushort serviceid = Utils.Utils.toShort(v[bytesprocessed+2], v[bytesprocessed+3]); /* Program number */
                        ushort lcn = Utils.Utils.toShort((byte)(v[bytesprocessed + 4] ^ 0xFC), v[bytesprocessed + 5]);
                        //log.DebugFormat("    FS Stream: {0} ({0:X}), {1} - {2}", streamid, lcn, (v[bytesprocessed + 4] & 0x80)>>7);
                        ServiceListItem? service = networkservices.Find(x => x.service_id == serviceid);
                        if (service == null)
                        {
                            service = new ServiceListItem();
                            service.service_id = serviceid;
                            service.lcn = lcn;
                            service.lcnleftpart = v[bytesprocessed+4] & 0xFC;
                            service.service_type = -1;
                            service.transportstreamid = tsp.transportstreamid;
                            networkservices.Add(service);
                        }
                        else
                        {
                            service.lcn = lcn;
                        }
                        bytesprocessed += 4;
                    }
                    break;
            }
            return length + 2;
        }

        private void processLinkageDescriptor(Span<byte> span)
        {
            /* There may be multiple linkages of the same type. We will only store the last one */
            short transport_stream_id = (short)((span[0] << 8) | (span[1] & 0xff));
            short original_network_id = (short)((span[2] << 8) | (span[3] & 0xff));
            short service_id = (short)((span[4] << 8) | (span[5] & 0xff)); 
            byte linkagetype = span[6];
            if (linkagetype == 0x01)
            {
                Linkage? bouquetlinkage = bouquetlinkages.Find(x => x.transportstreamid == transport_stream_id);
                if (bouquetlinkage == null)
                {
                    bouquetlinkage = new Linkage();
                    bouquetlinkages.Add(bouquetlinkage);
                }
                bouquetlinkage.transportstreamid = transport_stream_id;
                bouquetlinkage.networkid = original_network_id;
                bouquetlinkage.serviceid = service_id;
            }
            if (linkagetype == 0x02)
            {
                Linkage? epglinkage = epglinkages.Find(x => x.transportstreamid == transport_stream_id);
                if (epglinkage == null)
                {
                    epglinkage = new Linkage();
                    epglinkages.Add(epglinkage);
                }
                epglinkage.transportstreamid = transport_stream_id;
                epglinkage.networkid = original_network_id;
                epglinkage.serviceid = service_id;
            }
            if (linkagetype == 0x04)
            {
                Linkage? silinkage = silinkages.Find(x => x.transportstreamid == transport_stream_id);
                if (silinkage == null)
                {
                    silinkage = new Linkage();
                    silinkages.Add(silinkage);
                }
                silinkage.transportstreamid = transport_stream_id;
                silinkage.networkid = original_network_id;
                silinkage.serviceid = service_id;
            }
        }

        private void processterrestrialdescriptor(Span<byte> span, Transponder tsp)
        {
            log.Debug("Terrestrial delivery system NOT SUPPORTED");
        }
        private void processcabledescriptor(Span<byte> v, Transponder tsp)
        {
            byte[] frequency = new byte[4];
            Array.Copy(v.ToArray(), 0, frequency, 0, 4);
            int fec_outer = v[5] & 0x0F;
            int modtype = (v[6]);
            byte[] symbol_rate = new byte[4];
            Array.Copy(v.ToArray(), 7, symbol_rate, 0, 4);
            int fec_inner = (symbol_rate[3] & 0x0f);
            tsp.samplerate = Utils.Utils.bcdtoint(symbol_rate) / 100;
            log.DebugFormat("Transport: Freq {0}, Symbolrate: {1}, Fec_inner: {2}, Fec_outer: {3}",
                Utils.Utils.bcdtohex(frequency),
                Utils.Utils.bcdtohex(symbol_rate, symbol_rate.Length * 2 - 1),
                fec_inner,
                fec_outer
                );
            tsp.frequency = Utils.Utils.bcdtoint(frequency) / 10;
            tsp.frequencydecimal = Decimal.Divide(Utils.Utils.bcdtoint(frequency), 10);
            tsp.polarisation = Transponder.e_polarisation.none;

            switch (modtype)
            {
                case 0: tsp.mtype = Transponder.e_mtype.auto; break;
                case 1: tsp.mtype = Transponder.e_mtype.qam16; break;
                case 2: tsp.mtype = Transponder.e_mtype.qam32; break;
                case 3: tsp.mtype = Transponder.e_mtype.qam64; break;
                case 4: tsp.mtype = Transponder.e_mtype.qam128; break;
                case 5: tsp.mtype = Transponder.e_mtype.qam256; break;
                default: tsp.mtype = Transponder.e_mtype.auto; break;
            }
            tsp.dvbsystem = Transponder.e_dvbsystem.DVB_C;
        }

        private void processsatellitedescriptor(Span<byte> v, Transponder tsp)
        {
            /* Details: see a38_dvb-si_specification */
            byte[] frequency = new byte[4];
            Array.Copy(v.ToArray(), 0, frequency, 0, 4);
            byte[] orbit_position = new byte[2];
            Array.Copy(v.ToArray(), 4, orbit_position, 0, 2);
            int west_eastflag = (v[6] & 0x80) >> 7; /* 0: West, 1: East */
            int polarization = (v[6] & 0x60) >> 5; /* 00: H, 01: V, 10: Left, 11; Right */
            int dvbsystem = (v[6] & 0x04) >> 2; /* 1: DVB-S2, 0: DVB-S */
            int roll_off = -1;
            if (dvbsystem == 1)
            {
                roll_off = (v[6] & 0x18) >> 3;
            }
            int modtype = (v[6] & 0x03);
            byte[] symbol_rate = new byte[4];
            Array.Copy(v.ToArray(), 7, symbol_rate, 0, 4);
            int fec = (symbol_rate[3] & 0x0f);
            //log.DebugFormat("Transport descriptor id: {0} with length {1}", id, length);
            log.DebugFormat("Transport: Freq {0}, Orbit: {1}, Symbolrate: {2}, Fec: {3}",
                Utils.Utils.bcdtohex(frequency),
                Utils.Utils.bcdtohex(orbit_position),
                Utils.Utils.bcdtohex(symbol_rate, symbol_rate.Length * 2 - 1),
                fec
                );
            tsp.frequency = Utils.Utils.bcdtoint(frequency) / 100;
            tsp.frequencydecimal = Decimal.Divide(Utils.Utils.bcdtoint(frequency), 100);
            switch (polarization)
            {
                case 00: tsp.polarisation = Transponder.e_polarisation.Horizontal; break;
                case 01: tsp.polarisation = Transponder.e_polarisation.Vertical; break;
                case 10: tsp.polarisation = Transponder.e_polarisation.circular_left; break;
                case 11: tsp.polarisation = Transponder.e_polarisation.circular_right; break;
            }
            switch (dvbsystem)
            {
                case 0: tsp.dvbsystem = Transponder.e_dvbsystem.DVB_S; break;
                case 1: tsp.dvbsystem = Transponder.e_dvbsystem.DVB_S2; break;
            }
            tsp.orbit = orbit_position;
            tsp.samplerate = Utils.Utils.bcdtoint(symbol_rate) / 100;
            tsp.diseqcposition = diseqcposition;
            switch (fec)
            {
                case 0: tsp.fec = Transponder.e_fec.undefined; break;
                case 1: tsp.fec = Transponder.e_fec.fec_12; break;
                case 2: tsp.fec = Transponder.e_fec.fec_23; break;
                case 3: tsp.fec = Transponder.e_fec.fec_34; break;
                case 4: tsp.fec = Transponder.e_fec.fec_56; break;
                case 5: tsp.fec = Transponder.e_fec.fec_78; break;
                case 6: tsp.fec = Transponder.e_fec.fec_89; break;
                case 7: tsp.fec = Transponder.e_fec.fec_35; break;
                case 8: tsp.fec = Transponder.e_fec.fec_45; break;
                case 9: tsp.fec = Transponder.e_fec.fec_910; break;
                case 15: tsp.fec = Transponder.e_fec.none; break;
                default: tsp.fec = Transponder.e_fec.reserved; break;

            }
            switch (modtype)
            {
                case 0: tsp.mtype = Transponder.e_mtype.auto; break;
                case 1: tsp.mtype = Transponder.e_mtype.qpsk; break;
                case 2: tsp.mtype = Transponder.e_mtype.psk8; break;
                case 3: tsp.mtype = Transponder.e_mtype.qam16; break;
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
        public void assign(List<Channel> channels)
        {
            foreach (Channel c in channels)
            {
                c.lcn = 99999;
            }

            int lcn = 1;
            foreach (ServiceListItem sli in networkservices)
            {
                Channel? c = channels.Find(x => x.service_id == sli.service_id);
                if (c != null)
                {
                    c.lcn = sli.lcn;
                }
            }
        }

    }
}
