using Sat2Ip;
using System.Collections.Generic;

namespace Interfaces
{
    public class Network
    {
        public int networkid { get; set; }
        public string networkname { get; set; }
        public List<Transponder> transponders { get; set; }
        public bool nitcomplete 
        {  get { return _complete; }
        }
        private bool _complete;
        private bool[] sections;

        public Network()
        {
            transponders = new List<Transponder>();
            _complete = false;
        }
        public void sectionprocessed(int sectionnr, int lastsectionnr)
        {
            if (sections == null || sections.Length != lastsectionnr + 1)
            {
                sections = new bool[lastsectionnr + 1];
            }
            sections[sectionnr] = true;
            foreach (bool b in sections)
            {
                if (!b)
                    return;
            }
            _complete = true;
        }
    }
}
