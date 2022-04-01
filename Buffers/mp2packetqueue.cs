using Protocol;
using Sat2Ip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Circularbuffer
{
    public class mp2packetqueue
    {
        private ConcurrentQueue<Mpeg2Packet> _queue;
        private int _maxsize = 0;
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#pragma warning restore CS8602 // Dereference of a possibly null reference.

        public mp2packetqueue(int maxsize)
        {
            _queue = new ConcurrentQueue<Mpeg2Packet>();
            _maxsize = maxsize;
        }
        public void add(Mpeg2Packet b)
        {
            lock (_queue)
            {
                if (_queue.Count < _maxsize)
                {
                    _queue.Enqueue(b);
                }
                else
                {
                    log.Debug("Circular buffer too small");
                }
            }
        }
        public bool get(out Mpeg2Packet? b)
        {
            bool result;
            if (_queue.Count > 0)
            {
                lock (_queue)
                {
                    result = _queue.TryDequeue(out b);
                    if (!result)
                        b = null;
                }
            }
            else
            {
                result = false;
                b = null;
            }
            return result;
        }
        public Mpeg2Packet? get()
        {
            bool result;
            Mpeg2Packet? b;
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    result = _queue.TryDequeue(out b);
                    if (!result)
                        b = null;
                }
                else
                {
                    result = false;
                    b = null;
                }
            }
            return b;
        }
        public int getBufferedsize()
        {
            return _queue.Count;
        }

    }
}
