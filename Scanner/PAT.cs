using Sat2Ip;
using System;
using System.Collections.Generic;
using static Interfaces.DVBBase;

namespace Interfaces
{
    public class PAT : DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ushort transportstreamid { get; set; }
        public bool expectNIT { get; set; }
        public class PATEntry
        {
            public int serviceid { get; set; }
            public ushort programpid { get; set; }
        }
        public List<PATEntry> pids { get; set; }
        public PAT()
        {
            log.Debug("PAT created");
            expectNIT = false;
            pids = new List<PATEntry>();
        }
        public override void addsection(tableHeader hdr, Span<byte> span)
        {
            base.addsection(hdr, span);
            transportstreamid = (ushort)hdr.streamid;
        }
        protected override void processsection(Span<byte> section)
        {
            /*
            Program Association Table(PAT) section 
            syntax syntax bit index	# of bits	mnemonic
            table_id                    0   8   uimsbf
            section_syntax_indicator    8   1   bslbf
            '0'                         9   1   bslbf
            reserved                    10  2   bslbf
            section_length              12  12  uimsbf
            transport_stream_id         24  16  uimsbf
            reserved                    40  2   bslbf
            version_number              42  5   uimsbf
            current_next_indicator      47  1   bslbf
            section_number              48  8   bslbf
            last_section_number         56  8   bslbf
            for i = 0 to N
              program_number            56 + (i * 4)    16  uimsbf
              reserved                  72 + (i * 4)    3   bslbf
              if program_number = 0
                    network_PID         75 + (i * 4)    13  uimsbf
              else
                    program_map_pid     75 + (i * 4)    13  uimsbf
              end if
            next
            CRC_32                      88 + (i * 4)    32  rpchof
            Table section legend
            */

            if (pids == null)
                pids = new List<PATEntry>();
            int bytesprocessed = 0;
            int i = 0;
            while (bytesprocessed < section.Length - 4)
            {
                ushort program_number = Utils.Utils.toShort(section[bytesprocessed], section[bytesprocessed + 1]);
                ushort network_pid = 0;
                ushort program_map_pid = 0;
                PATEntry entry;
                if (program_number == 0)
                {
                    network_pid = (Utils.Utils.toShort((byte)(section[bytesprocessed + 2] & 0x1F), section[bytesprocessed + 3]));
                    entry = new PATEntry();
                    entry.programpid = network_pid;
                    entry.serviceid = 0;
                    log.DebugFormat("Expect to receive NIT on PID: {0}", network_pid);
                    expectNIT = true;
                }
                else
                {
                    program_map_pid = (Utils.Utils.toShort((byte)(section[bytesprocessed + 2] & 0x1F), section[bytesprocessed + 3]));
                    entry = new PATEntry();
                    entry.programpid = program_map_pid;
                    entry.serviceid = program_number;
                }
                pids.Add(entry);
                bytesprocessed += 4;
                i++;
            }
        }
    }
}
