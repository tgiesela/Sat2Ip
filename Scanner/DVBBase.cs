using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public abstract class DVBBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public struct tableHeader
        {
            public int syntaxindicator;
            public short sectionlength;
            public int streamid;
            public int programnumber;
            public int versionnr;
            public int currNextInd;
            public int sectionnr;
            public int lastsectionnr;
        };
        private class section
        {
            internal byte[] data;
        }
        private section[] msgsections;
        private bool _complete;
        public bool complete { get { return _complete; } set { _complete = value; } }
        internal DVBBase()
        {
            _complete = false;
        }
        public virtual bool verifyCRC(Span<byte> msg, Span<byte> crcreceived)
        {
            uint crccalculated = Utils.Utils.crc32bmpeg2(msg, msg.Length);
            if (BitConverter.ToUInt32(crcreceived) == crccalculated)
                return false;
            else
                return true;
        }
        public virtual void addsection(tableHeader hdr, Span<byte> span)
        {
            if (!verifyCRC(span.Slice(1), span.Slice(1 + span.Length - 5, 4)))
            {
                log.DebugFormat("CRC Invalid, section not added");
                return;
            }
            if (span.Length < hdr.sectionlength - 5) /* Section includes 5 bytes of the header */
            {
                log.DebugFormat("Short payload! Length msg: {0}, Length header: {1}", span.Length, hdr.sectionlength);
                return;
            }

            if (msgsections == null)
            {
                int nrofsections = hdr.lastsectionnr + 1;
                msgsections = new section[nrofsections];
                for (int i = 0; i < msgsections.Length; i++)
                {
                    msgsections[i] = new section();
                }
            }
            if (msgsections[hdr.sectionnr].data == null)
            {
                msgsections[hdr.sectionnr].data = new byte[span.Length];
                Array.Copy(span.ToArray(), msgsections[hdr.sectionnr].data, span.Length);
            }
            for (int i = 0; i < msgsections.Length; i++)
            {
                if (msgsections[i].data == null)
                    return;
            }
            for (int i = 0; i < msgsections.Length; i++)
            {
                processsection(new Span<byte>(msgsections[i].data));
            }
            this._complete = true;
        }
        abstract protected void processsection(Span<byte> span);
        internal static string getStringFromDescriptor(byte[] descriptor, int offset, ref int lenused)
        {
            if (descriptor.Length <= 0)  return null;
            int strlen = descriptor[offset];
            int strencoding;
            if (strlen > 0)
            {
                if (descriptor[offset + 1] < 0x20)
                {
                    strencoding = descriptor[offset + 1];
                    strlen--;
                    lenused = strlen + 1;
                    Encoding ascii = Encoding.ASCII;
                    Encoding iso;
                    string asciistring = Encoding.Latin1.GetString(descriptor, offset + 2, strlen);
                    iso = Encoding.Default;
                    switch (strencoding)
                    {
                        case 0x10:
                            int subset = strencoding = descriptor[offset + 3];
                            asciistring = Encoding.Latin1.GetString(descriptor, offset + 4, strlen - 2);
                            break;
                        default:
                            iso = Encoding.Default;
                            break;
                    }
                    return asciistring;
                }
                else
                {
                    strencoding = 0;
                    lenused = strlen;
                    return Encoding.Latin1.GetString(descriptor, offset + 1, strlen);
                }
            }
            else
            {
                return "";
            }
        }
    }
}
