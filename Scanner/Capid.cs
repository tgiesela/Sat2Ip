using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2Ip
{
    [Serializable]
    public class capid
    {
        private ushort _CA_PID;
        private ushort _CA_System_ID;
        private byte[] _cadescriptor;
        public byte[] Cadescriptor
        {
            get
            {
                return _cadescriptor;
            }

            set
            {
                _cadescriptor = new byte[value.Length];
                Buffer.BlockCopy(value, 0, _cadescriptor, 0, value.Length);
            }
        }

        public ushort CA_System_ID
        {
            get
            {
                return _CA_System_ID;
            }

            set
            {
                _CA_System_ID = value;
            }
        }

        public ushort CA_PID
        {
            get
            {
                return _CA_PID;
            }

            set
            {
                _CA_PID = value;
            }
        }
    }
}
