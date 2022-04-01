using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    [Serializable]
    public class Linkage
    {
        public short transportstreamid { get; set; }
        public short serviceid { get; set; }
        public short networkid { get; set; }
        public byte linkagetype { get; internal set; }
    }
}
