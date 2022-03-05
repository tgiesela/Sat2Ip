using Sat2Ip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Interfaces
{
    public class Network
    {
        public int networkid { get; set; }
        public string networkname { get; set; }
        public List<Transponder> transponders { get; }
        public bool nitcomplete 
        {  get { return _complete; }
        }
        private int _nitreceivecount;
        private int _highestsectionnr;
        private bool _complete;

        public Network()
        {
            transponders = new List<Transponder>();
            _nitreceivecount = 0;
            _highestsectionnr = 0;
            _complete = false;
        }
        public bool processsection(int sectionnr, int lastsectionnr)
        {
            _highestsectionnr = lastsectionnr;
            if (sectionnr == _nitreceivecount)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void sectionprocessed(int sectionnr)
        {
            if (_nitreceivecount >= _highestsectionnr)
            {
                _complete = true;
            }
            _nitreceivecount++;
        }
    }
}
