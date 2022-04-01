using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public class FastScanLocation
    {
        public string name { get; set; }
        public int position { get; set; }
        public int frequency { get; set; }
        public int symbolrate { get; set; }
        public string polarisation { get; set; }
        public string delsys { get; set; }
        public int pid { get; set; }
        public int diseqcposition { get; set; }
    }
}
