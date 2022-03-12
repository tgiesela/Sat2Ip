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
                newpayload.payloadlength = 0;
                newpayload.expectedlength = 0;
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
        }
        public Payload storePayload(Mpeg2Packet packet)
        {
            Payload payload = findPayload(packet.pid, packet.payloadstartindicator);
            if (packet.payloadstartindicator == 1) /* Start of payload*/
            {
                if (payload.payloadlength > 0) /* Still payload in buffer, reset before continuing */
                {
                    if (payload.type != Payload.packettype.pes) { 
                        /* PES packets of a video stream may have expectedlength = 0. Which is an undefined packet length
                         * In that case it is just treated as a new packet.*/
                        log.DebugFormat("WARN: Payload in buffer while receiving new payload. PID {0}, explen: {1}, rcvdlen: {2}",
                            packet.pid, payload.expectedlength, payload.payloadlength);
                        invokeprocesspayload(payload);
                    }
                    else
                    {
                        payload.clear();
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
                        if ((payload.payloadlength + bytestocopy) > payload.maxpayloadlength)
                        {
                            log.DebugFormat("Payload does not fit in buffer ({0} + {1} > {2}.", payload.payloadlength, bytestocopy, payload.maxpayloadlength);
                            throw new Exception("Payload does not fit in buffer.");
                        }
                        System.Buffer.BlockCopy(packet.payload, 0, payload.data, payload.payloadlength, bytestocopy);
                        payload.payloadlength = payload.payloadlength + bytestocopy;
                    }
                    else /* table */
                    {
                        /* The first byte (pointer) points to the start of the packet
                         * The part between pointer and start of packet is not counted as payload.
                         * We will only copy the part starting from pointer and modify the pointer byte to be zero.
                         */

                        /* v[offset]+1 = table id */
                        payload.type = Payload.packettype.table;
                        int pointer = packet.payload[0];
                        if (packet.payload.Length < (pointer + 3) || pointer > 184)
                        {
                            log.Debug("Malformatted packet, clear payload");
                            Utils.Utils.DumpBytes(packet.buffer, packet.buffer.Length);
                            Utils.Utils.DumpBytes(packet.payload, packet.payload.Length);
                            payload.clear();
                            return payload;
                        }
                        payload.expectedlength = (Utils.Utils.toShort(packet.payload[pointer+2], packet.payload[pointer+3]) & 0x0FFF) + 4;
                        bytestocopy = 188 - packet.headerlen - pointer;
                        //log.DebugFormat("Store payload for PID {0}: , with exp. length: {1} and packet length: {2}, CC={3}",
                        //    packet.pid, payload.expectedlength, bytestocopy,packet.continuitycounter);

                        if ((payload.payloadlength + bytestocopy) > payload.maxpayloadlength)
                        {
                            log.DebugFormat("Payload does not fit in buffer ({0} + {1} > {2}.", payload.payloadlength, packet.payloadlength, payload.maxpayloadlength);
                            throw new Exception("Payload does not fit in buffer.");
                        }
                        System.Buffer.BlockCopy(packet.payload, pointer, payload.data, payload.payloadlength, bytestocopy);
                        payload.payloadlength = payload.payloadlength + bytestocopy;
                        payload.data[0] = 0;
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
                        log.DebugFormat("There should have been something in the buffer for PID: {0}, exp.len: {1}, type: {2}",
                            payload.payloadpid, payload.expectedlength, payload.type);
                    }
                    else
                    {
                        int bytestocopy = 188 - packet.headerlen;
                        System.Buffer.BlockCopy(packet.payload, 0, payload.data, payload.payloadlength, bytestocopy); /* copy payload without header */
                        payload.payloadlength = payload.payloadlength + bytestocopy;
                    }
                    if ((payload.expectedlength > 0) && payload.isComplete())
                    {
                        invokeprocesspayload(payload);
                    }
                }
            }
            return payload;
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
