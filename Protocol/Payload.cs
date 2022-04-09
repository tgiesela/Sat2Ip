using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Protocol
{
    public class Payload
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
                            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        internal int maxpayloadlength = 512*1024;
        public byte[] data { get; set;  }
        public int payloadlength { get; set; } = 0;
        public int payloadpid { get; set; } = 0;
        public int expectedlength { get; set; }
        public bool scambled { get; internal set; }
        public int adaptationlength { get; internal set; }
        public int continuitycounter { get; internal set; }

        internal enum packettype
        {
            pes,
            table,
            unknown
        }
        internal packettype type;
        public Payload()
        {
            data = new byte[maxpayloadlength];
            payloadlength = 0;
            expectedlength = 0;
            adaptationlength = 0;
            continuitycounter = -1;
        }
        public bool isComplete()
        {
            return payloadlength >= expectedlength;
        }
        public byte[] getDatapart(int start, int length)
        {
            Span<byte> span = data;
            return span.Slice(start, length).ToArray();
        }

        public void clear()
        {
            payloadlength = 0;
            expectedlength = 0;
            adaptationlength = 0;
        }

        internal bool isnextcc(int cc)
        {
            if (this.continuitycounter == -1) /* First packet */
            {
                this.continuitycounter = cc;
                return true;
            }
            this.continuitycounter++;
            if (this.continuitycounter > 15)
            {
                this.continuitycounter = 0;
            }
            if (this.continuitycounter == cc)
                return true;
            else
            {
                log.DebugFormat("expected CC {0}, received CC: {1}", this.continuitycounter, cc);
                this.continuitycounter = cc;
                return false;
            }
        }
    }
    public class Payloads
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private List<Payload> payloads = new List<Payload>();
        public delegate void processpayload(Payload payload);
        processpayload cb_processpayload;
        public Payloads(processpayload callback)
        {
            cb_processpayload = callback;
        }
        public Payloads()
        {
            cb_processpayload = null;
        }
        private Payload findPayload(ushort pid, int pusi)
        {
            foreach (Payload p in payloads)
            {
                if (p.payloadpid == pid) return p;
            }
            if (pusi == 1)
            {
                log.DebugFormat("New payload created for pid {0}", pid);
                Payload newpayload = new Payload();
                newpayload.payloadpid = pid;
                newpayload.type = Payload.packettype.unknown;
                payloads.Add(newpayload);
                return newpayload;
            }
            else 
            { 
                return null; 
            }
        }
        public void resetPayload(Payload payload)
        {
            payload.payloadlength = 0;
            payload.expectedlength = 0;
            payload.adaptationlength = 0;
        }
        private void appendtopayload(Payload payload, Mpeg2Packet packet, int offsetfrom, int bytestocopy)
        {
            if ((payload.payloadlength + bytestocopy) > payload.maxpayloadlength)
            {
                log.DebugFormat("Payload does not fit in buffer ({0} + {1} > {2}.", payload.payloadlength, bytestocopy, payload.maxpayloadlength);
                throw new Exception("Payload does not fit in buffer.");
            }
            System.Buffer.BlockCopy(packet.payload, offsetfrom, payload.data, payload.payloadlength, bytestocopy);
            payload.payloadlength = payload.payloadlength + bytestocopy;
            payload.adaptationlength = payload.adaptationlength + packet.adaptationlength;
        }
        public Payload storePayload(Mpeg2Packet packet)
        {
            Payload payload = findPayload(packet.pid, packet.payloadstartindicator);
            if (payload != null)
            {
                if (!payload.isnextcc(packet.continuitycounter))
                {
                    log.DebugFormat("CC-error for PID {0} (0x{0:X4}), CC-received: {1}. PAYLOAD RESET!", payload.payloadpid, packet.continuitycounter);
                    payload.clear();
                }
            }
            if (packet.payloadstartindicator == 1) /* Start of payload */
            {
                /*
                 * If the payload starts after a non-zero pointer, the data before the payload start (pointer)
                 * should be appended to the payload which is already there.
                 */
                int pointer = packet.payload[0];
                if (payload.payloadlength > 0) /* Still payload in buffer, reset before continuing */
                {
                    if (payload.type == Payload.packettype.pes) 
                    {
                        /* PES packets of a video stream may have expectedlength = 0. Which is an undefined packet length
                         * In that case it is just treated as a new packet.*/
                        payload.clear();
                    }
                    else
                    { 
                        if (pointer > 0)
                        {
                            appendtopayload(payload, packet, 0, pointer);
                        }
                        if (!payload.isComplete())
                        {
                            log.DebugFormat("WARN: Payload in buffer while receiving new payload. PID {0}, explen: {1}, rcvdlen: {2}, adaptationlength: {3}",
                                packet.pid, payload.expectedlength, payload.payloadlength, packet.adaptationlength);
                        }
                        invokeprocesspayload(payload);
                    }
                }
                if (packet.scramblingcontrol == 0)
                {
                    payload.scambled = false;
                    int bytestocopy;
                    if (packet.payload[0] == 0 && packet.payload[1] == 0 && packet.payload[2] == 1) /* PES header */
                    {
                        /* offset + 3 = pes type */
                        payload.type = Payload.packettype.pes;
                        payload.expectedlength = Utils.Utils.toShort(packet.payload[4], packet.payload[5]);
                        bytestocopy = 188 - packet.headerlen;
                        appendtopayload(payload, packet, 0, bytestocopy);
                    }
                    else /* table */
                    {
                        /* The first byte after the header (pointer) points to the start of the payload.
                         * The part between header and start of payload belongs to a previous payload. It has been processed a few lines above this code.
                         * We will only copy the part starting from pointer and modify the pointer byte to be zero.
                         * There may be multiple (small) payloads in the same packet. As long as there is remaining data in the packet and it is not stuffing,
                         * it will be processed here.
                         */
                        int offset = pointer + 1;
                        while (offset < packet.payloadlength && packet.payload[offset] != 0xff)
                        {
                            int tablelen = getTablelen(packet, offset);
                            payload.expectedlength = tablelen + 3;
                            payload.type = Payload.packettype.table;
                            if ((tablelen+3) > packet.payloadlength - offset)
                                bytestocopy = packet.payloadlength - offset;
                            else
                                bytestocopy = tablelen + 3;
                            appendtopayload(payload, packet, offset, bytestocopy);
                            if (payload.isComplete())
                                invokeprocesspayload(payload);
                            offset += tablelen + 3;
                        }
                        if (packet.payload.Length < (pointer + 3) || pointer > 184)
                        {
                            log.Debug("Malformed packet, clear payload");
                            Utils.Utils.DumpBytes(packet.buffer, packet.buffer.Length);
                            Utils.Utils.DumpBytes(packet.payload, packet.payload.Length);
                            payload.clear();
                            return payload;
                        }
                        //bytestocopy = 188 - packet.headerlen - pointer;
                        //appendtopayload(payload, packet, pointer, bytestocopy);
                        //payload.data[0] = 0;
                    }
                    if ((payload.expectedlength > 0) && payload.isComplete() && payload.type != Payload.packettype.pes)
                    {
                        invokeprocesspayload(payload);
                    }

                }
                else
                {
                    payload.scambled = true;
                    payload.type = Payload.packettype.pes;
                }
            }
            else
            {
                if (payload != null && payload.scambled == false && payload.type == Payload.packettype.table)
                {
                    //log.DebugFormat("Append payload for PID {0}: , with length: {1}, expected {2}, current {3}, CC={4}", 
                    //    packet.pid, 188 - packet.headerlen, payload.expectedlength, payload.payloadlength, packet.continuitycounter);
                    if ((payload.payloadlength + packet.payloadlength) > payload.maxpayloadlength)
                    {
                        log.DebugFormat("Payload does not fit in buffer ({0} + {1} > {2}.", payload.payloadlength, packet.payloadlength, payload.maxpayloadlength);
                        throw new Exception("Payload does not fit in buffer.");
                    }
                    if (payload.payloadlength == 0)
                    {
                        /* There are payloads with a lot of stuffing (DVB-CC for example) */
                        /* The first part is already processed, but after that multiple packets with stuffing are being sent */
                        /* The packet will be discarded */
                        //log.DebugFormat("There should have been something in the buffer for PID: {0}, exp.len: {1}, type: {2}",
                        //    payload.payloadpid, payload.expectedlength, payload.type);
                    }
                    else
                    {
                        int bytestocopy = 188 - packet.headerlen;
                        appendtopayload(payload, packet, 0, bytestocopy);

                        // System.Buffer.BlockCopy(packet.payload, 0, payload.data, payload.payloadlength, bytestocopy); /* copy payload without header */
                        // payload.payloadlength = payload.payloadlength + bytestocopy;
                        // payload.adaptationlength = payload.adaptationlength + packet.adaptationlength;
                    }
                    if ((payload.expectedlength > 0) && payload.isComplete())
                    {
                        invokeprocesspayload(payload);
                    }
                }
            }
            return payload;
        }

        private int getTablelen(Mpeg2Packet packet, int offset)
        {
            return (Utils.Utils.toShort(packet.payload[offset + 1], packet.payload[offset + 2]) & 0x0FFF);
        }

        private void invokeprocesspayload(Payload payload)
        {
            //log.DebugFormat("Processing payload for pid {0} with length: {1}",payload.payloadpid, payload.payloadlength);
            if (cb_processpayload != null)
                cb_processpayload(payload);
            payload.clear();
        }
    }
}
