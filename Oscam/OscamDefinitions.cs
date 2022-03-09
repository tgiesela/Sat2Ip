using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oscam
{
    public class Definitions
    {
        public struct ca_descr_type
        {
            public uint index;
            public uint parity;    /* 0 == even, 1 == odd */
            public byte[] cw;
        }

    }
}
