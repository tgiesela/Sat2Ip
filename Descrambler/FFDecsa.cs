using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static Descrambler.DescramblerNew;

namespace Descrambler
{
    public class FFDecsa
    {
        /*-------------------*/
        /* FFdecsa interface */
        /*-------------------*/
        // -- how many packets can be decrypted at the same time
        // This is an info about internal decryption parallelism.
        // You should try to call decrypt_packets with more packets than the number
        // returned here for performance reasons (use get_suggested_cluster_size to know
        // how many).
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern int get_internal_parallelism();

        // -- how many packets you should have in a cluster when calling decrypt_packets
        // This is a suggestion to achieve optimal performance; typically a little
        // higher than what get_internal_parallelism returns.
        // Passing less packets could slow down the decryption.
        // Passing more packets is never bad (if you don't spend a lot of time building
        // the list).
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern int get_suggested_cluster_size();

        // -- alloc & free the key structure
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr get_key_struct();

        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern void free_key_struct(IntPtr keys);

        // -- set control words, 8 bytes each
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern void set_control_words(IntPtr keys, IntPtr even, IntPtr odd);

        // -- set even control word, 8 bytes
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern void set_even_control_word([In] IntPtr keys, byte[] even);

        // -- set odd control word, 8 bytes
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        static extern void set_odd_control_word([In] IntPtr keys, byte[] odd);

        // -- get control words, 8 bytes each
        //void get_control_words(void *keys, unsigned char *even, unsigned char *odd);

        // -- decrypt many TS packets
        // This interface is a bit complicated because it is designed for maximum speed.
        // Please read doc/how_to_use.txt.
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,
            CallingConvention = CallingConvention.Cdecl)]
        //static extern int decrypt_packets(IntPtr keys, IntPtr[] cluster);
        static extern int decrypt_packets(IntPtr keys, [In][Out] string[] cluster);
        [DllImport(@"E:\Sat2IP\Projects\Sat2IpClient\Debug\FFdecsa.dll",
            CharSet = CharSet.Ansi,EntryPoint = "decrypt_packets",
            CallingConvention = CallingConvention.Cdecl)]
        //static extern int decrypt_packets(IntPtr keys, IntPtr[] cluster);
        static extern int decrypt_packetsbyte(IntPtr keys, [In][Out] IntPtr[] cluster);

        //        [MarshalAs(UnmanagedType.AsAny)]byte[] keys
        private int paralleldecrypts = 0;
        private int batchsize = 0;
        private const int MAX_CSA_IDX = 16;
        private int cs;
        private uint[,,] des_key_schedule = new uint[MAX_CSA_IDX,2,32];
        private uint[] algo =new uint[MAX_CSA_IDX];
        private DateTime[] cwSeen = new DateTime[MAX_CSA_IDX];   // last time the CW for the related key was seen
        private IntPtr[] keys = new IntPtr[MAX_CSA_IDX];
        /*-----------------------*/
        /* End FFdecsa interface */
        /*-----------------------*/

        public struct csa_batch
        {
            public IntPtr datastart;  // Pointer to payload
            public IntPtr dataend;    // Pointer to end of payload 
        };

        public FFDecsa()
        {
            batchsize = get_suggested_cluster_size() / 2;
            paralleldecrypts = get_internal_parallelism();
            for (int i = 0; i < MAX_CSA_IDX; i++)
            {
                keys[i] = get_key_struct();
            }
        }
        ~FFDecsa()
        {
            for (int i = 0; i < MAX_CSA_IDX; i++)
            {
                if (keys[i] != IntPtr.Zero)
                    free_key_struct(keys[i]);
            }
        }
        public bool SetDescr(ca_descr_type ca_descr)
        {
            lock (keys)
            {
                int idx = (int)ca_descr.index;
                if (idx < MAX_CSA_IDX)
                {
                    cwSeen[idx] = DateTime.Now;
                    if (ca_descr.parity == 0)
                    {
                        set_even_control_word(keys[idx], ca_descr.cw);
                    }
                    else
                    {
                        set_odd_control_word(keys[idx], ca_descr.cw);
                    }
                }
            }
            return true;
        }

        public int getOptimalSize()
        {
            return batchsize;
        }

        public void DecryptMultiple(csa_batch[] pcks)
        {
            {
                GCHandle handle = GCHandle.Alloc(pcks, GCHandleType.Pinned);
                //decrypt_packets(keys[0], handle.AddrOfPinnedObject());
                handle.Free();
            }
        }
        public void DecryptPackets(byte[] cluster)
        {
            byte[] managedArray = new byte[512];
            Marshal.Copy(keys[0], managedArray, 0, 512);
            string[] scluster = new string[10];
            scluster[0] = "AAAAAAAAAA";
            scluster[1] = "FFFFFFFFFF";
            IntPtr[] pcluster = new IntPtr[10];
            pcluster[0] = Marshal.UnsafeAddrOfPinnedArrayElement(cluster, 0);
            pcluster[1] = Marshal.UnsafeAddrOfPinnedArrayElement(cluster, 188);
            pcluster[2] = IntPtr.Zero;
            pcluster[3] = IntPtr.Zero;
            decrypt_packets(keys[0], scluster);
            decrypt_packetsbyte(keys[0], pcluster);
        }
    }
}
