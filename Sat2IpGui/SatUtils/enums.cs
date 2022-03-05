using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sat2IpGui.SatUtils
{
	public enum polarisation { Horizontal = 'h', Vertical = 'v', circular_left = 'l', circular_right = 'r' };
	public enum dvbsystem
	{
		DVB_S = 1,
		DVB_S2 = 2
	};
	public enum fec
	{
		fec_12 = 12,
		fec_23 = 23,
		fec_34 = 34,
		fec_45 = 45,
		fec_56 = 56,
		fec_78 = 78,
		fec_89 = 89,
		fec_910 = 910
	};
	public enum mtype { qpsk, psk8 };
}
