using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public static class Utils
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public static void DumpBytes(byte[] bytes, int length)
        {
            int remainlen = length;
            for (int i = 0; i < (length / 16) + 1; i = i + 1)
            {
                StringBuilder hex = new StringBuilder(16 * 2);
                for (int j = 0; j < Math.Min(16,remainlen); j++)
                    hex.AppendFormat(" {0:x2}", bytes[i * 16 + j]);
                log.Debug(hex.ToString());
                remainlen = remainlen - 16;
            }
        }
        public static ushort toShort(byte byte1, byte byte2)
        {
            return (ushort)((ushort)(byte1 << 8) + (ushort)byte2);
        }
        public static byte[] fromShort(ushort val)
        {
            byte[] bArr = new byte[2];
            bArr = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bArr);
            return bArr;
        }
        public static int toInt(byte v1, byte v2, byte v3, byte v4)
        {
            return ((int)(v1 << 24) + (int)(v2 << 16) + (int)(v3 << 8) + (int)v4);
        }
        public static long toLong(byte v1, byte v2, byte v3, byte v4, byte v5, byte v6, byte v7, byte v8)
        {
            return ((long)(v1 << 56) + (long)(v2 << 48) + (long)(v3 << 40) + (long)(v4 << 32) + (long)(v5 << 24) + (long)(v6 << 16) + (long)(v7 << 8) + (long)(v8));
        }
        public static string bcdtohex(byte[] barr, int outputlength)
        {
            StringBuilder result = new StringBuilder();
            if (outputlength > barr.Length * 2)
            {
                throw new Exception("Output length bigger than input array length");
            }
            StringBuilder sb = new StringBuilder(outputlength);
            foreach (byte b in barr)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString().Substring(0, outputlength);
        }
        public static string bcdtohex(byte[] barr)
        {
            return bcdtohex(barr, barr.Length*2);
        }

        public static int bcdtoint(byte[] barr)
        {
            int result = 0;
            foreach (byte b in barr)
            {
                result = result * 100;
                result = result + (((b & 0xf0) >> 4) * 10) + (b&0x0f);
            }
            return result;
        }
        public static string getStorageFolder()
        {
            String allusersprofile = Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
            String storagefolder = allusersprofile + "\\" + "Sat2IpGui\\";
            Directory.CreateDirectory(storagefolder);
            return storagefolder;
        }
        public static uint crc32bmpeg2(ReadOnlySpan<byte> message, int l)
        {
            /*
             * As noted in the crcalc web page, crc32/mpeg2 uses a left shifting (not reflected) 
             * CRC along with the CRC polynomial 0x104C11DB7 and initial CRC value of 0xFFFFFFFF,
             * and not post complemented:
             */
            int i, j;
            uint crc, msb;

            crc = 0xFFFFFFFF;
            for (i = 0; i < l; i++)
            {
                // xor next byte to upper bits of crc
                crc ^= (((uint)message[i]) << 24);
                for (j = 0; j < 8; j++)
                {    // Do eight times.
                    msb = crc >> 31;
                    crc <<= 1;
                    crc ^= (0 - msb) & 0x04C11DB7;
                }
            }
            return crc;         // don't complement crc on output
        }
    }
}
