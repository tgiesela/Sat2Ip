using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class FastScanBouquet
    {
        public Network network { get; set; }
        public FastScanLocation location { get; set; }

    }
}
